namespace InvestDashboard.Application.DTOs.Portfolio
{
    public class CreatePortfolioDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
    }
}
