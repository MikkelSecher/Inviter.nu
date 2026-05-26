using System.Threading.Channels;
using Inviter.Api.Infrastructure.Email;

namespace Inviter.Api.Tests;

public class FakeEmailQueue : IEmailQueue
{
    private readonly Channel<QueuedEmail> _idleChannel = Channel.CreateUnbounded<QueuedEmail>();
    private readonly List<QueuedEmail> _enqueued = new();
    private readonly object _gate = new();

    public IReadOnlyList<QueuedEmail> Enqueued
    {
        get { lock (_gate) return _enqueued.ToList(); }
    }

    public void Enqueue(QueuedEmail message)
    {
        lock (_gate) _enqueued.Add(message);
    }

    public IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken ct)
        => _idleChannel.Reader.ReadAllAsync(ct);
}
