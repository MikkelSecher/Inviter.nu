namespace Inviter.Api.Email;

public interface IEmailSender
{
    Task SendAsync(QueuedEmail message, CancellationToken ct);
}
