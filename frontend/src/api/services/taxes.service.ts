import { api } from '../client'
import type { ApiResponse, TaxaEconomicaDto, CriarTaxaEconomicaRequest, AtualizarTaxaEconomicaRequest } from '../dtos'

const BASE = '/api/v1/taxes'

export const taxesService = {
  getAll: () =>
    api.get<ApiResponse<TaxaEconomicaDto[]>>(BASE),

  getById: (id: string) =>
    api.get<ApiResponse<TaxaEconomicaDto>>(`${BASE}/${id}`),

  create: (data: CriarTaxaEconomicaRequest) =>
    api.post<ApiResponse<TaxaEconomicaDto>>(BASE, data),

  update: (id: string, data: AtualizarTaxaEconomicaRequest) =>
    api.put<ApiResponse<TaxaEconomicaDto>>(`${BASE}/${id}`, data),

  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`${BASE}/${id}`),
}
