import { api } from '../client'
import type { RespostaApi } from '../dtos'

export interface TitularDto {
  id: string
  grupoId: string
  nome: string
  parentesco?: string
  usuarioId?: string
}

export type SalvarTitular = Pick<TitularDto, 'nome' | 'parentesco' | 'usuarioId'>

export const holderService = {
  list: (grupoId: string) => api.get<RespostaApi<TitularDto[]>>(`/grupos-carteiras/${grupoId}/titulares`),
  create: (grupoId: string, request: SalvarTitular) =>
    api.post<RespostaApi<TitularDto>>(`/grupos-carteiras/${grupoId}/titulares`, request),
  update: (grupoId: string, id: string, request: SalvarTitular) =>
    api.put<RespostaApi<TitularDto>>(`/grupos-carteiras/${grupoId}/titulares/${id}`, request),
}
