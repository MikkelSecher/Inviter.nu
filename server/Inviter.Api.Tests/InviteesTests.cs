using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inviter.Api.Domain;
using Inviter.Api.Features.Invitees;
using Inviter.Api.Features.Rsvps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Inviter.Api.Tests;

public class InviteesTests : IClassFixture<InviterApiFactory>
{
    private readonly InviterApiFactory _factory;
    private readonly HttpClient _client;

    public InviteesTests(InviterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<AddInviteesResponse> AddAsync(string adminToken, params (string email, string? name)[] entries)
    {
        var req = new AddInviteesRequest(entries.Select(e => new AddInviteeEntry(e.email, e.name)).ToList());
        var resp = await _client.PostAsJsonAsync($"/api/manage/{adminToken}/invitees", req, TestJson.Options);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AddInviteesResponse>(TestJson.Options))!;
    }

    // ---------- GET /api/invite/{token}/guest/{token} ----------

    [Fact]
    public async Task Prefill_Happy_ReturnsNameAndEmail()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", "Anne"));

        var resp = await _client.GetAsync(
            $"/api/invite/{ev.InviteToken}/guest/{added.Added[0].PersonalInviteToken}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<InviteePrefillDto>(TestJson.Options);
        Assert.Equal("Anne", dto!.Name);
        Assert.Equal("anne@example.com", dto.Email);
        Assert.Null(dto.RsvpStatus);
    }

    [Fact]
    public async Task Prefill_WithExistingRsvp_ReturnsLatestRsvp()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", "Anne"));
        var invitee = added.Added[0];

        var first = new CreateRsvpRequest(
            "Anne",
            RsvpStatus.Yes,
            "Glæder mig",
            "anne@example.com",
            null,
            invitee.PersonalInviteToken);
        var firstResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", first, TestJson.Options);
        firstResp.EnsureSuccessStatusCode();

        await Task.Delay(5);

        var latest = new CreateRsvpRequest(
            "Anne Andersen",
            RsvpStatus.No,
            "Jeg kan desværre ikke",
            "anne.alt@example.com",
            null,
            invitee.PersonalInviteToken);
        var latestResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", latest, TestJson.Options);
        latestResp.EnsureSuccessStatusCode();

        var resp = await _client.GetAsync(
            $"/api/invite/{ev.InviteToken}/guest/{invitee.PersonalInviteToken}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<InviteePrefillDto>(TestJson.Options);
        Assert.Equal("Anne", dto!.Name);
        Assert.Equal("anne@example.com", dto.Email);
        Assert.Equal("Anne Andersen", dto.RsvpGuestName);
        Assert.Equal(RsvpStatus.No, dto.RsvpStatus);
        Assert.Equal("Jeg kan desværre ikke", dto.RsvpComment);
        Assert.Equal("anne.alt@example.com", dto.RsvpEmail);
        Assert.Null(dto.RsvpPhone);
    }

    [Fact]
    public async Task Prefill_UnknownId_Returns404()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var resp = await _client.GetAsync(
            $"/api/invite/{ev.InviteToken}/guest/no-such-guest");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Prefill_WrongInviteToken_Returns404()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", null));
        var resp = await _client.GetAsync($"/api/invite/wrong-token/guest/{added.Added[0].PersonalInviteToken}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- GET /api/manage/{adminToken}/invitees ----------

    [Fact]
    public async Task List_JoinsLatestRsvpStatusByEmailCaseInsensitive()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        await AddAsync(ev.AdminToken,
            ("Anne@Example.com", "Anne"),
            ("bo@example.com", "Bo"));

        var rsvp = new CreateRsvpRequest("Anne", RsvpStatus.Yes, null, "anne@example.com", null);
        var s = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", rsvp, TestJson.Options);
        s.EnsureSuccessStatusCode();

        var list = await _client.GetFromJsonAsync<List<InviteeDto>>(
            $"/api/manage/{ev.AdminToken}/invitees", TestJson.Options);

        Assert.NotNull(list);
        var anne = list!.Single(i => i.Email == "Anne@Example.com");
        var bo = list.Single(i => i.Email == "bo@example.com");
        Assert.Equal(RsvpStatus.Yes, anne.RsvpStatus);
        Assert.Null(bo.RsvpStatus);
    }

