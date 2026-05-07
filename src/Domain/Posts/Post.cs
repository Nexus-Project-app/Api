using Domain.Tags;
using Domain.Users;
using SharedKernel;

namespace Domain.Posts;

public class Post : Entity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Deleted { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public List<Tag> Tags { get; set; } = [];
    public List<Comment.Comment> Comments { get; set; } = [];
}
