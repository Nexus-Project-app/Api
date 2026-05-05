using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.GetById;

internal sealed class GetPostByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetPostByIdQuery, PostResponse>
{
    public async Task<Result<PostResponse>> Handle(GetPostByIdQuery query, CancellationToken cancellationToken)
    {
        var post = await context.Posts
            .Where(p => p.Id == query.PostId && p.AuthorId == userContext.UserId && p.Deleted == null)
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
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
        {
            return Result.Failure<PostResponse>(PostErrors.NotFound(query.PostId));
        }

        return post;
    }
}
