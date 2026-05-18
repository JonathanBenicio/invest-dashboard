using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class CarteiraDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Balance { get; set; }
        public List<PosicaoInvestimentoDto> Positions { get; set; } = new();
        public decimal TotalValue { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalGain { get; set; }
        public decimal GainPercentage { get; set; }
        public string Currency { get; set; } = "BRL";
        public bool IsActive { get; set; } = true;
        
        // Extended fields for UI
        public string? BankId { get; set; }
        public string? BankName { get; set; }
        public string? BankLogo { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public int? AssetsCount { get; set; }
        public decimal? Profitability { get; set; }
    }
}
