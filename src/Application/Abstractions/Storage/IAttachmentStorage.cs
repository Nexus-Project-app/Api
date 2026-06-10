namespace Application.Abstractions.Storage;

public interface IAttachmentStorage
{
    Task<string> UploadAsync(string key, Stream content, string contentType);
    Task<Stream> DownloadAsync(string key);
    Task DeleteAsync(string key);
    string GetPermanentUrl(string key);
}
