using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tags;
using Domain.Groups;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.Create;

internal sealed class CreatePostCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext,
    ITagService tagService)
    : ICommandHandler<CreatePostCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        if (command.GroupId.HasValue)
        {
            var isMember = await context.GroupMembers
                .AnyAsync(m => m.GroupId == command.GroupId.Value && m.UserId == userContext.UserId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<Guid>(GroupErrors.NotMember);
            }
        }

        var now = dateTimeProvider.UtcNow;

        var tags = await tagService.ResolveTagsAsync(command.Tags, cancellationToken);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Content = command.Content,
            AuthorId = userContext.UserId,
            Tags = tags,
            Created = now,
            Updated = now,            
            GroupId = command.GroupId
        };

        post.Raise(new PostCreatedDomainEvent(post.Id));

        context.Posts.Add(post);
        await context.SaveChangesAsync(cancellationToken);

        return post.Id;
    }
}
