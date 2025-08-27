/**
 * Testing Utilities and Helpers
 * Provides common testing utilities following CFR Part 11 compliance patterns
 */

import React, { ReactElement } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { usePlatformStore } from '@/store/platformStore'
import type { User, AuthToken, Permission } from '@/types'

// Custom render function with providers
const AllTheProviders = ({ children }: { children: React.ReactNode }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        cacheTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllTheProviders, ...options })

// Re-export everything
export * from '@testing-library/react'

// Override render method
export { customRender as render }

// Test data factories following CFR Part 11 compliance
export const createMockUser = (overrides: Partial<User> = {}): User => ({
  id: `test-user-${Date.now()}`,
  username: 'test.user',
  email: 'test.user@test.local',
  fullName: 'Test User',
  role: 'Operator',
  permissions: ['VIEW_DEVICES', 'VIEW_OEE'],
  hierarchyAssignments: [],
  status: 'active',
  createdAt: new Date('2025-08-27T00:00:00Z'),
  updatedAt: new Date('2025-08-27T00:00:00Z'),
  ...overrides
})

export const createMockToken = (overrides: Partial<AuthToken> = {}): AuthToken => ({
  accessToken: `mock.jwt.token.${Date.now()}`,
  refreshToken: `mock.refresh.token.${Date.now()}`,
  expiresAt: new Date(Date.now() + 3600000), // 1 hour from now
  refreshExpiresAt: new Date(Date.now() + 86400000), // 24 hours from now
  ...overrides
})

export const createMockAdminUser = (): User => createMockUser({
  username: 'test.admin',
  role: 'SystemAdmin',
  permissions: [
    'SYSTEM_ADMIN',
    'MANAGE_USERS',
    'MANAGE_HIERARCHY',
    'MANAGE_SECURITY',
    'VIEW_USERS',
    'VIEW_HIERARCHY',
    'VIEW_SECURITY',
    'VIEW_DEVICES',
    'VIEW_OEE'
  ]
})

export const createMockSupervisorUser = (): User => createMockUser({
  username: 'test.supervisor',
  role: 'Supervisor',
  permissions: [
    'MANAGE_OEE',
    'MANAGE_SCHEDULING',
    'VIEW_DEVICES',
    'VIEW_OEE',
    'VIEW_SCHEDULING'
  ]
})

// Store helpers for testing
export const setupAuthenticatedUser = (user?: User, token?: AuthToken) => {
  const store = usePlatformStore.getState()
  
  const testUser = user || createMockUser()
  const testToken = token || createMockToken()
  
  store.setUser(testUser)
  store.setToken(testToken)
  store.setPermissions(testUser.permissions)
  store.setAuthenticated(true)
  
  return { user: testUser, token: testToken }
}

export const setupUnauthenticatedUser = () => {
  const store = usePlatformStore.getState()
  store.clearAuth()
}

// CFR Part 11 compliant test data generators
export const createComplianceTestData = <T,>(
  data: T,
  quality: 'good' | 'uncertain' | 'bad' | 'unavailable' | 'simulated' = 'good',
  warning?: string
) => ({
  ...data,
  quality,
  timestamp: new Date('2025-08-27T10:00:00Z'),
  warning,
  auditInfo: {
    sourceSystem: quality === 'simulated' ? 'TEST-SIMULATOR' : 'ADAM-6000-TEST',
    dataIntegrity: quality === 'good' ? 'verified' : 
                   quality === 'simulated' ? 'synthetic' : 'questionable',
    createdBy: 'test-system',
    createdAt: new Date('2025-08-27T10:00:00Z')
  }
})

// Error simulation helpers
export const simulateNetworkError = () => {
  throw new Error('Network error: Unable to connect to server')
}

export const simulateAuthenticationError = () => {
  throw new Error('Authentication failed: Invalid credentials')
}

export const simulateValidationError = (field: string) => {
  throw new Error(`Validation failed: ${field} is required`)
}

// Data quality test helpers
export const createUnavailableData = <T,>(warning = 'Device offline - no data available') => 
  createComplianceTestData(null as T, 'unavailable', warning)

export const createSimulatedData = <T,>(data: T, warning = 'TEST ENVIRONMENT - Simulated data only') =>
  createComplianceTestData(data, 'simulated', warning)

export const createUncertainData = <T,>(data: T, warning = 'Data quality uncertain - verify before use') =>
  createComplianceTestData(data, 'uncertain', warning)

export const createBadData = <T,>(warning = 'Data quality poor - do not use for decisions') =>
  createComplianceTestData(null as T, 'bad', warning)

// Mock API response generators
export const createSuccessResponse = <T,>(data: T) => ({
  success: true,
  data,
  timestamp: new Date().toISOString()
})

export const createErrorResponse = (error: string, status = 500) => ({
  success: false,
  error,
  status,
  timestamp: new Date().toISOString()
})

export const createPaginatedResponse = <T,>(items: T[], totalCount?: number) => ({
  success: true,
  data: {
    items,
    totalCount: totalCount || items.length,
    currentPage: 1,
    pageSize: 10
  }
})

// Permission testing helpers
export const hasPermissionInList = (permissions: Permission[], required: Permission): boolean => {
  return permissions.includes(required)
}

export const hasAnyPermissionInList = (permissions: Permission[], required: Permission[]): boolean => {
  return required.some(permission => permissions.includes(permission))
}

export const hasAllPermissionsInList = (permissions: Permission[], required: Permission[]): boolean => {
  return required.every(permission => permissions.includes(permission))
}

// Wait helpers for async operations
export const waitForAuthenticationUpdate = () => new Promise(resolve => setTimeout(resolve, 100))
export const waitForApiCall = () => new Promise(resolve => setTimeout(resolve, 50))

// Test environment validation
export const validateTestEnvironment = () => {
  if (process.env.NODE_ENV !== 'test') {
    throw new Error('These utilities should only be used in test environment')
  }
}

// CFR Part 11 compliance test assertions
export const assertNoSyntheticData = (data: any) => {
  expect(data).not.toMatch(/Math\.random|faker|synthetic|generated/i)
  expect(data).not.toHaveProperty('_synthetic')
  expect(data).not.toHaveProperty('_generated')
}

export const assertDataQualityIndicated = (element: HTMLElement) => {
  // Should have quality indicator
  expect(
    element.querySelector('[data-testid*="quality"]') ||
    element.textContent?.includes('VERIFIED') ||
    element.textContent?.includes('UNCERTAIN') ||
    element.textContent?.includes('NO DATA') ||
    element.textContent?.includes('SIMULATED')
  ).toBeTruthy()
}

export const assertAuditTrailPresent = (data: any) => {
  expect(data).toHaveProperty('timestamp')
  expect(data).toHaveProperty('auditInfo')
  if (data.auditInfo) {
    expect(data.auditInfo).toHaveProperty('sourceSystem')
    expect(data.auditInfo).toHaveProperty('dataIntegrity')
  }
}

// Cleanup helpers
export const cleanupTestEnvironment = () => {
  // Clear all stores
  usePlatformStore.getState().clearAuth()
  
  // Clear any lingering timers
  vi.clearAllTimers()
  
  // Clear console mocks
  vi.clearAllMocks()
}

// Performance testing helpers
export const measureRenderTime = async (renderFn: () => void): Promise<number> => {
  const start = performance.now()
  renderFn()
  await waitForApiCall()
  const end = performance.now()
  return end - start
}

export const assertRenderPerformance = (renderTime: number, maxTime = 100) => {
  expect(renderTime).toBeLessThan(maxTime)
}