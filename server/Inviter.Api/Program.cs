using System.Text.Json.Serialization;
using Inviter.Api.Data;
using Inviter.Api.Features.Admin;
using Inviter.Api.Features.Events;
using Inviter.Api.Features.Invitees;
using Inviter.Api.Features.Rsvps;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Images;
using Inviter.Api.Infrastructure.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicy = "DevClient";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
                  ?? "Data Source=inviter.db"));

builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opt.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(o => o.AddPolicy(DevCorsPolicy, p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddOpenApi();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddSingleton<IEmailQueue, ChannelEmailQueue>();
builder.Services.AddHostedService<EmailDispatcher>();

builder.Services.AddSingleton(sp =>
{
    var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var dataPath = string.IsNullOrWhiteSpace(appOptions.DataPath)
        ? Path.Combine(env.ContentRootPath, "data")
        : appOptions.DataPath;
    return new EventImageStorage(Path.Combine(dataPath, "event-images"));
});
builder.Services.AddSingleton<ImageProcessor>();

builder.Services.AddSingleton<AppMetrics>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddMeter(AppMetrics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

var imageStorage = app.Services.GetRequiredService<EventImageStorage>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageStorage.RootPath),
    RequestPath = "/api/event-images",
});

app.MapEventEndpoints();
app.MapRsvpEndpoints();
app.MapInviteeEndpoints();
app.MapAdminEndpoints();
app.MapPrometheusScrapingEndpoint();
app.Services.GetRequiredService<AppMetrics>();

app.Run();

public partial class Program;
