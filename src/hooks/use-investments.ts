import { useQuery } from '@tanstack/react-query'
import { investmentService } from '@/api/services/investment.service'
import type { InvestmentFilters } from '@/api/dtos'

export function useFixedIncomeInvestments(filters: InvestmentFilters = {}) {
  return useQuery({
    queryKey: ['investments', 'fixed-income', filters],
    queryFn: () => investmentService.getAll({ ...filters, type: 'fixed_income' }),
  })
}
