import type { CapacitorConfig } from '@capacitor/cli'

const config: CapacitorConfig = {
  appId: 'com.investdashboard.app',
  appName: 'Invest Dashboard',
  webDir: 'dist',
  server: {
    hostname: 'invest-dashboard',
    androidScheme: 'https',
  },
  android: {
    resolveServiceWorkerRequests: true,
  },

  plugins: {
    SplashScreen: {
      launchAutoHide: true,
      backgroundColor: '#0f172a',
      showSpinner: false,
    },
    StatusBar: {
      style: 'dark',
      backgroundColor: '#0f172a',
    }
  }
}

export default config
