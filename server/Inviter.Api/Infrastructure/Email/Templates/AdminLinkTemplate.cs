using System.Globalization;
using System.Net;
using Inviter.Api.Domain;

namespace Inviter.Api.Infrastructure.Email.Templates;

public static class AdminLinkTemplate
{
    private static readonly CultureInfo DanishCulture = new("da-DK");

    public static QueuedEmail Build(Event ev, string baseUrl)
    {
        var adminUrl = $"{baseUrl.TrimEnd('/')}/manage/{ev.AdminToken}";
        var inviteUrl = $"{baseUrl.TrimEnd('/')}/invite/{ev.InviteToken}";
        var title = WebUtility.HtmlEncode(ev.Title);
        var startsLocal = ev.StartsAt.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture);
        var greeting = string.IsNullOrWhiteSpace(ev.OrganizerName)
            ? "Hej"
            : $"Hej {WebUtility.HtmlEncode(ev.OrganizerName)}";

        var subject = $"Dit admin-link til \"{ev.Title}\"";

        var html = $"""
<!doctype html>
<html lang="da">
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2a1a1a; background: #fdf8f3; margin: 0; padding: 24px;">
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 560px; margin: 0 auto; background: #fffdf9; border-radius: 12px; padding: 32px;">
    <tr><td>
      <h1 style="font-family: Georgia, 'Times New Roman', serif; font-size: 24px; margin: 0 0 16px;">Dit event er oprettet</h1>
      <p style="margin: 0 0 16px; line-height: 1.5;">{greeting},</p>
      <p style="margin: 0 0 16px; line-height: 1.5;">"<strong>{title}</strong>" er nu klar. Du kan styre tilmeldinger, opdatere detaljer og se hvem der kommer via dit admin-link:</p>
      <p style="margin: 0 0 24px;">
        <a href="{adminUrl}" style="display: inline-block; background: #6b1f2c; color: #fdf8f3; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 500;">Ã…bn admin-side</a>
      </p>
      <p style="margin: 0 0 8px; line-height: 1.5;"><strong>HvornÃ¥r:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      <p style="margin: 0 0 24px; line-height: 1.5;"><strong>Invite-link til gÃ¦sterne:</strong><br><a href="{inviteUrl}" style="color: #6b1f2c;">{inviteUrl}</a></p>
      <hr style="border: none; border-top: 1px solid #e8dccd; margin: 24px 0;">
      <p style="margin: 0; font-size: 13px; color: #8a6e6e; line-height: 1.5;">Gem denne mail â€” admin-linket er dit eneste adgangspunkt og kan ikke gendannes hvis du mister det.</p>
    </td></tr>
  </table>
</body>
</html>
""";

        var text = $"""
{(string.IsNullOrWhiteSpace(ev.OrganizerName) ? "Hej" : $"Hej {ev.OrganizerName}")},

Dit event "{ev.Title}" er oprettet.

HvornÃ¥r: {startsLocal}

Admin-link (gem denne mail!):
{adminUrl}

Invite-link til gÃ¦sterne:
{inviteUrl}

Admin-linket er dit eneste adgangspunkt og kan ikke gendannes hvis du mister det.
""";

        return new QueuedEmail(
            ToAddress: ev.OrganizerEmail!,
            ToName: ev.OrganizerName,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            Kind: "AdminLink");
    }
}
