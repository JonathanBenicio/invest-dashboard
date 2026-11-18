using System.Text.Json;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class AvaliacaoTesouroAppService(
    ICarteiraRepository carteiras,
    IBasePosicaoTesouroRepository bases,
    IRepositorioLoteImportacaoTesouro lotes,
    ITesouroTransparenteConsulta cotacoes,
    IUsuarioAtualService usuario) : IAvaliacaoTesouroAppService
{

    public async Task<AvaliacaoPosicaoTesouroDto?> ConsultarAsync(Guid posicaoId, CancellationToken cancellationToken = default)
    {
        var usuarioId = usuario.UserId;
        if (usuarioId is null) return null;

        var encontrada = await carteiras.GetPositionByIdForUserAsync(posicaoId, usuarioId.Value.ToString(), cancellationToken);
        if (encontrada is null || encontrada.Value.Position.Ativo is not RendaFixa ativo || ativo.Subtype != "TESOURO_DIRETO")
            return null;

        var carteira = encontrada.Value.Portfolio;
        var posicao = encontrada.Value.Position;
        var dataVencimento = DateOnly.FromDateTime(ativo.MaturityDate);
        var baseAtiva = (await bases.GetAtivasAsync(carteira.Id, cancellationToken))
            .SingleOrDefault(item => item.AtivoId == posicao.AtivoId);
        var previa = await CarregarPreviaAsync(carteira.Id, usuarioId.Value, baseAtiva, cancellationToken);
        var dataExtrato = baseAtiva?.DataReferencia;
        var snapshot = ExtrairSnapshot(previa, dataExtrato);
        var avisos = new List<string>();

        var tipoTitulo = MapearTipoTitulo(ativo);
        if (string.IsNullOrEmpty(tipoTitulo))
            return SemCotacao(posicaoId, posicao.AtivoId, posicao.Ticker, ativo, posicao.Quantity,
                snapshot, "Não foi possível reconhecer o tipo deste título no catálogo público.");

        var catalogo = await cotacoes.ListarTitulosAsync(cancellationToken);
        var tituloDisponivel = catalogo.Titulos.FirstOrDefault(item =>
            item.DataVencimento == dataVencimento &&
            string.Equals(item.TipoTitulo, tipoTitulo, StringComparison.OrdinalIgnoreCase));
        if (tituloDisponivel is null)
            return SemCotacao(posicaoId, posicao.AtivoId, posicao.Ticker, ativo, posicao.Quantity,
                snapshot, "O Tesouro Transparente não publicou cotação para este tipo e vencimento.");

        var serie = await cotacoes.ConsultarAsync(tipoTitulo, tituloDisponivel.DataVencimento,
            tituloDisponivel.UltimaDataDisponivel, tituloDisponivel.UltimaDataDisponivel, cancellationToken);
        var ultima = serie.Cotacoes.LastOrDefault();
        if (ultima is null)
            return SemCotacao(posicaoId, posicao.AtivoId, posicao.Ticker, ativo, posicao.Quantity,
                snapshot, "Não há PU base disponível para este título no catálogo consultado.");

        var valorBruto = decimal.Round(posicao.Quantity * ultima.PuBaseManha, 2, MidpointRounding.AwayFromZero);
        var motivo = baseAtiva is null
            ? "Não há extrato confirmado com dados de aquisição para calcular tributos e custos por lote."
            : "O cálculo líquido atual aguarda metodologia e custos atuais reconciliados. Os impostos e taxas disponíveis pertencem à data do extrato.";
        if (baseAtiva is null)
            avisos.Add("Sem extrato confirmado, o valor bruto é calculado pela quantidade atual e pelo PU base público.");
        else
            avisos.Add("Valores de IR, IOF e taxas do extrato são um retrato da data de referência do arquivo, não encargos atuais.");

        return new AvaliacaoPosicaoTesouroDto(posicaoId, ativo.Id, posicao.Ticker, ativo.Name, dataVencimento,
            posicao.Quantity, serie.Origem, serie.UrlFonte, ultima.DataBase, serie.ColetadoEmUtc, ultima.PuBaseManha,
            valorBruto, dataExtrato, snapshot.ValorBruto, snapshot.ImpostoRenda, snapshot.Iof, snapshot.ValorLiquido,
            dataExtrato, snapshot.TaxaB3, snapshot.TaxaInstituicao, null, null, null, null, motivo, avisos);
    }

    private async Task<PreviaTesouroDto?> CarregarPreviaAsync(Guid carteiraId, Guid usuarioId, BasePosicaoTesouro? baseAtiva,
        CancellationToken cancellationToken)
    {
        if (baseAtiva is null) return null;
        var lote = await lotes.BuscarPorIdAsync(carteiraId, usuarioId, baseAtiva.LoteId, cancellationToken);
        if (lote is null) return null;
        try { return JsonSerializer.Deserialize<PreviaTesouroDto>(lote.PreviaJson); }
        catch (JsonException) { return null; }
    }

    private static SnapshotExtrato ExtrairSnapshot(PreviaTesouroDto? previa, DateOnly? dataExtrato)
    {
        if (previa is null || previa.Posicoes.Count == 0)
            return new SnapshotExtrato(null, null, null, null, null, null, dataExtrato);

        var linhas = previa.Posicoes.Select(item => item.DadosInterpretados).ToArray();
        static decimal? Somar(IReadOnlyList<DadosPosicaoTesouroDto?> itens, Func<DadosPosicaoTesouroDto, decimal?> selecionar) =>
            itens.All(item => item is not null && selecionar(item) is not null)
                ? itens.Sum(item => selecionar(item!)!.Value)
                : null;

        var taxaB3 = Somar(linhas, item => item.TaxaDevidaB3);
        var taxaInstituicao = Somar(linhas, item => item.TaxaDevidaInstituicao);
        return new SnapshotExtrato(
            Somar(linhas, item => item.ValorBruto),
            Somar(linhas, item => item.ImpostoPrevistoIr),
            Somar(linhas, item => item.ImpostoPrevistoIof),
            taxaB3,
            taxaInstituicao,
            Somar(linhas, item => item.ValorLiquido),
            dataExtrato);
    }

    private static string MapearTipoTitulo(RendaFixa ativo)
    {
        var nome = ativo.Name;
        if (nome.Contains("Renda+", StringComparison.OrdinalIgnoreCase)) return "Tesouro Renda+ Aposentadoria Extra";
        if (nome.Contains("Educa+", StringComparison.OrdinalIgnoreCase)) return "Tesouro Educa+";
        if (ativo.Indexer.Contains("SELIC", StringComparison.OrdinalIgnoreCase)) return "Tesouro Selic";
        if (ativo.Indexer.Contains("IPCA", StringComparison.OrdinalIgnoreCase))
            return nome.Contains("Juros Semestrais", StringComparison.OrdinalIgnoreCase)
                ? "Tesouro IPCA+ com Juros Semestrais" : "Tesouro IPCA+";
        if (ativo.Indexer.Contains("IGPM", StringComparison.OrdinalIgnoreCase)) return "Tesouro IGPM+ com Juros Semestrais";
        if (ativo.Indexer.Contains("PREFIXADO", StringComparison.OrdinalIgnoreCase))
            return nome.Contains("Juros Semestrais", StringComparison.OrdinalIgnoreCase)
                ? "Tesouro Prefixado com Juros Semestrais" : "Tesouro Prefixado";
        return string.Empty;
    }

    private static AvaliacaoPosicaoTesouroDto SemCotacao(Guid positionId, Guid assetId, string ticker, RendaFixa asset,
        decimal quantity, SnapshotExtrato snapshot, string reason) =>
        new(positionId, assetId, ticker, asset.Name, DateOnly.FromDateTime(asset.MaturityDate), quantity,
            null, null, null, null, null, null, snapshot.Data, snapshot.ValorBruto, snapshot.ImpostoRenda,
            snapshot.Iof, snapshot.ValorLiquido, snapshot.Data, snapshot.TaxaB3, snapshot.TaxaInstituicao,
            null, null, null, null, reason, []);

    private sealed record SnapshotExtrato(decimal? ValorBruto, decimal? ImpostoRenda, decimal? Iof,
        decimal? TaxaB3, decimal? TaxaInstituicao, decimal? ValorLiquido, DateOnly? Data);
}