    [Fact]
    public async Task List_UnknownAdminToken_Returns404()
    {
        var resp = await _client.GetAsync("/api/manage/wrong/invitees");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- POST /api/manage/{adminToken}/invitees ----------

    [Fact]
    public async Task Add_HappyPath_PersistsAll()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var resp = await AddAsync(ev.AdminToken,
            ("a@example.com", "A"),
            ("b@example.com", null));

        Assert.Equal(2, resp.Added.Count);
        Assert.Empty(resp.SkippedDuplicates);
        Assert.Empty(resp.SkippedInvalid);
        Assert.All(resp.Added, i => Assert.False(string.IsNullOrEmpty(i.PersonalInviteToken)));
        Assert.All(resp.Added, i =>
        {
            Assert.Equal(1, i.SendCount);
            Assert.NotNull(i.LastSentAt);
        });

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Equal(2, sent.Count);
        Assert.All(sent, e => Assert.Equal("Invitation", e.Kind));
        Assert.Equal(new[] { "a@example.com", "b@example.com" }, sent.Select(e => e.ToAddress).OrderBy(x => x));
    }

    [Fact]
    public async Task Add_NameOnlyGuest_GeneratesPersonalInviteToken_WhenEventRequiresEmailContact()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        var emailsBefore = _factory.Emails.Enqueued.Count;
        var req = new AddInviteesRequest(new List<AddInviteeEntry>
            { new(null, "Anne") });

        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<AddInviteesResponse>(TestJson.Options);
        var added = Assert.Single(dto!.Added);
        Assert.Equal("Anne", added.Name);
        Assert.Null(added.Email);
        Assert.False(string.IsNullOrWhiteSpace(added.PersonalInviteToken));
        Assert.Null(added.LastSentAt);
        Assert.Equal(0, added.SendCount);
        Assert.Equal(emailsBefore, _factory.Emails.Enqueued.Count);
    }

    [Fact]
    public async Task Add_WithSendInvitationsFalse_DoesNotEnqueueEmails()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var emailsBefore = _factory.Emails.Enqueued.Count;
        var req = new AddInviteesRequest(
            new List<AddInviteeEntry> { new("anne@example.com", "Anne") },
            sendInvitations: false);

        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<AddInviteesResponse>(TestJson.Options);
        var added = Assert.Single(dto!.Added);
        Assert.Equal("anne@example.com", added.Email);
        Assert.Null(added.LastSentAt);
        Assert.Equal(0, added.SendCount);
        Assert.Equal(emailsBefore, _factory.Emails.Enqueued.Count);
    }

    [Fact]
    public async Task Add_EmptyEntries_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var req = new AddInviteesRequest(new List<AddInviteeEntry>());
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        var errors = TestHelpers.GetErrors(problem, "entries");
        Assert.Contains(errors, e => e.Contains("navn"));
    }

    [Fact]
    public async Task Add_TooManyEntries_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var entries = Enumerable.Range(0, 201)
            .Select(i => new AddInviteeEntry($"user{i}@example.com", null))
            .ToList();
        var req = new AddInviteesRequest(entries);
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        var errors = TestHelpers.GetErrors(problem, "entries");
        Assert.Contains(errors, e => e.Contains("200"));
    }

    [Fact]
    public async Task Add_DuplicatesAcrossExistingAndBatch_AreSkippedCaseInsensitive()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        await AddAsync(ev.AdminToken, ("anne@example.com", null));

        var resp = await AddAsync(ev.AdminToken,
            ("ANNE@example.com", null),
            ("bo@example.com", null),
            ("Bo@Example.com", null));

        Assert.Single(resp.Added);
        Assert.Equal("bo@example.com", resp.Added[0].Email);
        Assert.Equal(2, resp.SkippedDuplicates.Count);
    }

    [Fact]
    public async Task Add_InvalidEmails_AreSkipped()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var resp = await AddAsync(ev.AdminToken,
            ("not-an-email", null),
            ("ok@example.com", null),
            ("also bad", null));

        Assert.Single(resp.Added);
        Assert.Equal(2, resp.SkippedInvalid.Count);
    }

    // ---------- PUT /api/manage/{adminToken}/invitees/{id} ----------

    [Fact]
    public async Task Update_Happy_ChangesNameAndEmail()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", "Anne"));
        var id = added.Added[0].Id;
        var token = added.Added[0].PersonalInviteToken;

        var req = new UpdateInviteeRequest("anne2@example.com", "Anne 2");
        var resp = await _client.PutAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/{id}", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<InviteeDto>(TestJson.Options);
        Assert.Equal("Anne 2", dto!.Name);
        Assert.Equal("anne2@example.com", dto.Email);
        Assert.Equal(token, dto.PersonalInviteToken);
    }

    [Fact]
    public async Task Update_CanRemoveEmail_WhenNameRemains()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", "Anne"));

        var req = new UpdateInviteeRequest(null, "Anne");
        var resp = await _client.PutAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/{added.Added[0].Id}", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<InviteeDto>(TestJson.Options);
        Assert.Null(dto!.Email);
    }

    [Fact]
    public async Task Add_UnknownAdminToken_Returns404()
    {
        var req = new AddInviteesRequest(new List<AddInviteeEntry>
            { new("ok@example.com", null) });
        var resp = await _client.PostAsJsonAsync(
            "/api/manage/wrong-token/invitees", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- DELETE /api/manage/{adminToken}/invitees/{id} ----------

    [Fact]
    public async Task Delete_Happy_RemovesInvitee()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken, ("anne@example.com", null));
        var inviteeId = added.Added[0].Id;

        var del = await _client.DeleteAsync($"/api/manage/{ev.AdminToken}/invitees/{inviteeId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var list = await _client.GetFromJsonAsync<List<InviteeDto>>(
            $"/api/manage/{ev.AdminToken}/invitees", TestJson.Options);
        Assert.DoesNotContain(list!, i => i.Id == inviteeId);
    }

    [Fact]
    public async Task Delete_UnknownAdminToken_Returns404()
    {
        var resp = await _client.DeleteAsync($"/api/manage/wrong/invitees/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownInviteeId_Returns404()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var resp = await _client.DeleteAsync(
            $"/api/manage/{ev.AdminToken}/invitees/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- POST /api/manage/{adminToken}/invitees/send ----------

    [Fact]
    public async Task Send_All_EnqueuesInvitationForEach()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken,
            ("a@example.com", "A"),
            ("b@example.com", null));
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new SendInvitationsRequest(InviteeIds: null, OnlyUnsent: false);
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendInvitationsResponse>(TestJson.Options);
        Assert.Equal(2, dto!.Enqueued);

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Equal(2, sent.Count);
        Assert.All(sent, e => Assert.Equal("InvitationResend", e.Kind));

        var list = await _client.GetFromJsonAsync<List<InviteeDto>>(
            $"/api/manage/{ev.AdminToken}/invitees", TestJson.Options);
        Assert.All(list!, i =>
        {
            Assert.Equal(2, i.SendCount);
            Assert.NotNull(i.LastSentAt);
        });
    }

    [Fact]
    public async Task Send_SkipsInviteesWithoutEmail()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var add = new AddInviteesRequest(new List<AddInviteeEntry>
        {
            new("a@example.com", "A"),
            new(null, "No Email"),
        });
        var addResp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees", add, TestJson.Options);
        addResp.EnsureSuccessStatusCode();
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new SendInvitationsRequest(InviteeIds: null, OnlyUnsent: false);
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendInvitationsResponse>(TestJson.Options);
        Assert.Equal(1, dto!.Enqueued);
        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).Single();
        Assert.Equal("a@example.com", sent.ToAddress);
    }

    [Fact]
    public async Task Send_BySubsetIds_OnlySendsToThose()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var added = await AddAsync(ev.AdminToken,
            ("a@example.com", null),
            ("b@example.com", null),
            ("c@example.com", null));
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var pick = new[] { added.Added[0].Id, added.Added[2].Id };
        var req = new SendInvitationsRequest(InviteeIds: pick, OnlyUnsent: false);
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);

        var dto = await resp.Content.ReadFromJsonAsync<SendInvitationsResponse>(TestJson.Options);
        Assert.Equal(2, dto!.Enqueued);

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        var addresses = sent.Select(e => e.ToAddress).OrderBy(a => a).ToArray();
        Assert.Equal(new[] { "a@example.com", "c@example.com" }, addresses);
    }

    [Fact]
    public async Task Send_OnlyUnsent_SkipsAlreadySent_AndUsesInvitationResendKindOnRepeat()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        await AddAsync(ev.AdminToken, ("a@example.com", null));

        var emailsBefore = _factory.Emails.Enqueued.Count;

        var onlyUnsentReq = new SendInvitationsRequest(null, OnlyUnsent: true);
        var onlyUnsentResp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/send", onlyUnsentReq, TestJson.Options);
        var onlyUnsentDto = await onlyUnsentResp.Content.ReadFromJsonAsync<SendInvitationsResponse>(TestJson.Options);
        Assert.Equal(0, onlyUnsentDto!.Enqueued);
        Assert.Equal(emailsBefore, _factory.Emails.Enqueued.Count);

        var resendReq = new SendInvitationsRequest(null, OnlyUnsent: false);
        await _client.PostAsJsonAsync($"/api/manage/{ev.AdminToken}/invitees/send", resendReq, TestJson.Options);

        var newest = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Single(newest);
        Assert.Equal("InvitationResend", newest[0].Kind);

        var list = await _client.GetFromJsonAsync<List<InviteeDto>>(
            $"/api/manage/{ev.AdminToken}/invitees", TestJson.Options);
        Assert.Equal(2, list![0].SendCount);
    }

    [Fact]
    public async Task Send_NoMatchingInvitees_ReturnsZero()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var req = new SendInvitationsRequest(null, OnlyUnsent: false);
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendInvitationsResponse>(TestJson.Options);
        Assert.Equal(0, dto!.Enqueued);
    }

    [Fact]
    public async Task Send_UnknownAdminToken_Returns404()
    {
        var req = new SendInvitationsRequest(null, OnlyUnsent: false);
        var resp = await _client.PostAsJsonAsync(
            "/api/manage/wrong-token/invitees/send", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- POST /api/manage/{adminToken}/invitees/message ----------

    [Fact]
    public async Task SendMessage_All_EnqueuesForInviteesAndRsvpEmails()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        await AddAsync(ev.AdminToken, ("anne@example.com", "Anne"));

        var rsvp = new CreateRsvpRequest("Bo", RsvpStatus.Yes, null, "bo@example.com", null);
        var rsvpResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", rsvp, TestJson.Options);
        rsvpResp.EnsureSuccessStatusCode();
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new SendGuestMessageRequest(
            GuestMessageAudience.All,
            "Praktisk info",
            "Vi starter kl. 18.");
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/message", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendGuestMessageResponse>(TestJson.Options);
        Assert.Equal(2, dto!.Enqueued);

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).ToList();
        Assert.Equal(new[] { "anne@example.com", "bo@example.com" },
            sent.Select(e => e.ToAddress).OrderBy(x => x).ToArray());
        Assert.All(sent, e =>
        {
            Assert.Equal("GuestMessage", e.Kind);
            Assert.Equal("Praktisk info", e.Subject);
            Assert.Contains("Vi starter kl. 18.", e.TextBody);
        });
    }

    [Fact]
    public async Task SendMessage_Yes_UsesLatestRsvpStatus()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);
        var added = await AddAsync(ev.AdminToken,
            ("anne@example.com", "Anne"),
            ("bo@example.com", "Bo"));

        var first = new CreateRsvpRequest(
            "Anne",
            RsvpStatus.No,
            null,
            "anne@example.com",
            null,
            added.Added[0].PersonalInviteToken);
        var firstResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", first, TestJson.Options);
        firstResp.EnsureSuccessStatusCode();

        await Task.Delay(5);

        var latest = new CreateRsvpRequest(
            "Anne",
            RsvpStatus.Yes,
            null,
            "anne@example.com",
            null,
            added.Added[0].PersonalInviteToken);
        var latestResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", latest, TestJson.Options);
        latestResp.EnsureSuccessStatusCode();

        var bo = new CreateRsvpRequest(
            "Bo",
            RsvpStatus.No,
            null,
            "bo@example.com",
            null,
            added.Added[1].PersonalInviteToken);
        var boResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", bo, TestJson.Options);
        boResp.EnsureSuccessStatusCode();
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new SendGuestMessageRequest(
            GuestMessageAudience.Yes,
            "Til jer der kommer",
            "Vi glæder os.");
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/message", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendGuestMessageResponse>(TestJson.Options);
        Assert.Equal(1, dto!.Enqueued);

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).Single();
        Assert.Equal("anne@example.com", sent.ToAddress);
    }

    [Fact]
    public async Task SendMessage_No_IncludesUnlinkedRsvpEmail()
    {
        var ev = await TestHelpers.CreateEventAsync(_client,
            contactRequirement: ContactRequirement.Email);

        var no = new CreateRsvpRequest("Nora", RsvpStatus.No, null, "nora@example.com", null);
        var noResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", no, TestJson.Options);
        noResp.EnsureSuccessStatusCode();

        var yes = new CreateRsvpRequest("Yasmin", RsvpStatus.Yes, null, "yasmin@example.com", null);
        var yesResp = await _client.PostAsJsonAsync(
            $"/api/invite/{ev.InviteToken}/rsvp", yes, TestJson.Options);
        yesResp.EnsureSuccessStatusCode();
        var emailsBefore = _factory.Emails.Enqueued.Count;

        var req = new SendGuestMessageRequest(
            GuestMessageAudience.No,
            "Tak for svar",
            "Tak fordi du gav besked.");
        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/message", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SendGuestMessageResponse>(TestJson.Options);
        Assert.Equal(1, dto!.Enqueued);

        var sent = _factory.Emails.Enqueued.Skip(emailsBefore).Single();
        Assert.Equal("nora@example.com", sent.ToAddress);
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var req = new SendGuestMessageRequest(GuestMessageAudience.All, "Info", " ");

        var resp = await _client.PostAsJsonAsync(
            $"/api/manage/{ev.AdminToken}/invitees/message", req, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        var errors = TestHelpers.GetErrors(problem, "message");
        Assert.Contains(errors, e => e.Contains("påkrævet"));
    }

    [Fact]
    public async Task SendMessage_UnknownAdminToken_Returns404()
    {
        var req = new SendGuestMessageRequest(GuestMessageAudience.All, "Info", "Hej");
        var resp = await _client.PostAsJsonAsync(
            "/api/manage/wrong-token/invitees/message", req, TestJson.Options);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- Inline image in invitation email ----------

    private async Task UploadImageAsync(string adminToken)
    {
        using var image = new Image<Rgba32>(48, 32, new Rgba32(120, 40, 60));
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        var content = new ByteArrayContent(ms.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent { { content, "file", "pic.png" } };
        var resp = await _client.PostAsync($"/api/manage/{adminToken}/image", form);
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Send_WithoutImage_HasNoInlineAttachment()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        await AddAsync(ev.AdminToken, ("a@example.com", null));
        var before = _factory.Emails.Enqueued.Count;

        var req = new SendInvitationsRequest(null, OnlyUnsent: false);
        await _client.PostAsJsonAsync($"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);

        var sent = _factory.Emails.Enqueued.Skip(before).Single();
        Assert.True(sent.InlineAttachments is null or { Count: 0 });
        Assert.DoesNotContain("cid:event-image-", sent.HtmlBody);
    }

    [Fact]
    public async Task Send_WithImage_EmbedsInlineJpegWithMatchingCid()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        await UploadImageAsync(ev.AdminToken);
        await AddAsync(ev.AdminToken, ("a@example.com", null));
        var before = _factory.Emails.Enqueued.Count;

        var req = new SendInvitationsRequest(null, OnlyUnsent: false);
        await _client.PostAsJsonAsync($"/api/manage/{ev.AdminToken}/invitees/send", req, TestJson.Options);

        var sent = _factory.Emails.Enqueued.Skip(before).Single();
        Assert.NotNull(sent.InlineAttachments);
        var attachment = Assert.Single(sent.InlineAttachments!);
        Assert.Equal($"event-image-{ev.Id}", attachment.ContentId);
        Assert.Equal("image/jpeg", attachment.MediaType);
        Assert.NotEmpty(attachment.Content);
        Assert.Contains($"cid:event-image-{ev.Id}", sent.HtmlBody);
    }
}
