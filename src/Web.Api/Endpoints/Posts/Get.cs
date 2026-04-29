using Application.Abstractions.Messaging;
using Application.Common;
using Application.Posts.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("posts", async (
            int page,
            int pageSize,
            IQueryHandler<GetPostsQuery, PagedList<PostResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPostsQuery(page, pageSize);

            Result<PagedList<PostResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
