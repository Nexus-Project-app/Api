using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tags;
using Domain.Posts;
using Domain.Tags;
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
        DateTime now = dateTimeProvider.UtcNow;

        List<Tag> tags = await tagService.ResolveTagsAsync(command.Tags, cancellationToken);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Content = command.Content,
            AuthorId = userContext.UserId,
            Tags = tags,
            Created = now,
        };

        post.Raise(new PostCreatedDomainEvent(post.Id));

        context.Posts.Add(post);
        await context.SaveChangesAsync(cancellationToken);

        return post.Id;
    }
}
