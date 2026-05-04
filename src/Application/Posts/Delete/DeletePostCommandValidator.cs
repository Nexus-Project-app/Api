using FluentValidation;

namespace Application.Posts.Delete;

public class DeletePostCommandValidator : AbstractValidator<DeletePostCommand>
{

    public DeletePostCommandValidator()
    {
        RuleFor(p => p.Id).NotNull().WithMessage("Id cannot be null");
    }
}
