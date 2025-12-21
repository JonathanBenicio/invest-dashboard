/**
 * Investment DTOs
 * Types for fixed income and variable income investments
 */

import type { BaseEntity, PaginationParams } from './base.dto'

/**
 * Investment type enum
 */
export type InvestmentType = 'fixed_income' | 'variable_income'

/**
 * Fixed income investment subtypes
 */
export type FixedIncomeType = 'CDB' | 'LCI' | 'LCA' | 'TESOURO_DIRETO' | 'DEBENTURE' | 'CRI' | 'CRA'

/**
 * Variable income investment subtypes
 */
export type VariableIncomeType = 'ACAO' | 'FII' | 'ETF' | 'BDR' | 'CRYPTO'

/**
 * Base investment entity
 */
export interface InvestmentDto extends BaseEntity {
  portfolioId: string
  name: string
  ticker?: string
  type: InvestmentType
  subtype: FixedIncomeType | VariableIncomeType
  quantity: number
  averagePrice: number
  currentPrice: number
  totalInvested: number
  currentValue: number
  gain: number
  gainPercentage: number
  currency: string
}

/**
 * Fixed income specific fields
 */
export interface FixedIncomeDto extends InvestmentDto {
  type: 'fixed_income'
  subtype: FixedIncomeType
  issuer: string
  interestRate: number
  indexer?: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  maturityDate: string
  purchaseDate: string
}

/**
 * Variable income specific fields
 */
export interface VariableIncomeDto extends InvestmentDto {
  type: 'variable_income'
  subtype: VariableIncomeType
  sector?: string
  dividendYield?: number
  lastDividend?: number
}

/**
 * Create fixed income investment request
 */
export interface CreateFixedIncomeRequest {
  portfolioId: string
  name: string
  subtype: FixedIncomeType
  issuer: string
  quantity: number
  averagePrice: number
  interestRate: number
  indexer?: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  maturityDate: string
  purchaseDate: string
}

/**
 * Create variable income investment request
 */
export interface CreateVariableIncomeRequest {
  portfolioId: string
  ticker: string
  subtype: VariableIncomeType
  quantity: number
  averagePrice: number
  purchaseDate: string
}

/**
 * Update investment request
 */
export interface UpdateInvestmentRequest {
  quantity?: number
  averagePrice?: number
  currentPrice?: number
}

/**
 * Investment list filters
 */
export interface InvestmentFilters extends PaginationParams {
  portfolioId?: string
  type?: InvestmentType
  subtype?: FixedIncomeType | VariableIncomeType
  search?: string
}

/**
 * Investment summary for dashboard
 */
export interface InvestmentSummaryDto {
  totalInvested: number
  currentValue: number
  totalGain: number
  gainPercentage: number
  fixedIncomeTotal: number
  variableIncomeTotal: number
  topPerformers: InvestmentDto[]
  worstPerformers: InvestmentDto[]
}
