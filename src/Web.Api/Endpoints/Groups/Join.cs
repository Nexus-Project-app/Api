using Application.Abstractions.Messaging;
using Application.Groups.Join;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Join : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("groups/{id:guid}/join", async (
            Guid id,
            ICommandHandler<JoinGroupCommand> handler,
            CancellationToken cancellationToken) =>
        {
            JoinGroupCommand command = new(id);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
