using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email.Templates;

namespace Inviter.Api.Tests;

public class EmailTemplateEncodingTests
{
    [Fact]
    public void Templates_RenderDanishCharactersWithoutMojibake()
    {
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Åbent hus på Sølystgård",
            Description = "Vi glæder os til æbleskiver, øl og blåbær.",
            Location = "København Ø",
            StartsAt = new DateTime(2026, 12, 24, 18, 0, 0, DateTimeKind.Utc),
            RsvpDeadline = new DateTime(2026, 12, 20, 12, 0, 0, DateTimeKind.Utc),
            InviteToken = "invite-token",
            AdminToken = "admin-token",
            OrganizerEmail = "host@example.com",
            OrganizerName = "Jørgen Åse",
        };
        var invitee = new Invitee
        {
            Email = "guest@example.com",
            Name = "Søren Ægir",
            PersonalInviteToken = "guest-token",
        };
        var rsvp = new Rsvp
        {
            GuestName = "Søren Ægir",
            Status = RsvpStatus.Maybe,
            Comment = "Jeg tager kage med æbler og blåbær.",
            Email = "guest@example.com",
            SubmittedAt = new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        var emails = new[]
        {
            AdminLinkTemplate.Build(ev, "https://example.test"),
            InvitationTemplate.Build(ev, invitee, "https://example.test", isResend: true),
            RsvpConfirmationTemplate.Build(ev, rsvp, "https://example.test"),
            RsvpNotificationTemplate.Build(ev, rsvp, "https://example.test"),
        };
        var rendered = string.Join("\n", emails.SelectMany(e => new[] { e.Subject, e.HtmlBody, e.TextBody }));

        Assert.Contains("Påmindelse", rendered);
        Assert.Contains("Hvornår", rendered);
        Assert.Contains("gæsterne", rendered);
        Assert.Contains("Måske", rendered);
        Assert.Contains("ændre", rendered);
        Assert.Contains("åbne", rendered);
        Assert.DoesNotContain("Ã", rendered);
        Assert.DoesNotContain("â", rendered);
        Assert.DoesNotContain("�", rendered);
    }
}
