using Domain.Posts;
using Domain.Users;
using SharedKernel;

namespace Domain.Comment;

public class Comment : Entity
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Deleted { get; set; }
}
