using FluentValidation;

namespace Application.Posts.Update;

internal sealed class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(c => c.PostId).NotEmpty();
        RuleFor(c => c.Title).MaximumLength(200).When(c => c.Title is not null);
        RuleFor(c => c.Content).MaximumLength(10_000).When(c => c.Content is not null);
        RuleForEach(c => c.Tags).NotEmpty().MaximumLength(50).When(c => c.Tags is not null);
    }
}
