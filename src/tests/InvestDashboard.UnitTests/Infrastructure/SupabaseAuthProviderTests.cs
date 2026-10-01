using System.Net;
using System.Security.Authentication;
using System.Text;
using FluentAssertions;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class SupabaseAuthProviderTests
{
    private const string ProjectUrl = "https://project.supabase.co";
    private const string PublishableKey = "sb_publishable_unit_test_key";

    [Fact]
    public async Task LoginAsync_SendsPasswordGrantAndMapsUserSession()
    {
        string? submittedBody = null;
        using var httpClient = CreateHttpClient(async (request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.ToString().Should().Be($"{ProjectUrl}/auth/v1/token?grant_type=password");
            request.Headers.GetValues("apikey").Should().ContainSingle().Which.Should().Be(PublishableKey);
            submittedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(HttpStatusCode.OK, """
                {"access_token":"upstream-access","refresh_token":"upstream-refresh","user":{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001","email":"investor@example.com","user_metadata":{"name":"Investidor"}}}
                """);
        });
        var provider = CreateProvider(httpClient);

        var result = await provider.LoginAsync("investor@example.com", "password123");

        result.AccessToken.Should().Be("upstream-access");
        result.RefreshToken.Should().Be("upstream-refresh");
        result.User.Id.Should().Be("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001");
        result.User.Email.Should().Be("investor@example.com");
        result.User.Name.Should().Be("Investidor");
        result.User.Role.Should().Be("user");
        submittedBody.Should().Contain("\"email\":\"investor@example.com\"");
        submittedBody.Should().Contain("\"password\":\"password123\"");
    }

    [Fact]
    public async Task LoginAsync_MapsInvalidCredentialsToAuthenticationFailure()
    {
        using var httpClient = CreateHttpClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var provider = CreateProvider(httpClient);

        var action = () => provider.LoginAsync("investor@example.com", "wrong-password");

        await action.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task RegisterAsync_MapsConfirmationRequiredResponse()
    {
        string? submittedBody = null;
        using var httpClient = CreateHttpClient(async (request, cancellationToken) =>
        {
            request.RequestUri!.ToString().Should().Be($"{ProjectUrl}/auth/v1/signup");
            submittedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(HttpStatusCode.OK, """
                {"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002","email":"new-investor@example.com","user_metadata":{"name":"Nova Pessoa"}}
                """);
        });
        var provider = CreateProvider(httpClient);

        var result = await provider.RegisterAsync("Nova Pessoa", "new-investor@example.com", "password123");

        result.RequiresEmailConfirmation.Should().BeTrue();
        result.AccessToken.Should().BeEmpty();
        result.User.Id.Should().Be("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002");
        result.User.Email.Should().Be("new-investor@example.com");
        result.User.Name.Should().Be("Nova Pessoa");
        submittedBody.Should().Contain("\"data\":{\"name\":\"Nova Pessoa\"}");
    }

    [Fact]
    public async Task RegisterAsync_MapsProviderConflict()
    {
        using var httpClient = CreateHttpClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)));
        var provider = CreateProvider(httpClient);

        var action = () => provider.RegisterAsync("Nova Pessoa", "used@example.com", "password123");

        await action.Should().ThrowAsync<RegistrationConflictException>();
    }

    [Fact]
    public async Task RevokeAsync_SendsApiKeyAndBearerAccessToken()
    {
        using var httpClient = CreateHttpClient((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.ToString().Should().Be($"{ProjectUrl}/auth/v1/logout?scope=local");
            request.Headers.GetValues("apikey").Should().ContainSingle().Which.Should().Be(PublishableKey);
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("upstream-access");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        var provider = CreateProvider(httpClient);

        await provider.RevokeAsync("upstream-access");
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
        new(new StubHttpMessageHandler(sendAsync));

    private static SupabaseAuthProvider CreateProvider(HttpClient httpClient)
    {
        var configuration = new ConfigurationManager
        {
            ["Storage:SupabaseUrl"] = ProjectUrl,
            ["Storage:SupabaseApiKey"] = PublishableKey
        };
        return new SupabaseAuthProvider(httpClient, configuration, NullLogger<SupabaseAuthProvider>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
    }
}
