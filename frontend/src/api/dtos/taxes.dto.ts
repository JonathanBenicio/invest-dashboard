export interface TaxaEconomicaDto {
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

export interface CriarTaxaEconomicaRequest {
  name: string
  symbol: string
  currentValue: number
  previousValue: number
  description: string
  source: string
}

export interface AtualizarTaxaEconomicaRequest {
  name: string
  symbol: string
  currentValue: number
  previousValue: number
  description: string
  source: string
}
