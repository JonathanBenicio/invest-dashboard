using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InvestDbContext _context;

        public UnitOfWork(InvestDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (!_context.Database.IsRelational())
                return NoopUnitOfWorkTransaction.Instance;

            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new UnitOfWorkTransaction(transaction);
        }

        private sealed class UnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default) =>
                transaction.CommitAsync(cancellationToken);

            public ValueTask DisposeAsync() => transaction.DisposeAsync();
        }

        private sealed class NoopUnitOfWorkTransaction : IUnitOfWorkTransaction
        {
            public static NoopUnitOfWorkTransaction Instance { get; } = new();

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
