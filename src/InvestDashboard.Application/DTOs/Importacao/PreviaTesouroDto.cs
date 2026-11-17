using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record CampoPreviaTesouroDto(
    [property: JsonPropertyName("rotulo")] string Rotulo,
    [property: JsonPropertyName("valor")] string Valor);

public sealed record MovimentoPreviaTesouroDto(
    [property: JsonPropertyName("campos")] IReadOnlyList<CampoPreviaTesouroDto> Campos,
    [property: JsonPropertyName("dadosInterpretados")] DadosMovimentoTesouroDto? DadosInterpretados = null);

/// <summary>Interpretação preliminar, sem identidade, conciliação ou confirmação.</summary>
public sealed record DadosMovimentoTesouroDto(
    [property: JsonPropertyName("data")] DateOnly? Data,
    [property: JsonPropertyName("natureza")] string? Natureza,
    [property: JsonPropertyName("tipo")] string? Tipo,
    [property: JsonPropertyName("quantidade")] decimal? Quantidade,
    [property: JsonPropertyName("preco")] decimal? Preco,
    [property: JsonPropertyName("taxaAnual")] decimal? TaxaAnual,
    [property: JsonPropertyName("valor")] decimal? Valor,
    [property: JsonPropertyName("camposNaoInterpretados")] IReadOnlyList<string> CamposNaoInterpretados,
    [property: JsonPropertyName("camposNaoAplicaveis")] IReadOnlyList<string>? CamposNaoAplicaveis = null);

public sealed record PosicaoPreviaTesouroDto(
    [property: JsonPropertyName("numeroLinha")] int NumeroLinha,
    [property: JsonPropertyName("camposBrutos")] IReadOnlyDictionary<string, string> CamposBrutos,
    [property: JsonPropertyName("dadosInterpretados")] DadosPosicaoTesouroDto? DadosInterpretados = null);

/// <summary>Interpretação preliminar dos dados de posição do HTML; não representa valor atual garantido.</summary>
public sealed record DadosPosicaoTesouroDto(
    [property: JsonPropertyName("dataAplicacao")] DateOnly? DataAplicacao,
    [property: JsonPropertyName("quantidadeTitulos")] decimal? QuantidadeTitulos,
    [property: JsonPropertyName("precoAplicacao")] decimal? PrecoAplicacao,
    [property: JsonPropertyName("valorInvestido")] decimal? ValorInvestido,
    [property: JsonPropertyName("rentabilidadeContratada")] string RentabilidadeContratada,
    [property: JsonPropertyName("rentabilidadeBrutaAnualizada")] decimal? RentabilidadeBrutaAnualizada,
    [property: JsonPropertyName("rentabilidadeBrutaAcumulada")] decimal? RentabilidadeBrutaAcumulada,
    [property: JsonPropertyName("valorBruto")] decimal? ValorBruto,
    [property: JsonPropertyName("diasCorridos")] int? DiasCorridos,
    [property: JsonPropertyName("aliquotaIr")] decimal? AliquotaIr,
    [property: JsonPropertyName("impostoPrevistoIr")] decimal? ImpostoPrevistoIr,
    [property: JsonPropertyName("impostoPrevistoIof")] decimal? ImpostoPrevistoIof,
    [property: JsonPropertyName("taxaDevidaB3")] decimal? TaxaDevidaB3,
    [property: JsonPropertyName("taxaDevidaInstituicao")] decimal? TaxaDevidaInstituicao,
    [property: JsonPropertyName("valorLiquido")] decimal? ValorLiquido,
    [property: JsonPropertyName("camposNaoInterpretados")] IReadOnlyList<string> CamposNaoInterpretados);

/// <summary>Leitura preliminar sem conciliação ou efeito sobre carteira.</summary>
public sealed record PreviaTesouroDto(
    [property: JsonPropertyName("carteiraId")] Guid CarteiraId,
    [property: JsonPropertyName("formato")] string Formato,
    [property: JsonPropertyName("layout")] string Layout,
    [property: JsonPropertyName("titulo")] string? Titulo,
    [property: JsonPropertyName("vencimento")] string? Vencimento,
    [property: JsonPropertyName("vencimentoOriginal")] string? VencimentoOriginal,
    [property: JsonPropertyName("posicoes")] IReadOnlyList<PosicaoPreviaTesouroDto> Posicoes,
    [property: JsonPropertyName("movimentacoes")] IReadOnlyList<MovimentoPreviaTesouroDto> Movimentacoes,
    [property: JsonPropertyName("linhasNaoInterpretadas")] IReadOnlyList<int> LinhasNaoInterpretadas,
    [property: JsonPropertyName("avisos")] IReadOnlyList<string> Avisos,
    [property: JsonPropertyName("exigeRevisao")] bool ExigeRevisao,
    [property: JsonPropertyName("coberturaCompleta")] bool CoberturaCompleta,
    [property: JsonPropertyName("podeConfirmar")] bool PodeConfirmar);
