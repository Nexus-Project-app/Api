using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Comment;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Comment.Update;

internal sealed class UpdateCommentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UpdateCommentCommand>
{
    public async Task<Result> Handle(UpdateCommentCommand command, CancellationToken cancellationToken)
    {
        Domain.Comment.Comment? comment = await context.Comments
            .SingleOrDefaultAsync(c => c.Id == command.CommentId, cancellationToken);

        if (comment is null)
        {
            return Result.Failure(CommentErrors.NotFound(command.CommentId));
        }

        if (comment.Deleted is not null)
        {
            return Result.Failure(CommentErrors.Deleted(command.CommentId));
        }

        if (comment.AuthorId != userContext.UserId)
        {
            return Result.Failure(CommentErrors.Unauthorized());
        }

        comment.Content = command.Content;
        comment.Updated = dateTimeProvider.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
