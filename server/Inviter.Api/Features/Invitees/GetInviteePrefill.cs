using Inviter.Api.Contracts;
using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class GetInviteePrefill
{
    public static async Task<IResult> Handle(string inviteToken, Guid inviteeId, AppDbContext db)
    {
        var prefill = await db.Invitees.AsNoTracking()
            .Where(i => i.Id == inviteeId && i.Event.InviteToken == inviteToken)
            .Select(i => new InviteePrefillDto(i.Name, i.Email))
            .FirstOrDefaultAsync();
        return prefill is null ? Results.NotFound() : Results.Ok(prefill);
    }
}
