using System.Net;
using System.Net.Http.Json;
using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Features.Admin;
using Inviter.Api.Infrastructure.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inviter.Api.Tests;

public class AdminMetricsTests
{
    [Fact]
    public async Task Wrong_slug_returns_404()
    {
        using var factory = new MetricsFactory(slug: "secret");
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/admin/wrong/metrics");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Empty_configured_slug_returns_404_even_for_empty_request()
    {
        using var factory = new MetricsFactory(slug: "");
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/admin//metrics");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Correct_slug_with_empty_db_returns_zeros()
    {
        using var factory = new MetricsFactory(slug: "secret");
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/admin/secret/metrics");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<MetricsSnapshot>(TestJson.Options);
        Assert.NotNull(dto);
        Assert.Equal(0, dto!.Events);
        Assert.Equal(0, dto.Rsvps);
        Assert.Equal(0, dto.Invitees);
        Assert.Equal(0, dto.Emails);
        Assert.Equal("all", dto.Period);
        Assert.False(dto.UpcomingOnly);
    }

    [Fact]
    public async Task UpcomingOnly_filters_events_rsvps_and_invitees()
    {
        using var factory = new MetricsFactory(slug: "secret");
        await SeedAsync(factory, db =>
        {
            var now = DateTime.UtcNow;
            var future1 = NewEvent(startsAt: now.AddDays(7));
            var future2 = NewEvent(startsAt: now.AddDays(14));
            var past = NewEvent(startsAt: now.AddDays(-3));
            db.Events.AddRange(future1, future2, past);

            db.Rsvps.AddRange(
                new Rsvp { Id = Guid.NewGuid(), EventId = future1.Id, GuestName = "A", Status = RsvpStatus.Yes, SubmittedAt = now },
                new Rsvp { Id = Guid.NewGuid(), EventId = future2.Id, GuestName = "B", Status = RsvpStatus.Yes, SubmittedAt = now },
                new Rsvp { Id = Guid.NewGuid(), EventId = past.Id, GuestName = "C", Status = RsvpStatus.Yes, SubmittedAt = now });

            db.Invitees.AddRange(
                new Invitee { Id = Guid.NewGuid(), EventId = future1.Id, Email = "a@x", AddedAt = now },
                new Invitee { Id = Guid.NewGuid(), EventId = past.Id, Email = "b@x", AddedAt = now });

            db.EmailLogs.AddRange(
                new EmailLog { Id = Guid.NewGuid(), Kind = "Invitation", SentAt = now },
                new EmailLog { Id = Guid.NewGuid(), Kind = "Invitation", SentAt = now });
        });

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/admin/secret/metrics?upcomingOnly=true");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<MetricsSnapshot>(TestJson.Options);
        Assert.Equal(2, dto!.Events);
        Assert.Equal(2, dto.Rsvps);
        Assert.Equal(1, dto.Invitees);
        Assert.Equal(2, dto.Emails);
        Assert.True(dto.UpcomingOnly);
    }

    [Fact]
    public async Task Period_7d_excludes_event_created_10_days_ago()
    {
        using var factory = new MetricsFactory(slug: "secret");
        await SeedAsync(factory, db =>
        {
            var now = DateTime.UtcNow;
            db.Events.Add(NewEvent(createdAt: now.AddDays(-10)));
            db.Events.Add(NewEvent(createdAt: now.AddDays(-2)));
        });

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/admin/secret/metrics?period=7d");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<MetricsSnapshot>(TestJson.Options);
        Assert.Equal(1, dto!.Events);
        Assert.Equal("7d", dto.Period);
    }

    private static Event NewEvent(DateTime? startsAt = null, DateTime? createdAt = null)
    {
        var now = DateTime.UtcNow;
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = "T",
            StartsAt = startsAt ?? now.AddDays(7),
            CreatedAt = createdAt ?? now,
            InviteToken = Guid.NewGuid().ToString("N")[..12],
            AdminToken = Guid.NewGuid().ToString("N"),
            AllowMaybe = true,
        };
    }

    private static async Task SeedAsync(MetricsFactory factory, Action<AppDbContext> seed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    private sealed class MetricsFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly string _slug;

        public MetricsFactory(string slug)
        {
            _slug = slug;
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:BaseUrl"] = "https://test.invitér.nu",
                    ["App:DashboardSlug"] = _slug,
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(_connection));

                services.RemoveAll<IEmailQueue>();
                services.AddSingleton<IEmailQueue>(new FakeEmailQueue());
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _connection.Dispose();
            base.Dispose(disposing);
        }
    }
}
