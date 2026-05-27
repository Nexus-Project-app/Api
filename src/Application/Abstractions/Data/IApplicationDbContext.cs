using Domain.Likes;
using Domain.Posts;
using Domain.Tags;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    DbSet<Post> Posts { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Domain.Comment.Comment> Comments { get; }
    DbSet<Like> Likes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
