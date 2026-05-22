using Inviter.Api.Domain;

namespace Inviter.Api.Features.Rsvps;

public record CreateRsvpRequest(
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone);

public record RsvpDto(
    Guid Id,
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone,
    DateTime SubmittedAt);
