using Application.Abstractions.Messaging;

namespace Application.Comment.Update;

public sealed class UpdateCommentCommand : ICommand
{
    public Guid CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}
