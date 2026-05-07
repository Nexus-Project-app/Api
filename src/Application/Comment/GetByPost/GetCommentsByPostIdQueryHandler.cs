using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Comment;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Comment.GetByPost;

internal sealed class GetCommentsByPostIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetCommentsByPostIdQuery, PagedList<CommentResponse>>
{
    public async Task<Result<PagedList<CommentResponse>>> Handle(GetCommentsByPostIdQuery query, CancellationToken cancellationToken)
    {
        var postExists = await context.Posts
            .AnyAsync(p => p.Id == query.PostId && p.Deleted == null, cancellationToken);

        if (!postExists)
        {
            return Result.Failure<PagedList<CommentResponse>>(PostErrors.NotFound(query.PostId));
        }

        var baseQuery = context.Comments
            .Where(c => c.PostId == query.PostId && c.Deleted == null)
            .OrderByDescending(c => c.Created);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                Content = c.Content,
                Created = c.Created,
                Updated = c.Updated
            })
            .ToListAsync(cancellationToken);

        return new PagedList<CommentResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}