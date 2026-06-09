using Application.Abstractions.Messaging;
using Domain.Groups;

namespace Application.Groups.UpdateMemberRole;

public sealed record UpdateMemberRoleCommand(Guid GroupId, Guid UserId, GroupMemberRole Role) : ICommand;
