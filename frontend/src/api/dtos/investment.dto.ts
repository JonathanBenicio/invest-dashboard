/**
 * DTOs de Investimento
 * Tipos para investimentos de renda fixa e variável
 */

import type { PaginationParams } from './base.dto'

/**
 * Tipo de investimento
 */
export type TipoInvestimento = 'fixed_income' | 'variable_income'

/**
 * Subtipos de renda fixa
 */
export type TipoRendaFixa = 'CDB' | 'LCI' | 'LCA' | 'TESOURO_DIRETO' | 'DEBENTURE' | 'CRI' | 'CRA'

/**
 * Subtipos de renda variável
 */
export type TipoRendaVariavel = 'ACAO' | 'FII' | 'ETF' | 'BDR' | 'CRYPTO'

/**
 * Entidade base de investimento
 */
export interface PosicaoInvestimentoDto {
  id: string
  createdAt?: string
  updatedAt?: string
  portfolioId: string
  assetId: string
  status: 'open' | 'closed'
  purchaseDate?: string
  name: string
  ticker: string
  type: TipoInvestimento
  subtype: TipoRendaFixa | TipoRendaVariavel
  sector?: string
  quantity: number
  averagePrice: number
  currentPrice: number
  totalInvested: number
  currentValue: number
  gain: number
  gainPercentage: number
  currency: string
  issuer?: string
  sector?: string
  interestRate?: number
  indexer?: string
  maturityDate?: string
}

export interface PrecoHistoricoDto {
  date: string
  price: number
  source: string
  isAdjusted: boolean
}

/**
 * Campos específicos de renda fixa
 */
export interface RendaFixaDto extends PosicaoInvestimentoDto {
  type: 'fixed_income'
  subtype: TipoRendaFixa
}

/**
 * Campos específicos de renda variável
 */
export interface RendaVariavelDto extends PosicaoInvestimentoDto {
  type: 'variable_income'
  subtype: TipoRendaVariavel
  sector?: string
  dividendYield?: number
  lastDividend?: number
}

/**
 * Requisição para criar renda fixa
 */
export interface CriarRendaFixaRequest {
  portfolioId: string
  name: string
  subtype: TipoRendaFixa
  issuer: string
  principal: number
  statementValue: number
  interestRate: number
  indexer: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  maturityDate: string
  purchaseDate: string
  idempotencyKey: string
}

/**
 * Requisição para criar renda variável
 */
export interface CriarRendaVariavelRequest {
  portfolioId: string
  ticker: string
  subtype: TipoRendaVariavel
  name?: string
  sector?: string
  quantity: number
  unitPrice: number
  fees: number
  transactionDate: string
  idempotencyKey: string
}

/**
 * Requisição para atualizar investimento
 */
export interface AtualizarInvestimentoRequest {
  totalValue: number
  date: string
}

/**
 * Filtros para lista de investimentos
 */
export interface InvestimentoFiltros extends PaginationParams {
  portfolioId?: string
  type?: TipoInvestimento
  subtype?: TipoRendaFixa | TipoRendaVariavel
  search?: string
  issuer?: string
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

/**
 * Resumo de investimento para dashboard
 */
export interface ResumoInvestimentoDto {
  totalInvested: number
  currentValue: number
  totalGain: number
  gainPercentage: number
  fixedIncomeTotal: number
  variableIncomeTotal: number
  topPerformers: PosicaoInvestimentoDto[]
  worstPerformers: PosicaoInvestimentoDto[]
}

export interface ProventoDto {
  id: string
  ticker: string
  type: string
  amount: number
  exDate?: string
  paymentDate?: string
}
