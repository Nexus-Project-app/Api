using Application.Abstractions.Messaging;

namespace Application.Posts.Create;

public sealed class CreatePostCommand : ICommand<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public Guid? GroupId { get; set; }
}
