import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // "@/lib/utils" means "src/lib/utils"
    alias: { '@': path.resolve(import.meta.dirname, './src') },
  },
  server: {
    port: 5174,
    strictPort: true,
    // The browser only ever talks to localhost:5174. Vite forwards /api to the
    // ASP.NET Core API, so the auth cookie is first-party and no CORS is needed.
    proxy: {
      '/api': {
        target: 'https://localhost:7120',
        secure: false, // accept the ASP.NET Core development certificate
      },
    },
  },
})
