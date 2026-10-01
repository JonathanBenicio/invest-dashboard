using InvestDashboard.Application.DTOs.Portfolio;

namespace InvestDashboard.Application.Interfaces;

public interface IGrupoCarteirasAppService
{
    Task<GrupoCarteirasDto> CriarAsync(string nome, string usuarioId, string email, string nomeUsuario, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrupoCarteirasDto>> ListarAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembroGrupoDto>?> ListarMembrosAsync(Guid grupoId, string usuarioId, CancellationToken cancellationToken = default);
    Task<bool?> AlterarPapelAsync(Guid grupoId, Guid membroId, string papel, string usuarioId, CancellationToken cancellationToken = default);
    Task<bool?> DesativarAsync(Guid grupoId, Guid membroId, string usuarioId, CancellationToken cancellationToken = default);
    Task<ResultadoConviteGrupoDto?> ConvidarAsync(Guid grupoId, string email, string papel, string usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConvitePendenteGrupoDto>> ListarConvitesPendentesAsync(string email, CancellationToken cancellationToken = default);
    Task<bool?> AceitarConviteAsync(Guid grupoId, Guid conviteId, string usuarioId, string email, string nome, CancellationToken cancellationToken = default);
}
