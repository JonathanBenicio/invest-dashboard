using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record PosicaoInvestimentoConsolidadoDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("chaveConsolidacao")] string ChaveConsolidacao,
    [property: JsonPropertyName("posicaoId")] Guid? PosicaoId,
    [property: JsonPropertyName("ativoId")] Guid AtivoId,
    [property: JsonPropertyName("grupoId")] Guid? GrupoId,
    [property: JsonPropertyName("grupoNome")] string? GrupoNome,
    [property: JsonPropertyName("titularId")] Guid? TitularId,
    [property: JsonPropertyName("titular")] string? Titular,
    [property: JsonPropertyName("escopo")] string Escopo,
    [property: JsonPropertyName("carteiraId")] Guid? CarteiraId,
    [property: JsonPropertyName("carteiraNome")] string? CarteiraNome,
    [property: JsonPropertyName("quantidadeCarteiras")] int QuantidadeCarteiras,
    [property: JsonPropertyName("dataAvaliacaoMaisAntiga")] DateOnly? DataAvaliacaoMaisAntiga,
    [property: JsonPropertyName("dataAvaliacaoMaisRecente")] DateOnly? DataAvaliacaoMaisRecente,
    [property: JsonPropertyName("datasAvaliacaoDiferentes")] bool DatasAvaliacaoDiferentes,
    [property: JsonPropertyName("datasAvaliacaoCompletas")] bool DatasAvaliacaoCompletas,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("subtipo")] string Subtipo,
    [property: JsonPropertyName("setor")] string? Setor,
    [property: JsonPropertyName("situacao")] string Situacao,
    [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("precoMedio")] decimal PrecoMedio,
    [property: JsonPropertyName("precoAtual")] decimal? PrecoAtual,
    [property: JsonPropertyName("totalInvestido")] decimal TotalInvestido,
    [property: JsonPropertyName("valorAtual")] decimal ValorAtual,
    [property: JsonPropertyName("ganho")] decimal Ganho,
    [property: JsonPropertyName("percentualGanho")] decimal PercentualGanho,
    [property: JsonPropertyName("moeda")] string Moeda,
    [property: JsonPropertyName("emissor")] string? Emissor,
    [property: JsonPropertyName("taxaJuros")] decimal? TaxaJuros,
    [property: JsonPropertyName("indexador")] string? Indexador,
    [property: JsonPropertyName("dataVencimento")] DateTime? DataVencimento,
    [property: JsonPropertyName("liquidez")] string? Liquidez,
    [property: JsonPropertyName("convencao")] string? Convencao);

public sealed record ContribuicaoPosicaoInvestimentoDto(
    [property: JsonPropertyName("posicaoId")] Guid PosicaoId,
    [property: JsonPropertyName("carteiraId")] Guid CarteiraId,
    [property: JsonPropertyName("carteiraNome")] string CarteiraNome,
    [property: JsonPropertyName("titularId")] Guid? TitularId,
    [property: JsonPropertyName("titular")] string Titular,
    [property: JsonPropertyName("instituicaoFinanceiraId")] Guid? InstituicaoFinanceiraId,
    [property: JsonPropertyName("instituicaoFinanceira")] string? InstituicaoFinanceira,
    [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("totalInvestido")] decimal TotalInvestido,
    [property: JsonPropertyName("valorAtual")] decimal ValorAtual,
    [property: JsonPropertyName("ganho")] decimal Ganho,
    [property: JsonPropertyName("dataAvaliacao")] DateOnly? DataAvaliacao,
    [property: JsonPropertyName("situacao")] string Situacao);

public sealed record DetalheInvestimentoConsolidadoDto(
    [property: JsonPropertyName("consolidado")] PosicaoInvestimentoConsolidadoDto Consolidado,
    [property: JsonPropertyName("carteiras")] IReadOnlyList<ContribuicaoPosicaoInvestimentoDto> Carteiras);
