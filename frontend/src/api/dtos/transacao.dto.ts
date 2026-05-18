export interface TransacaoDto {
  id: string
  carteiraId: string
  ativoId?: string
  ticker?: string
  tipo: 'Buy' | 'Sell' | 'Deposit' | 'Withdrawal'
  quantidade: number
  precoUnitario: number
  taxaCorretagem: number
  valorTotal: number
  dataTransacao: string
  observacoes?: string
}

export interface RegistrarTransacaoRequest {
  carteiraId: string
  ativoId?: string
  ticker?: string
  tipo: 'Buy' | 'Sell' | 'Deposit' | 'Withdrawal'
  quantidade: number
  precoUnitario: number
  taxaCorretagem: number
  dataTransacao: string
  observacoes?: string
}

export interface TransacaoFiltros {
  carteiraId?: string
  tipo?: string
  ticker?: string
}
