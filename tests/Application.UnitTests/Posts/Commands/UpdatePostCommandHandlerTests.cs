using Application.Posts.Update;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Commands;

public sealed class UpdatePostCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ITagService _tagService;
    private readonly UpdatePostCommandHandler _handler;

    public UpdatePostCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _tagService = Substitute.For<ITagService>();

        _handler = new UpdatePostCommandHandler(
            _context,
            _dateTimeProvider,
            _userContext,
            _tagService);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenPostExistsAndUserIsAuthor()
    {
        var authorId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existingPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            Content = "Old Content",
            AuthorId = authorId,
            Tags = []
        };

        UpdatePostCommand command = new()
        {
            PostId = existingPost.Id,
            Title = "New Title",
            Content = "New Content"
        };

        _userContext.UserId.Returns(authorId);
        _dateTimeProvider.UtcNow.Returns(now);

        DbSet<Post> postsDbSet = new List<Post> { existingPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        existingPost.Title.ShouldBe("New Title");
        existingPost.Content.ShouldBe("New Content");
        existingPost.Updated.ShouldBe(now);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        UpdatePostCommand command = new() { PostId = Guid.NewGuid(), Title = "Title" };

        DbSet<Post> postsDbSet = new List<Post>().AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPostIsAlreadyDeleted()
    {
        var authorId = Guid.NewGuid();
        var deletedPost = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Deleted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        UpdatePostCommand command = new() { PostId = deletedPost.Id, Title = "Title" };

        _userContext.UserId.Returns(authorId);

        DbSet<Post> postsDbSet = new List<Post> { deletedPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthor()
    {
        var existingPost = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = Guid.NewGuid()
        };

        UpdatePostCommand command = new() { PostId = existingPost.Id, Title = "Title" };

        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Post> postsDbSet = new List<Post> { existingPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Failure);
        result.Error.Code.ShouldBe("Posts.Unauthorized");
    }
}
