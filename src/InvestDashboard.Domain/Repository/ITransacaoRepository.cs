using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Domain.Repository;

public interface ITransacaoRepository
{
    Task<Transacao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transacao>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Transacao transacao, CancellationToken cancellationToken = default);
    void Update(Transacao transacao);
    void Delete(Transacao transacao);
}
