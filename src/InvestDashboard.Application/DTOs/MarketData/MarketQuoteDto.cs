namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record MarketQuoteDto(
    string Symbol,
    string Name,
    decimal Price,
    DateTime ObservedAtUtc,
    string Currency,
    string? Sector,
    string? Subtype);
