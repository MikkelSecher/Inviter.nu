using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Features.Rsvps;

public static class SubmitRsvp
{
    public static async Task<IResult> Handle(
        string inviteToken,
        CreateRsvpRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        if (string.IsNullOrWhiteSpace(req.GuestName))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["guestName"] = new[] { "Navn er påkrævet." }
            });
        if (!Enum.IsDefined(typeof(RsvpStatus), req.Status))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Ugyldig status." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        if (ev.RsvpDeadline.HasValue && DateTime.UtcNow > ev.RsvpDeadline.Value)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["rsvp"] = new[] { "Tilmeldingen er lukket." }
            });

        if (req.Status == RsvpStatus.Maybe && !ev.AllowMaybe)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "'Måske' er ikke tilladt for dette event." }
            });

        var (contactErrors, email, phone) = ApplyContactRequirement(ev.ContactRequirement, req);
        if (contactErrors is not null) return Results.ValidationProblem(contactErrors);

        var invitee = await ResolveInviteeAsync(ev.Id, req, email, db);

        var rsvp = new Rsvp
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            InviteeId = invitee?.Id,
            GuestName = req.GuestName.Trim(),
            Status = req.Status,
            Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            Email = email,
            Phone = phone,
            SubmittedAt = DateTime.UtcNow
        };
        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();

        var baseUrl = appOptions.Value.BaseUrl;
        if (!string.IsNullOrEmpty(rsvp.Email))
        {
            emailQueue.Enqueue(RsvpConfirmationTemplate.Build(ev, rsvp, baseUrl));
        }
        if (!string.IsNullOrEmpty(ev.OrganizerEmail))
        {
            emailQueue.Enqueue(RsvpNotificationTemplate.Build(ev, rsvp, baseUrl));
        }

        return Results.Ok(new RsvpDto(
            rsvp.Id, rsvp.InviteeId, invitee?.Name, invitee?.Email,
            rsvp.GuestName, rsvp.Status, rsvp.Comment,
            rsvp.Email, rsvp.Phone, rsvp.SubmittedAt));
    }

    private static async Task<Invitee?> ResolveInviteeAsync(
        Guid eventId,
        CreateRsvpRequest req,
        string? normalizedEmail,
        AppDbContext db)
    {
        var inviteeToken = req.InviteeToken?.Trim();
        if (!string.IsNullOrEmpty(inviteeToken))
        {
            var byToken = await db.Invitees.AsNoTracking()
                .FirstOrDefaultAsync(i => i.EventId == eventId && i.PersonalInviteToken == inviteeToken);
            if (byToken is not null) return byToken;
        }

        if (!string.IsNullOrEmpty(normalizedEmail))
        {
            var key = normalizedEmail.ToLowerInvariant();
            return await db.Invitees.AsNoTracking()
                .FirstOrDefaultAsync(i => i.EventId == eventId
                    && i.Email != null
                    && i.Email.ToLower() == key);
        }

        return null;
    }

    private static (Dictionary<string, string[]>? errors, string? email, string? phone) ApplyContactRequirement(
        ContactRequirement requirement, CreateRsvpRequest req)
    {
        switch (requirement)
        {
            case ContactRequirement.Email:
                var trimmedEmail = req.Email?.Trim();
                if (string.IsNullOrEmpty(trimmedEmail) || !Validation.LooksLikeEmail(trimmedEmail))
                    return (new Dictionary<string, string[]>
                    {
                        ["email"] = new[] { "Email er påkrævet og skal være en gyldig email-adresse." }
                    }, null, null);
                return (null, trimmedEmail, null);

            case ContactRequirement.Phone:
                var trimmedPhone = req.Phone?.Trim();
                if (string.IsNullOrEmpty(trimmedPhone) || trimmedPhone.Length < 5)
                    return (new Dictionary<string, string[]>
                    {
                        ["phone"] = new[] { "Telefonnummer er påkrævet (mindst 5 tegn)." }
                    }, null, null);
                return (null, null, trimmedPhone);

            default:
                return (null, null, null);
        }
    }
}
