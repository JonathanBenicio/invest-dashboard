import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import App from "./App.tsx"
import "./index.css"

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

// Enable MSW in development or when USE_MSW is explicitly set
async function enableMocking() {
  const shouldUseMSW = import.meta.env.DEV || import.meta.env.VITE_USE_MSW === 'true'

  console.log('[MSW] Configuration:', {
    isDev: import.meta.env.DEV,
    VITE_USE_MSW: import.meta.env.VITE_USE_MSW,
    shouldUseMSW,
  })

  if (shouldUseMSW) {
    console.log('[MSW] Importing worker...')
    const { worker } = await import('./mocks/browser')
    console.log('[MSW] Starting worker...')

    const result = await worker.start({
      onUnhandledRequest: 'warn',
    })

    console.log('[MSW] Worker started successfully!')
    return result
  } else {
    console.log('[MSW] Skipping MSW initialization')
  }
}

enableMocking().then(() => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </StrictMode>
  )
})
