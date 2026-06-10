using Application.Abstractions.Messaging;

namespace Application.Groups.KickMember;

public sealed record KickMemberCommand(Guid GroupId, Guid UserId) : ICommand;
