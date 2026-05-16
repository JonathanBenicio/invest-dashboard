import { api } from '../client'
import type { ApiResponse, EconomicRateDto, CreateEconomicRateRequest } from '../dtos'

const BASE = '/api/v1/taxes'

export const taxesService = {
  getAll: () =>
    api.get<ApiResponse<EconomicRateDto[]>>(BASE),

  getById: (id: string) =>
    api.get<ApiResponse<EconomicRateDto>>(`${BASE}/${id}`),

  create: (data: CreateEconomicRateRequest) =>
    api.post<ApiResponse<EconomicRateDto>>(BASE, data),

  update: (id: string, data: CreateEconomicRateRequest) =>
    api.put<ApiResponse<EconomicRateDto>>(`${BASE}/${id}`, data),

  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`${BASE}/${id}`),
}
