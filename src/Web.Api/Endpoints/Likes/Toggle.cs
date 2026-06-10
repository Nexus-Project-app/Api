using Application.Abstractions.Messaging;
using Application.Likes.Toggle;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Likes;

internal sealed class Toggle : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("posts/{postId:guid}/likes", async (
            Guid postId,
            ICommandHandler<ToggleLikeCommand, ToggleLikeResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ToggleLikeCommand(postId);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
