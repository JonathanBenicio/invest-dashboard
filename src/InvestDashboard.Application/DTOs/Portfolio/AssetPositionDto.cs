using System;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class AssetPositionDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid AssetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'fixed_income' | 'variable_income'
        public string Subtype { get; set; } = string.Empty; // e.g., 'CDB', 'ACAO', 'FII'
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Gain { get; set; }
        public decimal GainPercentage { get; set; }
        public string Currency { get; set; } = "BRL";
    }
}
