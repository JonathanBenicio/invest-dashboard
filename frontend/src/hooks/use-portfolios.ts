import { useQuery } from '@tanstack/react-query'
import { portfolioService } from '@/api/services/portfolio.service'
import { investmentService } from '@/api/services/investment.service'
import type { CarteiraFiltros, InvestimentoFiltros } from '@/api/dtos'

export function usePortfolios(filters: CarteiraFiltros = {}) {
  return useQuery({
    queryKey: ['portfolios', filters],
    queryFn: () => portfolioService.getAll(filters),
  })
}

export function useAllAccessiblePortfolios() {
  return useQuery({
    queryKey: ['portfolios', 'all-accessible'],
    queryFn: async () => {
      const primeira = await portfolioService.getAll({ pagina: 1, itensPorPagina: 100 })
      const carteiras = [...primeira.dados]
      for (let pagina = 2; pagina <= primeira.paginacao.totalPaginas; pagina += 1) {
        const resposta = await portfolioService.getAll({ pagina, itensPorPagina: 100 })
        carteiras.push(...resposta.dados)
      }
      return { ...primeira, dados: carteiras }
    },
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
export function usePortfolioInvestments(portfolioId: string, filters: InvestimentoFiltros = {}) {
  return useQuery({
    queryKey: ['portfolio', portfolioId, 'investments', filters],
    queryFn: () => investmentService.getByPortfolio(portfolioId, filters),
    enabled: !!portfolioId,
  })
}
