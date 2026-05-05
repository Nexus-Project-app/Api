using Domain.Posts;
using SharedKernel;

namespace Domain.Tags;

public class Tag : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Post> Posts { get; set; } = [];
}
