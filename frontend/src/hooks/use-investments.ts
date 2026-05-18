import { useQuery } from '@tanstack/react-query'
import { investmentService } from '@/api/services/investment.service'
import type { InvestimentoFiltros } from '@/api/dtos'

export function useFixedIncomeInvestments(filters: InvestimentoFiltros = {}) {
  return useQuery({
    queryKey: ['investments', 'fixed-income', filters],
    queryFn: () => investmentService.getAll({ ...filters, type: 'fixed_income' }),
  })
}
