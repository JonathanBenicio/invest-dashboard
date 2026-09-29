import { create } from 'zustand'

interface MarketDataState {
  prices: Record<string, number>
  setPrice: (ticker: string, price: number) => void
  updatePrices: (updates: Record<string, number>) => void
}

export const useMarketDataStore = create<MarketDataState>((set) => ({
  prices: {},
  setPrice: (ticker, price) =>
    set((state) => ({
      prices: {
        ...state.prices,
        [ticker.toUpperCase()]: price
      }
    })),
  updatePrices: (updates) =>
    set((state) => {
      const newPrices = { ...state.prices }
      Object.entries(updates).forEach(([ticker, price]) => {
        newPrices[ticker.toUpperCase()] = price
      })
      return { prices: newPrices }
    })
}))
