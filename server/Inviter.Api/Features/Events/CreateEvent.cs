using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Inviter.Api.Infrastructure.Tokens;
using Inviter.Api.Shared;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Features.Events;

public static class CreateEvent
{
    public static async Task<IResult> Handle(
        CreateEventRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        var errors = EventValidation.Validate(
            req.Title, req.StartsAt, req.RsvpDeadline, req.ContactRequirement, req.OrganizerEmail);
        if (errors is not null) return Results.ValidationProblem(errors);

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
            OrganizerEmail = Validation.NormalizeOrganizerEmail(req.OrganizerEmail),
            OrganizerName = Validation.NormalizeOptional(req.OrganizerName),
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
}
