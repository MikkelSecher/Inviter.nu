namespace Inviter.Api.Email;

public record QueuedEmail(
    string ToAddress,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    string Kind);
