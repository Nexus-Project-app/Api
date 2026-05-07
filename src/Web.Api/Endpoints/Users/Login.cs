using System.Security.Claims;
using Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authorization;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me", (ClaimsPrincipal claimsPrincipal, IUserContext userContext) =>
        {
            // IUserContext.UserId fait une recherche en base par KeycloakId,
            // ce qui garantit de retourner le vrai User.Id (même pour les anciens
            // comptes dont l'Id ne correspond pas au sub Keycloak).
            var roles = claimsPrincipal
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Results.Ok(new
            {
                id       = userContext.UserId.ToString(),
                email    = claimsPrincipal.FindFirstValue(ClaimTypes.Email),
                username = claimsPrincipal.FindFirstValue(ClaimTypes.Name),
                roles,
            });
        })
        .RequireAuthorization()
        .WithTags(Tags.Users);
    }
}
