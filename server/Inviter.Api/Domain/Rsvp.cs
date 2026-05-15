namespace Inviter.Api.Domain;

public class Rsvp
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string GuestName { get; set; } = "";
    public RsvpStatus Status { get; set; }
    public string? Comment { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime SubmittedAt { get; set; }

    public Event? Event { get; set; }
}
