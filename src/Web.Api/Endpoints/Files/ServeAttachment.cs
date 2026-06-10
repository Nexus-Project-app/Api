using Application.Abstractions.Storage;

namespace Web.Api.Endpoints.Files;

internal sealed class ServeAttachment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{**key}", Handle)
            .WithTags(Tags.Files)
            .AllowAnonymous();
    }

    internal static async Task<IResult> Handle(string key, IAttachmentStorage storage)
    {
        Stream stream;
        try
        {
            stream = await storage.DownloadAsync(key);
        }
        catch (Exception)
        {
            return Results.NotFound();
        }

        var contentType = MimeFromKey(key);
        return Results.Stream(stream, contentType, enableRangeProcessing: false);
    }

    private static string MimeFromKey(string key)
    {
        var ext = Path.GetExtension(key).ToUpperInvariant();
        return ext switch
        {
            ".JPG" or ".JPEG" => "image/jpeg",
            ".PNG"            => "image/png",
            ".GIF"            => "image/gif",
            ".WEBP"           => "image/webp",
            ".PDF"            => "application/pdf",
            ".ZIP"            => "application/zip",
            ".MD"             => "text/markdown",
            ".TXT"            => "text/plain",
            ".DOCX"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _                 => "application/octet-stream",
        };
    }
}
