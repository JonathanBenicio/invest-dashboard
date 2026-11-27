import { api } from '@/api/client'
import type { RespostaApi } from '@/api/dtos/base.dto'

export interface PreviaTesouro {
  carteiraId: string
  formato: string
  layout: string | null
  titulo: string | null
  vencimento: string | null
  vencimentoOriginal: string | null
  mesReferencia?: number | null
  anoReferencia?: number | null
  instituicaoHtml?: string | null
  posicoes: {
    numeroLinha: number
    camposBrutos: Record<string, string>
    dadosInterpretados?: { camposNaoInterpretados: string[] } | null
  }[]
  movimentacoes: {
    campos: { rotulo: string; valor: string }[]
    dadosInterpretados?: {
      data: string | null
      natureza: string | null
      tipo: string | null
      quantidade: number | null
      preco: number | null
      taxaAnual: number | null
      valor: number | null
      camposNaoInterpretados: string[]
      camposNaoAplicaveis?: string[] | null
      camposNaoInformados?: string[] | null
    } | null
  }[]
  linhasNaoInterpretadas: number[]
  avisos: string[]
  exigeRevisao: boolean
  coberturaCompleta: boolean
  podeConfirmar: boolean
}

export interface TituloTesouroPublico {
  tipoTitulo: string
  dataVencimento: string
  ultimaDataDisponivel: string
}

export interface ResumoLoteTesouro {
  id: string
  carteiraId: string
  dataReferencia: string
  formato: string
  estado: string
  criadoEmUtc: string
  confirmadoEmUtc: string | null
}

export interface LoteTesouro extends ResumoLoteTesouro { previa: PreviaTesouro; hashPrevia: string }

export type TipoDivergenciaSnapshotTesouro =
  | 'snapshot_anterior_indisponivel'
  | 'posicao_nao_observada'
  | 'movimento_manual_na_janela'
  | 'saldo_nao_explicado'
  | 'cobertura_nao_comprovada'
  | 'movimento_sem_quantidade'
  | 'lote_anterior_sem_identidade'
  | 'lote_nao_observado'
  | 'evento_ambiguo'
  | 'evento_anterior_sem_identidade'
  | 'evento_nao_observado'

export interface ConciliacaoTesouro {
  carteiraVersao: number
  dataReferencia: string
  dataBaseAtiva?: string | null
  ativoId: string | null
  ativos: { ativoId: string; ticker: string; nome: string; vencimento: string | null }[]
  eventos: { indice: number; dadosInterpretados: PreviaTesouro['movimentacoes'][number]['dadosInterpretados']; candidatos: string[] }[]
  transacoes: { id: string; tipo: string; data: string; quantidade: number; preco: number; posteriorBase: boolean }[]
  podeConfirmar: boolean
  posicaoPropostaDisponivel: boolean
  podeCriarPosicao: boolean
  importacaoInicialSemRevisao?: boolean
  criacaoPosicaoSemRevisao?: boolean
  criarPosicao: boolean
  motivosCriacaoPosicao?: string[] | null
  encerramentoPosicao?: {
    podeEncerrar: boolean
    quantidadeAnterior: number | null
    quantidadeReconciliada: number | null
    motivo: string | null
  } | null
  comparacaoSnapshot?: {
    hash: string
    dataBaseAnterior: string | null
    quantidadeAnterior: number
    quantidadeProposta: number | null
    requerAceite: boolean
    divergencias: { tipo: TipoDivergenciaSnapshotTesouro; chave: string; descricao: string; anterior: string | null; atual: string | null }[]
  } | null
}

export interface RevisaoTesouro {
  id: string
  indiceEvento: number
  revisao: number
  decisao: 'vincular' | 'sem_vinculo'
  ativoId: string | null
  transacaoId: string | null
  carteiraVersao: number
  criadoEmUtc: string
  divergencias?: string[]
}

