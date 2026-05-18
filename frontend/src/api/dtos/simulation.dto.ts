export interface SimulacaoRequest {
  initialAmount: number
  monthlyContribution: number
  years: number
  annualInterestRate: number
  strategy: 'deterministic' | 'montecarlo'
  volatility?: number
  numberOfSimulations?: number
}

export interface SimulacaoPontoDto {
  month: number
  invested: number
  total: number
  interest: number
}

export interface SimulacaoResponse {
  points: SimulacaoPontoDto[]
  finalAmount: number
  totalInvested: number
  totalInterest: number
  strategyName: string
}

export interface SimulacaoEstrategia {
  id: string
  name: string
  description: string
}
