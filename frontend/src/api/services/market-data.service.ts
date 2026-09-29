import { api } from '@/api/client'
import type { ApiResponse, MarketHistoryPointDto, MarketQuoteDto, MarketSearchResultDto } from '@/api/dtos'

export const marketDataService = {
  search: (query: string) =>
    api.get<ApiResponse<MarketSearchResultDto[]>>('/market-data/search', { params: { query } }),

  getQuotes: (symbols: string[]) =>
    api.get<ApiResponse<MarketQuoteDto[]>>('/market-data/quotes', {
      params: { symbols: symbols.join(',') },
    }),

  getDailyHistory: (symbols: string[], startDate: string, endDate: string) =>
    api.get<ApiResponse<MarketHistoryPointDto[]>>('/market-data/history', {
      params: { symbols: symbols.join(','), startDate, endDate },
    }),
}
