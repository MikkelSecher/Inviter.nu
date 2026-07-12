namespace Inviter.Api.Domain;

public class Invitee
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = default!;
    public string PersonalInviteToken { get; set; } = "";
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? LastSentAt { get; set; }
    public int SendCount { get; set; }
    public List<Rsvp> Rsvps { get; set; } = new();
}
