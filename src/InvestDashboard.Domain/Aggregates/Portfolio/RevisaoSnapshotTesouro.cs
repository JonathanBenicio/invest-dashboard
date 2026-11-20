using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

/// <summary>Confirmação auditável do usuário para divergências entre snapshots do mesmo título.</summary>
public sealed class RevisaoSnapshotTesouro : Entity<Guid>
{
    public Guid CarteiraId { get; private set; }
    public Guid LoteId { get; private set; }
    public Guid AutorId { get; private set; }
    public Guid AtivoId { get; private set; }
    public string HashComparacao { get; private set; }
    public string DivergenciasJson { get; private set; }
    public string Decisao { get; private set; }
    public int CarteiraVersao { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public RevisaoSnapshotTesouro(Guid id, Guid carteiraId, Guid loteId, Guid autorId, Guid ativoId,
        string hashComparacao, string divergenciasJson, string decisao, int carteiraVersao, DateTime criadoEmUtc) : base(id)
    {
        if (id == Guid.Empty || carteiraId == Guid.Empty || loteId == Guid.Empty || autorId == Guid.Empty || ativoId == Guid.Empty)
            throw new ArgumentException("Identificadores da revisão de snapshot são obrigatórios.");
        if (hashComparacao is null || hashComparacao.Length != 64 || hashComparacao.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Hash da comparação inválido.", nameof(hashComparacao));
        if (string.IsNullOrWhiteSpace(divergenciasJson) || divergenciasJson.Length > 1_000_000)
            throw new ArgumentException("Resumo de divergências inválido ou muito grande.", nameof(divergenciasJson));
        if (decisao != "aceitar_nova_base") throw new ArgumentException("Decisão de snapshot inválida.", nameof(decisao));
        if (carteiraVersao < 0) throw new ArgumentOutOfRangeException(nameof(carteiraVersao));
        if (criadoEmUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Data deve estar em UTC.", nameof(criadoEmUtc));

        CarteiraId = carteiraId;
        LoteId = loteId;
        AutorId = autorId;
        AtivoId = ativoId;
        HashComparacao = hashComparacao.ToLowerInvariant();
        DivergenciasJson = divergenciasJson;
        Decisao = decisao;
        CarteiraVersao = carteiraVersao;
        CriadoEmUtc = criadoEmUtc;
    }

#pragma warning disable CS8618
    private RevisaoSnapshotTesouro() { }
#pragma warning restore CS8618
}
