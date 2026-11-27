using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

/// <summary>Prévia estruturada e auditável; nunca contém o arquivo de origem.</summary>
public sealed class LoteImportacaoTesouro : Entity<Guid>
{
    public Guid CarteiraId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public string HashConteudo { get; private set; }
    public string Formato { get; private set; }
    public string PreviaJson { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public string Estado { get; private set; }
    public DateTime? ConfirmadoEmUtc { get; private set; }

    public LoteImportacaoTesouro(Guid id, Guid carteiraId, Guid usuarioId, DateOnly dataReferencia,
        string hashConteudo, string formato, string previaJson, DateTime criadoEmUtc) : base(id)
    {
        if (carteiraId == Guid.Empty || usuarioId == Guid.Empty) throw new ArgumentException("Carteira e usuário são obrigatórios.");
        if (dataReferencia == default) throw new ArgumentException("Data de referência obrigatória.", nameof(dataReferencia));
        if (hashConteudo is null || hashConteudo.Length != 64 || hashConteudo.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Hash SHA-256 inválido.", nameof(hashConteudo));
        if (formato is not ("html" or "xlsx")) throw new ArgumentException("Formato inválido.", nameof(formato));
        if (string.IsNullOrWhiteSpace(previaJson) || previaJson.Length > 4_000_000)
            throw new ArgumentException("Prévia inválida ou muito grande.", nameof(previaJson));
        if (criadoEmUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Data de criação deve estar em UTC.", nameof(criadoEmUtc));
        CarteiraId = carteiraId; UsuarioId = usuarioId; DataReferencia = dataReferencia;
        HashConteudo = hashConteudo.ToLowerInvariant(); Formato = formato; PreviaJson = previaJson;
        CriadoEmUtc = criadoEmUtc; Estado = "Pendente";
    }

    public void MarcarConfirmado(DateTime confirmadoEmUtc)
    {
        if (Estado != "Pendente") throw new InvalidOperationException("Lote já foi processado.");
        if (confirmadoEmUtc.Kind != DateTimeKind.Utc || confirmadoEmUtc < CriadoEmUtc)
            throw new ArgumentException("Data de confirmação inválida.", nameof(confirmadoEmUtc));
        Estado = "Confirmado"; ConfirmadoEmUtc = confirmadoEmUtc;
    }

    public void AtualizarPreviaPendente(string previaJson)
    {
        if (Estado != "Pendente") throw new InvalidOperationException("Somente uma prévia pendente pode ser atualizada.");
        if (string.IsNullOrWhiteSpace(previaJson) || previaJson.Length > 4_000_000)
            throw new ArgumentException("Prévia inválida ou muito grande.", nameof(previaJson));
        PreviaJson = previaJson;
    }

#pragma warning disable CS8618
    private LoteImportacaoTesouro() { }
#pragma warning restore CS8618
}
