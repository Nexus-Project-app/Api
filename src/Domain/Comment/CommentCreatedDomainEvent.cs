using SharedKernel;

namespace Domain.Comment;

public sealed record CommentCreatedDomainEvent (Guid CommentId)  : IDomainEvent;
