using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public sealed class TaxaEconomicaHistorico : Entity<Guid>
{
    public Guid TaxaId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public string? UnidadeAnterior { get; private set; }
    public string? UnidadeNova { get; private set; }
    public string? PeriodicidadeAnterior { get; private set; }
    public string? PeriodicidadeNova { get; private set; }
    public string Origem { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public string ResponsavelUserId { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }

    public TaxaEconomicaHistorico(Guid id, Guid taxaId, decimal valorAnterior, decimal valorNovo, string origem, DateOnly dataReferencia, string responsavelUserId, DateTime atualizadoEmUtc, string? unidadeAnterior = null, string? unidadeNova = null, string? periodicidadeAnterior = null, string? periodicidadeNova = null) : base(id)
    {
        TaxaId = taxaId;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        UnidadeAnterior = unidadeAnterior;
        UnidadeNova = unidadeNova;
        PeriodicidadeAnterior = periodicidadeAnterior;
        PeriodicidadeNova = periodicidadeNova;
        Origem = origem;
        DataReferencia = dataReferencia;
        ResponsavelUserId = responsavelUserId;
        AtualizadoEmUtc = DateTime.SpecifyKind(atualizadoEmUtc, DateTimeKind.Utc);
    }

#pragma warning disable CS8618
    private TaxaEconomicaHistorico() { }
#pragma warning restore CS8618
}
