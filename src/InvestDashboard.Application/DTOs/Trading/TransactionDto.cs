using System;

namespace InvestDashboard.Application.DTOs.Trading
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid? AssetId { get; set; }
        public string? Ticker { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal BrokerageFee { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
    }
}
