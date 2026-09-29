using InvestDashboard.Application.DTOs.Auth;

namespace InvestDashboard.Application.Interfaces;

public interface IAuthenticationAppService
{
    Task<AuthSessionDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthSessionDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthSessionDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid? sessionId, string? refreshToken, CancellationToken cancellationToken = default);
}
