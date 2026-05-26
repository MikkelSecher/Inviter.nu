using System.Globalization;
using System.Net;
using Inviter.Api.Domain;

namespace Inviter.Api.Infrastructure.Email.Templates;

public static class RsvpConfirmationTemplate
{
    private static readonly CultureInfo DanishCulture = new("da-DK");

    public static QueuedEmail Build(Event ev, Rsvp rsvp, string baseUrl)
    {
        var inviteUrl = $"{baseUrl.TrimEnd('/')}/invite/{ev.InviteToken}";
        var title = WebUtility.HtmlEncode(ev.Title);
        var guestName = WebUtility.HtmlEncode(rsvp.GuestName);
        var startsLocal = ev.StartsAt.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture);
        var location = string.IsNullOrWhiteSpace(ev.Location) ? null : ev.Location;
        var statusLabel = rsvp.Status switch
        {
            RsvpStatus.Yes => "Du kommer",
            RsvpStatus.No => "Du kommer ikke",
            RsvpStatus.Maybe => "Du svarede mÃ¥ske",
            _ => "Dit svar er registreret"
        };

        var subject = $"Tak for dit svar til \"{ev.Title}\"";

        var locationLine = location is null
            ? ""
            : $"<p style=\"margin: 0 0 8px; line-height: 1.5;\"><strong>Sted:</strong> {WebUtility.HtmlEncode(location)}</p>";

        var html = $"""
<!doctype html>
<html lang="da">
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2a1a1a; background: #fdf8f3; margin: 0; padding: 24px;">
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 560px; margin: 0 auto; background: #fffdf9; border-radius: 12px; padding: 32px;">
    <tr><td>
      <h1 style="font-family: Georgia, 'Times New Roman', serif; font-size: 24px; margin: 0 0 16px;">Tak for dit svar</h1>
      <p style="margin: 0 0 16px; line-height: 1.5;">Hej {guestName},</p>
      <p style="margin: 0 0 16px; line-height: 1.5;">Vi har modtaget dit svar til "<strong>{title}</strong>": <strong>{statusLabel}</strong>.</p>
      <p style="margin: 0 0 8px; line-height: 1.5;"><strong>HvornÃ¥r:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      {locationLine}
      <p style="margin: 16px 0 24px; line-height: 1.5;">Du kan Ã¦ndre dit svar nÃ¥r som helst ved at Ã¥bne invitationen igen:</p>
      <p style="margin: 0 0 24px;">
        <a href="{inviteUrl}" style="display: inline-block; background: #6b1f2c; color: #fdf8f3; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 500;">Ã…bn invitation</a>
      </p>
      <hr style="border: none; border-top: 1px solid #e8dccd; margin: 24px 0;">
      <p style="margin: 0; font-size: 13px; color: #8a6e6e; line-height: 1.5;">Dette er en automatisk bekrÃ¦ftelse. Du modtager den fordi du svarede pÃ¥ en invitation til {title}.</p>
    </td></tr>
  </table>
</body>
</html>
""";

        var textLocationLine = location is null ? "" : $"Sted: {location}\n";

        var text = $"""
Hej {rsvp.GuestName},

Vi har modtaget dit svar til "{ev.Title}": {statusLabel}.

HvornÃ¥r: {startsLocal}
{textLocationLine}
Du kan Ã¦ndre dit svar nÃ¥r som helst:
{inviteUrl}
""";

        return new QueuedEmail(
            ToAddress: rsvp.Email!,
            ToName: rsvp.GuestName,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            Kind: "RsvpConfirmation");
    }
}
