/**
 * Base API Response Types
 * Common types used across all API responses
 */

/**
 * Standard API Response wrapper
 */
export interface RespostaApi<T> {
  dados: T
  sucesso: boolean
  mensagem?: string
}

/**
 * Paginated API Response
 */
export interface RespostaPaginada<T> {
  dados: T[]
  sucesso: boolean
  mensagem?: string
  paginacao: {
    pagina: number
    itensPorPagina: number
    totalItens: number
    totalPaginas: number
    temProximaPagina: boolean
    temPaginaAnterior: boolean
  }
}

/**
 * Pagination request parameters
 */
export interface ParametrosPaginacao {
  pagina?: number
  itensPorPagina?: number
  ordenarPor?: string
  ordem?: 'asc' | 'desc'
}

/**
 * Base entity with common fields
 */
export interface EntidadeBase {
  id: string
  criadoEm: string
  atualizadoEm: string
}

/**
 * Error response from API
 */
export interface RespostaErro {
  sucesso: false
  mensagem: string
  codigo?: string
  erros?: Record<string, string[]>
}
