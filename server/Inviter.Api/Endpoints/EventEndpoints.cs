using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Inviter.Api.Domain;
using Inviter.Api.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api");

        api.MapPost("/events", CreateEvent);
        api.MapGet("/invite/{inviteToken}", GetByInviteToken);
        api.MapPost("/invite/{inviteToken}/rsvp", SubmitRsvp);
        api.MapGet("/manage/{adminToken}", GetByAdminToken);
        api.MapPut("/manage/{adminToken}", UpdateByAdminToken);
        api.MapDelete("/manage/{adminToken}/rsvp/{rsvpId:guid}", DeleteRsvp);
    }

    private static async Task<IResult> CreateEvent(CreateEventRequest req, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["title"] = new[] { "Title is required." }
            });

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Description = (req.Description ?? "").Trim(),
            StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc),
            InviteToken = TokenGenerator.NewInviteToken(),
            AdminToken = TokenGenerator.NewAdminToken(),
            CreatedAt = DateTime.UtcNow
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var dto = new EventCreatedDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt);

        return Results.Created($"/api/manage/{ev.AdminToken}", dto);
    }

    private static async Task<IResult> GetByInviteToken(string inviteToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        return Results.Ok(new EventPublicDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt, ev.InviteToken));
    }

    private static async Task<IResult> SubmitRsvp(string inviteToken, CreateRsvpRequest req, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.GuestName))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["guestName"] = new[] { "Name is required." }
            });
        if (!Enum.IsDefined(typeof(RsvpStatus), req.Status))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Invalid status." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        var rsvp = new Rsvp
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            GuestName = req.GuestName.Trim(),
            Status = req.Status,
            Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            SubmittedAt = DateTime.UtcNow
        };
        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();

        return Results.Ok(new RsvpDto(
            rsvp.Id, rsvp.GuestName, rsvp.Status, rsvp.Comment, rsvp.SubmittedAt));
    }

    private static async Task<IResult> GetByAdminToken(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .Include(x => x.Rsvps)
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvps = ev.Rsvps
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new RsvpDto(r.Id, r.GuestName, r.Status, r.Comment, r.SubmittedAt))
            .ToList();

        return Results.Ok(new EventAdminDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt, rsvps));
    }

    private static async Task<IResult> UpdateByAdminToken(string adminToken, UpdateEventRequest req, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["title"] = new[] { "Title is required." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        ev.Title = req.Title.Trim();
        ev.Description = (req.Description ?? "").Trim();
        ev.StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteRsvp(string adminToken, Guid rsvpId, AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.Id == rsvpId && r.EventId == ev.Id);
        if (rsvp is null) return Results.NotFound();

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
