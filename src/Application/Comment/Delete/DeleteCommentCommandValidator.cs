using FluentValidation;

namespace Application.Comment.Delete;

internal sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
        RuleFor(c => c.CommentId).NotEmpty();
    }
}
