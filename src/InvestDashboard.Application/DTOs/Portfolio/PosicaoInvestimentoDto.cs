using System;
using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class PosicaoInvestimentoDto
    {
        public Guid Id { get; set; }
        [JsonPropertyName("portfolioId")]
        public Guid CarteiraId { get; set; }
        [JsonPropertyName("assetId")]
        public Guid AtivoId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'fixed_income' | 'variable_income'
        public string Subtype { get; set; } = string.Empty; // e.g., 'CDB', 'ACAO', 'FII'
        public string? Sector { get; set; }
        public string Status { get; set; } = "open";
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Gain { get; set; }
        public decimal GainPercentage { get; set; }
        public string Currency { get; set; } = "BRL";
        public string? Issuer { get; set; }
        public decimal? InterestRate { get; set; }
        public string? Indexer { get; set; }
        public DateTime? MaturityDate { get; set; }
    }
}
