import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/orders': 'http://localhost:5000',
      '/kitchen-board': 'http://localhost:5000',
    },
  },
})
