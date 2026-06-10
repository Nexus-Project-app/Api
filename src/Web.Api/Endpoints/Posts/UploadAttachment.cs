using Application.Abstractions.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Api.Endpoints;

namespace Web.Api.Endpoints.Posts;

internal sealed class UploadAttachment : IEndpoint
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/zip",
        "text/plain", "text/markdown",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("posts/{postId}/attachments", Handle)
            .WithTags(Tags.Posts)
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    internal static async Task<IResult> Handle(
        Guid postId,
        IFormFile file,
        IAttachmentStorage storage)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            object errorResponse = new { error = $"Type de fichier non autorisé : {file.ContentType}" };
            return Results.BadRequest(errorResponse);
        }

        if (file.Length > 20 * 1024 * 1024)
        {
            object errorResponse = new { error = "Le fichier dépasse la limite de 20 Mo." };
            return Results.BadRequest(errorResponse);
        }

        var ext = Path.GetExtension(file.FileName);
        var key = $"posts/{postId}/{Guid.NewGuid()}{ext}";

        await using var stream = file.OpenReadStream();
        var url = await storage.UploadAsync(key, stream, file.ContentType);

        object successResponse = new { key, url };
        return Results.Ok(successResponse);
    }
}
