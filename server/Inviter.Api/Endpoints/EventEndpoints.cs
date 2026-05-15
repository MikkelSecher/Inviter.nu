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
        var errors = ValidateEventOptions(req.Title, req.StartsAt, req.RsvpDeadline, req.ContactRequirement);
        if (errors is not null) return Results.ValidationProblem(errors);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Description = (req.Description ?? "").Trim(),
            StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc),
            InviteToken = TokenGenerator.NewInviteToken(),
            AdminToken = TokenGenerator.NewAdminToken(),
            CreatedAt = DateTime.UtcNow,
            AllowMaybe = req.AllowMaybe,
            RsvpDeadline = req.RsvpDeadline.HasValue
                ? DateTime.SpecifyKind(req.RsvpDeadline.Value, DateTimeKind.Utc)
                : null,
            ContactRequirement = req.ContactRequirement
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return Results.Created($"/api/manage/{ev.AdminToken}", new EventCreatedDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement));
    }

    private static async Task<IResult> GetByInviteToken(string inviteToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        return Results.Ok(new EventPublicDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt, ev.InviteToken,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement));
    }

    private static async Task<IResult> SubmitRsvp(string inviteToken, CreateRsvpRequest req, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.GuestName))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["guestName"] = new[] { "Navn er påkrævet." }
            });
        if (!Enum.IsDefined(typeof(RsvpStatus), req.Status))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "Ugyldig status." }
            });

        var ev = await db.Events.FirstOrDefaultAsync(x => x.InviteToken == inviteToken);
        if (ev is null) return Results.NotFound();

        if (ev.RsvpDeadline.HasValue && DateTime.UtcNow > ev.RsvpDeadline.Value)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["rsvp"] = new[] { "Tilmeldingen er lukket." }
            });

        if (req.Status == RsvpStatus.Maybe && !ev.AllowMaybe)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "'Måske' er ikke tilladt for dette event." }
            });

        string? email = null;
        string? phone = null;
        switch (ev.ContactRequirement)
        {
            case ContactRequirement.Email:
                var trimmedEmail = req.Email?.Trim();
                if (string.IsNullOrEmpty(trimmedEmail) || !LooksLikeEmail(trimmedEmail))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["email"] = new[] { "Email er påkrævet og skal være en gyldig email-adresse." }
                    });
                email = trimmedEmail;
                break;
            case ContactRequirement.Phone:
                var trimmedPhone = req.Phone?.Trim();
                if (string.IsNullOrEmpty(trimmedPhone) || trimmedPhone.Length < 5)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["phone"] = new[] { "Telefonnummer er påkrævet (mindst 5 tegn)." }
                    });
                phone = trimmedPhone;
                break;
        }

        var rsvp = new Rsvp
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            GuestName = req.GuestName.Trim(),
            Status = req.Status,
            Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            Email = email,
            Phone = phone,
            SubmittedAt = DateTime.UtcNow
        };
        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();

        return Results.Ok(new RsvpDto(
            rsvp.Id, rsvp.GuestName, rsvp.Status, rsvp.Comment,
            rsvp.Email, rsvp.Phone, rsvp.SubmittedAt));
    }

    private static async Task<IResult> GetByAdminToken(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .Include(x => x.Rsvps)
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var rsvps = ev.Rsvps
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new RsvpDto(r.Id, r.GuestName, r.Status, r.Comment, r.Email, r.Phone, r.SubmittedAt))
            .ToList();

        return Results.Ok(new EventAdminDto(
            ev.Id, ev.Title, ev.Description, ev.StartsAt,
            ev.InviteToken, ev.AdminToken, ev.CreatedAt,
            ev.AllowMaybe, ev.RsvpDeadline, ev.ContactRequirement,
            rsvps));
    }

    private static async Task<IResult> UpdateByAdminToken(string adminToken, UpdateEventRequest req, AppDbContext db)
    {
        var errors = ValidateEventOptions(req.Title, req.StartsAt, req.RsvpDeadline, req.ContactRequirement);
        if (errors is not null) return Results.ValidationProblem(errors);

        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        ev.Title = req.Title.Trim();
        ev.Description = (req.Description ?? "").Trim();
        ev.StartsAt = DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc);
        ev.AllowMaybe = req.AllowMaybe;
        ev.RsvpDeadline = req.RsvpDeadline.HasValue
            ? DateTime.SpecifyKind(req.RsvpDeadline.Value, DateTimeKind.Utc)
            : null;
        ev.ContactRequirement = req.ContactRequirement;
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

    private static Dictionary<string, string[]>? ValidateEventOptions(
        string title, DateTime startsAt, DateTime? rsvpDeadline, ContactRequirement contactRequirement)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = new[] { "Titel er påkrævet." };

        if (rsvpDeadline.HasValue && rsvpDeadline.Value > startsAt)
            errors["rsvpDeadline"] = new[] { "SU-deadline kan ikke ligge efter eventet." };

        if (!Enum.IsDefined(typeof(ContactRequirement), contactRequirement))
            errors["contactRequirement"] = new[] { "Ugyldigt kontaktkrav." };

        return errors.Count == 0 ? null : errors;
    }

    private static bool LooksLikeEmail(string s)
    {
        if (s.Contains(' ')) return false;
        var at = s.IndexOf('@');
        return at > 0 && at < s.Length - 3 && s.IndexOf('.', at) > at + 1;
    }
}
