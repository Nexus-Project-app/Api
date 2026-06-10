using Application.Abstractions.Messaging;
using Application.Groups.ReviewJoinRequest;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class ReviewJoinRequest : IEndpoint
{
    public sealed class Request
    {
        public bool Accept { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("groups/{id:guid}/requests/{requestId:guid}", async (
            Guid id,
            Guid requestId,
            Request request,
            ICommandHandler<ReviewJoinRequestCommand> handler,
            CancellationToken cancellationToken) =>
        {
            ReviewJoinRequestCommand command = new(id, requestId, request.Accept);
            var result = await handler.Handle(command, cancellationToken);
            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .RequireAuthorization();
    }
}
