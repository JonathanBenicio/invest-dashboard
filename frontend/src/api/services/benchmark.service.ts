import { api } from "@/api/client"
import type { RespostaApi } from "@/api/dtos/base.dto"
import type { SerieBenchmarkCdiDto } from "@/api/dtos/benchmark.dto"

export const benchmarkService = {
  getCdi: (dataDe: string, dataAte: string) =>
    api.get<RespostaApi<SerieBenchmarkCdiDto>>(`/benchmarks/cdi?dataDe=${dataDe}&dataAte=${dataAte}`),
}
