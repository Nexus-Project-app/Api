using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Keycloak;
using Domain.Users;
using Microsoft.Extensions.Configuration;

namespace Application.Keycloak;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public KeycloakService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var url =
            $"{_config["Keycloak:BaseUrl"]}/realms/{_config["Keycloak:Realm"]}/protocol/openid-connect/token";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _config["Keycloak:ClientId"]!,
            ["client_secret"] = _config["Keycloak:ClientSecret"]!,
            ["grant_type"] = "client_credentials"
        });

        using var response = await _http.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        return json!["access_token"]!.ToString()!;
    }

    public async Task<KeycloakUserDto?> GetUserByIdAsync(string userId)
    {
        var token = await GetAdminTokenAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_config["Keycloak:BaseUrl"]}/admin/realms/{_config["Keycloak:Realm"]}/users/{userId}"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
            

        var user = await response.Content.ReadFromJsonAsync<KeycloakUserDto>();

        return user;
    }

    public async Task UpdateUserAsync(Guid userId, UpdateKeycloakUserRequest request)
    {
        var token = await GetAdminTokenAsync();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_config["Keycloak:BaseUrl"]}/admin/realms/{_config["Keycloak:Realm"]}/users/{userId}"
        );

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        httpRequest.Content = JsonContent.Create(new
        {
            firstName = request.FirstName,
            lastName = request.LastName
        });

        var response = await _http.SendAsync(httpRequest);

        response.EnsureSuccessStatusCode();
    }
}
