using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Inviter.Api.Infrastructure.Tokens;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapPost("/events", CreateEvent);
        api.MapGet("/invite/{inviteToken}", GetByInviteToken);
        api.MapGet("/invite/{inviteToken}/invitee/{inviteeId:guid}", GetInviteePrefill);
        api.MapPost("/invite/{inviteToken}/rsvp", SubmitRsvp);
        api.MapGet("/manage/{adminToken}", GetByAdminToken);
        api.MapPut("/manage/{adminToken}", UpdateByAdminToken);
        api.MapDelete("/manage/{adminToken}/rsvp/{rsvpId:guid}", DeleteRsvp);
        api.MapGet("/manage/{adminToken}/invitees", ListInvitees);
        api.MapPost("/manage/{adminToken}/invitees", AddInvitees);
        api.MapDelete("/manage/{adminToken}/invitees/{inviteeId:guid}", DeleteInvitee);
        api.MapPost("/manage/{adminToken}/invitees/send", SendInvitations);
    }

    private static async Task<IResult> CreateEvent(
        CreateEventRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        var errors = ValidateEventOptions(
            req.Title, req.StartsAt, req.RsvpDeadline, req.ContactRequirement, req.OrganizerEmail);
        if (errors is not null) return Results.ValidationProblem(errors);

        var organizerEmail = Validation.NormalizeOrganizerEmail(req.OrganizerEmail);
        var organizerName = Validation.NormalizeOptional(req.OrganizerName);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Description = (req.Description ?? "").Trim(),
            Location = (req.Location ?? "").Trim(),
            StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc),
            InviteToken = TokenGenerator.NewInviteToken(),
            AdminToken = TokenGenerator.NewAdminToken(),
            CreatedAt = DateTime.UtcNow,
            AllowMaybe = req.AllowMaybe,
            RsvpDeadline = req.RsvpDeadline.HasValue
                ? DateTime.SpecifyKind(req.RsvpDeadline.Value, DateTimeKind.Utc)
                : null,
            ContactRequirement = req.ContactRequirement,
            OrganizerEmail = organizerEmail,
            OrganizerName = organizerName
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(ev.OrganizerEmail))
        {
            emailQueue.Enqueue(AdminLinkTemplate.Build(ev, appOptions.Value.BaseUrl));
        }

        return Results.Created($"/api/manage/{ev.AdminToken}", new EventCreatedDto(
            ev.Id, ev.Title, ev.Description, ev.Location, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement,
            ev.OrganizerEmail, ev.OrganizerName));
    }

    private static async Task<IResult> GetByInviteToken(string inviteToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        return Results.Ok(new EventPublicDto(
            ev.Id, ev.Title, ev.Description, ev.Location, ev.StartsAt, ev.InviteToken,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement));
    }

    private static async Task<IResult> GetInviteePrefill(string inviteToken, Guid inviteeId, AppDbContext db)
    {
        var prefill = await db.Invitees.AsNoTracking()
            .Where(i => i.Id == inviteeId && i.Event.InviteToken == inviteToken)
            .Select(i => new InviteePrefillDto(i.Name, i.Email))
            .FirstOrDefaultAsync();
        return prefill is null ? Results.NotFound() : Results.Ok(prefill);
    }

    private static async Task<IResult> SubmitRsvp(
        string inviteToken,
        CreateRsvpRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        if (string.IsNullOrWhiteSpace(req.GuestName))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["guestName"] = new[] { "Navn er påkrævet." }
            });
        if (!Enum.IsDefined(typeof(RsvpStatus), req.Status))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Ugyldig status." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        if (ev.RsvpDeadline.HasValue && DateTime.UtcNow > ev.RsvpDeadline.Value)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["rsvp"] = new[] { "Tilmeldingen er lukket." }
            });

        if (req.Status == RsvpStatus.Maybe && !ev.AllowMaybe)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "'Måske' er ikke tilladt for dette event." }
            });

        string? email = null;
        string? phone = null;
        switch (ev.ContactRequirement)
        {
            case ContactRequirement.Email:
                var trimmedEmail = req.Email?.Trim();
                if (string.IsNullOrEmpty(trimmedEmail) || !Validation.LooksLikeEmail(trimmedEmail))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["email"] = new[] { "Email er påkrævet og skal være en gyldig email-adresse." }
                    });
                email = trimmedEmail;
                break;
            case ContactRequirement.Phone:
                var trimmedPhone = req.Phone?.Trim();
                if (string.IsNullOrEmpty(trimmedPhone) || trimmedPhone.Length < 5)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["phone"] = new[] { "Telefonnummer er påkrævet (mindst 5 tegn)." }
                    });
                phone = trimmedPhone;
                break;
        }

        var rsvp = new Rsvp
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            GuestName = req.GuestName.Trim(),
            Status = req.Status,
            Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            Email = email,
            Phone = phone,
            SubmittedAt = DateTime.UtcNow
        };
        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();

        var baseUrl = appOptions.Value.BaseUrl;
        if (!string.IsNullOrEmpty(rsvp.Email))
        {
            emailQueue.Enqueue(RsvpConfirmationTemplate.Build(ev, rsvp, baseUrl));
        }
        if (!string.IsNullOrEmpty(ev.OrganizerEmail))
        {
            emailQueue.Enqueue(RsvpNotificationTemplate.Build(ev, rsvp, baseUrl));
        }

        return Results.Ok(new RsvpDto(
            rsvp.Id, rsvp.GuestName, rsvp.Status, rsvp.Comment,
            rsvp.Email, rsvp.Phone, rsvp.SubmittedAt));
    }

    private static async Task<IResult> GetByAdminToken(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .Include(x => x.Rsvps)
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvps = ev.Rsvps
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new RsvpDto(r.Id, r.GuestName, r.Status, r.Comment, r.Email, r.Phone, r.SubmittedAt))
            .ToList();

        return Results.Ok(new EventAdminDto(
            ev.Id, ev.Title, ev.Description, ev.Location, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement,
            ev.OrganizerEmail, ev.OrganizerName,
            rsvps));
    }

    private static async Task<IResult> UpdateByAdminToken(string adminToken, UpdateEventRequest req, AppDbContext db)
    {
        var errors = ValidateEventOptions(
            req.Title, req.StartsAt, req.RsvpDeadline, req.ContactRequirement, req.OrganizerEmail);
        if (errors is not null) return Results.ValidationProblem(errors);

        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        ev.Title = req.Title.Trim();
        ev.Description = (req.Description ?? "").Trim();
        ev.Location = (req.Location ?? "").Trim();
        ev.StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc);
        ev.AllowMaybe = req.AllowMaybe;
        ev.RsvpDeadline = req.RsvpDeadline.HasValue
            ? DateTime.SpecifyKind(req.RsvpDeadline.Value, DateTimeKind.Utc)
            : null;
        ev.ContactRequirement = req.ContactRequirement;
        ev.OrganizerEmail = Validation.NormalizeOrganizerEmail(req.OrganizerEmail);
        ev.OrganizerName = Validation.NormalizeOptional(req.OrganizerName);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteRsvp(string adminToken, Guid rsvpId, AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.Id == rsvpId && r.EventId == ev.Id);
        if (rsvp is null) return Results.NotFound();

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();
        return Results.NoContent();
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

    private static Dictionary<string, string[]>? ValidateEventOptions(
        string title, DateTime startsAt, DateTime? rsvpDeadline, ContactRequirement contactRequirement,
        string? organizerEmail)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = new[] { "Titel er påkrævet." };

        if (rsvpDeadline.HasValue && rsvpDeadline.Value > startsAt)
            errors["rsvpDeadline"] = new[] { "SU-deadline kan ikke ligge efter eventet." };

        if (!Enum.IsDefined(typeof(ContactRequirement), contactRequirement))
            errors["contactRequirement"] = new[] { "Ugyldigt kontaktkrav." };

        var trimmedOrganizerEmail = organizerEmail?.Trim();
        if (!string.IsNullOrEmpty(trimmedOrganizerEmail) && !Validation.LooksLikeEmail(trimmedOrganizerEmail))
            errors["organizerEmail"] = new[] { "Din email skal være en gyldig email-adresse." };

        return errors.Count == 0 ? null : errors;
    }
}
