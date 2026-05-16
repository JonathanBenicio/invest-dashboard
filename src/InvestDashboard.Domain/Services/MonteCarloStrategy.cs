namespace InvestDashboard.Domain.Services;

public class MonteCarloStrategy : ISimulationStrategy
{
    public string Name => "Estatístico (Monte Carlo)";
    public string Description => "Simulação probabilística com volatilidade, calculando mediana e percentis";

    public SimulationResult Simulate(SimulationParameters parameters)
    {
        var numSimulations = parameters.NumberOfSimulations ?? 1000;
        var volatility = parameters.Volatility ?? 15m;
        var monthlyVol = volatility / 100m / (decimal)Math.Sqrt(12);
        var monthlyRate = (decimal)Math.Pow(1 + (double)parameters.AnnualInterestRate / 100, 1.0 / 12.0) - 1;
        var totalMonths = parameters.Years * 12;

        var allSimulations = new decimal[numSimulations][];
        var rng = new Random(42);

        for (var sim = 0; sim < numSimulations; sim++)
        {
            allSimulations[sim] = new decimal[totalMonths + 1];
            var amount = parameters.InitialAmount;
            allSimulations[sim][0] = amount;

            for (var m = 1; m <= totalMonths; m++)
            {
                var shock = (decimal)(rng.NextDouble() * 2 - 1) * monthlyVol;
                var monthlyReturn = monthlyRate + shock;
                amount = amount * (1 + monthlyReturn) + parameters.MonthlyContribution;
                allSimulations[sim][m] = amount;
            }
        }

        var points = new List<SimulationPoint>();
        var totalInvested = parameters.InitialAmount;

        for (var i = 0; i <= totalMonths; i++)
        {
            var values = new decimal[numSimulations];
            for (var sim = 0; sim < numSimulations; sim++)
            {
                values[sim] = allSimulations[sim][i];
            }

            Array.Sort(values);
            var median = values[numSimulations / 2];
            var p5 = values[(int)(numSimulations * 0.05)];
            var p95 = values[(int)(numSimulations * 0.95)];

            points.Add(new SimulationPoint
            {
                Month = i,
                Invested = Math.Round(totalInvested, 2),
                Total = Math.Round(median, 2),
                Interest = Math.Round(median - totalInvested, 2)
            });

            if (i < totalMonths)
            {
                totalInvested += parameters.MonthlyContribution;
            }
        }

        return new SimulationResult
        {
            Points = points,
            FinalAmount = Math.Round(points[^1].Total, 2),
            TotalInvested = Math.Round(totalInvested, 2),
            TotalInterest = Math.Round(points[^1].Total - totalInvested, 2),
            StrategyName = Name
        };
    }
}
