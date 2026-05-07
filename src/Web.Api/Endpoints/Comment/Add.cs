using Application.Abstractions.Messaging;
using Application.Comment.Add;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Comment;

internal sealed class Add : IEndpoint
{
    public sealed class Request
    {
        public string Content { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("posts/{postId:guid}/comments", async (
            Guid postId,
            Request request,
            ICommandHandler<AddCommentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCommentCommand(postId, request.Content);

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
