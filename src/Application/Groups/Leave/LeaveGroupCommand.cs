using Application.Abstractions.Messaging;

namespace Application.Groups.Leave;

public sealed record LeaveGroupCommand(Guid GroupId) : ICommand;
