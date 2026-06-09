using Domain.Posts;
using Domain.Users;
using SharedKernel;

namespace Domain.Likes;

public class Like : Entity
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTime Created { get; set; }
}
