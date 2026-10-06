namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record ResumoCarteirasDto(decimal ValorTotal, decimal TotalInvestido, decimal GanhoTotal, decimal PercentualGanho, int QuantidadeCarteiras);
