/**
 * Brapi Service
 * Handles market data from brapi.dev
 */

import { BRAPI_CONFIG } from '../env'
import type {
  BrapiQuoteResponse,
  BrapiAvailableResponse,
  BrapiHistoricalResponse
} from '../dtos'

const getUrl = (endpoint: string, params: Record<string, string> = {}) => {
  const url = new URL(`${BRAPI_CONFIG.BASE_URL}${endpoint}`)

  if (BRAPI_CONFIG.TOKEN) {
    url.searchParams.append('token', BRAPI_CONFIG.TOKEN)
  }

  Object.entries(params).forEach(([key, value]) => {
    url.searchParams.append(key, value)
  })

  return url.toString()
}

const handleResponse = async <T>(response: Response): Promise<T> => {
  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}))
    throw new Error(errorBody.message || `Brapi request failed with status ${response.status}`)
  }
  return response.json()
}

export const brapiService = {
  /**
   * Get quotes for one or more tickers
   */
  getQuote: async (tickers: string[]): Promise<BrapiQuoteResponse> => {
    const url = getUrl(`/quote/${tickers.join(',')}`)
    const response = await fetch(url)
    return handleResponse<BrapiQuoteResponse>(response)
  },

  /**
   * Get all available tickers
   */
  getAvailableTickers: async (search?: string): Promise<BrapiAvailableResponse> => {
    const params: Record<string, string> = {}
    if (search) {
      params.search = search
    }
    const url = getUrl('/available', params)
    const response = await fetch(url)
    return handleResponse<BrapiAvailableResponse>(response)
  },

  /**
   * Get historical data for a ticker
   */
  getHistoricalData: async (
    ticker: string,
    range: string = '1mo',
    interval: string = '1d'
  ): Promise<BrapiHistoricalResponse> => {
    const url = getUrl(`/quote/${ticker}`, { range, interval })
    const response = await fetch(url)
    return handleResponse<BrapiHistoricalResponse>(response)
  }
}
