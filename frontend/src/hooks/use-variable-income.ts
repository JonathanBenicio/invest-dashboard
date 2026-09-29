import { useQuery } from '@tanstack/react-query'
import type { InvestimentoFiltros } from '@/api/dtos'
import { investmentService } from '@/api/services/investment.service'

export function useVariableIncomeInvestments(filters: InvestimentoFiltros = {}) {
  return useQuery({
    queryKey: ['investments', 'variable-income', filters],
    queryFn: () => investmentService.getAll({ ...filters, type: 'variable_income' }),
  })
}
