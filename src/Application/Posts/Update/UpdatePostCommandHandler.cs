using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tags;
using Domain.Posts;
using Domain.Tags;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.Update;

internal sealed class UpdatePostCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext,
    ITagService tagService)
    : ICommandHandler<UpdatePostCommand>
{
    public async Task<Result> Handle(UpdatePostCommand command, CancellationToken cancellationToken)
    {
        var post = await context.Posts
            .Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Id == command.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure(PostErrors.NotFound(command.PostId));
        }

        if (post.Deleted is not null)
        {
            return Result.Failure(PostErrors.Deleted(command.PostId));
        }

        if (post.AuthorId != userContext.UserId)
        {
            return Result.Failure(PostErrors.Unauthorized());
        }

        if (command.Title is not null)
        {
            post.Title = command.Title;
        }

        if (command.Content is not null)
        {
            post.Content = command.Content;
        }

        if (command.Tags is not null)
        {
            var tags = await tagService.ResolveTagsAsync(command.Tags, cancellationToken);
            post.Tags = tags;
        }

        post.Updated = dateTimeProvider.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
