using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Users;

public sealed record KeycloakUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerified
);

public sealed record UpdateKeycloakUserRequest(
    string FirstName,
    string LastName
);
