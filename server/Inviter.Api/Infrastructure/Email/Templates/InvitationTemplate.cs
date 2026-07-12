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
            : $"<p style=\"{EmailTemplateLayout.MetaStyle}\"><strong>Sted:</strong> {WebUtility.HtmlEncode(location)}</p>";

        var deadlineLine = ev.RsvpDeadline.HasValue
            ? $"<p style=\"margin: 0 0 16px; line-height: 1.55; color: {EmailTemplateLayout.Primary};\"><strong>Svar senest:</strong> {WebUtility.HtmlEncode(ev.RsvpDeadline.Value.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture))}</p>"
            : "";

        var descriptionBlock = string.IsNullOrWhiteSpace(ev.Description)
            ? ""
            : $"<p style=\"margin: 16px 0; line-height: 1.55; white-space: pre-wrap;\">{WebUtility.HtmlEncode(ev.Description)}</p>";

        var resendLine = isResend
            ? $"<p style=\"margin: 0 0 16px; line-height: 1.55; color: {EmailTemplateLayout.MutedForeground}; font-size: 14px;\">Du har modtaget denne invitation før - vi sender den igen som påmindelse.</p>"
            : "";

        var organizerSignature = organizer is null
            ? ""
            : $"<p style=\"margin: 24px 0 0; line-height: 1.55;\">Hilsen,<br>{WebUtility.HtmlEncode(organizer)}</p>";

        var imageRow = image is null
            ? ""
            : $"<tr><td style=\"padding: 0;\"><img src=\"cid:{image.ContentId}\" width=\"592\" alt=\"\" style=\"display: block; width: 100%; height: auto; border-radius: 8px 8px 0 0;\"></td></tr><tr><td style=\"height: 8px; background: {EmailTemplateLayout.Accent}; font-size: 0; line-height: 0;\">&nbsp;</td></tr>";

        var html = EmailTemplateLayout.Shell(
            $"Du er inviteret til {ev.Title}",
            $"""
      {EmailTemplateLayout.Eyebrow("Du er inviteret")}
      {EmailTemplateLayout.Heading(ev.Title)}
      {resendLine}
      <p style="{EmailTemplateLayout.ParagraphStyle}">{greeting},</p>
      <p style="{EmailTemplateLayout.MetaStyle}"><strong>Hvornår:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      {locationLine}
      {deadlineLine}
      {descriptionBlock}
      <p style="margin: 24px 0;">
        {EmailTemplateLayout.Button(inviteUrl, "Svar på invitationen")}
      </p>
      {EmailTemplateLayout.Note($"Eller åbn linket direkte:<br>{EmailTemplateLayout.TextLink(inviteUrl, inviteUrl)}")}
      {organizerSignature}
""",
            image is null ? null : imageRow);

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
