namespace InvestDashboard.Domain.Services;

public class TaxCalculationService : ITaxCalculationService
{
    private const decimal SwingTradeRate = 0.15m;
    private const decimal FiiRate = 0.20m;
    private const decimal DayTradeRate = 0.20m;
    private const decimal MonthlyExemptionThreshold = 20000m;

    public TaxCalculationResult CalculateSwingTradeTax(
        decimal totalSalesAmount,
        decimal totalCostAmount,
        decimal previousLosses,
        out decimal updatedLosses)
    {
        var grossProfit = totalSalesAmount - totalCostAmount;

        if (grossProfit <= 0)
        {
            updatedLosses = previousLosses + Math.Abs(grossProfit);
            return new TaxCalculationResult
            {
                GrossProfit = grossProfit,
                TaxRate = SwingTradeRate,
                TaxAmount = 0,
                NetProfit = grossProfit,
                IsExempt = false,
                RemainingLoss = updatedLosses
            };
        }

        var isExempt = totalSalesAmount <= MonthlyExemptionThreshold;
        string? exemptionReason = null;

        if (isExempt)
        {
            exemptionReason = $"Vendas no mês (R$ {totalSalesAmount:N2}) abaixo do limite de R$ {MonthlyExemptionThreshold:N2}";
            updatedLosses = previousLosses;
            return new TaxCalculationResult
            {
                GrossProfit = grossProfit,
                TaxRate = SwingTradeRate,
                TaxAmount = 0,
                NetProfit = grossProfit,
                IsExempt = true,
                ExemptionReason = exemptionReason,
                RemainingLoss = previousLosses
            };
        }

        var taxableProfit = grossProfit;
        if (previousLosses > 0)
        {
            var lossCompensation = Math.Min(previousLosses, taxableProfit);
            taxableProfit -= lossCompensation;
            updatedLosses = previousLosses - lossCompensation;
        }
        else
        {
            updatedLosses = 0;
        }

        var taxAmount = taxableProfit > 0 ? taxableProfit * SwingTradeRate : 0;
        var netProfit = grossProfit - taxAmount;

        return new TaxCalculationResult
        {
            GrossProfit = grossProfit,
            TaxRate = SwingTradeRate,
            TaxAmount = taxAmount,
            NetProfit = netProfit,
            IsExempt = false,
            RemainingLoss = updatedLosses
        };
    }

    public TaxCalculationResult CalculateFiiTax(
        decimal profit,
        decimal previousLosses,
        out decimal updatedLosses)
    {
        if (profit <= 0)
        {
            updatedLosses = previousLosses + Math.Abs(profit);
            return new TaxCalculationResult
            {
                GrossProfit = profit,
                TaxRate = FiiRate,
                TaxAmount = 0,
                NetProfit = profit,
                IsExempt = false,
                RemainingLoss = updatedLosses
            };
        }

        var taxableProfit = profit;
        if (previousLosses > 0)
        {
            var lossCompensation = Math.Min(previousLosses, taxableProfit);
            taxableProfit -= lossCompensation;
            updatedLosses = previousLosses - lossCompensation;
        }
        else
        {
            updatedLosses = 0;
        }

        var taxAmount = taxableProfit > 0 ? taxableProfit * FiiRate : 0;
        var netProfit = profit - taxAmount;

        return new TaxCalculationResult
        {
            GrossProfit = profit,
            TaxRate = FiiRate,
            TaxAmount = taxAmount,
            NetProfit = netProfit,
            IsExempt = false,
            RemainingLoss = updatedLosses
        };
    }

    public TaxCalculationResult CalculateDayTradeTax(
        decimal profit,
        decimal previousLosses,
        out decimal updatedLosses)
    {
        if (profit <= 0)
        {
            updatedLosses = previousLosses + Math.Abs(profit);
            return new TaxCalculationResult
            {
                GrossProfit = profit,
                TaxRate = DayTradeRate,
                TaxAmount = 0,
                NetProfit = profit,
                IsExempt = false,
                RemainingLoss = updatedLosses
            };
        }

        var taxableProfit = profit;
        if (previousLosses > 0)
        {
            var lossCompensation = Math.Min(previousLosses, taxableProfit);
            taxableProfit -= lossCompensation;
            updatedLosses = previousLosses - lossCompensation;
        }
        else
        {
            updatedLosses = 0;
        }

        var taxAmount = taxableProfit > 0 ? taxableProfit * DayTradeRate : 0;
        var netProfit = profit - taxAmount;

        return new TaxCalculationResult
        {
            GrossProfit = profit,
            TaxRate = DayTradeRate,
            TaxAmount = taxAmount,
            NetProfit = netProfit,
            IsExempt = false,
            RemainingLoss = updatedLosses
        };
    }
}
