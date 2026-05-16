using System;

namespace InvestDashboard.Application.DTOs.Taxes;

public class EconomicRateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Variation { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string LastUpdate { get; set; } = string.Empty;
}

public class CreateEconomicRateDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class UpdateEconomicRateDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
