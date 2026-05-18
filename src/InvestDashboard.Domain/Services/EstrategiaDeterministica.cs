namespace InvestDashboard.Domain.Services;

public class EstrategiaDeterministica : IEstrategiaSimulacao
{
    public string Nome => "Matemático (Determinístico)";
    public string Descricao => "Simulação baseada em juros compostos com taxa fixa";

    public SimulacaoResultado Simular(SimulacaoParametros parametros)
    {
        var taxaMensal = (decimal)Math.Pow(1 + (double)parametros.TaxaJurosAnual / 100, 1.0 / 12.0) - 1;
        var totalMeses = parametros.Anos * 12;
        var pontos = new List<SimulacaoPonto>();

        var valorAtual = parametros.ValorInicial;
        var totalInvestido = parametros.ValorInicial;

        for (var i = 0; i <= totalMeses; i++)
        {
            pontos.Add(new SimulacaoPonto
            {
                Mes = i,
                Investido = Math.Round(totalInvestido, 2),
                Total = Math.Round(valorAtual, 2),
                Juros = Math.Round(valorAtual - totalInvestido, 2)
            });

            if (i < totalMeses)
            {
                valorAtual = valorAtual * (1 + taxaMensal) + parametros.AporteMensal;
                totalInvestido += parametros.AporteMensal;
            }
        }

        return new SimulacaoResultado
        {
            Pontos = pontos,
            ValorFinal = Math.Round(valorAtual, 2),
            TotalInvestido = Math.Round(totalInvestido, 2),
            TotalJuros = Math.Round(valorAtual - totalInvestido, 2),
            NomeEstrategia = Nome
        };
    }
}
