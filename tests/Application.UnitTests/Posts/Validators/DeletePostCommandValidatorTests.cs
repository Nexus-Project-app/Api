using Application.Posts.Delete;
using FluentValidation.Results;

namespace Application.UnitTests.Posts.Validators;

public sealed class DeletePostCommandValidatorTests
{
    private readonly DeletePostCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenIdIsProvided()
    {
        DeletePostCommand command = new() { Id = Guid.NewGuid() };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsDefault()
    {
        DeletePostCommand command = new() { Id = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeletePostCommand.Id));
    }
}
