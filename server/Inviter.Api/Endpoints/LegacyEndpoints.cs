using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Endpoints;

public static class LegacyEndpoints
{
    public static void MapLegacyEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapGet("/invite/{inviteToken}/invitee/{inviteeId:guid}", GetInviteePrefill);
        api.MapGet("/manage/{adminToken}/invitees", ListInvitees);
        api.MapPost("/manage/{adminToken}/invitees", AddInvitees);
        api.MapDelete("/manage/{adminToken}/invitees/{inviteeId:guid}", DeleteInvitee);
        api.MapPost("/manage/{adminToken}/invitees/send", SendInvitations);
    }

    private static async Task<IResult> GetInviteePrefill(string inviteToken, Guid inviteeId, AppDbContext db)
    {
        var prefill = await db.Invitees.AsNoTracking()
            .Where(i => i.Id == inviteeId && i.Event.InviteToken == inviteToken)
            .Select(i => new InviteePrefillDto(i.Name, i.Email))
            .FirstOrDefaultAsync();
        return prefill is null ? Results.NotFound() : Results.Ok(prefill);
    }

    private const int MaxInviteesPerBulkAdd = 200;

    private static async Task<IResult> ListInvitees(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitees = await db.Invitees.AsNoTracking()
            .Where(i => i.EventId == ev.Id)
            .OrderBy(i => i.AddedAt)
            .ToListAsync();

        var rsvpStatusByEmail = await db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == ev.Id && r.Email != null)
            .GroupBy(r => r.Email!)
            .Select(g => new { Email = g.Key, Status = g.OrderByDescending(r => r.SubmittedAt).Select(r => r.Status).First() })
            .ToDictionaryAsync(x => x.Email!.ToLowerInvariant(), x => (RsvpStatus?)x.Status);

        var dtos = invitees.Select(i => new InviteeDto(
            i.Id, i.Email, i.Name, i.AddedAt, i.LastSentAt, i.SendCount,
            rsvpStatusByEmail.GetValueOrDefault(i.Email.ToLowerInvariant()))).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> AddInvitees(string adminToken, AddInviteesRequest req, AppDbContext db)
    {
        if (req.Entries is null || req.Entries.Count == 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["entries"] = new[] { "Tilføj mindst én email-adresse." }
            });

        if (req.Entries.Count > MaxInviteesPerBulkAdd)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["entries"] = new[] { $"Du kan højst tilføje {MaxInviteesPerBulkAdd} adresser ad gangen." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var existing = await db.Invitees.AsNoTracking()
            .Where(i => i.EventId == ev.Id)
            .Select(i => i.Email)
            .ToListAsync();
        var existingSet = new HashSet<string>(existing.Select(e => e.ToLowerInvariant()));
        var seenInBatch = new HashSet<string>();

        var added = new List<Invitee>();
        var skippedDuplicates = new List<string>();
        var skippedInvalid = new List<string>();

        foreach (var entry in req.Entries)
        {
            var email = entry.Email?.Trim();
            var name = string.IsNullOrWhiteSpace(entry.Name) ? null : entry.Name.Trim();
            if (string.IsNullOrEmpty(email) || email.Length > 320 || !Validation.LooksLikeEmail(email))
            {
                skippedInvalid.Add(entry.Email ?? "");
                continue;
            }
            var key = email.ToLowerInvariant();
            if (existingSet.Contains(key) || !seenInBatch.Add(key))
            {
                skippedDuplicates.Add(email);
                continue;
            }
            var invitee = new Invitee
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                Email = email,
                Name = name,
                AddedAt = DateTime.UtcNow,
            };
            db.Invitees.Add(invitee);
            added.Add(invitee);
        }

        if (added.Count > 0)
            await db.SaveChangesAsync();

        var dtos = added.Select(i => new InviteeDto(
            i.Id, i.Email, i.Name, i.AddedAt, i.LastSentAt, i.SendCount, null)).ToList();

        return Results.Ok(new AddInviteesResponse(dtos, skippedDuplicates, skippedInvalid));
    }

    private static async Task<IResult> DeleteInvitee(string adminToken, Guid inviteeId, AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitee = await db.Invitees.FirstOrDefaultAsync(i => i.Id == inviteeId && i.EventId == ev.Id);
        if (invitee is null) return Results.NotFound();

        db.Invitees.Remove(invitee);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> SendInvitations(
        string adminToken,
        SendInvitationsRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var query = db.Invitees.Where(i => i.EventId == ev.Id);
        if (req.InviteeIds is { Count: > 0 })
        {
            var ids = req.InviteeIds.ToHashSet();
            query = query.Where(i => ids.Contains(i.Id));
        }
        if (req.OnlyUnsent)
        {
            query = query.Where(i => i.LastSentAt == null);
        }

        var invitees = await query.ToListAsync();
        if (invitees.Count == 0)
            return Results.Ok(new SendInvitationsResponse(0));

        var now = DateTime.UtcNow;
        var baseUrl = appOptions.Value.BaseUrl;
        foreach (var invitee in invitees)
        {
            var isResend = invitee.SendCount > 0;
            emailQueue.Enqueue(InvitationTemplate.Build(ev, invitee, baseUrl, isResend));
            invitee.LastSentAt = now;
            invitee.SendCount += 1;
        }
        await db.SaveChangesAsync();

        return Results.Ok(new SendInvitationsResponse(invitees.Count));
    }
}
