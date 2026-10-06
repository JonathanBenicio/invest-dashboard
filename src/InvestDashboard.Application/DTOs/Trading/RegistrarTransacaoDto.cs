using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Trading
{
    public class RegistrarTransacaoDto
    {
        [JsonPropertyName("carteiraId")]
        [Required]
        public Guid CarteiraId { get; set; }
        [JsonPropertyName("ativoId")]
        public Guid? AtivoId { get; set; }
        [StringLength(20)]
        public string? Ticker { get; set; }
        [Required, RegularExpression("^([Bb][Uu][Yy]|[Ss][Ee][Ll][Ll])$", MatchTimeoutInMilliseconds = 1000)]
        public string Type { get; set; } = "Buy";
        [StringLength(20)]
        public string? AssetClass { get; set; }
        [StringLength(200)]
        public string? Name { get; set; }
        [StringLength(100)]
        public string? Sector { get; set; }
        [StringLength(200)]
        public string? Issuer { get; set; }
        [StringLength(20)]
        public string? Subtype { get; set; }
        [StringLength(50)]
        public string? Indexer { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime? MaturityDate { get; set; }
        [StringLength(80)]
        public string? Liquidity { get; set; }
        [StringLength(80)]
        public string? Convention { get; set; }
        [Required]
        public Guid IdempotencyKey { get; set; }
        [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal? InitialStatementValue { get; set; }
        [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal Quantity { get; set; }
        [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal UnitPrice { get; set; }
        [JsonPropertyName("taxas")]
        [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal BrokerageFee { get; set; }
        [JsonPropertyName("modalidadeFiscal")]
        [StringLength(20)]
        public string? ModalidadeFiscal { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
