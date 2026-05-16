using System;

namespace InvestDashboard.Application.DTOs.Trading
{
    public class RegisterTransactionDto
    {
        public Guid PortfolioId { get; set; }
        public Guid? AssetId { get; set; }
        public string? Ticker { get; set; }
        public string Type { get; set; } = "Buy"; // Buy, Sell, Deposit, Withdrawal
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal BrokerageFee { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
