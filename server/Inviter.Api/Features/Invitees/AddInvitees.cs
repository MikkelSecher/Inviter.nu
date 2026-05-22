using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class AddInvitees
{
    private const int MaxInviteesPerBulkAdd = 200;

    public static async Task<IResult> Handle(string adminToken, AddInviteesRequest req, AppDbContext db)
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
}
