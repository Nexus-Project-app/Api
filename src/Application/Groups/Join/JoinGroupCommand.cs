using Application.Abstractions.Messaging;

namespace Application.Groups.Join;

public sealed record JoinGroupCommand(Guid GroupId) : ICommand;
