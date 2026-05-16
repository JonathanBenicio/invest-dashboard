namespace InvestDashboard.Domain.Services;

public class SimulationPoint
{
    public int Month { get; set; }
    public decimal Invested { get; set; }
    public decimal Total { get; set; }
    public decimal Interest { get; set; }
}

public class SimulationResult
{
    public List<SimulationPoint> Points { get; set; } = new();
    public decimal FinalAmount { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalInterest { get; set; }
    public string StrategyName { get; set; } = string.Empty;
}

public class SimulationParameters
{
    public decimal InitialAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public int Years { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public decimal? Volatility { get; set; }
    public int? NumberOfSimulations { get; set; }
}

public interface ISimulationStrategy
{
    string Name { get; }
    string Description { get; }
    SimulationResult Simulate(SimulationParameters parameters);
}
