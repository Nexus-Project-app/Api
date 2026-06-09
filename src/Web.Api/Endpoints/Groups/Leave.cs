using Application.Abstractions.Messaging;
using Application.Groups.Leave;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Leave : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("groups/{id:guid}/members/me", async (
            Guid id,
            ICommandHandler<LeaveGroupCommand> handler,
            CancellationToken cancellationToken) =>
        {
            LeaveGroupCommand command = new(id);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
