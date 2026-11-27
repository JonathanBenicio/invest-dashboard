using System.Text.Json;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

/// <summary>Observação imutável de um evento do extrato, sem inferir efeitos financeiros.</summary>
public sealed class EventoOficialTesouro : Entity<Guid>
{
    public Guid LoteId { get; private set; }
    public Guid CarteiraId { get; private set; }
    public Guid AtivoId { get; private set; }
    public int IndiceEvento { get; private set; }
    public string EventoJson { get; private set; }
    public Guid? TransacaoId { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public EventoOficialTesouro(Guid id, Guid loteId, Guid carteiraId, Guid ativoId, int indiceEvento,
        string eventoJson, Guid? transacaoId, DateTime criadoEmUtc) : base(id)
    {
        if (id == Guid.Empty || loteId == Guid.Empty || carteiraId == Guid.Empty || ativoId == Guid.Empty || transacaoId == Guid.Empty)
            throw new ArgumentException("Identificadores inválidos.");
        if (indiceEvento < 0) throw new ArgumentOutOfRangeException(nameof(indiceEvento));
        if (string.IsNullOrWhiteSpace(eventoJson) || eventoJson.Length > 262_144)
            throw new ArgumentException("Evento inválido ou muito grande.", nameof(eventoJson));
        using var json = JsonDocument.Parse(eventoJson);
        if (json.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Evento inválido.", nameof(eventoJson));
        if (criadoEmUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Data deve estar em UTC.", nameof(criadoEmUtc));
        LoteId = loteId; CarteiraId = carteiraId; AtivoId = ativoId; IndiceEvento = indiceEvento;
        EventoJson = eventoJson; TransacaoId = transacaoId; CriadoEmUtc = criadoEmUtc;
    }

#pragma warning disable CS8618
    private EventoOficialTesouro() { }
#pragma warning restore CS8618
}
