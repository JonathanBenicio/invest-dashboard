namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class ResumoInvestimentoDto
{
    public decimal TotalInvestido { get; init; }
    public decimal ValorAtual { get; init; }
    public decimal GanhoTotal { get; init; }
    public decimal PercentualGanho { get; init; }
    public decimal GanhoRealizado { get; init; }
    public decimal GanhoNaoRealizado { get; init; }
    public decimal TotalRendaFixa { get; init; }
    public decimal TotalRendaVariavel { get; init; }
    public IReadOnlyList<PosicaoInvestimentoDto> MelhoresPosicoes { get; init; } = [];
    public IReadOnlyList<PosicaoInvestimentoDto> PioresPosicoes { get; init; } = [];
}
