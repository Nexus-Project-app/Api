using Application.Abstractions.Messaging;

namespace Application.Groups.GetJoinRequests;

public sealed record GetJoinRequestsQuery(Guid GroupId, Guid CurrentUserId) : IQuery<List<JoinRequestResponse>>;
