import { api } from '@/api/client'
import type { RespostaApi, PontoHistoricoMercadoDto, CotacaoMercadoDto, ResultadoBuscaMercadoDto } from '@/api/dtos'

export const marketDataService = {
  search: (query: string) =>
    api.get<RespostaApi<ResultadoBuscaMercadoDto[]>>('/market-data/search', { params: { consulta: query } }),

  getQuotes: (symbols: string[]) =>
    api.get<RespostaApi<CotacaoMercadoDto[]>>('/market-data/quotes', {
      params: { simbolos: symbols.join(',') },
    }),

  getDailyHistory: (symbols: string[], startDate: string, endDate: string) =>
    api.get<RespostaApi<PontoHistoricoMercadoDto[]>>('/market-data/history', {
      params: { simbolos: symbols.join(','), dataInicio: startDate, dataFim: endDate },
    }),
}
