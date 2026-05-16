using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Simulation;

public class SimulationRequestDto
{
    public decimal InitialAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public int Years { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public string Strategy { get; set; } = "deterministic";
    public decimal? Volatility { get; set; }
    public int? NumberOfSimulations { get; set; }
}

public class SimulationPointDto
{
    public int Month { get; set; }
    public decimal Invested { get; set; }
    public decimal Total { get; set; }
    public decimal Interest { get; set; }
}

public class SimulationResponseDto
{
    public List<SimulationPointDto> Points { get; set; } = new();
    public decimal FinalAmount { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalInterest { get; set; }
    public string StrategyName { get; set; } = string.Empty;
}
