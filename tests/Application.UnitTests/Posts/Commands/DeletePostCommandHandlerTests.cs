using Application.Posts.Delete;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Commands;

public sealed class DeletePostCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly DeletePostCommandHandler _handler;

    public DeletePostCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new DeletePostCommandHandler(_context, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Handle_ShouldReturnPostId_WhenPostExistsAndNotDeleted()
    {
        var authorId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existingPost = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Tags = []
        };

        DeletePostCommand command = new() { Id = existingPost.Id };

        _dateTimeProvider.UtcNow.Returns(now);
        _userContext.UserId.Returns(authorId);

        DbSet<Post> postsDbSet = new List<Post> { existingPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(existingPost.Id);
        existingPost.Deleted.ShouldBe(now);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        DeletePostCommand command = new() { Id = Guid.NewGuid() };

        DbSet<Post> postsDbSet = new List<Post>().AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

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
            Deleted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Tags = []
        };

        DeletePostCommand command = new() { Id = deletedPost.Id };

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        _userContext.UserId.Returns(authorId);

        DbSet<Post> postsDbSet = new List<Post> { deletedPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthor()
    {
        var existingPost = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Tags = []
        };

        DeletePostCommand command = new() { Id = existingPost.Id };

        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Post> postsDbSet = new List<Post> { existingPost }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Failure);
        result.Error.Code.ShouldBe("Posts.Unauthorized");
    }
}
