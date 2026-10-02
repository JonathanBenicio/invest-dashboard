import { api } from '../client'
import type { ConvitePendenteGrupoDto, GrupoCarteirasDto, MembroGrupoDto, ResultadoConviteGrupoDto, RespostaApi } from '../dtos'

export const groupService = {
  list: (): Promise<RespostaApi<GrupoCarteirasDto[]>> => api.get('/grupos-carteiras'),
  create: (nome: string): Promise<RespostaApi<GrupoCarteirasDto>> => api.post('/grupos-carteiras', { nome }),
  members: (grupoId: string): Promise<RespostaApi<MembroGrupoDto[]>> => api.get(`/grupos-carteiras/${grupoId}/membros`),
  invite: (grupoId: string, email: string, papel: MembroGrupoDto['papel']) =>
    api.post<RespostaApi<ResultadoConviteGrupoDto>>('/grupos-carteiras/' + grupoId + '/convites', { email, papel }),
  pendingInvitations: (): Promise<RespostaApi<ConvitePendenteGrupoDto[]>> => api.get('/grupos-carteiras/convites-pendentes'),
  acceptInvitation: (grupoId: string, conviteId: string) =>
    api.post<RespostaApi<boolean>>('/grupos-carteiras/' + grupoId + '/convites/' + conviteId + '/aceitar'),
  updateRole: (grupoId: string, membroId: string, papel: MembroGrupoDto['papel']) =>
    api.put<RespostaApi<boolean>>(`/grupos-carteiras/${grupoId}/membros/${membroId}/papel`, { papel }),
  deactivate: (grupoId: string, membroId: string) =>
    api.delete<RespostaApi<boolean>>(`/grupos-carteiras/${grupoId}/membros/${membroId}`),
}
