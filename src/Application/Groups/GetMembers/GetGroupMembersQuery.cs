using Application.Abstractions.Messaging;

namespace Application.Groups.GetMembers;

public sealed record GetGroupMembersQuery(Guid GroupId, Guid? CurrentUserId = null) : IQuery<List<GroupMemberResponse>>;
