using System.Security.Claims;
using Application.Abstractions.Data;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Middleware;

/// <summary>
/// Middleware qui garantit que l'utilisateur authentifié possède un enregistrement
/// en base de données. Si ce n'est pas le cas, il est créé automatiquement à partir
/// des claims du token Keycloak.
///
/// Doit être placé APRÈS UseAuthentication() et UseAuthorization().
/// </summary>
public sealed class EnsureCurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(sub))
            {
                // On vérifie par KeycloakId : cela fonctionne même si l'enregistrement
                // a été créé avant le fix (User.Id != sub).
                var exists = await dbContext.Users
                    .AnyAsync(u => u.KeycloakId == sub);

                if (!exists)
                {
                    var email     = context.User.FindFirstValue(ClaimTypes.Email)     ?? $"{sub}@unknown.local";
                    var firstName = context.User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
                    var lastName  = context.User.FindFirstValue(ClaimTypes.Surname)   ?? string.Empty;

                    // Pour les nouveaux utilisateurs, User.Id = sub Keycloak (cohérence avec RegisterUserCommandHandler)
                    var userId = Guid.TryParse(sub, out var g) ? g : Guid.NewGuid();

                    var user = new User
                    {
                        Id         = userId,
                        KeycloakId = sub,
                        Email      = email,
                        FirstName  = firstName,
                        LastName   = lastName,
                    };

                    await dbContext.Users.AddAsync(user);

                    try
                    {
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Race condition ou conflit : on laisse passer, UserContext gérera.
                        var logger = context.RequestServices
                            .GetRequiredService<ILogger<EnsureCurrentUserMiddleware>>();
                        logger.LogWarning(ex, "EnsureCurrentUser : impossible de créer l'utilisateur sub={Sub}", sub);
                    }
                }
            }
        }

        await next(context);
    }
}
