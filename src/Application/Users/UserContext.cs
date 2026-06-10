using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Microsoft.AspNetCore.Http;

namespace Application.Users;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _http;
    private readonly IApplicationDbContext _dbContext;

    public UserContext(IHttpContextAccessor http, IApplicationDbContext dbContext)
    {
        _http = http;
        _dbContext = dbContext;
    }

    public Guid UserId
    {
        get
        {
            // .NET 8+ JsonWebTokenHandler keeps "sub" as-is; fallback handles both cases
            var sub = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _http.HttpContext?.User.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User is not authenticated.");

            // Look up by KeycloakId so it works regardless of whether User.Id == sub
            var user = _dbContext.Users
                .FirstOrDefault(u => u.KeycloakId == sub);

            return user?.Id ?? throw new InvalidOperationException($"No local user found for Keycloak sub '{sub}'.");
        }
    }

    public string? Email =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
}
