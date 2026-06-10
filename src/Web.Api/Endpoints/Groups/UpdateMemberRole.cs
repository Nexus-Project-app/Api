using Application.Abstractions.Messaging;
using Application.Groups.UpdateMemberRole;
using Domain.Groups;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class UpdateMemberRole : IEndpoint
{
    public sealed class Request
    {
        public GroupMemberRole Role { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("groups/{id:guid}/members/{userId:guid}/role", async (
            Guid id,
            Guid userId,
            Request request,
            ICommandHandler<UpdateMemberRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            UpdateMemberRoleCommand command = new(id, userId, request.Role);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
