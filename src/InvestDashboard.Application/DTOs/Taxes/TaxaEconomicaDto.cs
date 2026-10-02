using System;

namespace InvestDashboard.Application.DTOs.Taxes;

public class TaxaEconomicaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Variation { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string LastUpdate { get; set; } = string.Empty;
    public Guid? GrupoId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Periodicity { get; set; } = string.Empty;
    public DateOnly ReferenceDate { get; set; }
    public string UpdatedByUserId { get; set; } = string.Empty;
    public List<TaxaEconomicaHistoricoDto> Historico { get; set; } = [];
}

public sealed class TaxaEconomicaHistoricoDto
{
    public decimal ValorAnterior { get; set; }
    public decimal ValorNovo { get; set; }
    public string? UnidadeAnterior { get; set; }
    public string? UnidadeNova { get; set; }
    public string? PeriodicidadeAnterior { get; set; }
    public string? PeriodicidadeNova { get; set; }
    public string Origem { get; set; } = string.Empty;
    public DateOnly DataReferencia { get; set; }
    public string ResponsavelUserId { get; set; } = string.Empty;
    public DateTime AtualizadoEmUtc { get; set; }
}

public class CriarTaxaEconomicaDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Unit { get; set; } = "Percentual";
    public string Periodicity { get; set; } = "Mensal";
    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public class AtualizarTaxaEconomicaDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Unit { get; set; } = "Percentual";
    public string Periodicity { get; set; } = "Mensal";
    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
