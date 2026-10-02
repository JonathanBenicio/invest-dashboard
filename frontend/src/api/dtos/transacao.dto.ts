export interface TransacaoDto {
  id: string
  carteiraId: string
  titularId?: string
  ativoId?: string
  ticker?: string
  tipo: 'Buy' | 'Sell'
  quantidade: number
  precoUnitario: number
  taxas: number
  valorTotal: number
  ganhoRealizado: number
  custoBaseRealizado: number
  modalidadeFiscal: 'NaoInformada' | 'Comum' | 'DayTrade'
  dataTransacao: string
  observacoes?: string
}

export interface RegistrarTransacaoRequest {
  carteiraId: string
  ativoId?: string
  ticker?: string
  tipo: 'Buy' | 'Sell'
  quantidade: number
  precoUnitario: number
  taxas: number
  modalidadeFiscal?: 'NaoInformada' | 'Comum' | 'DayTrade'
  dataTransacao: string
  chaveIdempotencia: string
  classeAtivo?: string
  nome?: string
  setor?: string
  subtipo?: string
  emissor?: string
  indexador?: string
  taxaJuros?: number
  dataVencimento?: string
  liquidez?: string
  convencao?: string
  valorInicialExtrato?: number
  observacoes?: string
}

export interface AtualizarTransacaoRequest {
  tipo: 'Buy' | 'Sell'
  quantidade: number
  precoUnitario: number
  taxas: number
  modalidadeFiscal?: 'NaoInformada' | 'Comum' | 'DayTrade'
  dataTransacao: string
  observacoes?: string
}

export interface TransacaoFiltros {
  carteiraId?: string
  tipo?: string
  ticker?: string
}
