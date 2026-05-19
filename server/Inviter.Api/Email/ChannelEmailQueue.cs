using System.Threading.Channels;

namespace Inviter.Api.Email;

public class ChannelEmailQueue : IEmailQueue
{
    private readonly Channel<QueuedEmail> _channel = Channel.CreateUnbounded<QueuedEmail>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public void Enqueue(QueuedEmail message)
    {
        _channel.Writer.TryWrite(message);
    }

    public IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
