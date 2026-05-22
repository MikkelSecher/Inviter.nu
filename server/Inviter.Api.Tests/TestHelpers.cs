using System.Net.Http.Json;
using System.Text.Json;
using Inviter.Api.Contracts;
using Inviter.Api.Domain;

namespace Inviter.Api.Tests;

internal static class TestHelpers
{
    public static async Task<EventCreatedDto> CreateEventAsync(
        HttpClient client,
        string title = "Min fødselsdag",
        bool allowMaybe = true,
        DateTime? startsAt = null,
        DateTime? rsvpDeadline = null,
        ContactRequirement contactRequirement = ContactRequirement.None,
        string? organizerEmail = null,
        string? organizerName = null)
    {
        var req = new CreateEventRequest(
            Title: title,
            Description: "",
            Location: "",
            StartsAt: startsAt ?? DateTime.UtcNow.AddDays(7),
            AllowMaybe: allowMaybe,
            RsvpDeadline: rsvpDeadline,
            ContactRequirement: contactRequirement,
            OrganizerEmail: organizerEmail,
            OrganizerName: organizerName);

        var resp = await client.PostAsJsonAsync("/api/events", req, TestJson.Options);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<EventCreatedDto>(TestJson.Options);
        return dto!;
    }

    public static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }

    public static string[] GetErrors(JsonDocument problem, string field)
    {
        if (!problem.RootElement.TryGetProperty("errors", out var errors)) return Array.Empty<string>();
        if (!errors.TryGetProperty(field, out var arr)) return Array.Empty<string>();
        return arr.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
    }
}
