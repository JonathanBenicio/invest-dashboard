namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class PontoHistoricoCarteiraDto
{
    public DateTime Date { get; init; }
    public decimal? TotalValue { get; init; }
    public bool IsComplete { get; init; }
    public List<string> MissingTickers { get; init; } = [];
}
