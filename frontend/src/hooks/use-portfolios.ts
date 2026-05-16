import { useQuery } from '@tanstack/react-query'
import { portfolioService } from '@/api/services/portfolio.service'
import { investmentService } from '@/api/services/investment.service'
import type { PortfolioFilters, InvestmentFilters } from '@/api/dtos'

export function usePortfolios(filters: PortfolioFilters = {}) {
  return useQuery({
    queryKey: ['portfolios', filters],
    queryFn: () => portfolioService.getAll(filters),
  })
}

export function usePortfolio(id: string) {
  return useQuery({
    queryKey: ['portfolio', id],
    queryFn: () => portfolioService.getById(id),
    enabled: !!id,
  })
}

export function usePortfolioSummary(id: string) {
  return useQuery({
    queryKey: ['portfolio', id, 'summary'],
    queryFn: () => portfolioService.getSummary(id),
    enabled: !!id,
  })
}

// Generic hook for investments in a portfolio, but specialized hooks are better for specific tables
export function usePortfolioInvestments(portfolioId: string, filters: InvestmentFilters = {}) {
  return useQuery({
    queryKey: ['portfolio', portfolioId, 'investments', filters],
    queryFn: () => investmentService.getByPortfolio(portfolioId, filters),
    enabled: !!portfolioId,
  })
}
