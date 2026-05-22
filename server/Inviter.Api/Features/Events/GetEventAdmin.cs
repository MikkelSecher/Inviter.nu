using Inviter.Api.Data;
using Inviter.Api.Features.Rsvps;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Events;

public static class GetEventAdmin
{
    public static async Task<IResult> Handle(string adminToken, AppDbContext db)
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
}
