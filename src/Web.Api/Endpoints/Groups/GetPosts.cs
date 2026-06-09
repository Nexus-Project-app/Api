using System.Security.Claims;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Groups.GetPosts;
using Application.Posts.Get;
using Microsoft.EntityFrameworkCore;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Groups;

internal sealed class GetPosts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups/{id:guid}/posts", async (
            Guid id,
            int page,
            int pageSize,
            ClaimsPrincipal principal,
            IQueryHandler<GetGroupPostsQuery, PagedList<PostResponse>> handler,
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

            GetGroupPostsQuery query = new(id, page, pageSize, currentUserId);
            var result = await handler.Handle(query, cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Groups)
        .AllowAnonymous();
    }
}
