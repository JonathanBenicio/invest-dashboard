import { create } from 'zustand'

export interface PriceObservation {
  price: number
  observedAtUtc: string
  source: string
}

interface MarketDataState {
  prices: Record<string, number>
  observations: Record<string, PriceObservation>
  setPrice: (ticker: string, price: number) => void
  setObservation: (ticker: string, observation: PriceObservation) => void
  updatePrices: (updates: Record<string, number>) => void
}

export const useMarketDataStore = create<MarketDataState>((set) => ({
  prices: {},
  observations: {},
  setPrice: (ticker, price) =>
    set((state) => ({
      prices: {
        ...state.prices,
        [ticker.toUpperCase()]: price
      }
    })),
  setObservation: (ticker, observation) =>
    set((state) => {
      const symbol = ticker.toUpperCase()
      const current = state.observations[symbol]
      if (current && Date.parse(current.observedAtUtc) > Date.parse(observation.observedAtUtc)) {
        return state
      }

      return {
        prices: { ...state.prices, [symbol]: observation.price },
        observations: { ...state.observations, [symbol]: observation },
      }
    }),
  updatePrices: (updates) =>
    set((state) => {
      const newPrices = { ...state.prices }
      Object.entries(updates).forEach(([ticker, price]) => {
        newPrices[ticker.toUpperCase()] = price
      })
      return { prices: newPrices }
    })
}))
