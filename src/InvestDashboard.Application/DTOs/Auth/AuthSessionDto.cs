using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Auth;

public sealed class AuthSessionDto
{
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public UserInfoDto User { get; init; } = new();
    public bool RequiresEmailConfirmation { get; init; }

    [JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonIgnore]
    public DateTime SessionExpiresAtUtc { get; init; }
}
