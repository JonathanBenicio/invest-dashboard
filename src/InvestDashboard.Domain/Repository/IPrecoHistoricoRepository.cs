using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Domain.Repository;

public interface IPrecoHistoricoRepository
{
    Task<IReadOnlyList<PrecoHistorico>> GetByAtivoIdAsync(Guid ativoId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task AddAsync(PrecoHistorico precoHistorico, CancellationToken cancellationToken = default);
}
