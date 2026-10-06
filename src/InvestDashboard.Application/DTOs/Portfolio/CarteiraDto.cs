using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class CarteiraDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? GrupoId { get; set; }
        public Guid? TitularId { get; set; }
        public bool TitularVinculado { get; set; }
        public string Titular { get; set; } = string.Empty;
        public string? Parentesco { get; set; }
        public Guid? InstituicaoFinanceiraId { get; set; }
        public string? InstituicaoFinanceira { get; set; }
        public string? TipoInstituicao { get; set; }
        public string Visibilidade { get; set; } = "Particular";
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
