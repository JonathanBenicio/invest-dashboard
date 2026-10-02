using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
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
            && value.Contains("path=/api/v1/auth", StringComparison.OrdinalIgnoreCase));

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
}
