/**
 * TanStack Query Keys
 * Centralized query keys for cache management
 */

export const queryKeys = {
  /**
   * Auth related keys
   */
  auth: {
    all: ['auth'] as const,
    user: () => [...queryKeys.auth.all, 'user'] as const,
  },

  /**
   * Portfolio related keys
   */
  portfolios: {
    all: ['portfolios'] as const,
    lists: () => [...queryKeys.portfolios.all, 'list'] as const,
    list: (filters?: Record<string, unknown>) =>
      [...queryKeys.portfolios.lists(), filters] as const,
    details: () => [...queryKeys.portfolios.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.portfolios.details(), id] as const,
    summary: (id: string) => [...queryKeys.portfolios.all, 'summary', id] as const,
  },

  /**
   * Investment related keys
   */
  investments: {
    all: ['investments'] as const,
    lists: () => [...queryKeys.investments.all, 'list'] as const,
    list: (filters?: Record<string, unknown>) =>
      [...queryKeys.investments.lists(), filters] as const,
    details: () => [...queryKeys.investments.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.investments.details(), id] as const,
    byPortfolio: (portfolioId: string) =>
      [...queryKeys.investments.all, 'portfolio', portfolioId] as const,
    summary: () => [...queryKeys.investments.all, 'summary'] as const,
  },
} as const
