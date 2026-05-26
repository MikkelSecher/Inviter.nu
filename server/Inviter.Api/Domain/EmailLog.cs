namespace Inviter.Api.Domain;

public class EmailLog
{
    public Guid Id { get; set; }
    public DateTime SentAt { get; set; }
    public string Kind { get; set; } = "";
}
