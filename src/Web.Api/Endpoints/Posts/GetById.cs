using Application.Abstractions.Messaging;
using Application.Posts.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("posts/{id:guid}", async (
            Guid id,
            IQueryHandler<GetPostByIdQuery, PostResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPostByIdQuery(id);

            var result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}
