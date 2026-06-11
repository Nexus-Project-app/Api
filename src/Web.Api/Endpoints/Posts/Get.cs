using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Posts.Get;
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
            ClaimsPrincipal principal,
            IQueryHandler<GetPostsQuery, PagedList<PostResponse>> handler,
            CancellationToken cancellationToken,
            string? search = null) =>
        {
            var currentUserSub = principal.Identity?.IsAuthenticated == true
                ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            var query = new GetPostsQuery(page, pageSize, currentUserSub, search);

            var result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .AllowAnonymous();
    }
}
