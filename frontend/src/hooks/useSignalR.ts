import { useEffect, useRef, useCallback } from 'react'
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'
import { useMarketDataStore } from '@/store/marketDataStore'
import { API_CONFIG } from '@/api/env'

export const useSignalR = (tickers: string[] = []) => {
  const connectionRef = useRef<HubConnection | null>(null)
  const { accessToken, isAuthenticated } = useAuthStore()
  const { setPrice } = useMarketDataStore()
  const activeTickersRef = useRef<string[]>([])

  // Normalize tickers list
  const cleanTickers = tickers.map(t => t.trim().toUpperCase()).filter(Boolean)

  const connect = useCallback(async () => {
    // If MSW is active, or not authenticated, or connection already exists/connecting, skip
    if (import.meta.env.VITE_USE_MSW === 'true' || !isAuthenticated || !accessToken) {
      return
    }

    if (connectionRef.current && connectionRef.current.state !== HubConnectionState.Disconnected) {
      return
    }

    const hubBaseUrl = API_CONFIG.BASE_URL || window.location.origin
    const hubUrl = `${hubBaseUrl.replace(/\/$/, '')}/hubs/market-data?access_token=${encodeURIComponent(accessToken)}`

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()

    connection.on('OnPriceUpdate', (data: { ticker: string; price: number }) => {
      if (data && data.ticker) {
        setPrice(data.ticker, data.price)
      }
    })

    try {
      await connection.start()
      console.log('[SignalR] Connected successfully.')
      connectionRef.current = connection

      // Subscribe to initial tickers if any
      if (activeTickersRef.current.length > 0) {
        await connection.send('Subscribe', activeTickersRef.current)
      }
    } catch (err) {
      console.error('[SignalR] Connection failed: ', err)
    }
  }, [accessToken, isAuthenticated, setPrice])

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
        console.log('[SignalR] Disconnected successfully.')
      } catch (err) {
        console.error('[SignalR] Error disconnecting: ', err)
      } finally {
        connectionRef.current = null
      }
    }
  }, [])

  // Subscribe functions exposed to components
  const subscribe = useCallback(async (tickersToSub: string[]) => {
    const list = tickersToSub.map(t => t.trim().toUpperCase()).filter(Boolean)
    if (list.length === 0) return

    // Track active tickers
    list.forEach(t => {
      if (!activeTickersRef.current.includes(t)) {
        activeTickersRef.current.push(t)
      }
    })

    if (connectionRef.current && connectionRef.current.state === HubConnectionState.Connected) {
      try {
        await connectionRef.current.send('Subscribe', list)
      } catch (err) {
        console.error('[SignalR] Failed to send Subscribe: ', err)
      }
    }
  }, [])

  const unsubscribe = useCallback(async (tickersToUnsub: string[]) => {
    const list = tickersToUnsub.map(t => t.trim().toUpperCase()).filter(Boolean)
    if (list.length === 0) return

    activeTickersRef.current = activeTickersRef.current.filter(t => !list.includes(t))

    if (connectionRef.current && connectionRef.current.state === HubConnectionState.Connected) {
      try {
        await connectionRef.current.send('Unsubscribe', list)
      } catch (err) {
        console.error('[SignalR] Failed to send Unsubscribe: ', err)
      }
    }
  }, [])

  // Initialize and handle tickers changes
  useEffect(() => {
    connect()

    return () => {
      // Don't disconnect immediately on hook unmount to keep connection alive for other hooks,
      // but we can unsubscribe the hook's specific tickers if desired.
    }
  }, [connect])

  // Handle dynamic tickers changes for this hook instance
  useEffect(() => {
    if (cleanTickers.length > 0) {
      subscribe(cleanTickers)

      return () => {
        unsubscribe(cleanTickers)
      }
    }
  }, [JSON.stringify(cleanTickers), subscribe, unsubscribe])

  // Complete cleanup on user logout
  useEffect(() => {
    if (!isAuthenticated) {
      disconnect()
      activeTickersRef.current = []
    }
  }, [isAuthenticated, disconnect])

  return {
    connectionState: connectionRef.current?.state || HubConnectionState.Disconnected,
    subscribe,
    unsubscribe
  }
}
