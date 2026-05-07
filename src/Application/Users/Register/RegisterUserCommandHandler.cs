using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(IApplicationDbContext context)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Idempotence : si l'utilisateur existe déjà via son KeycloakId, on retourne son Id
        var existing = await context.Users
            .FirstOrDefaultAsync(u => u.KeycloakId == command.KeycloakId, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        // L'Id local DOIT être le sub Keycloak pour que Post.AuthorId (FK) corresponde
        if (!Guid.TryParse(command.KeycloakId, out var userId))
        {
            return Result.Failure<Guid>(Error.Failure("Users.InvalidKeycloakId", "Le KeycloakId fourni n'est pas un GUID valide."));
        }

        var user = new User
        {
            Id = userId,
            KeycloakId = command.KeycloakId,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        context.Users.Add(user);

        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
