export interface EconomicRateDto {
  id: string
  name: string
  symbol: string
  currentValue: number
  previousValue: number
  variation: number
  description: string
  source: string
  lastUpdate: string
}

export interface CreateEconomicRateRequest {
  name: string
  symbol: string
  currentValue: number
  previousValue: number
  description: string
  source: string
}
