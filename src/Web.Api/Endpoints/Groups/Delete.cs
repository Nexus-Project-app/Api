using Application.Abstractions.Messaging;
using Application.Groups.Delete;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("groups/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteGroupCommand> handler,
            CancellationToken cancellationToken) =>
        {
            DeleteGroupCommand command = new(id);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
