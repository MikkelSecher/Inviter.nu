using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
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

        var query = db.Invitees.Where(i => i.EventId == ev.Id);
        if (req.InviteeIds is { Count: > 0 })
        {
            var ids = req.InviteeIds.ToHashSet();
            query = query.Where(i => ids.Contains(i.Id));
        }
        if (req.OnlyUnsent)
        {
            query = query.Where(i => i.LastSentAt == null);
        }

        var invitees = await query.ToListAsync();
        if (invitees.Count == 0)
            return Results.Ok(new SendInvitationsResponse(0));

        var now = DateTime.UtcNow;
        var baseUrl = appOptions.Value.BaseUrl;
        var image = await BuildImageAttachment(ev.Id, ev.ImageFileName, imageStorage, imageProcessor, ct);

        foreach (var invitee in invitees)
        {
            var isResend = invitee.SendCount > 0;
            emailQueue.Enqueue(InvitationTemplate.Build(ev, invitee, baseUrl, isResend, image));
            invitee.LastSentAt = now;
            invitee.SendCount += 1;
        }
        await db.SaveChangesAsync(ct);

        return Results.Ok(new SendInvitationsResponse(invitees.Count));
    }

    private static async Task<InlineAttachment?> BuildImageAttachment(
        Guid eventId,
        string? imageFileName,
        EventImageStorage storage,
        ImageProcessor processor,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(imageFileName)) return null;

        var webp = await storage.ReadAsync(imageFileName, ct);
        if (webp is null) return null;

        var jpeg = await processor.WebpToJpegAsync(webp, ct);
        return new InlineAttachment($"event-image-{eventId}", "image/jpeg", jpeg);
    }
}
