import { api } from '../client'
import type { ApiResponse, SimulationResponse, SimulationRequest, SimulationStrategy } from '../dtos'

const BASE = '/api/v1/simulation'

export const simulationService = {
  simulate: (data: SimulationRequest) =>
    api.post<ApiResponse<SimulationResponse>>(BASE, data),

  getStrategies: () =>
    api.get<ApiResponse<SimulationStrategy[]>>(`${BASE}/strategies`),
}
