using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Images;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inviter.Api.Tests;

public class InviterApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string _imageRoot = Path.Combine(
        Path.GetTempPath(), "inviter-tests", Guid.NewGuid().ToString("N"));
    public FakeEmailQueue Emails { get; } = new();
    public string ImageRoot => _imageRoot;

    public InviterApiFactory()
    {
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
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(_connection));

            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<IEmailQueue>(Emails);

            services.RemoveAll<EventImageStorage>();
            services.AddSingleton(new EventImageStorage(_imageRoot));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
            try { if (Directory.Exists(_imageRoot)) Directory.Delete(_imageRoot, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
        base.Dispose(disposing);
    }
}
