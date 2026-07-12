using Inviter.Api.Data;
using Inviter.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class ListInvitees
{
    public static async Task<IResult> Handle(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitees = await db.Invitees.AsNoTracking()
            .Where(i => i.EventId == ev.Id)
            .OrderBy(i => i.AddedAt)
            .ToListAsync();

        var rsvps = await db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == ev.Id)
            .Select(r => new { r.InviteeId, r.Email, r.Status, r.SubmittedAt })
            .ToListAsync();

        var rsvpStatusByInviteeId = rsvps
            .Where(r => r.InviteeId.HasValue)
            .GroupBy(r => r.InviteeId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (RsvpStatus?)g.OrderByDescending(r => r.SubmittedAt).First().Status);

        var rsvpStatusByEmail = rsvps
            .Where(r => r.Email is not null)
            .GroupBy(r => r.Email!.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => (RsvpStatus?)g.OrderByDescending(r => r.SubmittedAt).First().Status);

        var dtos = invitees.Select(i => new InviteeDto(
            i.Id, i.PersonalInviteToken, i.Email, i.Name, i.AddedAt, i.LastSentAt, i.SendCount,
            rsvpStatusByInviteeId.GetValueOrDefault(i.Id)
                ?? (i.Email is null ? null : rsvpStatusByEmail.GetValueOrDefault(i.Email.ToLowerInvariant())))).ToList();

        return Results.Ok(dtos);
    }
}
