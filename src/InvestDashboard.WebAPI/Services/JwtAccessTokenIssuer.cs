using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestDashboard.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace InvestDashboard.WebAPI.Services;

public sealed class JwtAccessTokenIssuer(IConfiguration configuration) : IAccessTokenIssuer
{
    public string? GetConfigurationError()
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            return "Jwt:Secret must be configured with at least 32 bytes.";
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
            return "Jwt:Issuer must be configured.";
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            return "Jwt:Audience must be configured.";
        return null;
    }

    public string Issue(Guid userId, Guid sessionId, string email, string name, string role, DateTime expiresAtUtc)
    {
        var configurationError = GetConfigurationError();
        if (configurationError is not null)
            throw new InvalidOperationException(configurationError);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("name", name),
                new Claim("role", role),
                new Claim("sid", sessionId.ToString("N"))
            ],
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
