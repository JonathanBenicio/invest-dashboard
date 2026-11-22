using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record SolicitarConciliacaoTesouroDto([property: JsonPropertyName("ativoId")] Guid? AtivoId);
public sealed record AtivoConciliacaoTesouroDto([property: JsonPropertyName("ativoId")] Guid AtivoId, [property: JsonPropertyName("ticker")] string Ticker);
public sealed record TransacaoConciliacaoTesouroDto(
    [property: JsonPropertyName("id")] Guid Id, [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("data")] DateTime Data, [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco, [property: JsonPropertyName("posteriorBase")] bool PosteriorBase);
public sealed record EventoConciliacaoTesouroDto(
    [property: JsonPropertyName("indice")] int Indice,
    [property: JsonPropertyName("dadosInterpretados")] DadosMovimentoTesouroDto? DadosInterpretados,
    [property: JsonPropertyName("candidatos")] IReadOnlyList<Guid> Candidatos);
public sealed record DivergenciaSnapshotTesouroDto(
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("descricao")] string Descricao,
    [property: JsonPropertyName("anterior")] string? Anterior,
    [property: JsonPropertyName("atual")] string? Atual);
public sealed record ComparacaoSnapshotTesouroDto(
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("dataBaseAnterior")] DateOnly? DataBaseAnterior,
    [property: JsonPropertyName("quantidadeAnterior")] decimal QuantidadeAnterior,
    [property: JsonPropertyName("quantidadeProposta")] decimal? QuantidadeProposta,
    [property: JsonPropertyName("divergencias")] IReadOnlyList<DivergenciaSnapshotTesouroDto> Divergencias,
    [property: JsonPropertyName("requerAceite")] bool RequerAceite);
public sealed record ConciliacaoTesouroDto(
    [property: JsonPropertyName("carteiraVersao")] int CarteiraVersao,
    [property: JsonPropertyName("dataReferencia")] DateOnly DataReferencia,
    [property: JsonPropertyName("ativoId")] Guid? AtivoId,
    [property: JsonPropertyName("ativos")] IReadOnlyList<AtivoConciliacaoTesouroDto> Ativos,
    [property: JsonPropertyName("eventos")] IReadOnlyList<EventoConciliacaoTesouroDto> Eventos,
    [property: JsonPropertyName("transacoes")] IReadOnlyList<TransacaoConciliacaoTesouroDto> Transacoes,
    [property: JsonPropertyName("podeConfirmar")] bool PodeConfirmar,
    [property: JsonPropertyName("dataBaseAtiva")] DateOnly? DataBaseAtiva = null,
    [property: JsonPropertyName("podeCriarPosicao")] bool PodeCriarPosicao = false,
    [property: JsonPropertyName("criarPosicao")] bool CriarPosicao = false,
    [property: JsonPropertyName("comparacaoSnapshot")] ComparacaoSnapshotTesouroDto? ComparacaoSnapshot = null,
    [property: JsonPropertyName("posicaoPropostaDisponivel")] bool PosicaoPropostaDisponivel = false,
    [property: JsonPropertyName("motivosCriacaoPosicao")] IReadOnlyList<string>? MotivosCriacaoPosicao = null,
    [property: JsonPropertyName("importacaoInicialSemRevisao")] bool ImportacaoInicialSemRevisao = false);
