import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import App from "./App.tsx"
import "./index.css"
import { Capacitor } from '@capacitor/core'
import { StatusBar, Style } from '@capacitor/status-bar'
import { SplashScreen } from '@capacitor/splash-screen'
import { defineCustomElements } from '@ionic/pwa-elements/loader'

// Create QueryClient instance
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

// Initialize Capacitor plugins
async function initCapacitor() {
  // Initialize PWA elements for camera support on web
  if (!Capacitor.isNativePlatform()) {
    defineCustomElements(window);
  }

  if (Capacitor.isNativePlatform()) {
    try {
      await StatusBar.setStyle({ style: Style.Dark })
      await StatusBar.setBackgroundColor({ color: '#0f172a' })
      await SplashScreen.hide()
    } catch (e) {
      console.warn('StatusBar/SplashScreen plugin not available', e)
    }
  }
}

export async function enableMocking() {
  const platform = Capacitor.getPlatform() // 'web' | 'android' | 'ios'
  const useByFlag = import.meta.env.VITE_USE_MSW === 'true'
  const useByDev = import.meta.env.DEV

  // ✅ Web dev: ON (sempre em DEV)
  // ✅ Web prod com flag: ON (GitHub Pages com VITE_USE_MSW=true)
  // ✅ Android nativo com flag: ON
  // ❌ iOS: OFF (evita falha de SW no empacotado)
  const shouldUseMSW =
    (platform === 'web' && (useByDev || useByFlag)) ||
    (platform === 'android' && useByFlag)

  console.log('[MSW] Configuration:', {
    platform,
    isDev: useByDev,
    VITE_USE_MSW: import.meta.env.VITE_USE_MSW,
    shouldUseMSW,
    isSecureContext: window.isSecureContext,
    location: window.location.href,
    baseUrl: import.meta.env.BASE_URL,
    hasServiceWorker: 'serviceWorker' in navigator,
  })

  if (!shouldUseMSW) {
    console.log('[MSW] Skipping MSW initialization')
    return
  }

  if (!('serviceWorker' in navigator)) {
    console.warn('[MSW] Service Worker API not available in this runtime.')
    return
  }

  if (!window.isSecureContext) {
    console.warn('[MSW] Not a secure context. SW/MSW will not register here.')
    return
  }

  try {
    console.log('[MSW] Importing worker...')
    const { worker } = await import('./mocks/browser')

    // ✅ URL consistente e correta mesmo se BASE_URL != '/'
    // BASE_URL é relativo (ex: '/' ou '/invest-dashboard/'), precisamos do origin
    const swUrl = new URL(
      'mockServiceWorker.js',
      window.location.origin + import.meta.env.BASE_URL
    ).toString()

    console.log('[MSW] SW URL:', swUrl)

    // Debug opcional: confirmar acesso ao arquivo
    try {
      const res = await fetch(swUrl, { cache: 'no-store' })
      console.log('[MSW] SW Script fetch:', res.status, res.statusText)
      if (!res.ok) console.warn('[MSW] SW script not reachable:', swUrl)
    } catch (e) {
      console.warn('[MSW] SW Script fetch error:', e)
    }

    console.log('[MSW] Starting worker...')
    await worker.start({
      onUnhandledRequest: 'warn',
      serviceWorker: {
        url: swUrl,
      },
    })

    console.log('[MSW] Worker started successfully!')
  } catch (error) {
    console.error('[MSW] Failed to start worker:', error)
    // não rethrow
  }
}


// Start the app
async function startApp() {
  // Initialize plugins
  await initCapacitor()

  // Try to enable mocking, but don't block app start if it's taking too long or fails
  // On native platforms, MSW initialization can sometimes hang or fail due to Service Worker restrictions
  if (Capacitor.isNativePlatform()) {
    enableMocking().catch(err => console.error('[MSW] Async error:', err))
  } else {
    await enableMocking()
  }

  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </StrictMode>
  )
}

startApp()
