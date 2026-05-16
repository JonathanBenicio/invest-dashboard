using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore;

public class InvestDbContext : DbContext
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<StockAsset> StockAssets => Set<StockAsset>();
    public DbSet<FiiAsset> FiiAssets => Set<FiiAsset>();
    public DbSet<CryptoAsset> CryptoAssets => Set<CryptoAsset>();
    public DbSet<FixedIncomeAsset> FixedIncomeAssets => Set<FixedIncomeAsset>();
    public DbSet<HistoricalPrice> HistoricalPrices => Set<HistoricalPrice>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<AssetPosition> AssetPositions => Set<AssetPosition>();

    public InvestDbContext(DbContextOptions<InvestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations automatically from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestDbContext).Assembly);
    }
}
