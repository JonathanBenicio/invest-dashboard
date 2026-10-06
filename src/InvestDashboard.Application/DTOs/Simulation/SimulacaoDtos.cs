using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Simulation;

public class SimulacaoRequestDto
{
    [JsonPropertyName("valorInicial")]
    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal ValorInicial { get; set; }

    [JsonPropertyName("aporteMensal")]
    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal AporteMensal { get; set; }

    [JsonPropertyName("anos")]
    [Range(1, 50)]
    public int Anos { get; set; }

    [JsonPropertyName("taxaJurosAnual")]
    [Range(typeof(decimal), "0", "1000", ParseLimitsInInvariantCulture = true)]
    public decimal TaxaJurosAnual { get; set; }

    [JsonPropertyName("estrategia")]
    [RegularExpression("^(deterministic|montecarlo)$", MatchTimeoutInMilliseconds = 1000)]
    public string Estrategia { get; set; } = "deterministic";

    [JsonPropertyName("volatilidade")]
    [Range(typeof(decimal), "1", "100", ParseLimitsInInvariantCulture = true)]
    public decimal? Volatilidade { get; set; }

    [JsonPropertyName("numeroSimulacoes")]
    [Range(1, 10000)]
    public int? NumeroSimulacoes { get; set; }
}

public class SimulacaoPontoDto
{
    [JsonPropertyName("mes")]
    public int Mes { get; set; }

    [JsonPropertyName("investido")]
    public decimal Investido { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("juros")]
    public decimal Juros { get; set; }
}

public class SimulacaoResponseDto
{
    [JsonPropertyName("pontos")]
    public List<SimulacaoPontoDto> Pontos { get; set; } = new();

    [JsonPropertyName("valorFinal")]
    public decimal ValorFinal { get; set; }

    [JsonPropertyName("totalInvestido")]
    public decimal TotalInvestido { get; set; }

    [JsonPropertyName("totalJuros")]
    public decimal TotalJuros { get; set; }

    [JsonPropertyName("nomeEstrategia")]
    public string NomeEstrategia { get; set; } = string.Empty;
}
