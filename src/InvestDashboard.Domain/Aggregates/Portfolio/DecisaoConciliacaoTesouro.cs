using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

/// <summary>Registro imutável de revisão; alterações geram uma nova revisão.</summary>
public sealed class DecisaoConciliacaoTesouro : Entity<Guid>
{
    public Guid CarteiraId { get; private set; }
    public Guid LoteId { get; private set; }
    public Guid AutorId { get; private set; }
    public Guid? AtivoId { get; private set; }
    public int IndiceEvento { get; private set; }
    public int Revisao { get; private set; }
    public string Decisao { get; private set; }
    public Guid? TransacaoId { get; private set; }
    public int CarteiraVersao { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public DecisaoConciliacaoTesouro(Guid id, Guid carteiraId, Guid loteId, Guid autorId, Guid? ativoId,
        int indiceEvento, int revisao, string decisao, Guid? transacaoId, int carteiraVersao, DateTime criadoEmUtc) : base(id)
    {
        if (id == Guid.Empty || carteiraId == Guid.Empty || loteId == Guid.Empty || autorId == Guid.Empty || ativoId == Guid.Empty)
            throw new ArgumentException("Identificadores de revisão são obrigatórios.");
        if (indiceEvento < 0 || revisao < 1 || carteiraVersao < 0)
            throw new ArgumentOutOfRangeException(nameof(indiceEvento), "Índice, revisão ou versão inválidos.");
        if (string.IsNullOrWhiteSpace(decisao) || decisao.Length > 40)
            throw new ArgumentException("Decisão inválida.", nameof(decisao));
        if (transacaoId == Guid.Empty) throw new ArgumentException("Transação inválida.", nameof(transacaoId));
        if (criadoEmUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Data deve estar em UTC.", nameof(criadoEmUtc));
        CarteiraId = carteiraId; LoteId = loteId; AutorId = autorId; AtivoId = ativoId;
        IndiceEvento = indiceEvento; Revisao = revisao; Decisao = decisao; TransacaoId = transacaoId;
        CarteiraVersao = carteiraVersao; CriadoEmUtc = criadoEmUtc;
    }

#pragma warning disable CS8618
    private DecisaoConciliacaoTesouro() { }
#pragma warning restore CS8618
}
