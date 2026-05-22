using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class DeleteInvitee
{
    public static async Task<IResult> Handle(string adminToken, Guid inviteeId, AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitee = await db.Invitees.FirstOrDefaultAsync(i => i.Id == inviteeId && i.EventId == ev.Id);
        if (invitee is null) return Results.NotFound();

        db.Invitees.Remove(invitee);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
