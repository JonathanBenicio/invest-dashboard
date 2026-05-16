using System.Threading;
using System.Threading.Tasks;
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
    }
}
