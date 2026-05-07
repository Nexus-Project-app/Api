using Application.Abstractions.Messaging;
using Application.Comment;
using Application.Comment.GetByPost;
using Application.Common;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Comment;

internal sealed class GetByPost : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("posts/{postId:guid}/comments", async (
            Guid postId,
            int page,
            int pageSize,
            IQueryHandler<GetCommentsByPostIdQuery, PagedList<CommentResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCommentsByPostIdQuery(postId, page, pageSize);

            var result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
