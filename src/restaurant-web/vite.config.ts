import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  // @ts-expect-error Vitest extends the Vite config with test options.
  test: {
    environment: 'jsdom',
  },
  server: {
    proxy: {
      '/orders': 'http://localhost:5000',
      '/kitchen-board': 'http://localhost:5000',
    },
  },
})
