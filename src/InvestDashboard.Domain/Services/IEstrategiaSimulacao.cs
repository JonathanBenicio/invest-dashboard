namespace InvestDashboard.Domain.Services;

public class SimulacaoPonto
{
    public int Mes { get; set; }
    public decimal Investido { get; set; }
    public decimal Total { get; set; }
    public decimal Juros { get; set; }
}

public class SimulacaoResultado
{
    public List<SimulacaoPonto> Pontos { get; set; } = new();
    public decimal ValorFinal { get; set; }
    public decimal TotalInvestido { get; set; }
    public decimal TotalJuros { get; set; }
    public string NomeEstrategia { get; set; } = string.Empty;
}

public class SimulacaoParametros
{
    public decimal ValorInicial { get; set; }
    public decimal AporteMensal { get; set; }
    public int Anos { get; set; }
    public decimal TaxaJurosAnual { get; set; }
    public decimal? Volatilidade { get; set; }
    public int? NumeroSimulacoes { get; set; }
}

public interface IEstrategiaSimulacao
{
    string Nome { get; }
    string Descricao { get; }
    SimulacaoResultado Simular(SimulacaoParametros parametros);
}
