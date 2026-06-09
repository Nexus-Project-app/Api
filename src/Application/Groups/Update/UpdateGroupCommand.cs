using Application.Abstractions.Messaging;
using Domain.Groups;

namespace Application.Groups.Update;

public sealed record UpdateGroupCommand(
    Guid GroupId,
    string Name,
    string Description,
    GroupVisibility Visibility) : ICommand;
