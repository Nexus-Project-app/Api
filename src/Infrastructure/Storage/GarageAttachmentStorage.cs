using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

internal sealed class GarageAttachmentStorage : IAttachmentStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public GarageAttachmentStorage(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucketName = configuration["Storage:BucketName"] ?? "nexus-attachments";
        _endpoint = configuration["Storage:Endpoint"] ?? string.Empty;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        PutObjectRequest request = new()
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        await _s3.PutObjectAsync(request);
        return GetPermanentUrl(key);
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        GetObjectResponse response = await _s3.GetObjectAsync(_bucketName, key);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucketName, key);
    }

    public string GetPermanentUrl(string key) =>
        $"{_endpoint.TrimEnd('/')}/{_bucketName}/{key}";

    internal static string ReadSecret(string secretPath, string fallback) =>
        File.Exists(secretPath) ? File.ReadAllText(secretPath).Trim() : fallback;
}
