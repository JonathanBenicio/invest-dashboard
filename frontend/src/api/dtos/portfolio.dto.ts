import type { PaginationParams } from './base.dto'

export interface CarteiraDto {
  id: string
  createdAt?: string
  updatedAt?: string
  name: string
  description?: string
  positions: import('./investment.dto').PosicaoInvestimentoDto[]
  totalValue: number
  totalInvested: number
  totalGain: number
  gainPercentage: number
  currency: string
  assetsCount: number
  isActive?: boolean
}

export interface ResumoCarteiraDto extends CarteiraDto {
  assetAllocation: AlocacaoAtivoDto[]
  performanceHistory: PontoPerformanceDto[]
}

export interface AlocacaoAtivoDto {
  category: string
  value: number
  percentage: number
  color?: string
}

export interface PontoPerformanceDto {
  date: string
  value: number
  percentageChange: number
}

export interface PontoHistoricoCarteiraDto {
  date: string
  totalValue: number | null
  isComplete: boolean
  missingTickers: string[]
}

export interface CriarCarteiraRequest {
  name: string
  description?: string
}

export interface AtualizarCarteiraRequest {
  name?: string
  description?: string
}

export interface CarteiraFiltros extends PaginationParams {
  search?: string
}
