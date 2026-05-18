using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Simulation;

public class SimulacaoRequestDto
{
    public decimal ValorInicial { get; set; }
    public decimal AporteMensal { get; set; }
    public int Anos { get; set; }
    public decimal TaxaJurosAnual { get; set; }
    public string Estrategia { get; set; } = "deterministic";
    public decimal? Volatilidade { get; set; }
    public int? NumeroSimulacoes { get; set; }
}

public class SimulacaoPontoDto
{
    public int Mes { get; set; }
    public decimal Investido { get; set; }
    public decimal Total { get; set; }
    public decimal Juros { get; set; }
}

public class SimulacaoResponseDto
{
    public List<SimulacaoPontoDto> Pontos { get; set; } = new();
    public decimal ValorFinal { get; set; }
    public decimal TotalInvestido { get; set; }
    public decimal TotalJuros { get; set; }
    public string NomeEstrategia { get; set; } = string.Empty;
}
