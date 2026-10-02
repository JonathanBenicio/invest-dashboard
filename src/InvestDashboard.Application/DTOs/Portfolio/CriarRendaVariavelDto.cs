using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class CriarRendaVariavelDto
{
    [Required]
    public Guid CarteiraId { get; set; }

    [Required, StringLength(20)]
    public string Ticker { get; set; } = string.Empty;

    [Required, RegularExpression("^(ACAO|FII|ETF|BDR|CRYPTO)$")]
    public string Subtype { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Sector { get; set; }

    [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal Fees { get; set; }

    public DateTime TransactionDate { get; set; }

    [Required]
    public Guid IdempotencyKey { get; set; }
}
