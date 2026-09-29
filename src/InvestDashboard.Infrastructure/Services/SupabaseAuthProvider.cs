using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestDashboard.Application.DTOs.Auth;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.Services;

public sealed class SupabaseAuthProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<SupabaseAuthProvider> logger) : IAuthProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "token?grant_type=password");
        request.Content = JsonContent.Create(new { email, password }, options: JsonOptions);
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Identity provider rejected a password login with status {StatusCode}.", response.StatusCode);
            if (response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized)
                throw new AuthenticationException("Credenciais inválidas ou e-mail ainda não confirmado.");
            throw new IdentityProviderUnavailableException();
        }

        var result = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>(JsonOptions, cancellationToken);
        return ToAuthResponse(result, "O provedor de identidade retornou uma sessão inválida.");
    }

    public async Task<AuthResponseDto> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "signup");
        request.Content = JsonContent.Create(new { email, password, data = new { name } }, options: JsonOptions);
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Identity provider rejected account registration with status {StatusCode}.", response.StatusCode);
            if ((int)response.StatusCode == 422)
                throw new RegistrationConflictException();
            throw new IdentityProviderUnavailableException();
        }

        var result = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>(JsonOptions, cancellationToken);
        if (result is null)
            throw new InvalidOperationException("O provedor de identidade retornou uma resposta inválida.");

        var user = result.User ?? new SupabaseUser
        {
            Id = result.Id,
            Email = result.Email,
            UserMetadata = result.UserMetadata
        };
        if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("O provedor de identidade retornou uma resposta inválida.");

        if (string.IsNullOrWhiteSpace(result.AccessToken))
        {
            return new AuthResponseDto
            {
                User = MapUser(user, name),
                RequiresEmailConfirmation = true
            };
        }

        if (string.IsNullOrWhiteSpace(result!.AccessToken) || string.IsNullOrWhiteSpace(result.RefreshToken))
            throw new InvalidOperationException("O provedor de identidade retornou uma sessão inválida.");

        return new AuthResponseDto
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            User = MapUser(user, name)
        };
    }

    public async Task RevokeAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "logout?scope=local");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Identity provider could not revoke the exchanged login session (status {StatusCode}).", response.StatusCode);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var baseUrl = configuration["Storage:SupabaseUrl"]?.TrimEnd('/');
        var apiKey = configuration["Storage:SupabaseApiKey"];

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Supabase Auth server configuration is incomplete.");
        }

        var request = new HttpRequestMessage(method, $"{baseUrl}/auth/v1/{path}");
        request.Headers.Add("apikey", apiKey);
        return request;
    }

    private static AuthResponseDto ToAuthResponse(SupabaseTokenResponse? response, string errorMessage)
    {
        if (response?.User is null
            || string.IsNullOrWhiteSpace(response.AccessToken)
            || string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return new AuthResponseDto
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            User = MapUser(response.User)
        };
    }

    private static UserInfoDto MapUser(SupabaseUser user, string? registrationName = null) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.UserMetadata?.Name ?? registrationName ?? string.Empty,
        Role = "user"
    };

    private sealed class SupabaseTokenResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("user_metadata")]
        public SupabaseUserMetadata? UserMetadata { get; init; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; init; } = string.Empty;

        [JsonPropertyName("user")]
        public SupabaseUser? User { get; init; }
    }

    private sealed class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("user_metadata")]
        public SupabaseUserMetadata? UserMetadata { get; init; }
    }

    private sealed class SupabaseUserMetadata
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
