using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Simulation;
using InvestDashboard.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/simulation")]
public class SimulationController : ControllerBase
{
    private readonly DeterministicStrategy _deterministicStrategy;
    private readonly MonteCarloStrategy _monteCarloStrategy;

    public SimulationController()
    {
        _deterministicStrategy = new DeterministicStrategy();
        _monteCarloStrategy = new MonteCarloStrategy();
    }

    [HttpPost]
    public ActionResult<ApiResponse<SimulationResponseDto>> Simulate([FromBody] SimulationRequestDto request)
    {
        var parameters = new SimulationParameters
        {
            InitialAmount = request.InitialAmount,
            MonthlyContribution = request.MonthlyContribution,
            Years = request.Years,
            AnnualInterestRate = request.AnnualInterestRate,
            Volatility = request.Volatility,
            NumberOfSimulations = request.NumberOfSimulations
        };

        ISimulationStrategy strategy = request.Strategy?.ToLower() switch
        {
            "montecarlo" or "monte-carlo" or "estatistico" => _monteCarloStrategy,
            _ => _deterministicStrategy
        };

        var result = strategy.Simulate(parameters);

        var response = new SimulationResponseDto
        {
            Points = result.Points.Select(p => new SimulationPointDto
            {
                Month = p.Month,
                Invested = p.Invested,
                Total = p.Total,
                Interest = p.Interest
            }).ToList(),
            FinalAmount = result.FinalAmount,
            TotalInvested = result.TotalInvested,
            TotalInterest = result.TotalInterest,
            StrategyName = result.StrategyName
        };

        return Ok(new ApiResponse<SimulationResponseDto>(response));
    }

    [HttpGet("strategies")]
    public ActionResult<ApiResponse<object>> GetStrategies()
    {
        var strategies = new[]
        {
            new { id = "deterministic", name = _deterministicStrategy.Name, description = _deterministicStrategy.Description },
            new { id = "montecarlo", name = _monteCarloStrategy.Name, description = _monteCarloStrategy.Description }
        };

        return Ok(new ApiResponse<object>(strategies));
    }
}
