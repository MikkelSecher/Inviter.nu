using Inviter.Api.Domain;

namespace Inviter.Api.Features.Rsvps;

public record CreateRsvpRequest(
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone,
    string? InviteeToken = null);

public record RsvpDto(
    Guid Id,
    Guid? InviteeId,
    string? InviteeName,
    string? InviteeEmail,
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone,
    DateTime SubmittedAt);

public record LinkRsvpInviteeRequest(Guid? InviteeId);
