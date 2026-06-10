using Application.Abstractions.Messaging;
using Application.Groups.Create;
using Domain.Groups;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GroupVisibility Visibility { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("groups", async (
            Request request,
            ICommandHandler<CreateGroupCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            CreateGroupCommand command = new(request.Name, request.Description, request.Visibility);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
