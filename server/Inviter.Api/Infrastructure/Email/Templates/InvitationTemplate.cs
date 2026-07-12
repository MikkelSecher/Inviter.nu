using System.Globalization;
using System.Net;
using Inviter.Api.Domain;

namespace Inviter.Api.Infrastructure.Email.Templates;

public static class InvitationTemplate
{
    private static readonly CultureInfo DanishCulture = new("da-DK");

    public static QueuedEmail Build(
        Event ev,
        Invitee invitee,
        string baseUrl,
        bool isResend,
        InlineAttachment? image = null)
    {
        var inviteUrl = $"{baseUrl.TrimEnd('/')}/invite/{ev.InviteToken}?g={invitee.PersonalInviteToken}";
        var title = WebUtility.HtmlEncode(ev.Title);
        var startsLocal = ev.StartsAt.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture);
        var location = string.IsNullOrWhiteSpace(ev.Location) ? null : ev.Location;
        var organizer = string.IsNullOrWhiteSpace(ev.OrganizerName) ? null : ev.OrganizerName;
        var greeting = string.IsNullOrWhiteSpace(invitee.Name)
            ? "Hej"
            : $"Hej {WebUtility.HtmlEncode(invitee.Name)}";

        var subject = isResend
            ? $"Påmindelse: Du er inviteret til \"{ev.Title}\""
            : $"Du er inviteret til \"{ev.Title}\"";

        var locationLine = location is null
            ? ""
            : $"<p style=\"margin: 0 0 8px; line-height: 1.5;\"><strong>Sted:</strong> {WebUtility.HtmlEncode(location)}</p>";

        var deadlineLine = ev.RsvpDeadline.HasValue
            ? $"<p style=\"margin: 0 0 16px; line-height: 1.5; color: #6b1f2c;\"><strong>Svar senest:</strong> {WebUtility.HtmlEncode(ev.RsvpDeadline.Value.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture))}</p>"
            : "";

        var descriptionBlock = string.IsNullOrWhiteSpace(ev.Description)
            ? ""
            : $"<p style=\"margin: 16px 0; line-height: 1.5; white-space: pre-wrap;\">{WebUtility.HtmlEncode(ev.Description)}</p>";

        var resendLine = isResend
            ? "<p style=\"margin: 0 0 16px; line-height: 1.5; color: #8a6e6e; font-size: 14px;\">Du har modtaget denne invitation før - vi sender den igen som påmindelse.</p>"
            : "";

        var organizerSignature = organizer is null
            ? ""
            : $"<p style=\"margin: 24px 0 0; line-height: 1.5;\">Hilsen,<br>{WebUtility.HtmlEncode(organizer)}</p>";

        var imageRow = image is null
            ? ""
            : $"<tr><td style=\"padding: 0;\"><img src=\"cid:{image.ContentId}\" width=\"560\" alt=\"\" style=\"display: block; width: 100%; height: auto; border-radius: 12px 12px 0 0;\"></td></tr>";

        var html = $"""
<!doctype html>
<html lang="da">
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2a1a1a; background: #fdf8f3; margin: 0; padding: 24px;">
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 560px; margin: 0 auto; background: #fffdf9; border-radius: 12px; overflow: hidden;">
    {imageRow}
    <tr><td style="padding: 32px;">
      <p style="margin: 0 0 8px; font-size: 12px; font-weight: 500; letter-spacing: 1.5px; text-transform: uppercase; color: #8a6e6e;">Du er inviteret</p>
      <h1 style="font-family: Georgia, 'Times New Roman', serif; font-size: 28px; margin: 0 0 24px; line-height: 1.15;">{title}</h1>
      {resendLine}
      <p style="margin: 0 0 16px; line-height: 1.5;">{greeting},</p>
      <p style="margin: 0 0 8px; line-height: 1.5;"><strong>Hvornår:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      {locationLine}
      {deadlineLine}
      {descriptionBlock}
      <p style="margin: 24px 0;">
        <a href="{inviteUrl}" style="display: inline-block; background: #6b1f2c; color: #fdf8f3; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 500;">Svar på invitationen</a>
      </p>
      <p style="margin: 0; font-size: 13px; color: #8a6e6e; line-height: 1.5;">Eller åbn linket direkte:<br><a href="{inviteUrl}" style="color: #6b1f2c; word-break: break-all;">{inviteUrl}</a></p>
      {organizerSignature}
    </td></tr>
  </table>
</body>
</html>
""";

        var textLocationLine = location is null ? "" : $"Sted: {location}\n";
        var textDeadlineLine = ev.RsvpDeadline.HasValue
            ? $"Svar senest: {ev.RsvpDeadline.Value.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture)}\n"
            : "";
        var textDescription = string.IsNullOrWhiteSpace(ev.Description) ? "" : $"\n{ev.Description}\n";
        var textResend = isResend ? "(Påmindelse - vi sender invitationen igen)\n\n" : "";
        var textSignature = organizer is null ? "" : $"\nHilsen,\n{organizer}\n";

        var text = $"""
{textResend}{(string.IsNullOrWhiteSpace(invitee.Name) ? "Hej" : $"Hej {invitee.Name}")},

Du er inviteret til "{ev.Title}".

Hvornår: {startsLocal}
{textLocationLine}{textDeadlineLine}{textDescription}
Svar på invitationen:
{inviteUrl}
{textSignature}
""";

        return new QueuedEmail(
            ToAddress: invitee.Email!,
            ToName: invitee.Name,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            Kind: isResend ? "InvitationResend" : "Invitation",
            InlineAttachments: image is null ? null : [image]);
    }
}
