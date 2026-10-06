using InvestDashboard.Application.DTOs.Taxes;

namespace InvestDashboard.Application.Interfaces;

public interface IEstimativaImpostoAppService
{
    Task<EstimativaImpostoMensalDto> CalcularMensalAsync(int ano, int mes, Guid grupoId, string? titular, Guid? titularId = null);
}