export interface ExtratoOficialTesouro {
  baseAtiva: {
    loteId: string
    dataReferencia: string
    confirmadoEmUtc: string
    quantidade: number
  } | null
  eventos: {
    indiceEvento: number
    transacaoId: string | null
    origem: 'extrato_oficial_tesouro'
    dados: PreviaTesouro['movimentacoes'][number] | null
  }[]
  proximoIndice: number | null
  resumoUltimaImportacao: {
    loteAnteriorId: string | null
    dataReferenciaAnterior: string | null
    dataReferenciaAtual: string
    quantidadeAnterior: number | null
    quantidadeAtual: number
    variacaoQuantidade: number | null
    divergencias: {
      tipo: string
      descricao: string
      anterior: string | null
      atual: string | null
    }[]
  } | null
}

export interface RegistrarRevisaoTesouro {
  indiceEvento: number
  decisao: RevisaoTesouro['decisao']
  ativoId: string | null
  transacaoId: string | null
  carteiraVersao: number
  revisaoAnterior: number
  hashPrevia: string
}

interface MetadadosTesouro {
  origem: string
  urlFonte: string
  coletadoEmUtc: string
  ultimaDataDisponivel: string
}

export interface CotacaoTesouroPublico {
  dataBase: string
  taxaCompraManha: number
  taxaVendaManha: number
  puCompraManha: number
  puVendaManha: number
  puBaseManha: number
}

export const tesouroDiretoService = {
  excluirPrevia: (carteiraId: string, loteId: string) => api.delete<void>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}`,
  ),
  extratoOficial: (carteiraId: string, ativoId: string, depoisDoIndice: number, loteId?: string) =>
    api.get<RespostaApi<ExtratoOficialTesouro>>(
      `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/posicoes/${encodeURIComponent(ativoId)}/extrato`,
      { params: { depoisDoIndice, loteId } },
    ),
  previa: (carteiraId: string, arquivo: File) => {
    const dados = new FormData()
    dados.append('arquivo', arquivo)
    return api.post<RespostaApi<PreviaTesouro>>(`/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/previa`, dados, { timeoutMs: 60_000 })
  },
  criarLote: (carteiraId: string, dataReferencia: string, arquivo: File) => {
    const dados = new FormData()
    dados.append('arquivo', arquivo)
    return api.post<RespostaApi<LoteTesouro>>(`/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes`, dados, {
      params: { dataReferencia }, timeoutMs: 60_000,
    })
  },
  lote: (carteiraId: string, loteId: string) => api.get<RespostaApi<LoteTesouro>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}`,
  ),
  lotes: (carteiraId: string, pagina = 1) => api.get<RespostaApi<ResumoLoteTesouro[]>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes`, { params: { pagina } },
  ),
  conciliacao: (carteiraId: string, loteId: string, ativoId: string | null) => api.post<RespostaApi<ConciliacaoTesouro>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}/conciliacao`, { ativoId },
  ),
  confirmar: (carteiraId: string, loteId: string, pedido: {
    ativoId: string | null; criarPosicao: boolean; carteiraVersao: number; revisoes: { indiceEvento: number; revisao: number }[]
    importacaoInicialSemRevisao?: boolean; criacaoPosicaoSemRevisao?: boolean; hashPrevia?: string
    hashDivergencias?: string | null; aceitarDivergencias?: boolean
  }) => api.post<RespostaApi<LoteTesouro>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}/confirmacao`, pedido,
  ),
  revisoes: (carteiraId: string, loteId: string, indiceEvento: number) => api.get<RespostaApi<RevisaoTesouro[]>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}/revisoes/${indiceEvento}`,
  ),
  registrarRevisao: (carteiraId: string, loteId: string, pedido: RegistrarRevisaoTesouro) => api.post<RespostaApi<RevisaoTesouro>>(
    `/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/lotes/${encodeURIComponent(loteId)}/revisoes`, pedido,
  ),
  titulos: () => api.get<RespostaApi<MetadadosTesouro & { titulos: TituloTesouroPublico[] }>>('/tesouro-direto/titulos', { timeoutMs: 130_000 }),
  cotacoes: (titulo: TituloTesouroPublico, dataDe: string, dataAte: string) =>
    api.get<RespostaApi<MetadadosTesouro & { cotacoes: CotacaoTesouroPublico[] }>>('/tesouro-direto/cotacoes', {
      params: { tipoTitulo: titulo.tipoTitulo, dataVencimento: titulo.dataVencimento, dataDe, dataAte }, timeoutMs: 130_000,
    }),
}
