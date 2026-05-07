using Application.Abstractions.Messaging;
using Application.Users.Register;
using Microsoft.AspNetCore.Authorization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Register : IEndpoint
{
    public sealed record Request(string Email, string KeycloakId, string FirstName, string LastName);
    
    [AllowAnonymous]
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/register", async (
            Request request,
            ICommandHandler<RegisterUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
                {
                    var command = new RegisterUserCommand(
                        request.Email,
                        request.KeycloakId,
                        request.FirstName,
                        request.LastName
                    );

                    Result<Guid> result = await handler.Handle(command, cancellationToken);

                    return result.Match(
                        id => Results.Ok(new { id, message = "Utilisateur créé ou existant" }),
                        CustomResults.Problem
                    );
                })
        .WithTags(Tags.Users);
    }
}
