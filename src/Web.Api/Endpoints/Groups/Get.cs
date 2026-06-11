using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Groups.Get;
using Microsoft.EntityFrameworkCore;
using Application.Abstractions.Data;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups", async (
            int page,
            int pageSize,
            ClaimsPrincipal principal,
            IQueryHandler<GetGroupsQuery, PagedList<GroupSummaryResponse>> handler,
            IApplicationDbContext context,
            CancellationToken cancellationToken,
            string? search = null) =>
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

            GetGroupsQuery query = new(page, pageSize, currentUserId, search);
            var result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .AllowAnonymous();
    }
}
