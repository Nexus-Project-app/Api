using Application.Abstractions.Messaging;
using Application.Common;
using Application.Posts.GetByUser;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class GetByUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("posts/user/{userId:guid}", async (
            Guid userId,
            int page,
            int pageSize,
            IQueryHandler<GetPostsByUserQuery, PagedList<PostResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPostsByUserQuery(userId, page, pageSize);

            Result<PagedList<PostResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
