using Application.Common;
using Application.Posts.GetByUser;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Queries;

public sealed class GetPostsByUserQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetPostsByUserQueryHandler _handler;

    public GetPostsByUserQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetPostsByUserQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedList_WhenUserHasPosts()
    {
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), AuthorId = targetUserId, Created = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), AuthorId = targetUserId, Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), AuthorId = otherUserId, Created = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), Tags = [] }
        };

        var postsDbSet = posts.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsByUserQuery(targetUserId, Page: 1, PageSize: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.ShouldAllBe(p => p.AuthorId == targetUserId);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoPosts()
    {
        var targetUserId = Guid.NewGuid();
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Tags = [] }
        };

        var postsDbSet = posts.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsByUserQuery(targetUserId, Page: 1, PageSize: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedPosts()
    {
        var targetUserId = Guid.NewGuid();
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), AuthorId = targetUserId, Created = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), AuthorId = targetUserId, Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Tags = [], Deleted = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
        };

        var postsDbSet = posts.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsByUserQuery(targetUserId, Page: 1, PageSize: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.ShouldHaveSingleItem();
    }
}
