namespace Inviter.Api.Domain;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public string InviteToken { get; set; } = "";
    public string AdminToken { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public bool AllowMaybe { get; set; }
    public DateTime? RsvpDeadline { get; set; }
    public ContactRequirement ContactRequirement { get; set; }

    public List<Rsvp> Rsvps { get; set; } = new();
}
