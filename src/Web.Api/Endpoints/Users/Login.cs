using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Users.Login;
using Microsoft.AspNetCore.Authorization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    [Authorize]
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new
            {
                id = user.FindFirst("sub")?.Value,
                email = user.FindFirst("email")?.Value,
                username = user.FindFirst("name")?.Value,
                roles = user.FindAll("realm_access").Select(x => x.Value)
            });
        })
        .WithTags(Tags.Users);
    }
}
