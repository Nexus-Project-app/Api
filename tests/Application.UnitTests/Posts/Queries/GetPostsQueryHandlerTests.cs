using Application.Common;
using Application.Posts.Get;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Posts.Queries;

public sealed class GetPostsQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetPostsQueryHandler _handler;

    public GetPostsQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetPostsQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedList_WhenPostsExist()
    {
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Created = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), Created = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Tags = [] }
        };

        DbSet<Post> postsDbSet = posts.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsQuery(Page: 1, PageSize: 2);

        Result<PagedList<PostResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(3);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.Items[0].Created.ShouldBe(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        result.Value.Items[1].Created.ShouldBe(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoPostsExist()
    {
        DbSet<Post> postsDbSet = new List<Post>().AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsQuery(Page: 1, PageSize: 10);

        Result<PagedList<PostResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedPosts()
    {
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Created = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Tags = [] },
            new Post { Id = Guid.NewGuid(), Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Tags = [], Deleted = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
        };

        DbSet<Post> postsDbSet = posts.AsQueryable().BuildMockDbSet();
        _context.Posts.Returns(postsDbSet);

        var query = new GetPostsQuery(Page: 1, PageSize: 10);

        Result<PagedList<PostResponse>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.ShouldHaveSingleItem();
    }
}
