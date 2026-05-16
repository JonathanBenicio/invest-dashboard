namespace InvestDashboard.Domain.Services;

public enum AssetTypeForTax
{
    Stock,
    Fii,
    Crypto,
    FixedIncome
}

public class TaxCalculationResult
{
    public decimal GrossProfit { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetProfit { get; set; }
    public bool IsExempt { get; set; }
    public string? ExemptionReason { get; set; }
    public decimal RemainingLoss { get; set; }
}

public interface ITaxCalculationService
{
    TaxCalculationResult CalculateSwingTradeTax(
        decimal totalSalesAmount,
        decimal totalCostAmount,
        decimal previousLosses,
        out decimal updatedLosses);

    TaxCalculationResult CalculateFiiTax(
        decimal profit,
        decimal previousLosses,
        out decimal updatedLosses);

    TaxCalculationResult CalculateDayTradeTax(
        decimal profit,
        decimal previousLosses,
        out decimal updatedLosses);
}
