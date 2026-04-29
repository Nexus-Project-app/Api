using SharedKernel;

namespace Domain.Posts;

public sealed record PostCreatedDomainEvent(Guid PostId) : IDomainEvent;
