using Inviter.Api.Domain;

namespace Inviter.Api.Contracts;

public record CreateEventRequest(string Title, string? Description, DateTime StartsAt);

public record UpdateEventRequest(string Title, string? Description, DateTime StartsAt);

public record EventPublicDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken);

public record EventCreatedDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt);

public record EventAdminDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt,
    IReadOnlyList<RsvpDto> Rsvps);

public record RsvpDto(
    Guid Id,
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    DateTime SubmittedAt);

public record CreateRsvpRequest(string GuestName, RsvpStatus Status, string? Comment);
