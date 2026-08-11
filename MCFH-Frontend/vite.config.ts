import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite' // Thêm dòng này

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(), // Thêm dòng này vào danh sách plugins
  ],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
  },
})