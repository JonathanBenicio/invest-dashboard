/**
 * DTOs de Investimento
 * Tipos para investimentos de renda fixa e variável
 */

import type { ParametrosPaginacao } from './base.dto'

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
  criadoEm?: string
  atualizadoEm?: string
  carteiraId: string
  carteiraNome?: string
  grupoId?: string
  titular?: string
  instituicaoFinanceira?: string
  ativoId: string
  situacao: 'open' | 'matured' | 'closed'
  dataCompra?: string
  nome: string
  ticker: string
  tipo: TipoInvestimento
  subtipo: TipoRendaFixa | TipoRendaVariavel
  setor?: string
  quantidade: number
  precoMedio: number
  precoAtual: number
  origemPrecoAtual?: string
  precoObservadoEmUtc?: string
  totalInvestido: number
  valorAtual: number
  ganho: number
  percentualGanho: number
  moeda: string
  emissor?: string
  taxaJuros?: number
  indexador?: string
  dataVencimento?: string
  liquidez?: string
  convencao?: string
}

export interface PrecoHistoricoDto {
  data: string
  preco: number
  origem: string
  ajustado: boolean
}

/**
 * Campos específicos de renda fixa
 */
export interface RendaFixaDto extends PosicaoInvestimentoDto {
  tipo: 'fixed_income'
  subtipo: TipoRendaFixa
}

/**
 * Campos específicos de renda variável
 */
export interface RendaVariavelDto extends PosicaoInvestimentoDto {
  tipo: 'variable_income'
  subtipo: TipoRendaVariavel
  setor?: string
  dividendYield?: number
  lastDividend?: number
}

/**
 * Requisição para criar renda fixa
 */
export interface CriarRendaFixaRequest {
  carteiraId: string
  nome: string
  subtipo: TipoRendaFixa
  emissor: string
  valorPrincipal: number
  valorExtrato: number
  taxaJuros: number
  indexador: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  dataVencimento: string
  dataCompra: string
  chaveIdempotencia: string
  liquidez?: string
  convencao?: string
}

/**
 * Requisição para criar renda variável
 */
export interface CriarRendaVariavelRequest {
  carteiraId: string
  ticker: string
  subtipo: TipoRendaVariavel
  nome?: string
  setor?: string
  quantidade: number
  precoUnitario: number
  taxas: number
  dataTransacao: string
  chaveIdempotencia: string
}

/**
 * Requisição para atualizar investimento
 */
export interface AtualizarInvestimentoRequest {
  valorTotal: number
  data: string
}

/**
 * Filtros para lista de investimentos
 */
export interface InvestimentoFiltros extends ParametrosPaginacao {
  carteiraId?: string
  grupoId?: string
  tipo?: TipoInvestimento
  subtipo?: TipoRendaFixa | TipoRendaVariavel
  busca?: string
  emissor?: string
  setor?: string
  situacao?: 'open' | 'matured' | 'closed'
}

/**
 * Resumo de investimento para dashboard
 */
export interface ResumoInvestimentoDto {
  totalInvestido: number
  valorAtual: number
  ganhoTotal: number
  percentualGanho: number
  totalRendaFixa: number
  totalRendaVariavel: number
  melhoresPosicoes: PosicaoInvestimentoDto[]
  pioresPosicoes: PosicaoInvestimentoDto[]
}

export interface ProventoDto {
  id: string
  ticker: string
  tipo: string
  valor: number
  dataEx?: string
  dataPagamento?: string
}
