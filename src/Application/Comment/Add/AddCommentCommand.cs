using Application.Abstractions.Messaging;

namespace Application.Comment.Add;

public sealed record AddCommentCommand(Guid PostId, string Content) : ICommand<Guid>;
