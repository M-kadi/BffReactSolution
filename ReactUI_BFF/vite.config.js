import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 54451,
    proxy: {
      '/bff': {
        target: 'https://localhost:5099',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
