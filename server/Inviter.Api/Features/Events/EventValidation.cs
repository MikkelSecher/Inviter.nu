using Inviter.Api.Domain;
using Inviter.Api.Shared;

namespace Inviter.Api.Features.Events;

internal static class EventValidation
{
    public static Dictionary<string, string[]>? Validate(
        string title,
        DateTime startsAt,
        DateTime? rsvpDeadline,
        ContactRequirement contactRequirement,
        string? organizerEmail)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = new[] { "Titel er påkrævet." };

        if (rsvpDeadline.HasValue && rsvpDeadline.Value > startsAt)
            errors["rsvpDeadline"] = new[] { "SU-deadline kan ikke ligge efter eventet." };

        if (!Enum.IsDefined(typeof(ContactRequirement), contactRequirement))
            errors["contactRequirement"] = new[] { "Ugyldigt kontaktkrav." };

        var trimmedOrganizerEmail = organizerEmail?.Trim();
        if (!string.IsNullOrEmpty(trimmedOrganizerEmail) && !Validation.LooksLikeEmail(trimmedOrganizerEmail))
            errors["organizerEmail"] = new[] { "Din email skal være en gyldig email-adresse." };

        return errors.Count == 0 ? null : errors;
    }
}
