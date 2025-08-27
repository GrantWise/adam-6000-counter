/**
 * MSW API Response Handlers
 * Mock API responses for testing - following CFR Part 11 compliance principles
 * CRITICAL: These mocks must NEVER generate synthetic data that could be mistaken for real industrial data
 */

import { http, HttpResponse } from 'msw'

// Test user data - clearly marked as test data
const mockUsers = [
  {
    userId: 'test-admin-001',
    username: 'test.admin',
    email: 'test.admin@test.local',
    fullName: 'Test Administrator',
    roles: ['SystemAdmin'],
    lastLogin: '2025-08-27T10:00:00Z',
    status: 'active'
  },
  {
    userId: 'test-supervisor-001', 
    username: 'test.supervisor',
    email: 'test.supervisor@test.local',
    fullName: 'Test Supervisor',
    roles: ['Supervisor'],
    lastLogin: '2025-08-27T09:30:00Z',
    status: 'active'
  },
  {
    userId: 'test-operator-001',
    username: 'test.operator',
    email: 'test.operator@test.local', 
    fullName: 'Test Operator',
    roles: ['Operator'],
    lastLogin: '2025-08-27T08:00:00Z',
    status: 'active'
  }
]

// Mock JWT tokens for testing
const generateMockToken = (userId: string) => ({
  accessToken: `mock.jwt.token.${userId}`,
  refreshToken: `mock.refresh.token.${userId}`,
  expiresAt: new Date(Date.now() + 3600000).toISOString(), // 1 hour
  refreshExpiresAt: new Date(Date.now() + 86400000).toISOString() // 24 hours
})

