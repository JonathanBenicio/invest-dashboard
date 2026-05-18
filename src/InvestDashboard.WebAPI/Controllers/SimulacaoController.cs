using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Simulation;
using InvestDashboard.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/simulation")]
public class SimulacaoController : ControllerBase
{
    private readonly EstrategiaDeterministica _estrategiaDeterministica;
    private readonly EstrategiaMonteCarlo _estrategiaMonteCarlo;

    public SimulacaoController()
    {
        _estrategiaDeterministica = new EstrategiaDeterministica();
        _estrategiaMonteCarlo = new EstrategiaMonteCarlo();
    }

    [HttpPost]
    public ActionResult<ApiResponse<SimulacaoResponseDto>> Simulate([FromBody] SimulacaoRequestDto request)
    {
        var parametros = new SimulacaoParametros
        {
            ValorInicial = request.ValorInicial,
            AporteMensal = request.AporteMensal,
            Anos = request.Anos,
            TaxaJurosAnual = request.TaxaJurosAnual,
            Volatilidade = request.Volatilidade,
            NumeroSimulacoes = request.NumeroSimulacoes
        };

        IEstrategiaSimulacao estrategia = request.Estrategia?.ToLower() switch
        {
            "montecarlo" or "monte-carlo" or "estatistico" => _estrategiaMonteCarlo,
            _ => _estrategiaDeterministica
        };

        var resultado = estrategia.Simular(parametros);

        var resposta = new SimulacaoResponseDto
        {
            Pontos = resultado.Pontos.Select(p => new SimulacaoPontoDto
            {
                Mes = p.Mes,
                Investido = p.Investido,
                Total = p.Total,
                Juros = p.Juros
            }).ToList(),
            ValorFinal = resultado.ValorFinal,
            TotalInvestido = resultado.TotalInvestido,
            TotalJuros = resultado.TotalJuros,
            NomeEstrategia = resultado.NomeEstrategia
        };

        return Ok(new ApiResponse<SimulacaoResponseDto>(resposta));
    }

    [HttpGet("strategies")]
    public ActionResult<ApiResponse<object>> GetStrategies()
    {
        var estrategias = new[]
        {
            new { id = "deterministic", nome = _estrategiaDeterministica.Nome, descricao = _estrategiaDeterministica.Descricao },
            new { id = "montecarlo", nome = _estrategiaMonteCarlo.Nome, descricao = _estrategiaMonteCarlo.Descricao }
        };

        return Ok(new ApiResponse<object>(estrategias));
    }
}
