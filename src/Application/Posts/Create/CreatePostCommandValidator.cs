using FluentValidation;

namespace Application.Posts.Create;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Content).NotEmpty().MaximumLength(10_000);
        RuleFor(c => c.Tags).NotNull();
        RuleForEach(c => c.Tags).NotEmpty().MaximumLength(50);
    }
}
