namespace InvestDashboard.Domain.Services;

public class DeterministicStrategy : ISimulationStrategy
{
    public string Name => "Matemático (Determinístico)";
    public string Description => "Simulação baseada em juros compostos com taxa fixa";

    public SimulationResult Simulate(SimulationParameters parameters)
    {
        var monthlyRate = (decimal)Math.Pow(1 + (double)parameters.AnnualInterestRate / 100, 1.0 / 12.0) - 1;
        var totalMonths = parameters.Years * 12;
        var points = new List<SimulationPoint>();

        var currentAmount = parameters.InitialAmount;
        var totalInvested = parameters.InitialAmount;

        for (var i = 0; i <= totalMonths; i++)
        {
            points.Add(new SimulationPoint
            {
                Month = i,
                Invested = Math.Round(totalInvested, 2),
                Total = Math.Round(currentAmount, 2),
                Interest = Math.Round(currentAmount - totalInvested, 2)
            });

            if (i < totalMonths)
            {
                currentAmount = currentAmount * (1 + monthlyRate) + parameters.MonthlyContribution;
                totalInvested += parameters.MonthlyContribution;
            }
        }

        return new SimulationResult
        {
            Points = points,
            FinalAmount = Math.Round(currentAmount, 2),
            TotalInvested = Math.Round(totalInvested, 2),
            TotalInterest = Math.Round(currentAmount - totalInvested, 2),
            StrategyName = Name
        };
    }
}
