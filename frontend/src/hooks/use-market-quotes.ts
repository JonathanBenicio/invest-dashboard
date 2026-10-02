import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { marketDataService } from '@/api/services/market-data.service'
import { useMarketDataStore } from '@/store/marketDataStore'
import { useSignalR } from '@/hooks/useSignalR'

export function useMarketQuotes(symbols: string[]) {
  const normalizedSymbols = useMemo(
    () => [...new Set(symbols.map(symbol => symbol.trim().toUpperCase()).filter(Boolean))].sort(),
    [symbols],
  )
  const observations = useMarketDataStore(state => state.observations)
  useSignalR(normalizedSymbols)

  const query = useQuery({
    queryKey: ['market-data', 'quotes', normalizedSymbols],
    queryFn: async () => (await marketDataService.getQuotes(normalizedSymbols)).dados,
    enabled: normalizedSymbols.length > 0,
    staleTime: 30_000,
    refetchInterval: 60_000,
    retry: 1,
  })

  const quotesBySymbol = useMemo(() => {
    const quotes = new Map((query.data ?? []).map(quote => [quote.simbolo.toUpperCase(), quote]))

    normalizedSymbols.forEach(symbol => {
      const realtime = observations[symbol]
      if (!realtime) return

      const fetched = quotes.get(symbol)
      if (!fetched || Date.parse(realtime.observedAtUtc) >= Date.parse(fetched.observadoEmUtc)) {
        quotes.set(symbol, {
          simbolo: symbol,
          nome: fetched?.nome ?? symbol,
          preco: realtime.price,
          observadoEmUtc: realtime.observedAtUtc,
          moeda: fetched?.moeda ?? 'BRL',
          setor: fetched?.setor,
          subtipo: fetched?.subtipo,
          origem: realtime.source,
        })
      }
    })

    return quotes
  }, [normalizedSymbols, observations, query.data])

  const unavailableSymbols = query.isLoading || query.isFetching
    ? []
    : normalizedSymbols.filter(symbol => !quotesBySymbol.has(symbol))

  return { ...query, quotesBySymbol, unavailableSymbols }
}

export function formatQuoteObservation(source: string, observedAtUtc: string): string {
  const observedAt = new Date(observedAtUtc)
  if (Number.isNaN(observedAt.getTime())) return `Cotação ${source}`

  const formattedDate = new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(observedAt)

  return `Cotação ${source} · ${formattedDate}`
}
