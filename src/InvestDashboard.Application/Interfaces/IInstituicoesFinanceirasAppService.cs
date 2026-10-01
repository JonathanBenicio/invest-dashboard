using InvestDashboard.Application.DTOs.Portfolio;

namespace InvestDashboard.Application.Interfaces;

public interface IInstituicoesFinanceirasAppService
{
    Task<IReadOnlyList<InstituicaoFinanceiraDto>> ListarAsync(Guid? grupoId, string usuarioId, CancellationToken cancellationToken = default);
    Task<InstituicaoFinanceiraDto> CriarOutraAsync(Guid grupoId, string nome, string usuarioId, CancellationToken cancellationToken = default);
}
