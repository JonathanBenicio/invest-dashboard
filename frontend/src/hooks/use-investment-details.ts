import { useQuery } from '@tanstack/react-query'
import { investmentService } from '@/api/services/investment.service'

export function useInvestment(id: string) {
  return useQuery({
    queryKey: ['investment', id],
    queryFn: () => investmentService.getById(id),
    enabled: !!id,
  })
}

export function useInvestmentTransactions(id: string) {
  return useQuery({
    queryKey: ['investment', id, 'transactions'],
    queryFn: () => investmentService.getTransactions(id),
    enabled: !!id,
  })
}