export const handlers = [
  // Authentication endpoints
  http.post('/api/auth/login', async ({ request }) => {
    const body = await request.json() as { username: string; password: string }
    
    // Find mock user
    const user = mockUsers.find(u => u.username === body.username)
    
    if (!user || body.password !== 'test123') {
      return HttpResponse.json({
        success: false,
        error: 'Invalid credentials'
      }, { status: 401 })
    }
    
    const tokens = generateMockToken(user.userId)
    
    return HttpResponse.json({
      success: true,
      user,
      ...tokens
    })
  }),

  http.post('/api/auth/logout', () => {
    return HttpResponse.json({ success: true })
  }),

  http.post('/api/auth/refresh', () => {
    return HttpResponse.json({
      success: true,
      token: generateMockToken('test-user')
    })
  }),

  // User Management endpoints
  http.get('/api/users', () => {
    return HttpResponse.json({
      success: true,
      data: {
        items: mockUsers,
        totalCount: mockUsers.length,
        currentPage: 1,
        pageSize: 10
      }
    })
  }),

  http.get('/api/users/:userId', ({ params }) => {
    const user = mockUsers.find(u => u.userId === params.userId)
    if (!user) {
      return HttpResponse.json({
        success: false,
        error: 'User not found'
      }, { status: 404 })
    }
    
    return HttpResponse.json({
      success: true,
      data: user
    })
  }),

  http.post('/api/users', async ({ request }) => {
    const body = await request.json() as any
    const newUser = {
      userId: `test-user-${Date.now()}`,
      ...body,
      status: 'active'
    }
    
    mockUsers.push(newUser)
    
    return HttpResponse.json({
      success: true,
      data: newUser
    }, { status: 201 })
  }),

  http.put('/api/users/:userId', async ({ params, request }) => {
    const body = await request.json() as any
    const userIndex = mockUsers.findIndex(u => u.userId === params.userId)
    
    if (userIndex === -1) {
      return HttpResponse.json({
        success: false,
        error: 'User not found'
      }, { status: 404 })
    }
    
    mockUsers[userIndex] = { ...mockUsers[userIndex], ...body }
    
    return HttpResponse.json({
      success: true,
      data: mockUsers[userIndex]
    })
  }),

  http.delete('/api/users/:userId', ({ params }) => {
    const userIndex = mockUsers.findIndex(u => u.userId === params.userId)
    
    if (userIndex === -1) {
      return HttpResponse.json({
        success: false,
        error: 'User not found'
      }, { status: 404 })
    }
    
    mockUsers.splice(userIndex, 1)
    
    return HttpResponse.json({ success: true })
  }),

  // Security Audit endpoints
  http.get('/api/security/audit-logs', () => {
    return HttpResponse.json({
      success: true,
      data: {
        items: [
          {
            id: 'audit-001',
            userId: 'test-admin-001',
            action: 'LOGIN',
            resource: 'Authentication',
            timestamp: new Date().toISOString(),
            ipAddress: '127.0.0.1',
            userAgent: 'Test Browser',
            status: 'SUCCESS'
          }
        ],
        totalCount: 1,
        currentPage: 1,
        pageSize: 10
      }
    })
  }),

  http.get('/api/security/alerts', () => {
    return HttpResponse.json({
      success: true,
      data: {
        items: [
          {
            id: 'alert-001',
            type: 'LOGIN_FAILURE',
            severity: 'medium',
            message: 'Multiple failed login attempts detected',
            timestamp: new Date().toISOString(),
            status: 'ACTIVE'
          }
        ],
        totalCount: 1,
        currentPage: 1,
        pageSize: 10
      }
    })
  }),

  // System Configuration endpoints
  http.get('/api/system/configuration', () => {
    return HttpResponse.json({
      success: true,
      data: {
        security: {
          sessionTimeout: 3600,
          maxLoginAttempts: 5,
          passwordPolicy: {
            minLength: 8,
            requireSpecialChars: true,
            requireNumbers: true
          }
        },
        system: {
          applicationName: 'Industrial ADAM Platform (TEST)',
          version: '1.0.0-test',
          environment: 'test'
        }
      }
    })
  }),

  // Device endpoints - CRITICAL: No synthetic measurement data
  http.get('/api/devices', () => {
    return HttpResponse.json({
      success: true,
      data: {
        items: [
          {
            id: 'device-001',
            name: 'Test Device 001',
            type: 'ADAM-6000',
            status: 'offline', // Always offline for test data
            location: 'Test Lab',
            lastSeen: null, // No fake timestamps
            // CRITICAL: No measurement values in test data
            dataQuality: 'unavailable',
            warning: 'TEST ENVIRONMENT - No real device data available'
          }
        ],
        totalCount: 1,
        currentPage: 1,
        pageSize: 10
      }
    })
  }),

  // OEE endpoints - CRITICAL: No synthetic production data
  http.get('/api/oee/metrics', () => {
    return HttpResponse.json({
      success: true,
      data: {
        // No calculated OEE values - this would be synthetic data
        message: 'OEE calculation requires live production data',
        dataQuality: 'unavailable',
        warning: 'TEST ENVIRONMENT - No production data available',
        complianceWarnings: [
          'No real production data available in test environment',
          'OEE calculations require live device connections'
        ]
      }
    })
  }),

  // System Health endpoints
  http.get('/api/admin/health/metrics', () => {
    return HttpResponse.json({
      success: true,
      data: {
        systemStatus: 'testing',
        services: [
          { name: 'Authentication', status: 'healthy', lastCheck: new Date().toISOString() },
          { name: 'Database', status: 'mock', lastCheck: new Date().toISOString() },
          { name: 'Device Connection', status: 'offline', lastCheck: new Date().toISOString() }
        ],
        dataQuality: 'simulated',
        warning: 'TEST ENVIRONMENT - Mock health data only'
      }
    })
  }),

  // Error simulation endpoints for testing error handling
  http.get('/api/test/error/500', () => {
    return HttpResponse.json({
      success: false,
      error: 'Internal server error (simulated)'
    }, { status: 500 })
  }),

  http.get('/api/test/error/401', () => {
    return HttpResponse.json({
      success: false,
      error: 'Unauthorized (simulated)'
    }, { status: 401 })
  }),

  http.get('/api/test/error/network', () => {
    // Simulate network error
    return HttpResponse.error()
  }),

  // Default fallback handler
  http.all('*', ({ request }) => {
    console.warn(`Unhandled ${request.method} request to ${request.url}`)
    
    return HttpResponse.json({
      success: false,
      error: 'API endpoint not mocked',
      url: request.url,
      method: request.method,
      dataQuality: 'unavailable',
      warning: 'API endpoint not available in test environment'
    }, { status: 404 })
  })
]