using System.Net;

namespace Inviter.Api.Tests;

public class MetricsEndpointTests : IClassFixture<InviterApiFactory>
{
    private readonly HttpClient _client;

    public MetricsEndpointTests(InviterApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Prometheus_endpoint_serves_inviter_api_metrics()
    {
        // Hit any endpoint first so AspNetCore instrumentation has data to emit.
        await _client.GetAsync("/api/manage/does-not-exist");

        var resp = await _client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var contentType = resp.Content.Headers.ContentType?.MediaType;
        Assert.NotNull(contentType);
        Assert.StartsWith("text/plain", contentType!, StringComparison.OrdinalIgnoreCase);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("events_total", body);
        Assert.Contains("rsvps_total", body);
        Assert.Contains("upcoming_events_total", body);
    }
}
