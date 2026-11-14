using InvestDashboard.Application.DTOs.Importacao;

namespace InvestDashboard.Application.Interfaces;

public interface ILeitorPreviaTesouro
{
    PreviaTesouroDto Ler(Guid carteiraId, string formato, Stream arquivo);
}
