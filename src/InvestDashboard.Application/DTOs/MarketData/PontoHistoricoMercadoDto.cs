namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record PontoHistoricoMercadoDto(
    string Symbol,
    DateTime DateUtc,
    decimal Price,
    string Source,
    bool IsAdjusted);
