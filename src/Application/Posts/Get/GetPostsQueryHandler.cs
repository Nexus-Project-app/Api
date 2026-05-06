using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.Get;

internal sealed class GetPostsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPostsQuery, PagedList<PostResponse>>
{
    public async Task<Result<PagedList<PostResponse>>> Handle(GetPostsQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = context.Posts
            .Where(p => p.Deleted == null)
            .OrderByDescending(p => p.Created);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                Title = p.Title,
                Content = p.Content,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Created = p.Created,
                Updated = p.Updated
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
