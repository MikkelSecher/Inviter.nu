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
        var title = WebUtility.HtmlEncode(ev.Title);
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
            RsvpStatus.Yes => "#3f7d4c",
            RsvpStatus.No => "#8a3a3a",
            RsvpStatus.Maybe => "#a37522",
            _ => "#6b1f2c"
        };

        var subject = $"{rsvp.GuestName} svarede på \"{ev.Title}\": {statusLabel}";

        var commentBlock = string.IsNullOrWhiteSpace(rsvp.Comment)
            ? ""
            : $"""
<p style="margin: 16px 0 8px; line-height: 1.5;"><strong>Kommentar:</strong></p>
<blockquote style="margin: 0 0 16px; padding: 12px 16px; background: #f6ece0; border-left: 3px solid #6b1f2c; border-radius: 4px; line-height: 1.5; white-space: pre-wrap;">{WebUtility.HtmlEncode(rsvp.Comment)}</blockquote>
""";

        var contactBlock = (rsvp.Email, rsvp.Phone) switch
        {
            ({ } e, _) when !string.IsNullOrWhiteSpace(e) => $"<p style=\"margin: 0 0 8px; line-height: 1.5;\"><strong>Email:</strong> <a href=\"mailto:{WebUtility.HtmlEncode(e)}\" style=\"color: #6b1f2c;\">{WebUtility.HtmlEncode(e)}</a></p>",
            (_, { } p) when !string.IsNullOrWhiteSpace(p) => $"<p style=\"margin: 0 0 8px; line-height: 1.5;\"><strong>Telefon:</strong> <a href=\"tel:{WebUtility.HtmlEncode(p)}\" style=\"color: #6b1f2c;\">{WebUtility.HtmlEncode(p)}</a></p>",
            _ => ""
        };

        var html = $"""
<!doctype html>
<html lang="da">
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2a1a1a; background: #fdf8f3; margin: 0; padding: 24px;">
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 560px; margin: 0 auto; background: #fffdf9; border-radius: 12px; padding: 32px;">
    <tr><td>
      <h1 style="font-family: Georgia, 'Times New Roman', serif; font-size: 22px; margin: 0 0 16px;">Nyt svar på "{title}"</h1>
      <p style="margin: 0 0 16px; line-height: 1.5;">{greeting},</p>
      <p style="margin: 0 0 8px; line-height: 1.5;"><strong>{guestName}</strong> har svaret: <span style="display: inline-block; padding: 2px 10px; background: {statusColor}; color: #fdf8f3; border-radius: 999px; font-size: 13px; font-weight: 500;">{statusLabel}</span></p>
      <p style="margin: 0 0 8px; line-height: 1.5;"><strong>Tidspunkt:</strong> {WebUtility.HtmlEncode(submittedLocal)}</p>
      {contactBlock}
      {commentBlock}
      <p style="margin: 24px 0;">
        <a href="{adminUrl}" style="display: inline-block; background: #6b1f2c; color: #fdf8f3; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 500;">Se alle svar</a>
      </p>
    </td></tr>
  </table>
</body>
</html>
""";

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
