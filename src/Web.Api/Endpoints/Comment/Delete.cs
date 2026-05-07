using Application.Abstractions.Messaging;
using Application.Comment.Delete;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Comment;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("comments/{commentId:guid}", async (
                Guid commentId,
                ICommandHandler<DeleteCommentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new DeleteCommentCommand { CommentId = commentId }, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Posts)
            .RequireAuthorization();
    }
}
