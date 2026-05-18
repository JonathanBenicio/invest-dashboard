using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore;

public class InvestDbContext : DbContext
{
    public DbSet<Ativo> Assets => Set<Ativo>();
    public DbSet<Acao> StockAssets => Set<Acao>();
    public DbSet<FundoImobiliario> FiiAssets => Set<FundoImobiliario>();
    public DbSet<Criptoativo> CryptoAssets => Set<Criptoativo>();
    public DbSet<RendaFixa> FixedIncomeAssets => Set<RendaFixa>();
    public DbSet<PrecoHistorico> HistoricalPrices => Set<PrecoHistorico>();
    public DbSet<Transacao> Transactions => Set<Transacao>();
    public DbSet<Carteira> Portfolios => Set<Carteira>();
    public DbSet<PosicaoInvestimento> AssetPositions => Set<PosicaoInvestimento>();
    public DbSet<TaxaEconomica> EconomicRates => Set<TaxaEconomica>();

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
