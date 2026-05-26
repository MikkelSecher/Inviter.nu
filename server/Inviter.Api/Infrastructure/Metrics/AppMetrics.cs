using System.Diagnostics.Metrics;
using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Infrastructure.Metrics;

public sealed class AppMetrics : IDisposable
{
    public const string MeterName = "Inviter.Api";

    private readonly Meter _meter = new(MeterName);
    private readonly IServiceScopeFactory _scopeFactory;

    public Counter<long> EmailsSent { get; }

    public AppMetrics(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        EmailsSent = _meter.CreateCounter<long>(
            name: "emails_sent",
            unit: "{email}",
            description: "Total emails successfully dispatched, tagged by kind.");

        _meter.CreateObservableGauge(
            name: "events_total",
            observeValue: () => Count(db => db.Events.CountAsync()),
            description: "Total number of events.");

        _meter.CreateObservableGauge(
            name: "rsvps_total",
            observeValue: () => Count(db => db.Rsvps.CountAsync()),
            description: "Total number of RSVPs.");

        _meter.CreateObservableGauge(
            name: "invitees_total",
            observeValue: () => Count(db => db.Invitees.CountAsync()),
            description: "Total number of invitees.");

        _meter.CreateObservableGauge(
            name: "emails_logged_total",
            observeValue: () => Count(db => db.EmailLogs.CountAsync()),
            description: "Total persisted email log rows.");

        _meter.CreateObservableGauge(
            name: "upcoming_events_total",
            observeValue: () =>
            {
                var now = DateTime.UtcNow;
                return Count(db => db.Events.CountAsync(e => e.StartsAt > now));
            },
            description: "Number of events with StartsAt in the future.");
    }

    private long Count(Func<AppDbContext, Task<int>> query)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return query(db).GetAwaiter().GetResult();
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose() => _meter.Dispose();
}
