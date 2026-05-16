/**
 * Brapi DTOs
 */

export interface BrapiQuote {
  symbol: string
  shortName: string
  longName: string
  currency: string
  regularMarketPrice: number
  regularMarketDayHigh: number
  regularMarketDayLow: number
  regularMarketDayRange: string
  regularMarketChange: number
  regularMarketChangePercent: number
  regularMarketTime: string
  marketCap: number
  regularMarketVolume: number
  regularMarketPreviousClose: number
  regularMarketOpen: number
  logourl: string
}

export interface BrapiQuoteResponse {
  results: BrapiQuote[]
  requestedAt: string
  took: string
}

export interface BrapiAvailableResponse {
  stocks: string[]
}

export interface BrapiHistoricalData {
  date: number
  open: number
  high: number
  low: number
  close: number
  volume: number
  adjustedClose: number
}

export interface BrapiHistoricalResponse {
  results: {
    symbol: string
    historicalDataPrice: BrapiHistoricalData[]
  }[]
  requestedAt: string
  took: string
}
