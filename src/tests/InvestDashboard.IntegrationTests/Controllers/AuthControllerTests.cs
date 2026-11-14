using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InvestDashboard.Application.DTOs.Auth;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false
    });
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Login_IssuesApiTokenAndKeepsRefreshTokenInHttpOnlyCookie()
    {
        var response = await LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadApiResponseAsync<AuthSessionResponse>(response);
        body!.Dados!.TokenAcesso.Should().NotBeNullOrWhiteSpace();
        body.Dados.ExpiraEmSegundos.Should().Be(900);
        body.Dados.Usuario.Email.Should().Be(FakeAuthProvider.TestEmail);
        response.Headers.GetValues("Set-Cookie").Should().ContainSingle(value =>
            value.Contains("refresh_token=", StringComparison.Ordinal)
            && value.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && value.Contains("path=/api/v1/auth", StringComparison.OrdinalIgnoreCase)
            && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)
            && value.Contains("max-age=604800", StringComparison.OrdinalIgnoreCase)
            && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));

        response.Content.Headers.ContentLength.Should().BeGreaterThan(0);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("RefreshToken");
        content.Should().NotContain("upstream-session-token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "wrong@example.com",
            senha = "WrongPass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenIdentityProviderIsUnavailable_ReturnsServiceUnavailableWithoutProviderDetails()
    {
        using var unavailableFactory = CustomWebApplicationFactory.CreateWithAuthProvider(() => new UnavailableAuthProvider());
        using var client = unavailableFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "investor@example.com",
            senha = "test-password-that-must-not-be-echoed"
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().NotContain("SUPABASE_TEST_FAILURE_MARKER");
        responseBody.Should().NotContain("test-password-that-must-not-be-echoed");
    }

    [Fact]
    public async Task Login_RevokesUpstreamSessionAndReturnsOnlyTheApiSession()
    {
        var authProvider = new TrackingAuthProvider();
        using var trackingFactory = CustomWebApplicationFactory.CreateWithAuthProvider(() => authProvider);
        using var client = trackingFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "investor@example.com",
            senha = "test-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authProvider.RevokedAccessToken.Should().Be("upstream-access-token");
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().NotContain("upstream-access-token");
        responseBody.Should().NotContain("upstream-refresh-token");
    }

    [Fact]
    public async Task Register_WithNewUser_ReturnsIssuedApiToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            nome = "New User",
            email = "newuser@test.com",
            senha = "NewPass@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadApiResponseAsync<AuthSessionResponse>(response);
        body!.Dados!.TokenAcesso.Should().NotBeNullOrWhiteSpace();
        body.Dados.Usuario.Email.Should().Be("newuser@test.com");

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "newuser@test.com",
            senha = "NewPass@123"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithExistingUser_ReturnsConflict()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            nome = FakeAuthProvider.TestName,
            email = FakeAuthProvider.TestEmail,
            senha = "NewPass@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Refresh_RotatesCookieAndRejectsReplayedToken()
    {
        var login = await LoginAsync();
        var originalCookie = ReadRefreshCookie(login);
        var accessToken = (await ReadApiResponseAsync<AuthSessionResponse>(login))!.Dados!.TokenAcesso;

        var refresh = await PostRefreshAsync(originalCookie);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedCookie = ReadRefreshCookie(refresh);
        rotatedCookie.Should().NotBe(originalCookie);

        var replay = await PostRefreshAsync(originalCookie);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var me = await _client.GetAsync("/api/v1/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredSession_ReturnsUnauthorized()
    {
        var repository = (FakeRefreshTokenSessionRepository)factory.Services.GetRequiredService<IRefreshTokenSessionRepository>();
        var expiredRefreshToken = repository.AddExpiredSession();

        var response = await PostRefreshAsync($"refresh_token={expiredRefreshToken}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidApiToken_ReturnsUserInfo()
    {
        var token = FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadApiResponseAsync<UserResponse>(response);
        body!.Dados!.Email.Should().Be(FakeAuthProvider.TestEmail);
        body.Dados.Id.Should().Be(FakeAuthProvider.TestUserId.ToString());
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithExpiredAccessToken_ReturnsUnauthorized()
    {
        var token = FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail, expiresInMinutes: -10);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Logout_RevokesSessionAndDeletesCookie()
    {
        var login = await LoginAsync();
        var cookie = ReadRefreshCookie(login);
        var token = (await ReadApiResponseAsync<AuthSessionResponse>(login))!.Dados!.TokenAcesso;
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logout = await _client.SendAsync(logoutRequest);
        logout.StatusCode.Should().Be(HttpStatusCode.OK);
        logout.Headers.GetValues("Set-Cookie").Should().ContainSingle(value =>
            value.StartsWith("refresh_token=", StringComparison.Ordinal)
            && value.Contains("expires=thu, 01 jan 1970", StringComparison.OrdinalIgnoreCase)
            && value.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)
            && value.Contains("path=/api/v1/auth", StringComparison.OrdinalIgnoreCase));

        var refresh = await PostRefreshAsync(cookie);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> LoginAsync() => await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        email = FakeAuthProvider.TestEmail,
        senha = "Test@123"
    });

    private async Task<HttpResponseMessage> PostRefreshAsync(string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return await _client.SendAsync(request);
    }

    private static string ReadRefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("refresh_token=", StringComparison.Ordinal))
            .Split(';', 2)[0];

    private static Task<RespostaApi<T>?> ReadApiResponseAsync<T>(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RespostaApi<T>>(JsonOptions);

    private sealed record RespostaApi<T>(T? Dados, bool Sucesso, string? Mensagem);
    private sealed record AuthSessionResponse(string TokenAcesso, int ExpiraEmSegundos, UserResponse Usuario, bool RequerConfirmacaoEmail);
    private sealed record UserResponse(string Id, string Email, string Nome, string Perfil);

    private sealed class UnavailableAuthProvider : IAuthProvider
    {
        public Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromException<AuthResponseDto>(new HttpRequestException("SUPABASE_TEST_FAILURE_MARKER"));

        public Task<AuthResponseDto> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthResponseDto> VerifyInvitationAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthResponseDto> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RevokeAsync(string accessToken, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TrackingAuthProvider : IAuthProvider
    {
        public string? RevokedAccessToken { get; private set; }

        public Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthResponseDto
            {
                AccessToken = "upstream-access-token",
                RefreshToken = "upstream-refresh-token",
                User = new UsuarioAutenticadoDto
                {
                    Id = FakeAuthProvider.TestUserId.ToString(),
                    Email = email,
                    Name = FakeAuthProvider.TestName
                }
            });

        public Task<AuthResponseDto> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthResponseDto> VerifyInvitationAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthResponseDto> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RevokeAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            RevokedAccessToken = accessToken;
            return Task.CompletedTask;
        }
    }
}
