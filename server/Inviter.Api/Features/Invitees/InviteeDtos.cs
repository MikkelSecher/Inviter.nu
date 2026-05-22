using Inviter.Api.Domain;

namespace Inviter.Api.Features.Invitees;

public record InviteeDto(
    Guid Id,
    string Email,
    string? Name,
    DateTime AddedAt,
    DateTime? LastSentAt,
    int SendCount,
    RsvpStatus? RsvpStatus);

public record InviteePrefillDto(string? Name, string Email);

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
