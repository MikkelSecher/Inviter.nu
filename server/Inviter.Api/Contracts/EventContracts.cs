using Inviter.Api.Domain;

namespace Inviter.Api.Contracts;

public record CreateEventRequest(
    string Title,
    string? Description,
    DateTime StartsAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement);

public record UpdateEventRequest(
    string Title,
    string? Description,
    DateTime StartsAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement);

public record EventPublicDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement);

public record EventCreatedDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement);

public record EventAdminDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement,
    IReadOnlyList<RsvpDto> Rsvps);

public record RsvpDto(
    Guid Id,
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone,
    DateTime SubmittedAt);

public record CreateRsvpRequest(
    string GuestName,
    RsvpStatus Status,
    string? Comment,
    string? Email,
    string? Phone);
