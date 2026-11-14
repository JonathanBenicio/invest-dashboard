import { api } from '@/api/client'
import type { RespostaApi } from '@/api/dtos/base.dto'

export interface PreviaTesouro {
  carteiraId: string
  formato: string
  layout: string | null
  titulo: string | null
  vencimento: string | null
  vencimentoOriginal: string | null
  posicoes: { numeroLinha: number; camposBrutos: Record<string, string> }[]
  movimentacoes: { campos: { rotulo: string; valor: string }[] }[]
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
  previa: (carteiraId: string, arquivo: File) => {
    const dados = new FormData()
    dados.append('arquivo', arquivo)
    return api.post<RespostaApi<PreviaTesouro>>(`/portfolios/${encodeURIComponent(carteiraId)}/tesouro-direto/previa`, dados, { timeoutMs: 60_000 })
  },
  titulos: () => api.get<RespostaApi<MetadadosTesouro & { titulos: TituloTesouroPublico[] }>>('/tesouro-direto/titulos', { timeoutMs: 130_000 }),
  cotacoes: (titulo: TituloTesouroPublico, dataDe: string, dataAte: string) =>
    api.get<RespostaApi<MetadadosTesouro & { cotacoes: CotacaoTesouroPublico[] }>>('/tesouro-direto/cotacoes', {
      params: { tipoTitulo: titulo.tipoTitulo, dataVencimento: titulo.dataVencimento, dataDe, dataAte }, timeoutMs: 130_000,
    }),
}
