using Application.Posts.Create;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Commands;

public sealed class CreatePostCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ITagService _tagService;
    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _tagService = Substitute.For<ITagService>();

        _handler = new CreatePostCommandHandler(
            _context,
            _dateTimeProvider,
            _userContext,
            _tagService);
    }

    [Fact]
    public async Task Handle_ShouldReturnPostId_WhenCommandIsValid()
    {
        var authorId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tagNames = new List<string> { "dotnet", "csharp" };
        var resolvedTags = new List<Tag>
        {
            new Tag { Id = Guid.NewGuid(), Name = "dotnet" },
            new Tag { Id = Guid.NewGuid(), Name = "csharp" }
        };

        CreatePostCommand command = new()
        {
            Title = "Test Title",
            Content = "Test Content",
            Tags = tagNames
        };

        _dateTimeProvider.UtcNow.Returns(now);
        _userContext.UserId.Returns(authorId);
        _tagService.ResolveTagsAsync(tagNames, Arg.Any<CancellationToken>())
                   .Returns(resolvedTags);

        var postsDbSet = new List<Post>().AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        Post? capturedPost = null;
        postsDbSet.When(x => x.Add(Arg.Any<Post>()))
                  .Do(ci => capturedPost = ci.Arg<Post>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        postsDbSet.Received(1).Add(Arg.Is<Post>(p =>
            p.Title == command.Title &&
            p.Content == command.Content &&
            p.AuthorId == authorId &&
            p.Created == now));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        capturedPost.ShouldNotBeNull();
        capturedPost!.DomainEvents.ShouldHaveSingleItem();
        capturedPost.DomainEvents[0].ShouldBeOfType<PostCreatedDomainEvent>();
        ((PostCreatedDomainEvent)capturedPost.DomainEvents[0]).PostId.ShouldBe(capturedPost.Id);
    }
}
