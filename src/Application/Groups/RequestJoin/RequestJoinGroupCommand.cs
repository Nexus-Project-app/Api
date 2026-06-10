using Application.Abstractions.Messaging;

namespace Application.Groups.RequestJoin;

public sealed record RequestJoinGroupCommand(Guid GroupId) : ICommand;
