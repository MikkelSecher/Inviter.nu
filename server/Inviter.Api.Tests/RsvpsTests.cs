using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Inviter.Api.Domain;
using Inviter.Api.Features.Events;
using Inviter.Api.Features.Rsvps;

namespace Inviter.Api.Tests;

public class RsvpsTests : IClassFixture<InviterApiFactory>
{
    private readonly InviterApiFactory _factory;
    private readonly HttpClient _client;

    public RsvpsTests(InviterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------- POST /api/invite/{token}/rsvp ----------

    [Theory]
    [InlineData(RsvpStatus.Yes)]
    [InlineData(RsvpStatus.No)]
    [InlineData(RsvpStatus.Maybe)]
    public async Task Submit_HappyPath_PersistsAndReturnsDto(RsvpStatus status)
    {
        var ev = await TestHelpers.CreateEventAsync(_client, allowMaybe: true);
        var req = new CreateRsvpRequest("  Anne  ", status, "  hej  ", null, null);

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<RsvpDto>(TestJson.Options);
        Assert.NotNull(dto);
        Assert.Equal("Anne", dto!.GuestName);
        Assert.Equal(status, dto.Status);
        Assert.Equal("hej", dto.Comment);
    }

    [Fact]
    public async Task Submit_EmptyName_Returns400_WithGuestNameError()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var req = new CreateRsvpRequest("   ", RsvpStatus.Yes, null, null, null);

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("Navn er påkrævet.", TestHelpers.GetErrors(problem, "guestName"));
    }

    [Fact]
    public async Task Submit_InvalidStatusInteger_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);

        var body = JsonSerializer.Serialize(new
        {
            guestName = "Anne",
            status = 99,
            comment = (string?)null,
            email = (string?)null,
            phone = (string?)null,
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await _client.PostAsync($"/api/invite/{ev.InviteToken}/rsvp", content);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("Ugyldig status.", TestHelpers.GetErrors(problem, "status"));
    }

    [Fact]
    public async Task Submit_AfterDeadline_Returns400_RsvpClosed()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            startsAt: DateTime.UtcNow.AddDays(2),
            rsvpDeadline: DateTime.UtcNow.AddMinutes(-1));
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, null, null);

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("Tilmeldingen er lukket.", TestHelpers.GetErrors(problem, "rsvp"));
    }

    [Fact]
    public async Task Submit_MaybeWhenNotAllowed_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client, allowMaybe: false);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Maybe, null, null, null);

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.Contains("'Måske' er ikke tilladt for dette event.",
            TestHelpers.GetErrors(problem, "status"));
    }

    [Fact]
    public async Task Submit_EmailRequired_ButMissing_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, Email: null, Phone: null);

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.NotEmpty(TestHelpers.GetErrors(problem, "email"));
    }

    [Fact]
    public async Task Submit_EmailRequired_Provided_PersistsAndDiscardsPhone()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null,
            Email: "anne@example.com", Phone: "+4512345678");

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<RsvpDto>(TestJson.Options);
        Assert.Equal("anne@example.com", dto!.Email);
        Assert.Null(dto.Phone);
    }

    [Fact]
    public async Task Submit_PhoneRequired_ButMissing_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Phone);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, "anne@example.com", "123");

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.NotEmpty(TestHelpers.GetErrors(problem, "phone"));
    }

    [Fact]
    public async Task Submit_PhoneRequired_Provided_PersistsAndDiscardsEmail()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Phone);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null,
            Email: "anne@example.com", Phone: "+4512345678");

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<RsvpDto>(TestJson.Options);
        Assert.Equal("+4512345678", dto!.Phone);
        Assert.Null(dto.Email);
    }

    [Fact]
    public async Task Submit_ContactNone_DiscardsBothEmailAndPhone()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.None);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null,
            "anne@example.com", "+4512345678");

        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<RsvpDto>(TestJson.Options);
        Assert.Null(dto!.Email);
        Assert.Null(dto.Phone);
    }

    [Fact]
    public async Task Submit_WithEmailAndOrganizer_EnqueuesConfirmationAndNotification()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            organizerEmail: "host@example.com",
            contactRequirement: ContactRequirement.Email);
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null,
            "anne@example.com", null);
        var resp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);
        resp.EnsureSuccessStatusCode();

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Equal(2, sent.Count);
        Assert.Contains(sent, e => e.Kind == "RsvpConfirmation" && e.ToAddress == "anne@example.com");
        Assert.Contains(sent, e => e.Kind == "RsvpNotification" && e.ToAddress == "host@example.com");
    }

    [Fact]
    public async Task Submit_UnknownInviteToken_Returns404()
    {
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, null, null);
        var resp = await _client.PostAsJsonAsync(
            "/api/invite/no-such-token/rsvp", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- DELETE /api/manage/{adminToken}/rsvp/{rsvpId} ----------

    [Fact]
    public async Task Delete_Happy_RemovesRsvp()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var req = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, null, null);
        var submit = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", req, TestJson.Options);
        var rsvp = await submit.Content.ReadFromJsonAsync<RsvpDto>(TestJson.Options);

        var del = await _client.DeleteAsync($"/api/manage/{ev.AdminToken}/rsvp/{rsvp!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var refreshed = await _client.GetFromJsonAsync<EventAdminDto>(
            $"/api/manage/{ev.AdminToken}", TestJson.Options);
        Assert.Empty(refreshed!.Rsvps);
    }

    [Fact]
    public async Task Delete_UnknownAdminToken_Returns404()
    {
        var resp = await _client.DeleteAsync($"/api/manage/wrong-token/rsvp/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownRsvpId_Returns404()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var resp = await _client.DeleteAsync($"/api/manage/{ev.AdminToken}/rsvp/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
