using Application.Abstractions.Messaging;

namespace Application.Groups.GetById;

public sealed record GetGroupByIdQuery(Guid GroupId, Guid? CurrentUserId = null) : IQuery<GroupDetailResponse>;
