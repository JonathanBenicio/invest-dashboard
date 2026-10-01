namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record ProjecaoRendaFixaDto(
    Guid PosicaoId,
    string Ticker,
    decimal ValorObservado,
    DateTime? ObservadoEmUtc,
    decimal? ValorProjetadoBruto,
    DateTime DataVencimento,
    string EstadoProjecao,
    string? Motivo,
    string? TaxaSimbolo = null,
    decimal? TaxaValorObservado = null,
    string? TaxaUnidade = null,
    string? TaxaPeriodicidade = null,
    DateOnly? TaxaDataReferencia = null,
    string? TaxaOrigem = null,
    int? TaxaIdade = null,
    int? TaxaValidade = null,
    string? TaxaUnidadeValidade = null);

public sealed record ProjecaoRendaFixaConsolidadaDto(
    decimal ValorObservado,
    decimal? ValorProjetadoBruto,
    int QuantidadePosicoes,
    bool EstaCompleta,
    IReadOnlyList<string> PosicoesSemProjecao);
