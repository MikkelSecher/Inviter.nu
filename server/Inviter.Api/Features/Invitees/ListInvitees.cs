using Inviter.Api.Data;
using Inviter.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class ListInvitees
{
    public static async Task<IResult> Handle(string adminToken, AppDbContext db)
    {
        var ev = await db.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdminToken == adminToken);
        if (ev is null) return Results.NotFound();

        var invitees = await db.Invitees.AsNoTracking()
            .Where(i => i.EventId == ev.Id)
            .OrderBy(i => i.AddedAt)
            .ToListAsync();

        var rsvpStatusByEmail = await db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == ev.Id && r.Email != null)
            .GroupBy(r => r.Email!)
            .Select(g => new { Email = g.Key, Status = g.OrderByDescending(r => r.SubmittedAt).Select(r => r.Status).First() })
            .ToDictionaryAsync(x => x.Email!.ToLowerInvariant(), x => (RsvpStatus?)x.Status);

        var dtos = invitees.Select(i => new InviteeDto(
            i.Id, i.Email, i.Name, i.AddedAt, i.LastSentAt, i.SendCount,
            rsvpStatusByEmail.GetValueOrDefault(i.Email.ToLowerInvariant()))).ToList();

        return Results.Ok(dtos);
    }
}
