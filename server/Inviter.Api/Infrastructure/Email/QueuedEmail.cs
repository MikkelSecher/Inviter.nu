namespace Inviter.Api.Infrastructure.Email;

public record QueuedEmail(
    string ToAddress,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    string Kind,
    IReadOnlyList<InlineAttachment>? InlineAttachments = null);
