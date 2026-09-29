using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class CarteiraDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PosicaoInvestimentoDto> Positions { get; set; } = new();
        public decimal TotalValue { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalGain { get; set; }
        public decimal UnrealizedGain { get; set; }
        public decimal RealizedGain { get; set; }
        public decimal GainPercentage { get; set; }
        public string Currency { get; set; } = "BRL";
        public int AssetsCount { get; set; }
    }
}
