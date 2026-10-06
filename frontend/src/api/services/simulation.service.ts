import { api } from '../client'
import type { RespostaApi, SimulacaoResponse, SimulacaoRequest, SimulacaoEstrategia } from '../dtos'

const BASE = '/simulation'

export const simulationService = {
  simulate: (data: SimulacaoRequest) =>
    api.post<RespostaApi<SimulacaoResponse>>(BASE, data),

  getStrategies: () =>
    api.get<RespostaApi<SimulacaoEstrategia[]>>(`${BASE}/strategies`),
}
