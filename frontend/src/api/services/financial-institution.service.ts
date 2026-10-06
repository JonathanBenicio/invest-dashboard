import { api } from '../client'
import type { InstituicaoFinanceiraDto, RespostaApi } from '../dtos'

export const financialInstitutionService = {
  list: (grupoId?: string): Promise<RespostaApi<InstituicaoFinanceiraDto[]>> =>
    api.get('/instituicoes-financeiras', { params: grupoId ? { grupoId } : undefined }),
  createOther: (grupoId: string, nome: string): Promise<RespostaApi<InstituicaoFinanceiraDto>> =>
    api.post('/instituicoes-financeiras', { nome }, { params: { grupoId } }),
}
