using Application.Posts.GetById;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Queries;

public sealed class GetPostByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IUserContext _userContext;
    private readonly GetPostByIdQueryHandler _handler;

    public GetPostByIdQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new GetPostByIdQueryHandler(_context, _userContext);
    }

    [Fact]
    public async Task Handle_ShouldReturnPostResponse_WhenPostBelongsToUser()
    {
        var authorId = Guid.NewGuid();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Test Title",
            Content = "Test Content",
            AuthorId = authorId,
            Created = created,
            Tags = [new Tag { Id = Guid.NewGuid(), Name = "dotnet" }]
        };

        _userContext.UserId.Returns(authorId);

        var postsDbSet = new List<Post> { post }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostByIdQuery(post.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(post.Id);
        result.Value.AuthorId.ShouldBe(authorId);
        result.Value.Title.ShouldBe(post.Title);
        result.Value.Content.ShouldBe(post.Content);
        result.Value.Created.ShouldBe(created);
        result.Value.Tags.ShouldHaveSingleItem();
        result.Value.Tags[0].ShouldBe("dotnet");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        _userContext.UserId.Returns(Guid.NewGuid());

        var postsDbSet = new List<Post>().AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPostBelongsToAnotherUser()
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Tags = []
        };

        _userContext.UserId.Returns(Guid.NewGuid());

        var postsDbSet = new List<Post> { post }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostByIdQuery(post.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPostIsDeleted()
    {
        var authorId = Guid.NewGuid();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Deleted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Tags = []
        };

        _userContext.UserId.Returns(authorId);

        var postsDbSet = new List<Post> { post }.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostByIdQuery(post.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
