namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record CotacaoMercadoDto(
    string Symbol,
    string Name,
    decimal Price,
    DateTime ObservedAtUtc,
    string Currency,
    string? Sector,
    string? Subtype,
    string Source = "brapi");
