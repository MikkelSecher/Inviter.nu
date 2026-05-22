using Inviter.Api.Domain;
using Inviter.Api.Features.Rsvps;

namespace Inviter.Api.Features.Events;

public record CreateEventRequest(
    string Title,
    string? Description,
    string? Location,
    DateTime StartsAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement,
    string? OrganizerEmail,
    string? OrganizerName);

public record UpdateEventRequest(
    string Title,
    string? Description,
    string? Location,
    DateTime StartsAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement,
    string? OrganizerEmail,
    string? OrganizerName);

public record EventPublicDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTime StartsAt,
    string InviteToken,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement);

public record EventCreatedDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement,
    string? OrganizerEmail,
    string? OrganizerName);

public record EventAdminDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateTime StartsAt,
    string InviteToken,
    string AdminToken,
    DateTime CreatedAt,
    bool AllowMaybe,
    DateTime? RsvpDeadline,
    ContactRequirement ContactRequirement,
    string? OrganizerEmail,
    string? OrganizerName,
    IReadOnlyList<RsvpDto> Rsvps);
