/// <reference types="vite/client" />
import { fileURLToPath, URL } from 'node:url'

import { defineConfig ,loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig(({command,mode}) => {
  return {
  plugins: [
    vue(),
  ],
  define: {
    'test': loadEnv(mode, process.cwd(),''),
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
}
})
