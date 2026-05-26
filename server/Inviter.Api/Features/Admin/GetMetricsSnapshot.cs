using System.Security.Cryptography;
using System.Text;
using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Features.Admin;

public static class GetMetricsSnapshot
{
    public static async Task<IResult> Handle(
        string slug,
        string? period,
        bool? upcomingOnly,
        AppDbContext db,
        IOptions<AppOptions> appOptions)
    {
        var configured = appOptions.Value.DashboardSlug;
        if (string.IsNullOrEmpty(configured)) return Results.NotFound();
        if (!FixedTimeEquals(configured, slug)) return Results.NotFound();

        var normalizedPeriod = (period ?? "all").ToLowerInvariant();
        var since = normalizedPeriod switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            "all" => (DateTime?)null,
            _ => (DateTime?)null,
        };
        var canonicalPeriod = normalizedPeriod switch
        {
            "7d" or "30d" or "90d" => normalizedPeriod,
            _ => "all",
        };

        var upcoming = upcomingOnly ?? false;
        var now = DateTime.UtcNow;

        var eventsQuery = db.Events.AsNoTracking().AsQueryable();
        if (since is { } s) eventsQuery = eventsQuery.Where(e => e.CreatedAt >= s);
        if (upcoming) eventsQuery = eventsQuery.Where(e => e.StartsAt > now);

        var rsvpsQuery = db.Rsvps.AsNoTracking().AsQueryable();
        if (since is { } rs) rsvpsQuery = rsvpsQuery.Where(r => r.SubmittedAt >= rs);
        if (upcoming) rsvpsQuery = rsvpsQuery.Where(r => r.Event!.StartsAt > now);

        var inviteesQuery = db.Invitees.AsNoTracking().AsQueryable();
        if (since is { } i) inviteesQuery = inviteesQuery.Where(x => x.AddedAt >= i);
        if (upcoming) inviteesQuery = inviteesQuery.Where(x => x.Event.StartsAt > now);

        var emailsQuery = db.EmailLogs.AsNoTracking().AsQueryable();
        if (since is { } e) emailsQuery = emailsQuery.Where(x => x.SentAt >= e);

        var snapshot = new MetricsSnapshot(
            Events: await eventsQuery.CountAsync(),
            Rsvps: await rsvpsQuery.CountAsync(),
            Invitees: await inviteesQuery.CountAsync(),
            Emails: await emailsQuery.CountAsync(),
            Period: canonicalPeriod,
            UpcomingOnly: upcoming);

        return Results.Ok(snapshot);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ab.Length != bb.Length) return false;
        return CryptographicOperations.FixedTimeEquals(ab, bb);
    }
}
