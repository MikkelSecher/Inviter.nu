namespace Inviter.Api.Infrastructure.Email;

public record InlineAttachment(string ContentId, string MediaType, byte[] Content);
