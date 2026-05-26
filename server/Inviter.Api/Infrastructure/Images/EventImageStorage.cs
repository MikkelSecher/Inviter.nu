namespace Inviter.Api.Infrastructure.Images;

public class EventImageStorage
{
    public string RootPath { get; }

    public EventImageStorage(string rootPath)
    {
        RootPath = rootPath;
        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveAsync(string fileName, byte[] content, CancellationToken ct)
        => await File.WriteAllBytesAsync(GetFullPath(fileName), content, ct);

    public async Task<byte[]?> ReadAsync(string fileName, CancellationToken ct)
    {
        var path = GetFullPath(fileName);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
    }

    public void Delete(string fileName)
    {
        var path = GetFullPath(fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    private string GetFullPath(string fileName) => Path.Combine(RootPath, fileName);
}
