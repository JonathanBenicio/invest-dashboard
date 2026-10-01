export interface SimulacaoRequest {
  valorInicial: number
  aporteMensal: number
  anos: number
  taxaJurosAnual: number
  estrategia: 'deterministic' | 'montecarlo'
  volatilidade?: number
  numeroSimulacoes?: number
}

export interface SimulacaoPontoDto {
  mes: number
  investido: number
  total: number
  juros: number
}

export interface SimulacaoResponse {
  pontos: SimulacaoPontoDto[]
  valorFinal: number
  totalInvestido: number
  totalJuros: number
  nomeEstrategia: string
}

export interface SimulacaoEstrategia {
  id: 'deterministic' | 'montecarlo'
  nome: string
  descricao: string
}
