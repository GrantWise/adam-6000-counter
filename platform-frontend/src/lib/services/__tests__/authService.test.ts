/**
 * Authentication Service Unit Tests
 * Tests critical authentication functionality following CFR Part 11 compliance
 * CRITICAL: Tests must verify no synthetic data generation in authentication
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { AuthService, authService } from '../authService'
import { usePlatformStore } from '@/store/platformStore'
import { apiClient } from '@/lib/api/client'
import type { LoginCredentials, UserRole } from '@/types'

// Mock the store
vi.mock('@/store/platformStore', () => ({
  usePlatformStore: {
    getState: vi.fn(),
  }
}))

// Mock the API client
vi.mock('@/lib/api/client', () => ({
  apiClient: {
    login: vi.fn(),
    logout: vi.fn(),
    refreshToken: vi.fn(),
  }
}))

describe('AuthService', () => {
  let mockStore: any
  let service: AuthService

  beforeEach(() => {
    // Reset all mocks
    vi.clearAllMocks()
    
    // Setup mock store
    mockStore = {
      auth: {
        isAuthenticated: false,
        user: null,
        token: null,
        permissions: []
      },
      setUser: vi.fn(),
      setToken: vi.fn(),
      setPermissions: vi.fn(),
      setAuthenticated: vi.fn(),
      setAuthLoading: vi.fn(),
      setDashboardLayout: vi.fn(),
      setHierarchyContext: vi.fn(),
      clearAuth: vi.fn()
    }
    
    vi.mocked(usePlatformStore.getState).mockReturnValue(mockStore)
    
    // Create fresh service instance for each test
    service = new AuthService()
  })

  afterEach(() => {
    vi.clearAllTimers()
  })

  describe('login', () => {
    const validCredentials: LoginCredentials = {
      username: 'test.admin',
      password: 'test123'
    }

    const mockBackendResponse = {
      data: {
        user: {
          userId: 'test-admin-001',
          username: 'test.admin',
          email: 'test.admin@test.local',
          fullName: 'Test Administrator',
          roles: ['SystemAdmin'],
          lastLogin: '2025-08-27T10:00:00Z'
        },
        accessToken: 'mock.jwt.token.test-admin-001',
        refreshToken: 'mock.refresh.token.test-admin-001',
        expiresAt: '2025-08-27T11:00:00Z',
        refreshExpiresAt: '2025-08-28T10:00:00Z'
      }
    }

    it('should successfully login with valid credentials', async () => {
      // Arrange
      vi.mocked(apiClient.login).mockResolvedValue(mockBackendResponse)

      // Act
      const result = await service.login(validCredentials)

      // Assert
      expect(result.success).toBe(true)
      expect(result.user).toBeDefined()
      expect(result.user?.username).toBe('test.admin')
      expect(result.user?.role).toBe('SystemAdmin')
      expect(result.redirectTo).toBe('/admin/dashboard')
      
      // Verify store was updated
      expect(mockStore.setUser).toHaveBeenCalledWith(expect.objectContaining({
        username: 'test.admin',
        role: 'SystemAdmin'
      }))
      expect(mockStore.setToken).toHaveBeenCalled()
      expect(mockStore.setAuthenticated).toHaveBeenCalledWith(true)
    })

    it('should handle login failure with invalid credentials', async () => {
      // Arrange
      vi.mocked(apiClient.login).mockRejectedValue(new Error('Invalid credentials'))

      // Act
      const result = await service.login(validCredentials)

      // Assert
      expect(result.success).toBe(false)
      expect(result.error).toContain('Invalid credentials')
      expect(mockStore.setAuthenticated).not.toHaveBeenCalled()
    })

    it('should validate login response format', async () => {
      // Arrange - Invalid response format
      const invalidResponse = {
        data: {
          // Missing required fields
          message: 'Success'
        }
      }
      vi.mocked(apiClient.login).mockResolvedValue(invalidResponse)

      // Act
      const result = await service.login(validCredentials)

      // Assert
      expect(result.success).toBe(false)
      expect(result.error).toContain('Invalid response format')
    })

    it('should map roles to permissions correctly', async () => {
      // Arrange
      vi.mocked(apiClient.login).mockResolvedValue(mockBackendResponse)

      // Act
      await service.login(validCredentials)

      // Assert - Verify SystemAdmin gets all permissions
      expect(mockStore.setPermissions).toHaveBeenCalledWith(
        expect.arrayContaining([
          'SYSTEM_ADMIN',
          'MANAGE_USERS', 
          'MANAGE_HIERARCHY',
          'MANAGE_SECURITY',
          'VIEW_USERS',
          'VIEW_SECURITY'
        ])
      )
    })

    it('should set correct dashboard layout based on role', async () => {
      // Arrange
      vi.mocked(apiClient.login).mockResolvedValue(mockBackendResponse)

      // Act
      await service.login(validCredentials)

      // Assert
      expect(mockStore.setDashboardLayout).toHaveBeenCalledWith('admin')
    })

    it('should redirect Operator to user dashboard', async () => {
      // Arrange
      const operatorResponse = {
        ...mockBackendResponse,
        data: {
          ...mockBackendResponse.data,
          user: {
            ...mockBackendResponse.data.user,
            roles: ['Operator']
          }
        }
      }
      vi.mocked(apiClient.login).mockResolvedValue(operatorResponse)

      // Act
      const result = await service.login(validCredentials)

      // Assert
      expect(result.redirectTo).toBe('/dashboard')
      expect(mockStore.setDashboardLayout).toHaveBeenCalledWith('user')
    })
  })

  describe('logout', () => {
    it('should successfully logout and clear data', async () => {
      // Arrange
      vi.mocked(apiClient.logout).mockResolvedValue({})
      
      // Act
      await service.logout()

      // Assert
      expect(apiClient.logout).toHaveBeenCalled()
      expect(mockStore.clearAuth).toHaveBeenCalled()
    })

    it('should clear data even if API call fails', async () => {
      // Arrange
      vi.mocked(apiClient.logout).mockRejectedValue(new Error('Network error'))
      
      // Act
      await service.logout()

      // Assert
      expect(mockStore.clearAuth).toHaveBeenCalled()
    })
  })

  describe('token refresh', () => {
    beforeEach(() => {
      mockStore.auth.token = {
        accessToken: 'old-token',
        refreshToken: 'refresh-token',
        expiresAt: new Date(Date.now() + 3600000),
        refreshExpiresAt: new Date(Date.now() + 86400000)
      }
    })

    it('should refresh token successfully', async () => {
      // Arrange
      const newToken = {
        accessToken: 'new-token',
        refreshToken: 'new-refresh-token',
        expiresAt: new Date(Date.now() + 3600000),
        refreshExpiresAt: new Date(Date.now() + 86400000)
      }
      
      vi.mocked(apiClient.refreshToken).mockResolvedValue({
        data: { success: true, token: newToken }
      })

      // Act
      const result = await service.refreshToken()

      // Assert
      expect(result).toBe(true)
      expect(mockStore.setToken).toHaveBeenCalledWith(newToken)
    })

    it('should logout user if refresh fails', async () => {
      // Arrange
      vi.mocked(apiClient.refreshToken).mockRejectedValue(new Error('Refresh failed'))

      // Act
      const result = await service.refreshToken()

      // Assert
      expect(result).toBe(false)
      expect(mockStore.clearAuth).toHaveBeenCalled()
    })

    it('should handle missing refresh token', async () => {
      // Arrange
      mockStore.auth.token = null

      // Act
      const result = await service.refreshToken()

      // Assert
      expect(result).toBe(false)
      expect(mockStore.clearAuth).toHaveBeenCalled()
    })
  })

  describe('permission checks', () => {
    beforeEach(() => {
      mockStore.auth.permissions = ['VIEW_USERS', 'MANAGE_USERS']
      mockStore.auth.user = { role: 'Admin' }
    })

    it('should correctly check user permissions', () => {
      // Act & Assert
      expect(service.hasPermission('VIEW_USERS')).toBe(true)
      expect(service.hasPermission('MANAGE_USERS')).toBe(true)
      expect(service.hasPermission('SYSTEM_ADMIN')).toBe(false)
    })

    it('should correctly check user roles', () => {
      // Act & Assert
      expect(service.hasRole('Admin')).toBe(true)
      expect(service.hasRole('SystemAdmin')).toBe(false)
      expect(service.hasRole('Operator')).toBe(false)
    })

    it('should check multiple roles correctly', () => {
      // Act & Assert
      expect(service.hasAnyRole(['Admin', 'SystemAdmin'])).toBe(true)
      expect(service.hasAnyRole(['Operator', 'Supervisor'])).toBe(false)
    })
  })

  describe('authentication state', () => {
    it('should return false when not authenticated', () => {
      // Arrange
      mockStore.auth.isAuthenticated = false

      // Act & Assert
      expect(service.isAuthenticated()).toBe(false)
    })

    it('should return true when fully authenticated', () => {
      // Arrange
      mockStore.auth.isAuthenticated = true
      mockStore.auth.user = { id: '1', username: 'test' }
      mockStore.auth.token = { accessToken: 'token' }

      // Act & Assert
      expect(service.isAuthenticated()).toBe(true)
    })

    it('should return false when missing user data', () => {
      // Arrange
      mockStore.auth.isAuthenticated = true
      mockStore.auth.user = null
      mockStore.auth.token = { accessToken: 'token' }

      // Act & Assert
      expect(service.isAuthenticated()).toBe(false)
    })

    it('should return false when missing token', () => {
      // Arrange
      mockStore.auth.isAuthenticated = true
      mockStore.auth.user = { id: '1', username: 'test' }
      mockStore.auth.token = null

      // Act & Assert
      expect(service.isAuthenticated()).toBe(false)
    })
  })

  describe('role-based permissions mapping', () => {
    it('should map SystemAdmin role correctly', () => {
      // Create a test instance to access private method via service
      const testService = new AuthService()
      
      // Test through login flow which uses the mapping
      const roles = ['SystemAdmin']
      const permissions = (testService as any).mapRolesToPermissions(roles)
      
      expect(permissions).toContain('SYSTEM_ADMIN')
      expect(permissions).toContain('MANAGE_USERS')
      expect(permissions).toContain('MANAGE_SECURITY')
      expect(permissions).toContain('VIEW_USERS')
      expect(permissions).toContain('VIEW_SECURITY')
    })

    it('should map Admin role correctly', () => {
      const testService = new AuthService()
      const roles = ['Admin']
      const permissions = (testService as any).mapRolesToPermissions(roles)
      
      expect(permissions).toContain('MANAGE_USERS')
      expect(permissions).toContain('VIEW_USERS')
      expect(permissions).not.toContain('SYSTEM_ADMIN')
    })

    it('should map Operator role correctly', () => {
      const testService = new AuthService()
      const roles = ['Operator']
      const permissions = (testService as any).mapRolesToPermissions(roles)
      
      expect(permissions).toContain('VIEW_DEVICES')
      expect(permissions).toContain('VIEW_OEE')
      expect(permissions).not.toContain('MANAGE_USERS')
      expect(permissions).not.toContain('SYSTEM_ADMIN')
    })

    it('should handle multiple roles without duplicates', () => {
      const testService = new AuthService()
      const roles = ['Admin', 'Supervisor']
      const permissions = (testService as any).mapRolesToPermissions(roles)
      
      // Should contain permissions from both roles without duplicates
      const uniquePermissions = [...new Set(permissions)]
      expect(permissions).toHaveLength(uniquePermissions.length)
    })
  })

  describe('CFR Part 11 Compliance', () => {
    it('should never generate synthetic user data', async () => {
      // Arrange - Simulate API failure
      vi.mocked(apiClient.login).mockRejectedValue(new Error('Network error'))
      const testCredentials: LoginCredentials = {
        username: 'test.user',
        password: 'test123'
      }

      // Act
      const result = await service.login(testCredentials)

      // Assert - Should fail gracefully without generating fake user data
      expect(result.success).toBe(false)
      expect(result.user).toBeUndefined()
      expect(result.error).toBeDefined()
      
      // Verify no fake authentication was set
      expect(mockStore.setUser).not.toHaveBeenCalled()
      expect(mockStore.setAuthenticated).not.toHaveBeenCalled()
    })

    it('should maintain data integrity during token refresh', async () => {
      // Arrange - Setup valid initial state
      mockStore.auth.token = {
        accessToken: 'valid-token',
        refreshToken: 'valid-refresh-token', 
        expiresAt: new Date(Date.now() + 3600000),
        refreshExpiresAt: new Date(Date.now() + 86400000)
      }

      // Simulate refresh failure
      vi.mocked(apiClient.refreshToken).mockRejectedValue(new Error('Refresh failed'))

      // Act
      const result = await service.refreshToken()

      // Assert - Should logout rather than maintain invalid auth state
      expect(result).toBe(false)
      expect(mockStore.clearAuth).toHaveBeenCalled()
    })

    it('should never cache or persist invalid authentication data', async () => {
      // Arrange - Invalid response that passes initial checks but is malformed
      const malformedResponse = {
        data: {
          accessToken: 'token',
          user: {
            userId: null, // Invalid user data
            username: '',
            roles: []
          },
          expiresAt: 'invalid-date'
        }
      }
      
      vi.mocked(apiClient.login).mockResolvedValue(malformedResponse)
      
      const testCredentials: LoginCredentials = {
        username: 'test.user',
        password: 'test123'
      }

      // Act
      const result = await service.login(testCredentials)

      // Assert
      expect(result.success).toBe(false)
      expect(mockStore.setUser).not.toHaveBeenCalled()
    })
  })
})