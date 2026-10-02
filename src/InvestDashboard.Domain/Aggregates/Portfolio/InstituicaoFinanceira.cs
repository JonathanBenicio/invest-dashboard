using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public sealed class InstituicaoFinanceira : AggregateRoot<Guid>
{
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public CategoriaInstituicaoFinanceira Categoria { get; private set; }
    public Guid? GrupoId { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }

    public bool Personalizada => GrupoId.HasValue;

    public InstituicaoFinanceira(
        Guid id,
        string nome,
        CategoriaInstituicaoFinanceira categoria,
        Guid? grupoId,
        DateTime criadoEmUtc) : base(id)
    {
        if (grupoId == Guid.Empty) throw new ArgumentException("Group cannot be empty.", nameof(grupoId));
        GrupoId = grupoId;
        CriadoEmUtc = NormalizeUtc(criadoEmUtc);
        AtualizarDados(nome, categoria, criadoEmUtc);
    }

    public void AtualizarDados(string nome, CategoriaInstituicaoFinanceira categoria, DateTime atualizadoEmUtc)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Institution name is required.", nameof(nome));
        if (nome.Trim().Length > 120) throw new ArgumentOutOfRangeException(nameof(nome));
        Nome = nome.Trim();
        NomeNormalizado = NormalizeName(nome);
        Categoria = categoria;
        AtualizadoEmUtc = NormalizeUtc(atualizadoEmUtc);
    }

    public static string NormalizeName(string nome) =>
        string.Join(' ', nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

#pragma warning disable CS8618
    private InstituicaoFinanceira() { }
#pragma warning restore CS8618

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
