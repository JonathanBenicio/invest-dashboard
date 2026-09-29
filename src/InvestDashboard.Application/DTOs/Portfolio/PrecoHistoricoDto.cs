namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class PrecoHistoricoDto
{
    public DateTime Date { get; init; }
    public decimal Price { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool IsAdjusted { get; init; }
}
