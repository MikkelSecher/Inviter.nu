using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Rsvps;

public static class DeleteRsvp
{
    public static async Task<IResult> Handle(string adminToken, Guid rsvpId, AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.Id == rsvpId && r.EventId == ev.Id);
        if (rsvp is null) return Results.NotFound();

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
