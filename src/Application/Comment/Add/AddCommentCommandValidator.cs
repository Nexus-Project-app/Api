using FluentValidation;

namespace Application.Comment.Add;

internal sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(c => c.PostId).NotEmpty();
        RuleFor(c => c.Content).NotEmpty().MaximumLength(2000);
    }
}
