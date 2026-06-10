using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Posts.GetById;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("posts/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetPostByIdQuery, PostResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var currentUserSub = principal.Identity?.IsAuthenticated == true
                ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            var query = new GetPostByIdQuery(id, currentUserSub);

            var result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .AllowAnonymous();
    }
}
