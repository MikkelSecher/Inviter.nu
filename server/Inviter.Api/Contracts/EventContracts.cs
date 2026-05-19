using Inviter.Api.Domain;

namespace Inviter.Api.Contracts;

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

public record InviteeDto(
    Guid Id,
    string Email,
    string? Name,
    DateTime AddedAt,
    DateTime? LastSentAt,
    int SendCount,
    RsvpStatus? RsvpStatus);

public record AddInviteesRequest(IReadOnlyList<AddInviteeEntry> Entries);

public record AddInviteeEntry(string Email, string? Name);

public record AddInviteesResponse(
    IReadOnlyList<InviteeDto> Added,
    IReadOnlyList<string> SkippedDuplicates,
    IReadOnlyList<string> SkippedInvalid);

public record SendInvitationsRequest(
    IReadOnlyList<Guid>? InviteeIds,
    bool OnlyUnsent);

public record SendInvitationsResponse(int Enqueued);
