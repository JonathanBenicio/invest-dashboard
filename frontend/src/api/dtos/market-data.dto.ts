export interface MarketQuoteDto {
  symbol: string
  name: string
  price: number
  observedAtUtc: string
  currency: string
  sector?: string
  subtype?: string
}

export interface MarketSearchResultDto {
  symbol: string
  name: string
  currency: string
  sector?: string
  subtype: string
}

export interface MarketHistoryPointDto {
  symbol: string
  dateUtc: string
  price: number
  source: string
  isAdjusted: boolean
}
