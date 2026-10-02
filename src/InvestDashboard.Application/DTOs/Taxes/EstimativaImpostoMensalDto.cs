using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Taxes;

public sealed class EstimativaImpostoMensalDto
{
    [JsonPropertyName("ano")]
    public int Ano { get; init; }
    [JsonPropertyName("mes")]
    public int Mes { get; init; }
    [JsonPropertyName("versaoRegras")]
    public string VersaoRegras { get; init; } = string.Empty;
    [JsonPropertyName("vendasAcoesComuns")]
    public decimal VendasAcoesComuns { get; init; }
    [JsonPropertyName("operacoesSemModalidade")]
    public int OperacoesSemModalidade { get; init; }
    [JsonPropertyName("operacoesForaEscopo")]
    public int OperacoesForaEscopo { get; init; }
    [JsonPropertyName("estimativaIncompleta")]
    public bool EstimativaIncompleta { get; init; }
    [JsonPropertyName("estimativaIr")]
    public decimal EstimativaIr { get; init; }
    [JsonPropertyName("categorias")]
    public IReadOnlyList<CategoriaImpostoEstimadoDto> Categorias { get; init; } = [];
    [JsonPropertyName("limitacoes")]
    public IReadOnlyList<string> Limitacoes { get; init; } = [];
    [JsonPropertyName("fontes")]
    public IReadOnlyList<string> Fontes { get; init; } = [];
}

public sealed class CategoriaImpostoEstimadoDto
{
    [JsonPropertyName("categoria")]
    public string Categoria { get; init; } = string.Empty;
    [JsonPropertyName("ganhoLiquido")]
    public decimal GanhoLiquido { get; init; }
    [JsonPropertyName("prejuizoCompensado")]
    public decimal PrejuizoCompensado { get; init; }
    [JsonPropertyName("prejuizoAcumulado")]
    public decimal PrejuizoAcumulado { get; init; }
    [JsonPropertyName("baseTributavel")]
    public decimal BaseTributavel { get; init; }
    [JsonPropertyName("aliquota")]
    public decimal Aliquota { get; init; }
    [JsonPropertyName("isento")]
    public bool Isento { get; init; }
    [JsonPropertyName("irEstimado")]
    public decimal IrEstimado { get; init; }
}
