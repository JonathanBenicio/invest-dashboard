namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record MarketHistoryPointDto(
    string Symbol,
    DateTime DateUtc,
    decimal Price,
    string Source,
    bool IsAdjusted);
