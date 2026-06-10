using Application.Abstractions.Messaging;
using Application.Keycloak.SetUserKeycloak;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Keycloak;

internal sealed class SetUserKeycloak : IEndpoint
{
    public sealed record Request(
        string FirstName,
        string LastName
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/me", async (
            Request request,
            ICommandHandler<SetUserKeycloakCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SetUserKeycloakCommand();

            Result<Guid> result = await handler.Handle(
                command,
                cancellationToken
            );

            return result.Match(
                id => Results.Ok(new
                {
                    id,
                    message = "Utilisateur mis à jour"
                }),
                CustomResults.Problem
            );
        })
        .WithTags("Keycloak")
        .RequireAuthorization();
    }
}
