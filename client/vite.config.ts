import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, import.meta.dirname, '')

  return {
    plugins: [vue(), tailwindcss()],
    resolve: {
      alias: {
        '@': path.resolve(import.meta.dirname, './src'),
      },
    },
    server: {
      proxy: {
        [env.VITE_API_BASE_URL || '/api']: {
          target: env.VITE_API_URL || env.VITE_API_TARGET_URL || env.VITE_PROXY_TARGET || 'http://localhost:5142',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
