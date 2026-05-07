using Application.Abstractions.Messaging;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(string Email,string KeycloakId,  string FirstName, string LastName)
    : ICommand<Guid>;
