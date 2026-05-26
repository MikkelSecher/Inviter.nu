namespace Inviter.Api.Features.Admin;

public record MetricsSnapshot(
    int Events,
    int Rsvps,
    int Invitees,
    int Emails,
    string Period,
    bool UpcomingOnly);
