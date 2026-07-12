using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Inviter.Api.Infrastructure.Email;

public class MailKitEmailSender : IEmailSender
{
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly ILogger<MailKitEmailSender> _log;

    public MailKitEmailSender(IOptionsMonitor<EmailOptions> options, ILogger<MailKitEmailSender> log)
    {
        _options = options;
        _log = log;
    }

    public async Task SendAsync(QueuedEmail message, CancellationToken ct)
    {
        var opt = _options.CurrentValue;
        if (!opt.IsConfigured)
        {
            _log.LogWarning("SMTP not configured (Email:SmtpHost is empty) - dropping {Kind} to {To}", message.Kind, message.ToAddress);
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(opt.FromName, opt.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? message.ToAddress, message.ToAddress));
        mime.Subject = message.Subject;

        var body = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
        };

        if (message.InlineAttachments is { Count: > 0 })
        {
            foreach (var attachment in message.InlineAttachments)
            {
                var resource = body.LinkedResources.Add(
                    attachment.ContentId,
                    attachment.Content,
                    ContentType.Parse(attachment.MediaType));
                resource.ContentId = attachment.ContentId;
            }
        }

        mime.Body = body.ToMessageBody();
        SetUtf8Charset(mime.Body);

        using var client = new SmtpClient();
        var secureSocketOptions = opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(opt.SmtpHost, opt.SmtpPort, secureSocketOptions, ct);
        if (!string.IsNullOrEmpty(opt.SmtpUser))
        {
            await client.AuthenticateAsync(opt.SmtpUser, opt.SmtpPassword, ct);
        }
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }

    private static void SetUtf8Charset(MimeEntity entity)
    {
        if (entity is TextPart textPart)
        {
            textPart.ContentType.Charset = "utf-8";
            return;
        }

        if (entity is Multipart multipart)
        {
            foreach (var part in multipart)
            {
                SetUtf8Charset(part);
            }
        }
    }
}
