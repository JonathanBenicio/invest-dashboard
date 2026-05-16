export interface SimulationRequest {
  initialAmount: number
  monthlyContribution: number
  years: number
  annualInterestRate: number
  strategy: 'deterministic' | 'montecarlo'
  volatility?: number
  numberOfSimulations?: number
}

export interface SimulationPointDto {
  month: number
  invested: number
  total: number
  interest: number
}

export interface SimulationResponse {
  points: SimulationPointDto[]
  finalAmount: number
  totalInvested: number
  totalInterest: number
  strategyName: string
}

export interface SimulationStrategy {
  id: string
  name: string
  description: string
}
