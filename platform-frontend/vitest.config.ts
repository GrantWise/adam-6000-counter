/// <reference types="vitest" />
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    // Environment configuration
    globals: true,
    environment: 'jsdom',
    
    // Setup files
    setupFiles: [
      './src/test/setup.ts',
      './src/test/msw-setup.ts'
    ],
    
    // Coverage configuration
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: [
        'src/**/*.{ts,tsx}',
        '!src/**/*.d.ts',
        '!src/test/**/*',
        '!src/main.tsx',
        '!src/vite-env.d.ts'
      ],
      exclude: [
        'node_modules/',
        'src/test/',
        'dist/',
        '**/*.d.ts',
        '**/*.config.ts',
        'src/components/ui/*.tsx', // Exclude shadcn/ui components from coverage
      ],
      // Coverage thresholds for critical business logic
      thresholds: {
        global: {
          branches: 70,
          functions: 70,
          lines: 70,
          statements: 70
        },
        // Higher thresholds for compliance-critical code
        'src/lib/services/': {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80
        },
        'src/lib/utils/apiValidation.ts': {
          branches: 90,
          functions: 90,
          lines: 90,
          statements: 90
        },
        'src/components/ui/data-quality-indicator.tsx': {
          branches: 85,
          functions: 85,
          lines: 85,
          statements: 85
        }
      }
    },
    
    // Test patterns
    include: [
      'src/**/__tests__/**/*.{test,spec}.{ts,tsx}',
      'src/**/*.{test,spec}.{ts,tsx}'
    ],
    
    // Test timeout and retries
    testTimeout: 10000,
    hookTimeout: 10000,
    
    // Reporter configuration
    reporter: [
      'verbose',
      'junit',
      'json'
    ],
    
    // Output configuration
    outputFile: {
      junit: './test-results/junit.xml',
      json: './test-results/results.json'
    },
    
    // Mocking configuration
    server: {
      deps: {
        inline: ['@microsoft/signalr']
      }
    },
    
    // Watch mode configuration
    watch: false,
    
    // Pool configuration for parallel testing
    pool: 'threads',
    poolOptions: {
      threads: {
        singleThread: false,
        maxThreads: 4,
        minThreads: 1
      }
    }
  }
})