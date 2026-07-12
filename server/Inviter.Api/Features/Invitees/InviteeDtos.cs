using Inviter.Api.Domain;

namespace Inviter.Api.Features.Invitees;

public record InviteeDto(
    Guid Id,
    string PersonalInviteToken,
    string? Email,
    string? Name,
    DateTime AddedAt,
    DateTime? LastSentAt,
    int SendCount,
    RsvpStatus? RsvpStatus);

public record InviteePrefillDto(
    string? Name,
    string? Email,
    string? RsvpGuestName,
    RsvpStatus? RsvpStatus,
    string? RsvpComment,
    string? RsvpEmail,
    string? RsvpPhone);

public record AddInviteesRequest
{
    public AddInviteesRequest()
    {
    }

    public AddInviteesRequest(IReadOnlyList<AddInviteeEntry> entries, bool sendInvitations = true)
    {
        Entries = entries;
        SendInvitations = sendInvitations;
    }

    public IReadOnlyList<AddInviteeEntry> Entries { get; init; } = [];
    public bool SendInvitations { get; init; } = true;
}

public record AddInviteeEntry(string? Email, string? Name);

public record AddInviteesResponse(
    IReadOnlyList<InviteeDto> Added,
    IReadOnlyList<string> SkippedDuplicates,
    IReadOnlyList<string> SkippedInvalid);

public record SendInvitationsRequest(
    IReadOnlyList<Guid>? InviteeIds,
    bool OnlyUnsent);

public record SendInvitationsResponse(int Enqueued);

public enum GuestMessageAudience
{
    All,
    Yes,
    No
}

public record SendGuestMessageRequest(
    GuestMessageAudience Audience,
    string Subject,
    string Message);

public record SendGuestMessageResponse(int Enqueued);

public record UpdateInviteeRequest(string? Email, string? Name);
