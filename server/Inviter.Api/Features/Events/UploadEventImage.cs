using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Images;
using Inviter.Api.Infrastructure.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Events;

public static class UploadEventImage
{
    private const long MaxUploadBytes = 8 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public static async Task<IResult> Handle(
        string adminToken,
        IFormFile? file,
        AppDbContext db,
        EventImageStorage storage,
        ImageProcessor processor,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem("Vælg et billede.");
        if (file.Length > MaxUploadBytes)
            return Problem("Billedet er for stort. Maks 8 MB.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            return Problem("Billedet skal være JPEG, PNG eller WebP.");

        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken, ct);
        if (ev is null) return Results.NotFound();

        byte[]? webp;
        await using (var stream = file.OpenReadStream())
        {
            webp = await processor.ToWebpAsync(stream, ct);
        }
        if (webp is null)
            return Problem("Billedet kunne ikke behandles. Prøv et andet billede.");

        var newFileName = TokenGenerator.NewImageToken() + ".webp";
        await storage.SaveAsync(newFileName, webp, ct);

        var oldFileName = ev.ImageFileName;
        ev.ImageFileName = newFileName;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldFileName))
            storage.Delete(oldFileName);

        return Results.Ok(new UploadEventImageResponse(EventImageUrl.Build(newFileName)!));
    }

    private static IResult Problem(string message)
        => Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [message] });
}
