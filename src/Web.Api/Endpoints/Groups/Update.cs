using Application.Abstractions.Messaging;
using Application.Groups.Update;
using Domain.Groups;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GroupVisibility Visibility { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("groups/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateGroupCommand> handler,
            CancellationToken cancellationToken) =>
        {
            UpdateGroupCommand command = new(id, request.Name, request.Description, request.Visibility);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
