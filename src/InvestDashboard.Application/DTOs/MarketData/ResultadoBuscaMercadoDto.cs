namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record ResultadoBuscaMercadoDto(string Symbol, string Name, string Currency, string? Sector, string Subtype);
