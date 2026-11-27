using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.UnitTests.Application;

public sealed class ResumoUltimaImportacaoTesouroTests
{
    [Fact]
    public void InformaBaseAnteriorIndisponivelMantendoDataEQuantidadeConhecidas()
    {
        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteAtual = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 14), Previa(carteiraId));
        var baseAnterior = Base(carteiraId, ativoId, loteId: Guid.NewGuid(), new DateOnly(2026, 11, 6), 5m);
        var baseAtual = Base(carteiraId, ativoId, loteAtual.Id, new DateOnly(2026, 11, 14), 4m);

        var resumo = ResumoUltimaImportacaoTesouro.Preparar(baseAtual, loteAtual, baseAnterior, null);

        resumo.Should().NotBeNull();
        resumo!.LoteAnteriorId.Should().BeNull();
        resumo.DataReferenciaAnterior.Should().Be(new DateOnly(2026, 11, 6));
        resumo.QuantidadeAnterior.Should().Be(5m);
        resumo.QuantidadeAtual.Should().Be(4m);
        resumo.VariacaoQuantidade.Should().Be(-1m);
        resumo.Divergencias.Should().Contain(item => item.Tipo == "snapshot_anterior_indisponivel");
    }

    [Fact]
    public void PreviaAtualInvalidaRetornaResumoComPendenciaDeLeitura()
    {
        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteAtual = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 14), "{invalido");
        var loteAnterior = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 6), Previa(carteiraId));
        loteAnterior.MarcarConfirmado(DateTime.UtcNow.AddMinutes(1));
        var baseAnterior = Base(carteiraId, ativoId, loteAnterior.Id, new DateOnly(2026, 11, 6), 5m);
        var baseAtual = Base(carteiraId, ativoId, loteAtual.Id, new DateOnly(2026, 11, 14), 4m);

        var resumo = ResumoUltimaImportacaoTesouro.Preparar(baseAtual, loteAtual, baseAnterior, loteAnterior);

        resumo.Should().NotBeNull();
        resumo!.Divergencias.Should().Contain(item => item.Tipo == "snapshot_atual_indisponivel");
    }

    [Fact]
    public void PreviaAnteriorInvalidaInformaIndisponibilidadeSemPerderBaseAnterior()
    {
        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteAtual = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 14), Previa(carteiraId));
        var loteAnterior = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 6), "{invalido");
        loteAnterior.MarcarConfirmado(DateTime.UtcNow.AddMinutes(1));
        var baseAnterior = Base(carteiraId, ativoId, loteAnterior.Id, new DateOnly(2026, 11, 6), 5m);
        var baseAtual = Base(carteiraId, ativoId, loteAtual.Id, new DateOnly(2026, 11, 14), 4m);

        var resumo = ResumoUltimaImportacaoTesouro.Preparar(baseAtual, loteAtual, baseAnterior, loteAnterior);

        resumo.Should().NotBeNull();
        resumo!.LoteAnteriorId.Should().Be(loteAnterior.Id);
        resumo.DataReferenciaAnterior.Should().Be(baseAnterior.DataReferencia);
        resumo.QuantidadeAnterior.Should().Be(baseAnterior.Quantidade);
        resumo.Divergencias.Should().Contain(item => item.Tipo == "snapshot_anterior_indisponivel");
    }

    [Fact]
    public void EncerramentoExibeComparacaoPersistidaSemRecalcularVendas()
    {
        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteAtual = Lote(carteiraId, Guid.NewGuid(), new DateOnly(2026, 11, 14), Previa(carteiraId));
        var baseAnterior = Base(carteiraId, ativoId, Guid.NewGuid(), new DateOnly(2026, 11, 6), 5m);
        var baseAtual = Base(carteiraId, ativoId, loteAtual.Id, new DateOnly(2026, 11, 14), 0m);
        var encerramento = new DivergenciaSnapshotTesouroDto("posicao_encerrada_por_venda", "posicao",
            "As vendas confirmadas zeraram a quantidade reconciliada.", "5", "0");
        var comparacaoPersistida = new ComparacaoSnapshotTesouroDto(new string('b', 64),
            baseAnterior.DataReferencia, 5m, 0m, [encerramento], true);

        var resumo = ResumoUltimaImportacaoTesouro.Preparar(baseAtual, loteAtual, baseAnterior,
            null, comparacaoPersistida);

        resumo.Should().NotBeNull();
        resumo!.QuantidadeAtual.Should().Be(0m);
        resumo.VariacaoQuantidade.Should().Be(-5m);
        resumo.Divergencias.Should().ContainSingle(item => item.Tipo == "posicao_encerrada_por_venda");
    }

    private static LoteImportacaoTesouro Lote(Guid carteiraId, Guid id, DateOnly dataReferencia, string previa) =>
        new(id, carteiraId, Guid.NewGuid(), dataReferencia, new string('a', 64), "html", previa, DateTime.UtcNow);

    private static BasePosicaoTesouro Base(Guid carteiraId, Guid ativoId, Guid loteId,
        DateOnly dataReferencia, decimal quantidade) =>
        new(Guid.NewGuid(), loteId, carteiraId, ativoId, dataReferencia, quantidade,
            quantidade * 100m, 100m);

    private static string Previa(Guid carteiraId) => JsonSerializer.Serialize(new PreviaTesouroDto(carteiraId,
        "html", "portal", "Tesouro Prefixado 2035", "2035-01-01", null, [], [], [], [],
        true, false, false));
}
