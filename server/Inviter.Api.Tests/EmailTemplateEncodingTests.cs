using Inviter.Api.Domain;
using Inviter.Api.Infrastructure.Email;
using Inviter.Api.Infrastructure.Email.Templates;

namespace Inviter.Api.Tests;

public class EmailTemplateEncodingTests
{
    [Fact]
    public void Templates_RenderDanishCharactersWithoutMojibake()
    {
        var emails = BuildSampleEmails();
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

    [Fact]
    public void Templates_UseAppDesignPalette()
    {
        var rendered = string.Join("\n", BuildSampleEmails().Select(e => e.HtmlBody));

        Assert.Contains("Inter", rendered);
        Assert.Contains("Fraunces", rendered);
        Assert.Contains("#f7faf3", rendered);
        Assert.Contains("#006049", rendered);
        Assert.Contains("#cfdecf", rendered);
        Assert.Contains("#ffe4af", rendered);
        Assert.Contains("border-radius: 8px", rendered);
        Assert.DoesNotContain("#6b1f2c", rendered);
        Assert.DoesNotContain("#fdf8f3", rendered);
        Assert.DoesNotContain("#fffdf9", rendered);
    }

    private static QueuedEmail[] BuildSampleEmails()
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

        return
        [
            AdminLinkTemplate.Build(ev, "https://example.test"),
            InvitationTemplate.Build(ev, invitee, "https://example.test", isResend: true),
            RsvpConfirmationTemplate.Build(ev, rsvp, "https://example.test"),
            RsvpNotificationTemplate.Build(ev, rsvp, "https://example.test"),
        ];
    }
}
