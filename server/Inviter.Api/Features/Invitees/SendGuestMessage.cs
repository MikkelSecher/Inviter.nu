using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class SendGuestMessage
{
    public static async Task<IResult> Handle(
        string adminToken,
        SendGuestMessageRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        CancellationToken ct)
    {
        var errors = Validate(req);
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken, ct);
        if (ev is null) return Results.NotFound();

        var invitees = await db.Invitees.AsNoTracking()
            .Where(i => i.EventId == ev.Id)
            .ToListAsync(ct);
        var rsvps = await db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == ev.Id)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

        var recipients = BuildRecipients(invitees, rsvps, req.Audience);
        var subject = req.Subject!.Trim();
        var message = req.Message!.Trim();

        foreach (var recipient in recipients)
        {
            emailQueue.Enqueue(GuestMessageTemplate.Build(
                ev,
                recipient.Email,
                recipient.Name,
                subject,
                message));
        }

        return Results.Ok(new SendGuestMessageResponse(recipients.Count));
    }

    private static Dictionary<string, string[]> Validate(SendGuestMessageRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Enum.IsDefined(typeof(GuestMessageAudience), req.Audience))
        {
            errors["audience"] = new[] { "Vælg hvem beskeden skal sendes til." };
        }

        var subject = req.Subject?.Trim();
        if (string.IsNullOrEmpty(subject))
        {
            errors["subject"] = new[] { "Emne er påkrævet." };
        }
        else if (subject.Length > 200)
        {
            errors["subject"] = new[] { "Emne må højst være 200 tegn." };
        }

        var message = req.Message?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            errors["message"] = new[] { "Besked er påkrævet." };
        }
        else if (message.Length > 4000)
        {
            errors["message"] = new[] { "Besked må højst være 4000 tegn." };
        }

        return errors;
    }

    private static IReadOnlyList<GuestMessageRecipient> BuildRecipients(
        IReadOnlyList<Invitee> invitees,
        IReadOnlyList<Rsvp> rsvps,
        GuestMessageAudience audience)
    {
        var recipients = new Dictionary<string, GuestMessageRecipient>(StringComparer.OrdinalIgnoreCase);
        var inviteeIds = invitees.Select(i => i.Id).ToHashSet();

        foreach (var invitee in invitees)
        {
            var latestRsvp = LatestRsvpForInvitee(invitee, rsvps);
            if (audience != GuestMessageAudience.All && latestRsvp?.Status != ToStatus(audience))
            {
                continue;
            }

            AddRecipient(
                recipients,
                invitee.Email ?? latestRsvp?.Email,
                invitee.Name ?? latestRsvp?.GuestName);
        }

        var latestUnlinkedByEmail = rsvps
            .Where(r => r.InviteeId is null || !inviteeIds.Contains(r.InviteeId.Value))
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .GroupBy(r => r.Email!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.SubmittedAt).First());

        foreach (var rsvp in latestUnlinkedByEmail)
        {
            if (audience != GuestMessageAudience.All && rsvp.Status != ToStatus(audience))
            {
                continue;
            }

            AddRecipient(recipients, rsvp.Email, rsvp.GuestName);
        }

        return recipients.Values.ToList();
    }

    private static Rsvp? LatestRsvpForInvitee(Invitee invitee, IReadOnlyList<Rsvp> rsvps)
    {
        var email = invitee.Email;
        return rsvps
            .Where(r => r.InviteeId == invitee.Id
                || (!string.IsNullOrWhiteSpace(email)
                    && !string.IsNullOrWhiteSpace(r.Email)
                    && string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefault();
    }

    private static void AddRecipient(
        Dictionary<string, GuestMessageRecipient> recipients,
        string? email,
        string? name)
    {
        var trimmedEmail = email?.Trim();
        if (string.IsNullOrEmpty(trimmedEmail)) return;

        recipients.TryAdd(
            trimmedEmail,
            new GuestMessageRecipient(
                trimmedEmail,
                string.IsNullOrWhiteSpace(name) ? null : name.Trim()));
    }

    private static RsvpStatus? ToStatus(GuestMessageAudience audience) =>
        audience switch
        {
            GuestMessageAudience.Yes => RsvpStatus.Yes,
            GuestMessageAudience.No => RsvpStatus.No,
            _ => null
        };

    private sealed record GuestMessageRecipient(string Email, string? Name);
}
