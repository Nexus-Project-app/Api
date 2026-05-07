using Application.Abstractions.Messaging;
using Application.Comment.Update;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Comment;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string Content { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("comments/{commentId:guid}", async (
                Guid commentId,
                Request request,
                ICommandHandler<UpdateCommentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateCommentCommand
                {
                    CommentId = commentId,
                    Content = request.Content
                };

                var result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Posts)
            .RequireAuthorization();
    }
}
