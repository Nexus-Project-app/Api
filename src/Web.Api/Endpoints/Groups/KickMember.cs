using Application.Abstractions.Messaging;
using Application.Groups.KickMember;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class KickMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("groups/{id:guid}/members/{userId:guid}", async (
            Guid id,
            Guid userId,
            ICommandHandler<KickMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            KickMemberCommand command = new(id, userId);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
