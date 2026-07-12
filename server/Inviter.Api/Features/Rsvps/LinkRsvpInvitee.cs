using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Rsvps;

public static class LinkRsvpInvitee
{
    public static async Task<IResult> Handle(
        string adminToken,
        Guid rsvpId,
        LinkRsvpInviteeRequest req,
        AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.Id == rsvpId && r.EventId == ev.Id);
        if (rsvp is null) return Results.NotFound();

        if (req.InviteeId is not null)
        {
            var inviteeExists = await db.Invitees.AsNoTracking()
                .AnyAsync(i => i.Id == req.InviteeId.Value && i.EventId == ev.Id);
            if (!inviteeExists) return Results.NotFound();
        }

        rsvp.InviteeId = req.InviteeId;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
