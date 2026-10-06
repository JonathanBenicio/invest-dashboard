using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public enum PapelGrupo { Admin = 1, Investidor = 2, Consulta = 3 }

public sealed class MembroGrupo : Entity<Guid>
{
    public Guid GrupoId { get; private set; }
    public string UsuarioId { get; private set; }
    public string Email { get; private set; }
    public string Nome { get; private set; }
    public PapelGrupo Papel { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime IngressouEmUtc { get; private set; }

    public MembroGrupo(Guid id, Guid grupoId, string usuarioId, string email, string nome, PapelGrupo papel, DateTime ingressouEmUtc) : base(id)
    {
        if (grupoId == Guid.Empty) throw new ArgumentException("Group is required.", nameof(grupoId));
        if (string.IsNullOrWhiteSpace(usuarioId)) throw new ArgumentException("User is required.", nameof(usuarioId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        GrupoId = grupoId;
        UsuarioId = usuarioId.Trim();
        Email = email.Trim().ToLowerInvariant();
        Nome = nome?.Trim() ?? string.Empty;
        Papel = papel;
        Ativo = true;
        IngressouEmUtc = DateTime.SpecifyKind(ingressouEmUtc, DateTimeKind.Utc);
    }

    public void AlterarPapel(PapelGrupo papel) => Papel = papel;
    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;

#pragma warning disable CS8618
    private MembroGrupo() { }
#pragma warning restore CS8618
}
