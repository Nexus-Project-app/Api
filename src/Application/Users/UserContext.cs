using System;
using System.Collections.Generic;
using System.Text;
using Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace Application.Users;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _http;

    public UserContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid UserId =>
        Guid.Parse(_http.HttpContext!.User.FindFirst("sub")!.Value);

    public string? Email =>
        _http.HttpContext!.User.FindFirst("email")?.Value;
}
