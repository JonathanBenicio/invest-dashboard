using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public sealed class ValuacaoPosicao : Entity<Guid>
{
    public Guid PosicaoId { get; private set; }
    public DateTime DataObservadaUtc { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal QuantidadeObservada { get; private set; }
    public DateTime RegistradaEmUtc { get; private set; }

    public ValuacaoPosicao(Guid id, Guid posicaoId, DateTime dataObservadaUtc, decimal precoUnitario,
        decimal quantidadeObservada, DateTime registradaEmUtc) : base(id)
    {
        if (posicaoId == Guid.Empty) throw new ArgumentException("Position ID is required.", nameof(posicaoId));
        if (precoUnitario < 0) throw new ArgumentOutOfRangeException(nameof(precoUnitario));
        if (quantidadeObservada <= 0) throw new ArgumentOutOfRangeException(nameof(quantidadeObservada));

        PosicaoId = posicaoId;
        DataObservadaUtc = ToUtc(dataObservadaUtc);
        PrecoUnitario = precoUnitario;
        QuantidadeObservada = quantidadeObservada;
        RegistradaEmUtc = ToUtc(registradaEmUtc);
    }

    public bool IsNewerThan(ValuacaoPosicao? other) => other is null ||
        DataObservadaUtc > other.DataObservadaUtc ||
        DataObservadaUtc == other.DataObservadaUtc &&
        (RegistradaEmUtc > other.RegistradaEmUtc ||
         RegistradaEmUtc == other.RegistradaEmUtc &&
         string.Compare(Id.ToString("N"), other.Id.ToString("N"), StringComparison.Ordinal) > 0);

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

#pragma warning disable CS8618
    private ValuacaoPosicao() { }
#pragma warning restore CS8618
}
