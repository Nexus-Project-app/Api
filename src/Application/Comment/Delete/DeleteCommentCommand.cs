using Application.Abstractions.Messaging;

namespace Application.Comment.Delete;

public sealed class DeleteCommentCommand : ICommand<Guid>
{
    public Guid CommentId { get; set; }
}
