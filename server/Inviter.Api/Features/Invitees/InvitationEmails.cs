using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;
using Inviter.Api.Infrastructure.Images;

namespace Inviter.Api.Features.Invitees;

internal static class InvitationEmails
{
    public static async Task EnqueueAsync(
        Event ev,
        IReadOnlyList<Invitee> invitees,
        IEmailQueue emailQueue,
        string baseUrl,
        EventImageStorage imageStorage,
        ImageProcessor imageProcessor,
        DateTime sentAt,
        CancellationToken ct)
    {
        var inviteesWithEmail = invitees.Where(i => i.Email != null).ToList();
        if (inviteesWithEmail.Count == 0) return;

        var image = await BuildImageAttachment(ev.Id, ev.ImageFileName, imageStorage, imageProcessor, ct);

        foreach (var invitee in inviteesWithEmail)
        {
            var isResend = invitee.SendCount > 0;
            emailQueue.Enqueue(InvitationTemplate.Build(ev, invitee, baseUrl, isResend, image));
            invitee.LastSentAt = sentAt;
            invitee.SendCount += 1;
        }
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
