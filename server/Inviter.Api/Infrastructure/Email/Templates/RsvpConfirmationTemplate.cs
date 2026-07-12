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
            RsvpStatus.Maybe => "Du svarede måske",
            _ => "Dit svar er registreret"
        };

        var subject = $"Tak for dit svar til \"{ev.Title}\"";

        var locationLine = location is null
            ? ""
            : $"<p style=\"{EmailTemplateLayout.MetaStyle}\"><strong>Sted:</strong> {WebUtility.HtmlEncode(location)}</p>";

        var html = EmailTemplateLayout.Shell(
            $"Vi har modtaget dit svar til {ev.Title}",
            $"""
      {EmailTemplateLayout.Eyebrow("Svar modtaget")}
      {EmailTemplateLayout.Heading("Tak for dit svar", 26)}
      <p style="{EmailTemplateLayout.ParagraphStyle}">Hej {guestName},</p>
      <p style="{EmailTemplateLayout.ParagraphStyle}">Vi har modtaget dit svar til "<strong>{title}</strong>": <strong>{statusLabel}</strong>.</p>
      <p style="{EmailTemplateLayout.MetaStyle}"><strong>Hvornår:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      {locationLine}
      <p style="margin: 16px 0 24px; line-height: 1.55;">Du kan ændre dit svar når som helst ved at åbne invitationen igen:</p>
      <p style="margin: 0 0 24px;">
        {EmailTemplateLayout.Button(inviteUrl, "Åbn invitation")}
      </p>
      {EmailTemplateLayout.Divider()}
      {EmailTemplateLayout.Note($"Dette er en automatisk bekræftelse. Du modtager den fordi du svarede på en invitation til {title}.")}
""");

        var textLocationLine = location is null ? "" : $"Sted: {location}\n";

        var text = $"""
Hej {rsvp.GuestName},

Vi har modtaget dit svar til "{ev.Title}": {statusLabel}.

Hvornår: {startsLocal}
{textLocationLine}
Du kan ændre dit svar når som helst:
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
