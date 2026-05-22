namespace Inviter.Api.Infrastructure.Email;

public interface IEmailQueue
{
    void Enqueue(QueuedEmail message);
    IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken ct);
}
