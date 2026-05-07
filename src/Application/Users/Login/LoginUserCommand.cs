using Application.Abstractions.Messaging;

namespace Application.Users.Login;

public sealed record LoginUserCommand(Guid id) : ICommand<string>;
