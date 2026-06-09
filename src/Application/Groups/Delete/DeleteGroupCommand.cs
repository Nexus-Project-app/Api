using Application.Abstractions.Messaging;

namespace Application.Groups.Delete;

public sealed record DeleteGroupCommand(Guid GroupId) : ICommand;
