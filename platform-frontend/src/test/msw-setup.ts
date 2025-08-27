/**
 * MSW (Mock Service Worker) Setup for Testing
 * Provides mock API responses for testing without calling real backends
 */

import { beforeAll, afterEach, afterAll } from 'vitest'
import { setupServer } from 'msw/node'
import { handlers } from './handlers'

// Setup MSW server for Node.js (test environment)
export const server = setupServer(...handlers)

// Start MSW server before all tests
beforeAll(() => {
  server.listen({
    onUnhandledRequest: 'error'
  })
})

// Reset handlers between tests
afterEach(() => {
  server.resetHandlers()
})

// Stop MSW server after all tests
afterAll(() => {
  server.close()
})