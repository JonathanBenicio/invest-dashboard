using InvestDashboard.Application.DTOs.Auth;

namespace InvestDashboard.Application.Interfaces;

public interface IAuthProvider
{
    Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> VerifyInvitationAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string accessToken, CancellationToken cancellationToken = default);
}
