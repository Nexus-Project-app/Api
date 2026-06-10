using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using DomainComment = Domain.Comment.Comment;
using DomainCommentEvent = Domain.Comment.CommentCreatedDomainEvent;

namespace Application.Comment.Add;

internal sealed class AddCommentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<AddCommentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        Post? post = await context.Posts
            .SingleOrDefaultAsync(p => p.Id == command.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure<Guid>(PostErrors.NotFound(command.PostId));
        }

        if (post.Deleted is not null)
        {
            return Result.Failure<Guid>(PostErrors.Deleted(command.PostId));
        }

        var comment = new DomainComment
        {
            Id = Guid.NewGuid(),
            PostId = command.PostId,
            AuthorId = userContext.UserId,
            Content = command.Content,
            Created = dateTimeProvider.UtcNow,
        };

        comment.Raise(new DomainCommentEvent(comment.Id));

        context.Comments.Add(comment);
        await context.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
