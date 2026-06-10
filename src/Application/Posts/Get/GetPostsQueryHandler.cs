using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

#pragma warning disable CA1308, CA1862

namespace Application.Posts.Get;

internal sealed class GetPostsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPostsQuery, PagedList<PostResponse>>
{
    public async Task<Result<PagedList<PostResponse>>> Handle(GetPostsQuery query, CancellationToken cancellationToken)
    {
        Guid? currentUserId = null;
        if (!string.IsNullOrEmpty(query.CurrentUserSub))
        {
            currentUserId = await context.Users
                .Where(u => u.KeycloakId == query.CurrentUserSub)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var filtered = context.Posts
            .Where(p => p.Deleted == null
                && (p.GroupId == null
                    || p.Group!.DeletedAt == null
                        && (p.Group.Visibility == GroupVisibility.Public
                            || currentUserId.HasValue && context.GroupMembers
                                .Any(m => m.GroupId == p.GroupId && m.UserId == currentUserId.Value))));

        if (!string.IsNullOrEmpty(query.Search))
        {
            var searchLower = query.Search.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Title.ToLowerInvariant().Contains(searchLower) ||
                p.Content.ToLowerInvariant().Contains(searchLower));
        }

        if (!string.IsNullOrEmpty(query.Tag))
        {
            var tagLower = query.Tag.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Tags.Any(t => t.Name.ToLowerInvariant().Contains(tagLower)));
        }

        if (!string.IsNullOrEmpty(query.Author))
        {
            var authorLower = query.Author.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Author.FirstName.ToLowerInvariant().Contains(authorLower) ||
                p.Author.LastName.ToLowerInvariant().Contains(authorLower));
        }

        var baseQuery = filtered.OrderByDescending(p => p.Created);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorName = (p.Author.FirstName + " " + p.Author.LastName).Trim(),
                Title = p.Title,
                Content = p.Content,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Created = p.Created,
                Updated = p.Updated,
                LikeCount = context.Likes.Count(l => l.PostId == p.Id),
                CommentCount = context.Comments.Count(c => c.PostId == p.Id && c.Deleted == null),
                IsLikedByCurrentUser = currentUserId.HasValue &&
                    context.Likes.Any(l => l.PostId == p.Id && l.AuthorId == currentUserId.Value),
                GroupId = p.GroupId,
                GroupName = p.Group != null ? p.Group.Name : null
            })
            .ToListAsync(cancellationToken);

        return new PagedList<PostResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}
