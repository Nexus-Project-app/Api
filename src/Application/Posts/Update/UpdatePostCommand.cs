using Application.Abstractions.Messaging;

namespace Application.Posts.Update;

public sealed class UpdatePostCommand : ICommand
{
    public Guid PostId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<string>? Tags { get; set; }
}
