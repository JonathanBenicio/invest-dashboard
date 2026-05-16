using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class TransactionRepository : ITransactionRepository
{
    private readonly InvestDbContext _context;

    public TransactionRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<Transaction>();

        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public void Update(Transaction transaction)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        _context.Transactions.Update(transaction);
    }

    public void Delete(Transaction transaction)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        _context.Transactions.Remove(transaction);
    }
}
