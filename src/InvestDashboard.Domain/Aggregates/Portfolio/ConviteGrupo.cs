using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public sealed class ConviteGrupo : Entity<Guid>
{
    public Guid GrupoId { get; private set; }
    public string Email { get; private set; }
    public PapelGrupo Papel { get; private set; }
    public string CriadoPorUsuarioId { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime ExpiraEmUtc { get; private set; }
    public DateTime? AceitoEmUtc { get; private set; }

    public ConviteGrupo(Guid id, Guid grupoId, string email, PapelGrupo papel, string criadoPorUsuarioId, DateTime criadoEmUtc, DateTime expiraEmUtc) : base(id)
    {
        if (grupoId == Guid.Empty) throw new ArgumentException("Group is required.", nameof(grupoId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(criadoPorUsuarioId)) throw new ArgumentException("Inviter is required.", nameof(criadoPorUsuarioId));
        if (expiraEmUtc <= criadoEmUtc) throw new ArgumentOutOfRangeException(nameof(expiraEmUtc));
        GrupoId = grupoId;
        Email = email.Trim().ToLowerInvariant();
        Papel = papel;
        CriadoPorUsuarioId = criadoPorUsuarioId;
        CriadoEmUtc = DateTime.SpecifyKind(criadoEmUtc, DateTimeKind.Utc);
        ExpiraEmUtc = DateTime.SpecifyKind(expiraEmUtc, DateTimeKind.Utc);
    }

    public bool EstaPendente(DateTime utcNow) => AceitoEmUtc is null && utcNow < ExpiraEmUtc;
    public void Aceitar(DateTime utcNow)
    {
        if (AceitoEmUtc.HasValue) throw new InvalidOperationException("Invitation was already accepted.");
        AceitoEmUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

#pragma warning disable CS8618
    private ConviteGrupo() { }
#pragma warning restore CS8618
}
