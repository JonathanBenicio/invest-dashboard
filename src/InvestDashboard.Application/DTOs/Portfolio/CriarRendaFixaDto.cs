using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class CriarRendaFixaDto
{
    [Required]
    public Guid CarteiraId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Subtype { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Issuer { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal Principal { get; set; }

    [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
    public decimal StatementValue { get; set; }

    [Range(typeof(decimal), "0", "10000", ParseLimitsInInvariantCulture = true)]
    public decimal InterestRate { get; set; }

    [Required, StringLength(20)]
    public string Indexer { get; set; } = string.Empty;

    public DateTime MaturityDate { get; set; }
    public DateTime PurchaseDate { get; set; }

    [StringLength(80)]
    public string? Liquidity { get; set; }

    [StringLength(80)]
    public string? Convention { get; set; }

    [Required]
    public Guid IdempotencyKey { get; set; }
}
