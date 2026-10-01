import { api } from '../client'
import type { RespostaApi, TaxaEconomicaDto, TaxaEconomicaHistoricoDto, CriarTaxaEconomicaRequest, AtualizarTaxaEconomicaRequest, EstimativaImpostoMensalDto } from '../dtos'

const BASE = '/taxes'

export const taxesService = {
  getAll: (grupoId: string) =>
    api.get<RespostaApi<TaxaEconomicaDto[]>>(BASE, { params: { grupoId } }),

  getById: (id: string, grupoId: string) =>
    api.get<RespostaApi<TaxaEconomicaDto>>(`${BASE}/${id}`, { params: { grupoId } }),

  getHistory: (id: string, grupoId: string) =>
    api.get<RespostaApi<TaxaEconomicaHistoricoDto[]>>(`${BASE}/${id}/historico`, { params: { grupoId } }),

  create: (grupoId: string, data: CriarTaxaEconomicaRequest) =>
    api.post<RespostaApi<TaxaEconomicaDto>>(BASE, data, { params: { grupoId } }),

  update: (id: string, grupoId: string, data: AtualizarTaxaEconomicaRequest) =>
    api.put<RespostaApi<TaxaEconomicaDto>>(`${BASE}/${id}`, data, { params: { grupoId } }),

  delete: (id: string, grupoId: string) =>
    api.delete<RespostaApi<null>>(`${BASE}/${id}`, { params: { grupoId } }),

  estimateMonthly: (year: number, month: number, grupoId: string, titular?: string, titularId?: string) =>
    api.get<RespostaApi<EstimativaImpostoMensalDto>>(`${BASE}/estimativa-mensal`, {
      params: { ano: year, mes: month, grupoId, titular, titularId },
    }),
}
