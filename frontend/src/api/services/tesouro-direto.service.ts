import { api } from '@/api/client'
import type { RespostaApi } from '@/api/dtos/base.dto'

export interface PreviaTesouro {
  carteiraId: string
  formato: string
  layout: string | null
  titulo: string | null
  vencimento: string | null
  vencimentoOriginal: string | null
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

export interface LoteTesouro extends ResumoLoteTesouro { previa: PreviaTesouro }

export interface ConciliacaoTesouro {
  carteiraVersao: number
  dataReferencia: string
  dataBaseAtiva?: string | null
  ativoId: string | null
  ativos: { ativoId: string; ticker: string }[]
  eventos: { indice: number; dadosInterpretados: PreviaTesouro['movimentacoes'][number]['dadosInterpretados']; candidatos: string[] }[]
  transacoes: { id: string; tipo: string; data: string; quantidade: number; preco: number; posteriorBase: boolean }[]
  podeConfirmar: boolean
  podeCriarPosicao: boolean
  criarPosicao: boolean
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

export interface RegistrarRevisaoTesouro {
  indiceEvento: number
  decisao: RevisaoTesouro['decisao']
  ativoId: string | null
  transacaoId: string | null
  carteiraVersao: number
  revisaoAnterior: number
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

export interface AvaliacaoPosicaoTesouroDto {
  posicaoId: string
  ativoId: string
  ticker: string
  titulo: string
  dataVencimento: string
  quantidade: number
  origemCotacao: string | null
  urlFonteCotacao: string | null
  dataCotacao: string | null
  coletadoEmUtc: string | null
  puBase: number | null
  valorBrutoMercado: number | null
  dataExtrato: string | null
  valorBrutoExtrato: number | null
  impostoRendaExtrato: number | null
  iofExtrato: number | null
  valorLiquidoExtrato: number | null
  dataTaxasExtrato: string | null
  taxaCustodiaExtrato: number | null
  taxaInstituicaoExtrato: number | null
  impostoRendaEstimado: number | null
  iofEstimado: number | null
  dataCalculoImpostos: string | null
  valorLiquidoEstimado: number | null
  motivoLiquidoIndisponivel: string | null
  avisos: string[]
}

export const tesouroDiretoService = {
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
  avaliacao: (posicaoId: string) =>
    api.get<RespostaApi<AvaliacaoPosicaoTesouroDto>>(`/investments/${encodeURIComponent(posicaoId)}/tesouro-direto/avaliacao`, { timeoutMs: 130_000 }),
}
