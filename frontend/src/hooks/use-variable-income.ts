import { useQuery } from '@tanstack/react-query'
import { investmentService } from '@/api/services/investment.service'
import type { InvestimentoFiltros } from '@/api/dtos'

export function useVariableIncomeInvestments(filters: InvestimentoFiltros = {}) {
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
