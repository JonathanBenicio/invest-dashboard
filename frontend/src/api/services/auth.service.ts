import { api } from '../client'
import type { RespostaApi, UsuarioAutenticadoDto, SessaoAutenticacaoDto, SolicitacaoLogin, SolicitacaoCadastro } from '../dtos'

const BASE = '/auth'

export const authService = {
  login: (credentials: SolicitacaoLogin) =>
    api.post<RespostaApi<SessaoAutenticacaoDto>>(`${BASE}/login`, credentials),

  register: (data: SolicitacaoCadastro) =>
    api.post<RespostaApi<SessaoAutenticacaoDto>>(`${BASE}/register`, data),

  acceptInvitation: (request: { grupoId: string; conviteId: string; tokenHash?: string; tokenSupabase?: string }) =>
    api.post<RespostaApi<SessaoAutenticacaoDto>>(`${BASE}/convites/aceitar`, request),

  refresh: () =>
    api.post<RespostaApi<SessaoAutenticacaoDto>>(`${BASE}/refresh`),

  logout: () =>
    api.post<RespostaApi<boolean>>(`${BASE}/logout`),

  me: () =>
    api.get<RespostaApi<UsuarioAutenticadoDto>>(`${BASE}/me`),
}
