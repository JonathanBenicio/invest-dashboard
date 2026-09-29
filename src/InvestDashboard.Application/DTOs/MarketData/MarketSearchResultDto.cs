namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record MarketSearchResultDto(string Symbol, string Name, string Currency, string? Sector, string Subtype);
