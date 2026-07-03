import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // In local dev, Mini-SSO must already be running at :12080 (see README).
      '/api/auth': {
        target: 'http://localhost:12080',
        changeOrigin: true,
      },
      '/api/todos': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
      '/healthz': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})
