namespace Inviter.Api.Email;

public interface IEmailQueue
{
    void Enqueue(QueuedEmail message);
    IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken ct);
}
