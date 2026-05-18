using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class AlocacaoAtivoDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Percentage { get; set; }
        public string? Color { get; set; }
    }

    public class PontoPerformanceDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PercentageChange { get; set; }
    }

    public class ResumoCarteiraDto : CarteiraDto
    {
        public List<AlocacaoAtivoDto> AssetAllocation { get; set; } = new();
        public List<PontoPerformanceDto> PerformanceHistory { get; set; } = new();
    }
}
