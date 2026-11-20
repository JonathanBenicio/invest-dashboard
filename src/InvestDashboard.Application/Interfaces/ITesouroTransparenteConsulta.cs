using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface ITesouroTransparenteConsulta
{
    Task<CatalogoTesouroDto> ListarTitulosAsync(CancellationToken cancellationToken = default);
    Task<SerieCotacoesTesouroDto> ConsultarAsync(string tipoTitulo, DateOnly dataVencimento, DateOnly dataDe,
        DateOnly dataAte, CancellationToken cancellationToken = default);
}
