using Application.Posts.Update;
using FluentValidation.Results;

namespace Application.UnitTests.Posts.Validators;

public sealed class UpdatePostCommandValidatorTests
{
    private readonly UpdatePostCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Title = "New Title",
            Content = "New content.",
            Tags = ["dotnet"]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOptionalFieldsAreNull()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Title = null,
            Content = null,
            Tags = null
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPostIdIsEmpty()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.Empty,
            Title = "Title"
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdatePostCommand.PostId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceeds200Characters()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Title = new string('a', 201)
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdatePostCommand.Title));
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentExceeds10000Characters()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Content = new string('a', 10_001)
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdatePostCommand.Content));
    }

    [Fact]
    public void Validate_ShouldFail_WhenATagIsEmptyInNonNullList()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Tags = [string.Empty]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith("Tags["));
    }

    [Fact]
    public void Validate_ShouldFail_WhenATagExceeds50CharactersInNonNullList()
    {
        UpdatePostCommand command = new()
        {
            PostId = Guid.NewGuid(),
            Tags = [new string('a', 51)]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith("Tags["));
    }
}
