using System.Threading.Channels;
using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Email;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inviter.Api.Tests;

public class EmailLogTests
{
    [Fact]
    public async Task Successful_send_writes_email_log_row()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connection));
        services.AddScoped<IEmailSender, AlwaysSuccessfulSender>();
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        var queue = new SingleMessageQueue(new QueuedEmail(
            ToAddress: "guest@example.test",
            ToName: null,
            Subject: "Hi",
            HtmlBody: "<p>Hi</p>",
            TextBody: "Hi",
            Kind: "Invitation"));

        var dispatcher = new EmailDispatcher(queue, provider, NullLogger<EmailDispatcher>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await dispatcher.StartAsync(cts.Token);
        await queue.Completed.WaitAsync(cts.Token);
        await dispatcher.StopAsync(CancellationToken.None);

        using var verifyScope = provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = await db.EmailLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Invitation", logs[0].Kind);
        Assert.NotEqual(default, logs[0].SentAt);
    }

    [Fact]
    public async Task Failed_send_does_not_write_email_log_row()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connection));
        services.AddScoped<IEmailSender, AlwaysFailingSender>();
        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        var queue = new SingleMessageQueue(new QueuedEmail(
            ToAddress: "guest@example.test",
            ToName: null,
            Subject: "Hi",
            HtmlBody: "<p>Hi</p>",
            TextBody: "Hi",
            Kind: "Invitation"));

        var dispatcher = new EmailDispatcher(queue, provider, NullLogger<EmailDispatcher>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await dispatcher.StartAsync(cts.Token);
        try { await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token); } catch { }
        await dispatcher.StopAsync(CancellationToken.None);

        using var verifyScope = provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.EmailLogs.ToListAsync());
    }

    private sealed class SingleMessageQueue : IEmailQueue
    {
        private readonly Channel<QueuedEmail> _channel = Channel.CreateUnbounded<QueuedEmail>();
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SingleMessageQueue(QueuedEmail message)
        {
            _channel.Writer.TryWrite(message);
            _channel.Writer.Complete();
        }

        public Task Completed => _completed.Task;

        public void Enqueue(QueuedEmail message) => _channel.Writer.TryWrite(message);

        public async IAsyncEnumerable<QueuedEmail> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var m in _channel.Reader.ReadAllAsync(ct))
            {
                yield return m;
            }
            _completed.TrySetResult();
        }
    }

    private sealed class AlwaysSuccessfulSender : IEmailSender
    {
        public Task SendAsync(QueuedEmail message, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class AlwaysFailingSender : IEmailSender
    {
        public Task SendAsync(QueuedEmail message, CancellationToken ct)
            => throw new InvalidOperationException("smtp down");
    }
}
