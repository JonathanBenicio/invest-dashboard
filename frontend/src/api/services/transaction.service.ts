import { api } from '@/api/client'
import type { RespostaApi, RegistrarTransacaoRequest, TransacaoDto } from '@/api/dtos'

export const transactionService = {
  create: (request: RegistrarTransacaoRequest): Promise<RespostaApi<TransacaoDto>> =>
    api.post('/transactions', request),

  update: (id: string, request: Omit<RegistrarTransacaoRequest, 'portfolioId' | 'assetId' | 'ticker' | 'idempotencyKey'>) =>
    api.patch<RespostaApi<TransacaoDto>>(`/transactions/${id}`, request),

  delete: (id: string): Promise<RespostaApi<boolean>> =>
    api.delete(`/transactions/${id}`),
}
