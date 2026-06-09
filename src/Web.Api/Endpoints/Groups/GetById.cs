using System.Security.Claims;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Groups.GetById;
using Microsoft.EntityFrameworkCore;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetGroupByIdQuery, GroupDetailResponse> handler,
            IApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            Guid? currentUserId = null;
            if (principal.Identity?.IsAuthenticated == true)
            {
                var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(sub))
                {
                    currentUserId = await context.Users
                        .Where(u => u.KeycloakId == sub)
                        .Select(u => (Guid?)u.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }

            GetGroupByIdQuery query = new(id, currentUserId);
            var result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .AllowAnonymous();
    }
}
