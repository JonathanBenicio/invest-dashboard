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

    private static AuthResponseDto CreateResponse(string email, string name) => new()
    {
        AccessToken = GenerateJwt(email),
        RefreshToken = "upstream-session-token-that-must-never-be-returned",
        User = new UserInfoDto { Id = TestUserId.ToString(), Email = email, Name = name }
    };

    public static string GenerateJwt(string email, int expiresInMinutes = 15)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("sub", TestUserId.ToString()),
            new Claim("email", email),
            new Claim("name", TestName),
            new Claim("role", "user"),
            new Claim("sid", TestSessionId.ToString("N"))
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
