import { defineConfig } from 'vitest/config'
import { resolve } from 'path'

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./test-setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        '__tests__/',
        '*.config.*',
        'coverage/',
        '.next/',
        'public/',
        'scripts/',
        'styles/',
        'components/ui/', // Exclude shadcn/ui components from coverage
        '**/*.d.ts',
        '**/*.test.ts',
        '**/*.test.tsx'
      ],
      thresholds: {
        global: {
          branches: 60,
          functions: 60,
          lines: 60,
          statements: 60
        },
        // Higher thresholds for critical domain logic
        'lib/domain/': {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80
        },
        'lib/calculations/': {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80
        }
      }
    },
    include: ['**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    exclude: [
      'node_modules',
      'dist',
      '.next',
      'coverage'
    ]
  },
  resolve: {
    alias: {
      '@': resolve(__dirname, './'),
      '@/lib': resolve(__dirname, './lib'),
      '@/components': resolve(__dirname, './components'),
      '@/config': resolve(__dirname, './config'),
      '@/hooks': resolve(__dirname, './hooks'),
      '@/types': resolve(__dirname, './lib/types')
    }
  }
})