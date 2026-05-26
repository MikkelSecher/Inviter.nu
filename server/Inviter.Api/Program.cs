using System.Text.Json.Serialization;
using Inviter.Api.Data;
using Inviter.Api.Features.Admin;
using Inviter.Api.Features.Events;
using Inviter.Api.Features.Invitees;
using Inviter.Api.Features.Rsvps;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Metrics;
using Microsoft.EntityFrameworkCore;
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

app.MapEventEndpoints();
app.MapRsvpEndpoints();
app.MapInviteeEndpoints();
app.MapAdminEndpoints();
app.MapPrometheusScrapingEndpoint();
app.Services.GetRequiredService<AppMetrics>();

app.Run();

public partial class Program;
