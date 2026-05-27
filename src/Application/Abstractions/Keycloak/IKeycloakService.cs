using System;
using System.Collections.Generic;
using System.Text;
using Domain.Users;

namespace Application.Abstractions.Keycloak;

public interface IKeycloakService
{
    Task<KeycloakUserDto?> GetUserByIdAsync(string userId);
    Task UpdateUserAsync(Guid userId, UpdateKeycloakUserRequest request);
}
