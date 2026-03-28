import { defineConfig } from "vite"
import react from "@vitejs/plugin-react-swc"
import path from "path"
import { componentTagger } from "lovable-tagger"

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  base: process.env.CAPACITOR === "true" ? "/" :
    process.env.GITHUB_PAGES === "true" ? "/invest-dashboard/" : "/",
  server: {
    host: "0.0.0.0",
    port: 5000,
    watch: {
      usePolling: true,
    },
    allowedHosts: true,
  },
  plugins: [react(), mode === "development" && componentTagger()].filter(Boolean),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
}))
