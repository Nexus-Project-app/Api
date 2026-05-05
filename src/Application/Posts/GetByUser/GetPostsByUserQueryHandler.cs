using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.GetByUser;

internal sealed class GetPostsByUserQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPostsByUserQuery, PagedList<PostResponse>>
{
    public async Task<Result<PagedList<PostResponse>>> Handle(GetPostsByUserQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Posts.Post> baseQuery = context.Posts
            .Where(p => p.AuthorId == query.UserId && p.Deleted == null)
            .OrderByDescending(p => p.Created);

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<PostResponse> items = await baseQuery
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
