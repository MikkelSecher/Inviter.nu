using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Invitees;

public static class GetInviteePrefill
{
    public static async Task<IResult> Handle(string inviteToken, string inviteeToken, AppDbContext db)
    {
        var prefill = await db.Invitees.AsNoTracking()
            .Where(i => i.PersonalInviteToken == inviteeToken && i.Event.InviteToken == inviteToken)
            .Select(i => new InviteePrefillDto(i.Name, i.Email))
            .FirstOrDefaultAsync();
        return prefill is null ? Results.NotFound() : Results.Ok(prefill);
    }
}
