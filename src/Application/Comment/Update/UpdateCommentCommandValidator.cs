using FluentValidation;

namespace Application.Comment.Update;

internal sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(c => c.CommentId).NotEmpty();
        RuleFor(c => c.Content).NotEmpty().MaximumLength(2000);
    }
}
