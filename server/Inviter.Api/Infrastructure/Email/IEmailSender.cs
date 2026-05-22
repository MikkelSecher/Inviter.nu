namespace Inviter.Api.Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(QueuedEmail message, CancellationToken ct);
}
