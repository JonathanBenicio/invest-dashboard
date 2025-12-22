import { useQuery } from '@tanstack/react-query'
import { investmentService } from '@/api/services/investment.service'
import type { InvestmentFilters } from '@/api/dtos'

export function useVariableIncomeInvestments(filters: InvestmentFilters = {}) {
  return useQuery({
    queryKey: ['investments', 'variable-income', filters],
    queryFn: () => investmentService.getAll({ ...filters, type: 'variable_income' }),
  })
}

export function useDividends() {
    return useQuery({
        queryKey: ['dividends'],
        queryFn: () => investmentService.getDividends(),
    })
}
