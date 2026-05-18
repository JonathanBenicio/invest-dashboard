namespace InvestDashboard.Domain.Services;

public class EstrategiaMonteCarlo : IEstrategiaSimulacao
{
    public string Nome => "Estatístico (Monte Carlo)";
    public string Descricao => "Simulação probabilística com volatilidade, calculando mediana e percentis";

    public SimulacaoResultado Simular(SimulacaoParametros parametros)
    {
        var numSimulacoes = parametros.NumeroSimulacoes ?? 1000;
        var volatilidade = parametros.Volatilidade ?? 15m;
        var volMensal = volatilidade / 100m / (decimal)Math.Sqrt(12);
        var taxaMensal = (decimal)Math.Pow(1 + (double)parametros.TaxaJurosAnual / 100, 1.0 / 12.0) - 1;
        var totalMeses = parametros.Anos * 12;

        var todasSimulacoes = new decimal[numSimulacoes][];
        var rng = new Random(42);

        for (var sim = 0; sim < numSimulacoes; sim++)
        {
            todasSimulacoes[sim] = new decimal[totalMeses + 1];
            var valor = parametros.ValorInicial;
            todasSimulacoes[sim][0] = valor;

            for (var m = 1; m <= totalMeses; m++)
            {
                var choque = (decimal)(rng.NextDouble() * 2 - 1) * volMensal;
                var retornoMensal = taxaMensal + choque;
                valor = valor * (1 + retornoMensal) + parametros.AporteMensal;
                todasSimulacoes[sim][m] = valor;
            }
        }

        var pontos = new List<SimulacaoPonto>();
        var totalInvestido = parametros.ValorInicial;

        for (var i = 0; i <= totalMeses; i++)
        {
            var valores = new decimal[numSimulacoes];
            for (var sim = 0; sim < numSimulacoes; sim++)
            {
                valores[sim] = todasSimulacoes[sim][i];
            }

            Array.Sort(valores);
            var mediana = valores[numSimulacoes / 2];
            var p5 = valores[(int)(numSimulacoes * 0.05)];
            var p95 = valores[(int)(numSimulacoes * 0.95)];

            pontos.Add(new SimulacaoPonto
            {
                Mes = i,
                Investido = Math.Round(totalInvestido, 2),
                Total = Math.Round(mediana, 2),
                Juros = Math.Round(mediana - totalInvestido, 2)
            });

            if (i < totalMeses)
            {
                totalInvestido += parametros.AporteMensal;
            }
        }

        return new SimulacaoResultado
        {
            Pontos = pontos,
            ValorFinal = Math.Round(pontos[^1].Total, 2),
            TotalInvestido = Math.Round(totalInvestido, 2),
            TotalJuros = Math.Round(pontos[^1].Total - totalInvestido, 2),
            NomeEstrategia = Nome
        };
    }
}
