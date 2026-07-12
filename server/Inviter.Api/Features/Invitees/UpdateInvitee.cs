using Inviter.Api.Data;
using Inviter.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class UpdateInvitee
{
    public static async Task<IResult> Handle(
        string adminToken,
        Guid inviteeId,
        UpdateInviteeRequest req,
        AppDbContext db)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitee = await db.Invitees.FirstOrDefaultAsync(i => i.Id == inviteeId && i.EventId == ev.Id);
        if (invitee is null) return Results.NotFound();

        var email = req.Email?.Trim();
        var name = string.IsNullOrWhiteSpace(req.Name) ? null : req.Name.Trim();
        if (string.IsNullOrEmpty(email)) email = null;

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["invitee"] = new[] { "Udfyld mindst navn eller email." }
            });

        if (email is not null && (email.Length > 320 || !Validation.LooksLikeEmail(email)))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Email skal være en gyldig email-adresse." }
            });

        if (email is not null)
        {
            var key = email.ToLowerInvariant();
            var duplicate = await db.Invitees.AsNoTracking()
                .AnyAsync(i => i.EventId == ev.Id
                    && i.Id != invitee.Id
                    && i.Email != null
                    && i.Email.ToLower() == key);
            if (duplicate)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "Email-adressen er allerede på gæstelisten." }
                });
        }

        invitee.Email = email;
        invitee.Name = name;
        await db.SaveChangesAsync();

        return Results.Ok(new InviteeDto(
            invitee.Id,
            invitee.PersonalInviteToken,
            invitee.Email,
            invitee.Name,
            invitee.AddedAt,
            invitee.LastSentAt,
            invitee.SendCount,
            null));
    }
}
