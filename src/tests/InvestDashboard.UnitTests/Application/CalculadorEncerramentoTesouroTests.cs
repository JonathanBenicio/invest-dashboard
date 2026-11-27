using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.UnitTests.Application;

public sealed class CalculadorEncerramentoTesouroTests
{
    [Fact]
    public void VendaIntegralComSecaoReconhecidaComprovaEncerramento()
    {
        var (portfolioId, assetId, loteId, saldo, previaAnterior) = CriarBase();
        var venda = Movimento(new DateOnly(2024, 3, 3), "Venda", "Débito", 1m, 22m);

        var resultado = CalculadorEncerramentoTesouro.Avaliar(Previa(portfolioId, [venda], true),
            new DateOnly(2024, 3, 3), saldo, previaAnterior, [], new Dictionary<int, DecisaoConciliacaoTesouro>());

        resultado.PodeEncerrar.Should().BeTrue();
        resultado.QuantidadeAnterior.Should().Be(1m);
        resultado.QuantidadeReconciliada.Should().Be(0m);
        resultado.Motivo.Should().Contain("As vendas registradas");
    }

    public static IEnumerable<object?[]> CasosQuePreservamBase => new object?[][]
    {
        [false, 1m, null],
        [true, 0.5m, 0.5m],
        [true, 2m, -1m]
    };

    [Theory]
    [MemberData(nameof(CasosQuePreservamBase))]
    public void MantemBaseQuandoSecaoAusenteOuVendaNaoZeraSaldo(bool secaoReconhecida,
        decimal quantidadeVendida, decimal? saldoEsperado)
    {
        var (portfolioId, _, _, saldo, previaAnterior) = CriarBase();
        var venda = Movimento(new DateOnly(2024, 3, 3), "Venda", "Débito", quantidadeVendida, 22m);

        var resultado = CalculadorEncerramentoTesouro.Avaliar(Previa(portfolioId, [venda], secaoReconhecida),
            new DateOnly(2024, 3, 3), saldo, previaAnterior, [], new Dictionary<int, DecisaoConciliacaoTesouro>());

        resultado.PodeEncerrar.Should().BeFalse();
        resultado.QuantidadeReconciliada.Should().Be(saldoEsperado);
        resultado.Motivo.Should().Contain("base ativa será preservada");
    }

    [Fact]
    public void MovimentoManualSoPodeContribuirDepoisDeVinculadoUmaVez()
    {
        var (portfolioId, assetId, loteId, saldo, previaAnterior) = CriarBase();
        var venda = Movimento(new DateOnly(2024, 3, 3), "Venda", "Débito", 1m, 22m);
        var previa = Previa(portfolioId, [venda], true);
        var manual = new Transacao(Guid.NewGuid(), "usuario", portfolioId, assetId, "TD2035",
            TipoTransacao.Sell, 1m, 22m, 0m, new DateTime(2024, 3, 3, 12, 0, 0, DateTimeKind.Utc));

        var semVinculo = CalculadorEncerramentoTesouro.Avaliar(previa, new DateOnly(2024, 3, 3), saldo,
            previaAnterior, [manual], new Dictionary<int, DecisaoConciliacaoTesouro>());
        var revisao = new DecisaoConciliacaoTesouro(Guid.NewGuid(), portfolioId, loteId,
            Guid.NewGuid(), assetId, 0, 1, "vincular", manual.Id, 1, DateTime.UtcNow);
        var comVinculo = CalculadorEncerramentoTesouro.Avaliar(previa, new DateOnly(2024, 3, 3), saldo,
            previaAnterior, [manual], new Dictionary<int, DecisaoConciliacaoTesouro> { [0] = revisao });

        semVinculo.PodeEncerrar.Should().BeFalse();
        semVinculo.Motivo.Should().Contain("manuais sem vínculo");
        comVinculo.PodeEncerrar.Should().BeTrue();
    }

    [Fact]
    public void VendasIdenticasSemIdentidadeEstavelNaoComprovamEncerramento()
    {
        var (portfolioId, _, _, saldo, previaAnterior) = CriarBase();
        var venda = Movimento(new DateOnly(2024, 3, 3), "Venda", "Débito", 0.5m, 22m);

        var resultado = CalculadorEncerramentoTesouro.Avaliar(Previa(portfolioId, [venda, venda], true),
            new DateOnly(2024, 3, 3), saldo, previaAnterior, [], new Dictionary<int, DecisaoConciliacaoTesouro>());

        resultado.PodeEncerrar.Should().BeFalse();
        resultado.Motivo.Should().Contain("ambíguas");
    }

    private static (Guid PortfolioId, Guid AssetId, Guid BatchId, BasePosicaoTesouro Saldo,
        PreviaTesouroDto Previa) CriarBase()
    {
        var portfolioId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var saldo = new BasePosicaoTesouro(Guid.NewGuid(), batchId, portfolioId, assetId,
            new DateOnly(2024, 3, 2), 1m, 20m, 22m);
        saldo.Ativar();
        var posicao = new PosicaoPreviaTesouroDto(1, new Dictionary<string, string>(),
            new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 1), 1m, 20m, 20m, "10%", null, null, 22m,
                1, null, null, null, null, null, 22m, []));
        return (portfolioId, assetId, batchId, saldo, Previa(portfolioId, [], true, [posicao]));
    }

    private static PreviaTesouroDto Previa(Guid portfolioId, IReadOnlyList<MovimentoPreviaTesouroDto> movimentos,
        bool secaoPosicoesReconhecida, IReadOnlyList<PosicaoPreviaTesouroDto>? posicoes = null) =>
        new(portfolioId, "html", "portal_tesouro_html", "Tesouro Prefixado 2035", "2035-01-01", null,
            posicoes ?? [], movimentos, [], [], true, false, false,
            SecaoPosicoesReconhecida: secaoPosicoesReconhecida);

    private static MovimentoPreviaTesouroDto Movimento(DateOnly data, string natureza, string tipo,
        decimal quantidade, decimal preco)
    {
        var dados = new DadosMovimentoTesouroDto(data, natureza, tipo, quantidade, preco, null,
            quantidade * preco, [], ["taxaAnual"]);
        return new MovimentoPreviaTesouroDto([], dados);
    }
}
