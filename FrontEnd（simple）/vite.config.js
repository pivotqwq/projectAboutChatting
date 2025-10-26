import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://47.99.79.0:9291',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path
      },
      '/chatservice': {
        target: 'http://47.99.79.0:9293',
        changeOrigin: true,
        secure: false,
        ws: true,  // 启用WebSocket代理（SignalR需要）
        rewrite: (path) => path.replace(/^\/chatservice/, '')
      }
    }
  }
})
