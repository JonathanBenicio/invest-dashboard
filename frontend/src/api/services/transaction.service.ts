import { api } from '@/api/client'
import type { ApiResponse, RegistrarTransacaoRequest, TransacaoDto } from '@/api/dtos'

export const transactionService = {
  create: (request: RegistrarTransacaoRequest): Promise<ApiResponse<TransacaoDto>> =>
    api.post('/transactions', request),

  update: (id: string, request: Omit<RegistrarTransacaoRequest, 'portfolioId' | 'assetId' | 'ticker' | 'idempotencyKey'>) =>
    api.patch<ApiResponse<TransacaoDto>>(`/transactions/${id}`, request),

  delete: (id: string): Promise<ApiResponse<boolean>> =>
    api.delete(`/transactions/${id}`),
}
