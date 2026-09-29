using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Trading;

public sealed class AtualizarTransacaoDto
{
    [Required, RegularExpression("^([Bb][Uu][Yy]|[Ss][Ee][Ll][Ll])$")]
    public string Type { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal BrokerageFee { get; set; }

    public DateTime TransactionDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
