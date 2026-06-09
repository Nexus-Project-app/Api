using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Posts.Get;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.GetPosts;

internal sealed class GetGroupPostsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGroupPostsQuery, PagedList<PostResponse>>
{
    public async Task<Result<PagedList<PostResponse>>> Handle(GetGroupPostsQuery query, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<PagedList<PostResponse>>(GroupErrors.NotFound);
        }

        if (group.Visibility == GroupVisibility.Private)
        {
            var isMember = query.CurrentUserId.HasValue &&
                await context.GroupMembers.AnyAsync(
                    m => m.GroupId == query.GroupId && m.UserId == query.CurrentUserId.Value,
                    cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedList<PostResponse>>(GroupErrors.NotMember);
            }
        }

        var baseQuery = context.Posts
            .Where(p => p.Deleted == null && p.GroupId == query.GroupId)
            .OrderByDescending(p => p.Created);

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
                IsLikedByCurrentUser = query.CurrentUserId.HasValue &&
                    context.Likes.Any(l => l.PostId == p.Id && l.AuthorId == query.CurrentUserId.Value),
                GroupId = p.GroupId,
                GroupName = group.Name
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
