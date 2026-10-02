using System;
using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Trading
{
    public class TransacaoDto
    {
        public Guid Id { get; set; }
        [JsonPropertyName("carteiraId")]
        public Guid CarteiraId { get; set; }
        public Guid? TitularId { get; set; }
        [JsonPropertyName("ativoId")]
        public Guid? AtivoId { get; set; }
        public string? Ticker { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        [JsonPropertyName("taxas")]
        public decimal BrokerageFee { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RealizedGain { get; set; }
        public decimal RealizedCostBasis { get; set; }
        [JsonPropertyName("modalidadeFiscal")]
        public string ModalidadeFiscal { get; set; } = "NaoInformada";
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
    }
}
