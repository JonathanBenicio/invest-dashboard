using System.Text;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public sealed class TitularCarteira : AggregateRoot<Guid>
{
    public Guid GrupoId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public string? UsuarioId { get; private set; }
    public string? Parentesco { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }

    public TitularCarteira(
        Guid id,
        Guid grupoId,
        string nome,
        string? usuarioId,
        string? parentesco,
        DateTime criadoEmUtc) : base(id)
    {
        if (grupoId == Guid.Empty) throw new ArgumentException("Group is required.", nameof(grupoId));
        GrupoId = grupoId;
        AtualizarDados(nome, usuarioId, parentesco, criadoEmUtc);
        CriadoEmUtc = NormalizeUtc(criadoEmUtc);
    }

    public void AtualizarDados(string nome, string? usuarioId, string? parentesco, DateTime atualizadoEmUtc)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Holder name is required.", nameof(nome));
        var normalizedName = NormalizeName(nome);
        if (normalizedName.Length > 160) throw new ArgumentOutOfRangeException(nameof(nome));

        Nome = nome.Trim();
        NomeNormalizado = normalizedName;
        UsuarioId = string.IsNullOrWhiteSpace(usuarioId) ? null : usuarioId.Trim();
        Parentesco = string.IsNullOrWhiteSpace(parentesco) ? null : parentesco.Trim();
        if (Parentesco?.Length > 80) throw new ArgumentOutOfRangeException(nameof(parentesco));
        AtualizadoEmUtc = NormalizeUtc(atualizadoEmUtc);
    }

    public static string NormalizeName(string nome) =>
        string.Join(' ', nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

#pragma warning disable CS8618
    private TitularCarteira() { }
#pragma warning restore CS8618

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
