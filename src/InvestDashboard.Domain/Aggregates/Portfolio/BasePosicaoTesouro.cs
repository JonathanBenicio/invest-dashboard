using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

/// <summary>Saldo confirmado de um ativo no fim do dia civil de referência.</summary>
public sealed class BasePosicaoTesouro : Entity<Guid>
{
    public Guid LoteId { get; private set; }
    public Guid CarteiraId { get; private set; }
    public Guid AtivoId { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal CustoTotal { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public bool Ativa { get; private set; }

    public BasePosicaoTesouro(Guid id, Guid loteId, Guid carteiraId, Guid ativoId, DateOnly dataReferencia,
        decimal quantidade, decimal custoTotal, decimal precoUnitario) : base(id)
    {
        if (id == Guid.Empty || loteId == Guid.Empty || carteiraId == Guid.Empty || ativoId == Guid.Empty)
            throw new ArgumentException("Identificadores da base são obrigatórios.");
        if (quantidade < 0 || custoTotal < 0 || precoUnitario < 0 ||
            quantidade == 0 && custoTotal != 0 || quantidade > 0 && custoTotal == 0)
            throw new ArgumentException("Quantidade, custo e preço da base são inválidos.");
        LoteId = loteId; CarteiraId = carteiraId; AtivoId = ativoId; DataReferencia = dataReferencia;
        Quantidade = quantidade; CustoTotal = custoTotal; PrecoUnitario = precoUnitario;
    }

    public void Ativar() => Ativa = true;
    public void Desativar() => Ativa = false;

#pragma warning disable CS8618
    private BasePosicaoTesouro() { }
#pragma warning restore CS8618
}
