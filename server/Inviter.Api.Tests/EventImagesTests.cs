using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Inviter.Api.Features.Events;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Inviter.Api.Tests;

public class EventImagesTests : IClassFixture<InviterApiFactory>
{
    private readonly InviterApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly byte[] SamplePng = MakePng(48, 32);

    private static byte[] MakePng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(120, 40, 60));
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public EventImagesTests(InviterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string contentType, string fileName)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { content, "file", fileName } };
    }

    private async Task<string?> GetPublicImageUrl(string inviteToken)
    {
        var dto = await _client.GetFromJsonAsync<EventPublicDto>(
            $"/api/invite/{inviteToken}", TestJson.Options);
        return dto!.ImageUrl;
    }

    private static string FileNameFromUrl(string imageUrl) => imageUrl.Split('/').Last();

    [Fact]
    public async Task Upload_HappyPath_StoresFileAndExposesUrl()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);

        var resp = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(SamplePng, "image/png", "pic.png"));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.StatusCode == HttpStatusCode.OK, $"Status {resp.StatusCode}: {body}");
        var dto = await resp.Content.ReadFromJsonAsync<UploadEventImageResponse>(TestJson.Options);
        Assert.NotNull(dto);
        Assert.StartsWith("/api/event-images/", dto!.ImageUrl);
        Assert.EndsWith(".webp", dto.ImageUrl);

        var fileName = FileNameFromUrl(dto.ImageUrl);
        Assert.True(File.Exists(Path.Combine(_factory.ImageRoot, fileName)));

        Assert.Equal(dto.ImageUrl, await GetPublicImageUrl(ev.InviteToken));
    }

    [Fact]
    public async Task Upload_Oversize_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var big = new byte[8 * 1024 * 1024 + 1];

        var resp = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(big, "image/png", "big.png"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await TestHelpers.ReadProblemAsync(resp);
        Assert.NotEmpty(TestHelpers.GetErrors(problem, "image"));
    }

    [Fact]
    public async Task Upload_DisallowedContentType_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var bytes = Encoding.UTF8.GetBytes("not an image");

        var resp = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(bytes, "text/plain", "note.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.NotEmpty(TestHelpers.GetErrors(await TestHelpers.ReadProblemAsync(resp), "image"));
    }

    [Fact]
    public async Task Upload_CorruptImageWithImageContentType_Returns400()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var bytes = Encoding.UTF8.GetBytes("this is not really a jpeg");

        var resp = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(bytes, "image/jpeg", "fake.jpg"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.NotEmpty(TestHelpers.GetErrors(await TestHelpers.ReadProblemAsync(resp), "image"));
    }

    [Fact]
    public async Task Upload_UnknownAdminToken_Returns404()
    {
        var resp = await _client.PostAsync(
            "/api/manage/wrong-token/image", FileContent(SamplePng, "image/png", "pic.png"));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Upload_Replace_DeletesOldFile()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);

        var first = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(SamplePng, "image/png", "a.png"));
        var firstUrl = (await first.Content.ReadFromJsonAsync<UploadEventImageResponse>(TestJson.Options))!.ImageUrl;
        var firstFile = Path.Combine(_factory.ImageRoot, FileNameFromUrl(firstUrl));
        Assert.True(File.Exists(firstFile));

        var second = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(SamplePng, "image/png", "b.png"));
        var secondUrl = (await second.Content.ReadFromJsonAsync<UploadEventImageResponse>(TestJson.Options))!.ImageUrl;
        var secondFile = Path.Combine(_factory.ImageRoot, FileNameFromUrl(secondUrl));

        Assert.NotEqual(firstUrl, secondUrl);
        Assert.False(File.Exists(firstFile));
        Assert.True(File.Exists(secondFile));
        Assert.Equal(secondUrl, await GetPublicImageUrl(ev.InviteToken));
    }

    [Fact]
    public async Task Delete_RemovesFileAndClearsUrl()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var upload = await _client.PostAsync(
            $"/api/manage/{ev.AdminToken}/image", FileContent(SamplePng, "image/png", "pic.png"));
        var url = (await upload.Content.ReadFromJsonAsync<UploadEventImageResponse>(TestJson.Options))!.ImageUrl;
        var file = Path.Combine(_factory.ImageRoot, FileNameFromUrl(url));
        Assert.True(File.Exists(file));

        var del = await _client.DeleteAsync($"/api/manage/{ev.AdminToken}/image");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        Assert.False(File.Exists(file));
        Assert.Null(await GetPublicImageUrl(ev.InviteToken));
    }

    [Fact]
    public async Task Delete_NoImage_IsNoOp()
    {
        var ev = await TestHelpers.CreateEventAsync(_client);
        var del = await _client.DeleteAsync($"/api/manage/{ev.AdminToken}/image");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownAdminToken_Returns404()
    {
        var resp = await _client.DeleteAsync("/api/manage/wrong-token/image");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
