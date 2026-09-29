export interface TransacaoDto {
  id: string
  portfolioId: string
  assetId?: string
  ticker?: string
  type: 'Buy' | 'Sell'
  quantity: number
  unitPrice: number
  fees: number
  totalAmount: number
  realizedGain: number
  realizedCostBasis: number
  transactionDate: string
  notes?: string
}

export interface RegistrarTransacaoRequest {
  portfolioId: string
  assetId?: string
  ticker?: string
  type: 'Buy' | 'Sell'
  quantity: number
  unitPrice: number
  fees: number
  transactionDate: string
  idempotencyKey: string
  assetClass?: string
  notes?: string
}

export interface TransacaoFiltros {
  portfolioId?: string
  type?: string
  ticker?: string
}
