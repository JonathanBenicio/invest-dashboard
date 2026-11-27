using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.UnitTests.Application;

public sealed class ConciliaTesouroTests
{
    [Fact]
    public void ApresentaTodosOsCandidatosSemConciliarAutomaticamenteNemConfundirTaxas()
    {
        var carteira = new Carteira(Guid.NewGuid(), "usuario", "Carteira");
        var ativo = Guid.NewGuid();
        var data = new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc);
        Transacao Operacao(Guid id, Guid carteiraId, Guid ativoId, DateTime dataOperacao) => new(id, "usuario", carteiraId, ativoId, "TESTE", TipoTransacao.Buy, 1m, 20m, 0m, dataOperacao);
        var primeira = Operacao(Guid.NewGuid(), carteira.Id, ativo, data);
        var segunda = Operacao(Guid.NewGuid(), carteira.Id, ativo, data);
        var posterior = Operacao(Guid.NewGuid(), carteira.Id, ativo, data.AddDays(5));
        var outroAtivo = Operacao(Guid.NewGuid(), carteira.Id, Guid.NewGuid(), data);
        carteira.ProcessTransaction(primeira, 20m, TipoAtivo.RendaFixa);
        DadosMovimentoTesouroDto Dados(string natureza) => new(new DateOnly(2024, 3, 2), natureza, "Crédito", 1m, 20m, null, 20m, []);
        var previa = new PreviaTesouroDto(carteira.Id, "html", "teste", "Titulo", "2035-01-01", null, [],
            [new([], Dados("Compra")), new([], Dados("Compra")), new([], Dados("Taxas")), new([], Dados("Compra") with { Tipo = "Débito" })], [], [], true, false, false);

        var resultado = ConciliaTesouro.Preparar(carteira, new DateOnly(2024, 3, 3), previa, ativo, [primeira, segunda, posterior, outroAtivo]);

        resultado.PodeConfirmar.Should().BeFalse();
        resultado.Eventos.Should().HaveCount(4);
        resultado.Eventos[0].Candidatos.Should().BeEquivalentTo([primeira.Id, segunda.Id]);
        resultado.Eventos[1].Candidatos.Should().BeEquivalentTo([primeira.Id, segunda.Id]);
        resultado.Eventos[2].Candidatos.Should().BeEmpty();
        resultado.Eventos[3].Candidatos.Should().BeEmpty();
        resultado.Transacoes.Should().HaveCount(3);
        resultado.Transacoes.Single(t => t.Id == posterior.Id).PosteriorBase.Should().BeTrue();
        resultado.CarteiraVersao.Should().Be(carteira.Version);
    }

    [Fact]
    public void RejeitaAssociacaoDeAtivoQueNaoPertenceACarteira()
    {
        var carteira = new Carteira(Guid.NewGuid(), "usuario", "Carteira");
        var previa = new PreviaTesouroDto(carteira.Id, "html", "teste", null, null, null, [], [], [], [], true, false, false);
        var acao = () => ConciliaTesouro.Preparar(carteira, new DateOnly(2024, 1, 1), previa, Guid.NewGuid(), []);
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ListaSomentePosicoesDoTesouroComTituloEVencimentoCompativeis()
    {
        var carteira = new Carteira(Guid.NewGuid(), "usuario", "Carteira");
        var vencimento = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var compativel = new RendaFixa(Guid.NewGuid(), "TD2035", "Tesouro Prefixado 2035",
            20m, DateTime.UtcNow, "PREFIXADO", null, vencimento, "Tesouro Nacional", "TESOURO_DIRETO");
        var outroVencimento = new RendaFixa(Guid.NewGuid(), "TD2034", "Tesouro Prefixado 2035",
            20m, DateTime.UtcNow, "PREFIXADO", null, vencimento.AddYears(-1), "Tesouro Nacional", "TESOURO_DIRETO");
        var outroProduto = new RendaFixa(Guid.NewGuid(), "CDB2035", "CDB Prefixado 2035",
            20m, DateTime.UtcNow, "PREFIXADO", 10m, vencimento, "Banco", "CDB");
        var data = new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc);

        void AdicionarPosicao(RendaFixa ativo)
        {
            var transacao = new Transacao(Guid.NewGuid(), "usuario", carteira.Id, ativo.Id, ativo.Ticker,
                TipoTransacao.Buy, 1m, 20m, 0m, data);
            carteira.ProcessTransaction(transacao, 20m, TipoAtivo.RendaFixa);
            carteira.Positions.Single(position => position.AtivoId == ativo.Id).UpdateMarketAsset(ativo);
        }

        AdicionarPosicao(compativel);
        AdicionarPosicao(outroVencimento);
        AdicionarPosicao(outroProduto);
        var previa = new PreviaTesouroDto(carteira.Id, "html", "portal_tesouro_html",
            "Tesouro Prefixado 2035", "2035-01-01", null, [], [], [], [], true, false, false);

        var resultado = ConciliaTesouro.Preparar(carteira, new DateOnly(2024, 3, 3), previa, null, []);

        resultado.Ativos.Should().ContainSingle();
        resultado.Ativos[0].AtivoId.Should().Be(compativel.Id);
        resultado.Ativos[0].Ticker.Should().Be(compativel.Ticker);
        resultado.Ativos[0].Nome.Should().Be(compativel.Name);
        resultado.Ativos[0].Vencimento.Should().Be(DateOnly.FromDateTime(compativel.MaturityDate));
    }
}
