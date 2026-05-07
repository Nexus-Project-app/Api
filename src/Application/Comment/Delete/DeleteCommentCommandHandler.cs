using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Comment;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Comment.Delete;

internal sealed class DeleteCommentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<DeleteCommentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteCommentCommand command, CancellationToken cancellationToken)
    {
        Domain.Comment.Comment? comment = await context.Comments
            .SingleOrDefaultAsync(c => c.Id == command.CommentId, cancellationToken);

        if (comment is null)
        {
            return Result.Failure<Guid>(CommentErrors.NotFound(command.CommentId));
        }

        if (comment.AuthorId != userContext.UserId)
        {
            return Result.Failure<Guid>(CommentErrors.Unauthorized());
        }

        if (comment.Deleted is not null)
        {
            return Result.Failure<Guid>(CommentErrors.Deleted(command.CommentId));
        }

        comment.Deleted = dateTimeProvider.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
