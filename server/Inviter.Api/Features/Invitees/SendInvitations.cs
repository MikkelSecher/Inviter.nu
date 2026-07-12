using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inviter.Api.Features.Invitees;

public static class SendInvitations
{
    public static async Task<IResult> Handle(
        string adminToken,
        SendInvitationsRequest req,
        AppDbContext db,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions,
        EventImageStorage imageStorage,
        ImageProcessor imageProcessor,
        CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken, ct);
        if (ev is null) return Results.NotFound();

        var query = db.Invitees.Where(i => i.EventId == ev.Id && i.Email != null);
        if (req.InviteeIds is { Count: > 0 })
        {
            var ids = req.InviteeIds.ToHashSet();
            query = query.Where(i => ids.Contains(i.Id));
        }
        if (req.OnlyUnsent)
        {
            query = query.Where(i => i.LastSentAt == null);
        }

        var invitees = await query.ToListAsync(ct);
        if (invitees.Count == 0)
            return Results.Ok(new SendInvitationsResponse(0));

        var now = DateTime.UtcNow;
        var baseUrl = appOptions.Value.BaseUrl;
        await InvitationEmails.EnqueueAsync(ev, invitees, emailQueue, baseUrl, imageStorage, imageProcessor, now, ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new SendInvitationsResponse(invitees.Count));
    }
}
