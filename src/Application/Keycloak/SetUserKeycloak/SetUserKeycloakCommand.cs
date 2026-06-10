using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Keycloak;
using Application.Abstractions.Messaging;
using Application.Users.GetByEmail;
using Domain.Users;
using SharedKernel;

namespace Application.Keycloak.SetUserKeycloak;


public sealed class SetUserKeycloakCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
