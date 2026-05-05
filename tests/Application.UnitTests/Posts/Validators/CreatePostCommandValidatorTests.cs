using Application.Posts.Create;
using FluentValidation.Results;

namespace Application.UnitTests.Posts.Validators;

public sealed class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = "Valid content.",
            Tags = ["dotnet"]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        CreatePostCommand command = new()
        {
            Title = string.Empty,
            Content = "Valid content.",
            Tags = []
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePostCommand.Title));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceeds200Characters()
    {
        CreatePostCommand command = new()
        {
            Title = new string('a', 201),
            Content = "Valid content.",
            Tags = []
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePostCommand.Title));
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentIsEmpty()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = string.Empty,
            Tags = []
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePostCommand.Content));
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentExceeds10000Characters()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = new string('a', 10_001),
            Tags = []
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePostCommand.Content));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsIsNull()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = "Valid content.",
            Tags = null!
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePostCommand.Tags));
    }

    [Fact]
    public void Validate_ShouldFail_WhenATagIsEmpty()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = "Valid content.",
            Tags = [string.Empty]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith("Tags["));
    }

    [Fact]
    public void Validate_ShouldFail_WhenATagExceeds50Characters()
    {
        CreatePostCommand command = new()
        {
            Title = "Valid Title",
            Content = "Valid content.",
            Tags = [new string('a', 51)]
        };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith("Tags["));
    }
}
