using Application.Abstractions.Messaging;
using Domain.Groups;

namespace Application.Groups.Create;

public sealed record CreateGroupCommand(
    string Name,
    string Description,
    GroupVisibility Visibility) : ICommand<Guid>;
