namespace Inviter.Api.Email;

public class EmailDispatcher : BackgroundService
{
    private static readonly TimeSpan[] RetryBackoff =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
    };

    private readonly IEmailQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailDispatcher> _log;

    public EmailDispatcher(IEmailQueue queue, IServiceProvider services, ILogger<EmailDispatcher> log)
    {
        _queue = queue;
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            await TrySendWithRetry(message, stoppingToken);
        }
    }

    private async Task TrySendWithRetry(QueuedEmail message, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        for (var attempt = 0; attempt < RetryBackoff.Length; attempt++)
        {
            if (ct.IsCancellationRequested) return;
            if (RetryBackoff[attempt] > TimeSpan.Zero)
            {
                try { await Task.Delay(RetryBackoff[attempt], ct); }
                catch (OperationCanceledException) { return; }
            }

            try
            {
                await sender.SendAsync(message, ct);
                if (attempt > 0)
                {
                    _log.LogInformation("Email {Kind} to {To} sent on attempt {Attempt}", message.Kind, message.ToAddress, attempt + 1);
                }
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                var isFinal = attempt == RetryBackoff.Length - 1;
                _log.Log(
                    isFinal ? LogLevel.Error : LogLevel.Warning,
                    ex,
                    "Email {Kind} to {To} failed on attempt {Attempt}{Final}",
                    message.Kind,
                    message.ToAddress,
                    attempt + 1,
                    isFinal ? " — giving up" : "");
            }
        }
    }
}
