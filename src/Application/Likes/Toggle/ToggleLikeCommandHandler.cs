using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Likes;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Likes.Toggle;

internal sealed class ToggleLikeCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ToggleLikeCommand, ToggleLikeResponse>
{
    public async Task<Result<ToggleLikeResponse>> Handle(ToggleLikeCommand command, CancellationToken cancellationToken)
    {
        var postExists = await context.Posts
            .AnyAsync(p => p.Id == command.PostId && p.Deleted == null, cancellationToken);

        if (!postExists)
        {
            return Result.Failure<ToggleLikeResponse>(PostErrors.NotFound(command.PostId));
        }

        var currentUserId = userContext.UserId;

        var existingLike = await context.Likes
            .FirstOrDefaultAsync(l => l.PostId == command.PostId && l.AuthorId == currentUserId, cancellationToken);

        bool isLiked;

        if (existingLike is not null)
        {
            // Unlike
            context.Likes.Remove(existingLike);
            isLiked = false;
        }
        else
        {
            // Like
            var like = new Like
            {
                Id = Guid.NewGuid(),
                PostId = command.PostId,
                AuthorId = currentUserId,
                Created = dateTimeProvider.UtcNow,
            };
            context.Likes.Add(like);
            isLiked = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        var likeCount = await context.Likes
            .CountAsync(l => l.PostId == command.PostId, cancellationToken);

        return new ToggleLikeResponse
        {
            IsLiked = isLiked,
            LikeCount = likeCount,
        };
    }
}
