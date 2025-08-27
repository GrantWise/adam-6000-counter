import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    // Chunk splitting for optimal caching
    rollupOptions: {
      output: {
        manualChunks: {
          // Vendor chunks
          vendor: ['react', 'react-dom', 'react-router-dom'],
          ui: [
            '@radix-ui/react-toast', 
            '@radix-ui/react-dialog', 
            '@radix-ui/react-dropdown-menu',
            '@radix-ui/react-select'
          ],
          charts: ['recharts'],
          
          // Module chunks will be defined dynamically
        }
      }
    },
    
    // Performance budgets
    chunkSizeWarningLimit: 200000, // 200KB per chunk
    
    // Asset optimization
    assetsInlineLimit: 4096, // Inline assets < 4KB
    
    // Source maps for debugging
    sourcemap: process.env.NODE_ENV === 'development'
  },
  
  // Development server configuration
  server: {
    port: 3000,
    host: true, // Allow external connections
    proxy: {
      '/api/auth': 'http://localhost:5139',
      '/api/devices': 'http://localhost:5139',
      '/api/oee': 'http://localhost:5140',
      '/api/scheduling': 'http://localhost:5141',
      '/api/security': 'http://localhost:5139',
      '/hubs': 'http://localhost:5139'
    }
  },
  
  // Test configuration
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'src/test/',
        'dist/',
        '**/*.d.ts',
        '**/*.config.ts'
      ]
    }
  }
})