import { api } from '../client'
import type { ApiResponse, SimulacaoResponse, SimulacaoRequest, SimulacaoEstrategia } from '../dtos'

const BASE = '/api/v1/simulation'

export const simulationService = {
  simulate: (data: SimulacaoRequest) =>
    api.post<ApiResponse<SimulacaoResponse>>(BASE, data),

  getStrategies: () =>
    api.get<ApiResponse<SimulacaoEstrategia[]>>(`${BASE}/strategies`),
}
