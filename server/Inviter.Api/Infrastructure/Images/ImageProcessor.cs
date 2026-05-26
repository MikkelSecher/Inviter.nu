using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Inviter.Api.Infrastructure.Images;

public class ImageProcessor
{
    private const int MaxDimension = 1600;

    private readonly ILogger<ImageProcessor> _log;

    public ImageProcessor(ILogger<ImageProcessor> log) => _log = log;

    /// Resizes (within MaxDimension, preserving aspect, never upscaling) and re-encodes as WebP.
    /// Returns null if the input can't be decoded as an image.
    public async Task<byte[]?> ToWebpAsync(Stream input, CancellationToken ct)
    {
        try
        {
            using var image = await Image.LoadAsync(input, ct);
            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension),
                }));
            }

            using var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms, new WebpEncoder { Quality = 80 }, ct);
            return ms.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to decode/process uploaded image");
            return null;
        }
    }

    /// Re-encodes stored WebP bytes as JPEG for broad email-client compatibility.
    public async Task<byte[]> WebpToJpegAsync(byte[] webp, CancellationToken ct)
    {
        using var image = Image.Load(webp);
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 82 }, ct);
        return ms.ToArray();
    }
}
