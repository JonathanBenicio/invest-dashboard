using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class RegistrarAvaliacaoDto
{
    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal TotalValue { get; set; }

    public DateTime Date { get; set; }
}
