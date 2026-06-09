using Application.Abstractions.Messaging;
using Application.Groups.RequestJoin;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class RequestJoin : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("groups/{id:guid}/requests", async (
            Guid id,
            ICommandHandler<RequestJoinGroupCommand> handler,
            CancellationToken cancellationToken) =>
        {
            RequestJoinGroupCommand command = new(id);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
