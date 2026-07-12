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
        var startsLocal = ev.StartsAt.ToLocalTime().ToString("dddd d. MMMM yyyy 'kl.' HH:mm", DanishCulture);
        var greeting = string.IsNullOrWhiteSpace(ev.OrganizerName)
            ? "Hej"
            : $"Hej {WebUtility.HtmlEncode(ev.OrganizerName)}";

        var subject = $"Dit admin-link til \"{ev.Title}\"";

        var html = EmailTemplateLayout.Shell(
            $"Dit event {ev.Title} er oprettet",
            $"""
      {EmailTemplateLayout.Eyebrow("Event oprettet")}
      {EmailTemplateLayout.Heading("Dit event er oprettet", 26)}
      <p style="{EmailTemplateLayout.ParagraphStyle}">{greeting},</p>
      <p style="{EmailTemplateLayout.ParagraphStyle}">"<strong>{WebUtility.HtmlEncode(ev.Title)}</strong>" er nu klar. Du kan styre tilmeldinger, opdatere detaljer og se hvem der kommer via dit admin-link:</p>
      <p style="margin: 0 0 24px;">
        {EmailTemplateLayout.Button(adminUrl, "Åbn admin-side")}
      </p>
      <p style="{EmailTemplateLayout.MetaStyle}"><strong>Hvornår:</strong> {WebUtility.HtmlEncode(startsLocal)}</p>
      <p style="margin: 0 0 24px; line-height: 1.55;"><strong>Invite-link til gæsterne:</strong><br>{EmailTemplateLayout.TextLink(inviteUrl, inviteUrl)}</p>
      {EmailTemplateLayout.Divider()}
      {EmailTemplateLayout.Note("Gem denne mail - admin-linket er dit eneste adgangspunkt og kan ikke gendannes hvis du mister det.")}
""");

        var text = $"""
{(string.IsNullOrWhiteSpace(ev.OrganizerName) ? "Hej" : $"Hej {ev.OrganizerName}")},

Dit event "{ev.Title}" er oprettet.

Hvornår: {startsLocal}

Admin-link (gem denne mail!):
{adminUrl}

Invite-link til gæsterne:
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
