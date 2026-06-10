using Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

internal sealed class FileSystemAttachmentStorage(IConfiguration configuration) : IAttachmentStorage
{
    private readonly string _basePath = configuration["Storage:BasePath"] ?? "/app/uploads";

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var filePath = ResolveSafePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using FileStream fileStream = new(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream);
        return GetPermanentUrl(key);
    }

    public Task<Stream> DownloadAsync(string key)
    {
        var filePath = ResolveSafePath(key);
        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteAsync(string key)
    {
        var filePath = ResolveSafePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public string GetPermanentUrl(string key) => $"/attachments/{key}";

    private string ResolveSafePath(string key)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(filePath);
        var fullBase = Path.GetFullPath(_basePath);
        if (!fullPath.StartsWith(fullBase + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Key '{key}' resolves outside storage base path.");
        }
        return fullPath;
    }
}
