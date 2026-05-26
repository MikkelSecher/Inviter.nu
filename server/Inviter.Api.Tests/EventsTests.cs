using System.Net;
using System.Net.Http.Json;
using Inviter.Api.Domain;
using Inviter.Api.Features.Events;

namespace Inviter.Api.Tests;

public class EventsTests : IClassFixture<InviterApiFactory>
{
    private readonly InviterApiFactory _factory;
    private readonly HttpClient _client;

    public EventsTests(InviterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------- POST /api/events ----------

    [Fact]
    public async Task Create_ReturnsCreated_WithTokensAndTrimmedFields()
    {
        var req = new CreateEventRequest(
            Title: "  Min fest  ",
            Description: "  Beskrivelse  ",
            Location: "  Hjemme  ",
            StartsAt: new DateTime(2026, 12, 24, 18, 0, 0, DateTimeKind.Utc),
            AllowMaybe: false,
            RsvpDeadline: new DateTime(2026, 12, 20, 23, 59, 0, DateTimeKind.Utc),
            ContactRequirement: ContactRequirement.Email,
            OrganizerEmail: null,
            OrganizerName: null);

        var resp = await _client.PostAsJsonAsync("/api/events", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<EventCreatedDto>(TestJson.Options);
        Assert.NotNull(dto);
        Assert.Equal("Min fest", dto!.Title);
        Assert.Equal("Beskrivelse", dto.Description);
        Assert.Equal("Hjemme", dto.Location);
        Assert.False(dto.AllowMaybe);
        Assert.Equal(ContactRequirement.Email, dto.ContactRequirement);
        Assert.False(string.IsNullOrEmpty(dto.InviteToken));
        Assert.False(string.IsNullOrEmpty(dto.AdminToken));
        Assert.NotEqual(dto.InviteToken, dto.AdminToken);
        Assert.Equal($"/api/manage/{dto.AdminToken}", resp.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_WithOrganizerEmail_EnqueuesAdminLinkEmail()
    {
        var emailsBefore = _factory.Emails.Enqueued.Count;
        var dto = await TestHelpers.CreateEventAsync(_client, organizerEmail: "mse@ecreo.dk", organizerName: "Mikkel");

        var newEmails = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Single(newEmails);
        Assert.Equal("AdminLink", newEmails[0].Kind);
        Assert.Equal("mse@ecreo.dk", newEmails[0].ToAddress);
        Assert.Equal("Mikkel", newEmails[0].ToName);
        Assert.Contains(dto.AdminToken, newEmails[0].TextBody);
    }

    [Fact]
    public async Task Create_WithoutOrganizerEmail_DoesNotEnqueue()
    {
        var emailsBefore = _factory.Emails.Enqueued.Count;
        await TestHelpers.CreateEventAsync(_client, organizerEmail: null);
        Assert.Equal(emailsBefore, _factory.Emails.Enqueued.Count);
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400WithTitleError()
    {
        var req = new CreateEventRequest("   ", null, null,
            DateTime.UtcNow.AddDays(2), true, null, ContactRequirement.None, null, null);

        var resp = await _client.PostAsJsonAsync("/api/events", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("Titel er påkrævet.", TestHelpers.GetErrors(problem, "title"));
    }

    [Fact]
    public async Task Create_RsvpDeadlineAfterStartsAt_Returns400()
    {
        var startsAt = DateTime.UtcNow.AddDays(7);
        var req = new CreateEventRequest("Fest", null, null, startsAt,
            true, RsvpDeadline: startsAt.AddHours(1),
            ContactRequirement.None, null, null);

        var resp = await _client.PostAsJsonAsync("/api/events", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("SU-deadline kan ikke ligge efter eventet.",
            TestHelpers.GetErrors(problem, "rsvpDeadline"));
    }

    [Fact]
    public async Task Create_InvalidOrganizerEmail_Returns400()
    {
        var req = new CreateEventRequest("Fest", null, null,
            DateTime.UtcNow.AddDays(2), true, null, ContactRequirement.None,
            OrganizerEmail: "ikke-en-email", OrganizerName: null);

        var resp = await _client.PostAsJsonAsync("/api/events", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.NotEmpty(TestHelpers.GetErrors(problem, "organizerEmail"));
    }

    // ---------- GET /api/invite/{token} ----------

    [Fact]
    public async Task GetByInviteToken_ReturnsPublicDtoWithoutAdminTokenOrRsvps()
    {
        var created = await TestHelpers.CreateEventAsync(_client);

        var resp = await _client.GetAsync($"/api/invite/{created.InviteToken}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"inviteToken\":", body);
        Assert.DoesNotContain("adminToken", body);
        Assert.DoesNotContain("\"rsvps\":", body);
    }

    [Fact]
    public async Task GetByInviteToken_Unknown_Returns404()
    {
        var resp = await _client.GetAsync("/api/invite/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- GET /api/manage/{adminToken} ----------

    [Fact]
    public async Task GetByAdminToken_ReturnsAdminDtoWithRsvpsList()
    {
        var created = await TestHelpers.CreateEventAsync(_client);

        var resp = await _client.GetAsync($"/api/manage/{created.AdminToken}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<EventAdminDto>(TestJson.Options);
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto!.Id);
        Assert.Equal(created.AdminToken, dto.AdminToken);
        Assert.Empty(dto.Rsvps);
    }

    [Fact]
    public async Task GetByAdminToken_Unknown_Returns404_NotUnauthorized()
    {
        var resp = await _client.GetAsync("/api/manage/clearly-wrong-admin-token");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- PUT /api/manage/{adminToken} ----------

    [Fact]
    public async Task Update_ReplacesEditableFields_AndReusesValidation()
    {
        var created = await TestHelpers.CreateEventAsync(_client, title: "Original");
        var newStart = DateTime.UtcNow.AddDays(14);

        var update = new UpdateEventRequest(
            "  Ny titel  ", "  Ny beskrivelse  ", "  Hos mor  ",
            newStart, AllowMaybe: false, RsvpDeadline: newStart.AddDays(-1),
            ContactRequirement.Phone, OrganizerEmail: "anne@example.com", OrganizerName: "Anne");

        var resp = await _client.PutAsJsonAsync($"/api/manage/{created.AdminToken}", update, TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var refreshed = await _client.GetFromJsonAsync<EventAdminDto>(
            $"/api/manage/{created.AdminToken}", TestJson.Options);
        Assert.NotNull(refreshed);
        Assert.Equal("Ny titel", refreshed!.Title);
        Assert.Equal("Ny beskrivelse", refreshed.Description);
        Assert.Equal("Hos mor", refreshed.Location);
        Assert.False(refreshed.AllowMaybe);
        Assert.Equal(ContactRequirement.Phone, refreshed.ContactRequirement);
        Assert.Equal("anne@example.com", refreshed.OrganizerEmail);
        Assert.Equal("Anne", refreshed.OrganizerName);
    }

    [Fact]
    public async Task Update_RsvpDeadlineAfterStart_Returns400()
    {
        var created = await TestHelpers.CreateEventAsync(_client);
        var startsAt = DateTime.UtcNow.AddDays(7);
        var update = new UpdateEventRequest("Fest", null, null, startsAt,
            true, startsAt.AddHours(2), ContactRequirement.None, null, null);

        var resp = await _client.PutAsJsonAsync($"/api/manage/{created.AdminToken}", update, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownAdminToken_Returns404()
    {
        var update = new UpdateEventRequest("Fest", null, null,
            DateTime.UtcNow.AddDays(2), true, null, ContactRequirement.None, null, null);
        var resp = await _client.PutAsJsonAsync("/api/manage/wrong-token", update, TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
