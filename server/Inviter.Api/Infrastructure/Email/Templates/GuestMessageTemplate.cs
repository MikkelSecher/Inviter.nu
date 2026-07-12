using System.Net;
using Inviter.Api.Domain;

namespace Inviter.Api.Infrastructure.Email.Templates;

public static class GuestMessageTemplate
{
    public static QueuedEmail Build(
        Event ev,
        string toAddress,
        string? toName,
        string subject,
        string message)
    {
        var greeting = string.IsNullOrWhiteSpace(toName)
            ? "Hej"
            : $"Hej {WebUtility.HtmlEncode(toName)}";

        var html = EmailTemplateLayout.Shell(
            subject,
            $"""
      {EmailTemplateLayout.Eyebrow(ev.Title)}
      {EmailTemplateLayout.Heading(subject, 24)}
      <p style="{EmailTemplateLayout.ParagraphStyle}">{greeting},</p>
      <p style="margin: 0 0 16px; line-height: 1.55; white-space: pre-wrap;">{WebUtility.HtmlEncode(message)}</p>
""");

        var text = $"""
{(string.IsNullOrWhiteSpace(toName) ? "Hej" : $"Hej {toName}")},

{message}
""";

        return new QueuedEmail(
            ToAddress: toAddress,
            ToName: toName,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            Kind: "GuestMessage");
    }
}
