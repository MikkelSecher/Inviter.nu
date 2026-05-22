using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Events;

public static class UpdateEvent
{
    public static async Task<IResult> Handle(string adminToken, UpdateEventRequest req, AppDbContext db)
    {
        var errors = EventValidation.Validate(
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
}
