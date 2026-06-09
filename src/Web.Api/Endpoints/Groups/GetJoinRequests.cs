using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Groups.GetJoinRequests;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class GetJoinRequests : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups/{id:guid}/requests", async (
            Guid id,
            IQueryHandler<GetJoinRequestsQuery, List<JoinRequestResponse>> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            GetJoinRequestsQuery query = new(id, userContext.UserId);
            var result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
