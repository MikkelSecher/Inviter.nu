using System.Globalization;
using System.Net;
using Inviter.Api.Domain;

namespace Inviter.Api.Infrastructure.Email.Templates;

public static class RsvpNotificationTemplate
{
    private static readonly CultureInfo DanishCulture = new("da-DK");

    public static QueuedEmail Build(Event ev, Rsvp rsvp, string baseUrl)
    {
        var adminUrl = $"{baseUrl.TrimEnd('/')}/manage/{ev.AdminToken}";
        var guestName = WebUtility.HtmlEncode(rsvp.GuestName);
        var submittedLocal = rsvp.SubmittedAt.ToLocalTime().ToString("d. MMMM yyyy 'kl.' HH:mm", DanishCulture);
        var greeting = string.IsNullOrWhiteSpace(ev.OrganizerName)
            ? "Hej"
            : $"Hej {WebUtility.HtmlEncode(ev.OrganizerName)}";
        var statusLabel = rsvp.Status switch
        {
            RsvpStatus.Yes => "Kommer",
            RsvpStatus.No => "Kommer ikke",
            RsvpStatus.Maybe => "Måske",
            _ => "Svar modtaget"
        };
        var statusColor = rsvp.Status switch
        {
            RsvpStatus.Yes => EmailTemplateLayout.StatusYes,
            RsvpStatus.No => EmailTemplateLayout.StatusNo,
            RsvpStatus.Maybe => EmailTemplateLayout.StatusMaybe,
            _ => EmailTemplateLayout.Muted
        };
        var statusForeground = rsvp.Status switch
        {
            RsvpStatus.Yes => EmailTemplateLayout.StatusYesForeground,
            RsvpStatus.No => EmailTemplateLayout.StatusNoForeground,
            RsvpStatus.Maybe => EmailTemplateLayout.StatusMaybeForeground,
            _ => EmailTemplateLayout.Foreground
        };

        var subject = $"{rsvp.GuestName} svarede på \"{ev.Title}\": {statusLabel}";

        var commentBlock = string.IsNullOrWhiteSpace(rsvp.Comment)
            ? ""
            : $"""
<p style="margin: 16px 0 8px; line-height: 1.55;"><strong>Kommentar:</strong></p>
<blockquote style="margin: 0 0 16px; padding: 12px 16px; background: {EmailTemplateLayout.Muted}; border-left: 3px solid {EmailTemplateLayout.Primary}; border-radius: 8px; line-height: 1.55; white-space: pre-wrap;">{WebUtility.HtmlEncode(rsvp.Comment)}</blockquote>
""";

        var contactBlock = (rsvp.Email, rsvp.Phone) switch
        {
            ({ } e, _) when !string.IsNullOrWhiteSpace(e) => $"<p style=\"{EmailTemplateLayout.MetaStyle}\"><strong>Email:</strong> <a href=\"mailto:{WebUtility.HtmlEncode(e)}\" style=\"color: {EmailTemplateLayout.Primary};\">{WebUtility.HtmlEncode(e)}</a></p>",
            (_, { } p) when !string.IsNullOrWhiteSpace(p) => $"<p style=\"{EmailTemplateLayout.MetaStyle}\"><strong>Telefon:</strong> <a href=\"tel:{WebUtility.HtmlEncode(p)}\" style=\"color: {EmailTemplateLayout.Primary};\">{WebUtility.HtmlEncode(p)}</a></p>",
            _ => ""
        };

        var html = EmailTemplateLayout.Shell(
            $"Nyt svar fra {rsvp.GuestName}",
            $"""
      {EmailTemplateLayout.Eyebrow("Nyt svar")}
      {EmailTemplateLayout.Heading($"Nyt svar på \"{ev.Title}\"", 24)}
      <p style="{EmailTemplateLayout.ParagraphStyle}">{greeting},</p>
      <p style="{EmailTemplateLayout.MetaStyle}"><strong>{guestName}</strong> har svaret: {EmailTemplateLayout.Pill(statusLabel, statusColor, statusForeground)}</p>
      <p style="{EmailTemplateLayout.MetaStyle}"><strong>Tidspunkt:</strong> {WebUtility.HtmlEncode(submittedLocal)}</p>
      {contactBlock}
      {commentBlock}
      <p style="margin: 24px 0;">
        {EmailTemplateLayout.Button(adminUrl, "Se alle svar")}
      </p>
""");

        var textContact = (rsvp.Email, rsvp.Phone) switch
        {
            ({ } e, _) when !string.IsNullOrWhiteSpace(e) => $"Email: {e}\n",
            (_, { } p) when !string.IsNullOrWhiteSpace(p) => $"Telefon: {p}\n",
            _ => ""
        };
        var textComment = string.IsNullOrWhiteSpace(rsvp.Comment) ? "" : $"\nKommentar:\n{rsvp.Comment}\n";

        var text = $"""
{(string.IsNullOrWhiteSpace(ev.OrganizerName) ? "Hej" : $"Hej {ev.OrganizerName}")},

{rsvp.GuestName} har svaret på "{ev.Title}": {statusLabel}.
Tidspunkt: {submittedLocal}
{textContact}{textComment}
Se alle svar:
{adminUrl}
""";

        return new QueuedEmail(
            ToAddress: ev.OrganizerEmail!,
            ToName: ev.OrganizerName,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            Kind: "RsvpNotification");
    }
}
