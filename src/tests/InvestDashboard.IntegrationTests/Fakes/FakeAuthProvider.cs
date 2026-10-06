using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Authentication;
using System.Text;
using InvestDashboard.Application.DTOs.Auth;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace InvestDashboard.IntegrationTests.Fakes;

public sealed class FakeAuthProvider : IAuthProvider
{
    public const string TestSecret = "integration_test_secret_key_that_is_long_enough_for_hmac256";
    public const string TestIssuer = "test-issuer";
    public const string TestAudience = "test-audience";
    public static readonly Guid TestUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001");
    public static readonly Guid TestSessionId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffff0001");
    public const string TestEmail = "test@investdashboard.com";
    public const string TestName = "Test User";
    public static readonly Guid SecondTestUserId = Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaa0002");
    public static readonly Guid SecondTestSessionId = Guid.Parse("dddddddd-eeee-ffff-aaaa-bbbbbbbb0002");
    public const string SecondTestEmail = "other@investdashboard.com";
    public const string SecondTestName = "Other Test User";

    private static readonly ConcurrentDictionary<string, (string Password, string Name)> Users = new(StringComparer.OrdinalIgnoreCase)
    {
        [TestEmail] = ("Test@123", TestName)
    };

    public Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (!Users.TryGetValue(email, out var user) || user.Password != password)
            throw new AuthenticationException("Credentials were rejected.");

        return Task.FromResult(CreateResponse(email, user.Name));
    }

    public Task<AuthResponseDto> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!Users.TryAdd(email, (password, name)))
            throw new RegistrationConflictException();

        return Task.FromResult(CreateResponse(email, name));
    }

    public Task RevokeAsync(string accessToken, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<AuthResponseDto> VerifyInvitationAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(tokenHash == "invite-second-user"
            ? new AuthResponseDto
            {
                AccessToken = GenerateJwtForUser(SecondTestUserId, SecondTestSessionId, SecondTestEmail, SecondTestName),
                RefreshToken = "invited-upstream-token",
                User = new UsuarioAutenticadoDto { Id = SecondTestUserId.ToString(), Email = SecondTestEmail, Name = SecondTestName }
            }
            : CreateResponse(TestEmail, TestName));

    public Task<AuthResponseDto> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(accessToken == "upstream-invited-access"
            ? new AuthResponseDto
            {
                AccessToken = accessToken,
                User = new UsuarioAutenticadoDto { Id = SecondTestUserId.ToString(), Email = SecondTestEmail, Name = SecondTestName }
            }
            : throw new AuthenticationException("Upstream access token is invalid."));

    private static AuthResponseDto CreateResponse(string email, string name) => new()
    {
        AccessToken = GenerateJwt(email),
        RefreshToken = "upstream-session-token-that-must-never-be-returned",
        User = new UsuarioAutenticadoDto { Id = TestUserId.ToString(), Email = email, Name = name }
    };

    public static string GenerateJwt(string email, int expiresInMinutes = 15)
        => GenerateJwtForUser(TestUserId, TestSessionId, email, TestName, expiresInMinutes);

    public static string GenerateJwtForUser(
        Guid userId,
        Guid sessionId,
        string email,
        string name,
        int expiresInMinutes = 15)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("email", email),
            new Claim("name", name),
            new Claim("role", "user"),
            new Claim("sid", sessionId.ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
