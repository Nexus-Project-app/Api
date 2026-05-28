namespace Application.Posts.Get;

public sealed class PostResponse
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}