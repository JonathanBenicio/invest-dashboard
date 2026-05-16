using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class AssetAllocationDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Percentage { get; set; }
        public string? Color { get; set; }
    }

    public class PerformancePointDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PercentageChange { get; set; }
    }

    public class PortfolioSummaryDto : PortfolioDto
    {
        public List<AssetAllocationDto> AssetAllocation { get; set; } = new();
        public List<PerformancePointDto> PerformanceHistory { get; set; } = new();
    }
}
