using InvestDashboard.Application.DTOs.Auth;

namespace InvestDashboard.Application.Interfaces;

public interface IAuthenticationAppService
{
    Task<SessaoAutenticacaoDto> LoginAsync(SolicitacaoLoginDto request, CancellationToken cancellationToken = default);
    Task<SessaoAutenticacaoDto> RegisterAsync(SolicitacaoCadastroDto request, CancellationToken cancellationToken = default);
    Task<SessaoAutenticacaoDto> AceitarConviteAsync(Guid groupId, Guid invitationId, string tokenHash, string? accessToken = null, CancellationToken cancellationToken = default);
    Task<SessaoAutenticacaoDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid? sessionId, string? refreshToken, CancellationToken cancellationToken = default);
}
