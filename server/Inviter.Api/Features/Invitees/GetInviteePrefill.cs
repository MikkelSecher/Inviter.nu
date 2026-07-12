using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class GetInviteePrefill
{
    public static async Task<IResult> Handle(string inviteToken, string inviteeToken, AppDbContext db)
    {
        var invitee = await db.Invitees.AsNoTracking()
            .Where(i => i.PersonalInviteToken == inviteeToken && i.Event.InviteToken == inviteToken)
            .Select(i => new { i.Id, i.EventId, i.Name, i.Email })
            .FirstOrDefaultAsync();
        if (invitee is null) return Results.NotFound();

        var rsvps = db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == invitee.EventId && r.InviteeId == invitee.Id);

        if (!string.IsNullOrEmpty(invitee.Email))
        {
            var emailKey = invitee.Email.ToLowerInvariant();
            rsvps = db.Rsvps.AsNoTracking()
                .Where(r => r.EventId == invitee.EventId
                    && (r.InviteeId == invitee.Id
                        || (r.Email != null && r.Email.ToLower() == emailKey)));
        }

        var latestRsvp = await rsvps
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new
            {
                r.GuestName,
                r.Status,
                r.Comment,
                r.Email,
                r.Phone
            })
            .FirstOrDefaultAsync();

        return Results.Ok(new InviteePrefillDto(
            invitee.Name,
            invitee.Email,
            latestRsvp?.GuestName,
            latestRsvp?.Status,
            latestRsvp?.Comment,
            latestRsvp?.Email,
            latestRsvp?.Phone));
    }
}
