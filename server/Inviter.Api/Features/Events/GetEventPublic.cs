using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Events;

public static class GetEventPublic
{
    public static async Task<IResult> Handle(string inviteToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        return Results.Ok(new EventPublicDto(
            ev.Id, ev.Title, ev.Description, ev.Location, ev.StartsAt, ev.InviteToken,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement));
    }
}
