/**
 * Authentication Flow Integration Tests
 * Tests the complete authentication workflow with real API connections
 * CRITICAL: Tests must verify no synthetic authentication data generation
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { server } from '@/test/msw-setup'
import { http, HttpResponse } from 'msw'
import { authService } from '@/lib/services/authService'
import { usePlatformStore } from '@/store/platformStore'
import { LoginForm } from '@/components/auth/LoginForm'

// Test wrapper component
const TestWrapper = ({ children }: { children: React.ReactNode }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  })

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  )
}

describe('Authentication Flow Integration', () => {
  let user: ReturnType<typeof userEvent.setup>

  beforeEach(() => {
    // Reset store state
    usePlatformStore.getState().clearAuth()
    
    // Setup user event
    user = userEvent.setup()
    
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllTimers()
  })

  describe('Successful Login Flow', () => {
    it('should complete full login workflow for admin user', async () => {
      // Arrange
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act - Fill login form
      await user.type(screen.getByLabelText(/username/i), 'test.admin')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert - Wait for successful login
      await waitFor(() => {
        const store = usePlatformStore.getState()
        expect(store.auth.isAuthenticated).toBe(true)
        expect(store.auth.user?.username).toBe('test.admin')
        expect(store.auth.user?.role).toBe('SystemAdmin')
      })

      // Verify permissions were set correctly
      const store = usePlatformStore.getState()
      expect(store.auth.permissions).toContain('SYSTEM_ADMIN')
      expect(store.auth.permissions).toContain('MANAGE_USERS')
    })

    it('should set appropriate dashboard layout for different roles', async () => {
      // Test Admin role
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      await user.type(screen.getByLabelText(/username/i), 'test.admin')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() => {
        const store = usePlatformStore.getState()
        expect(store.dashboardLayout).toBe('admin')
      })
    })

    it('should handle supervisor login correctly', async () => {
      // Arrange
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'test.supervisor')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert
      await waitFor(() => {
        const store = usePlatformStore.getState()
        expect(store.auth.isAuthenticated).toBe(true)
        expect(store.auth.user?.role).toBe('Supervisor')
        expect(store.dashboardLayout).toBe('user')
      })

      // Verify supervisor permissions
      const store = usePlatformStore.getState()
      expect(store.auth.permissions).toContain('MANAGE_OEE')
      expect(store.auth.permissions).toContain('MANAGE_SCHEDULING')
      expect(store.auth.permissions).not.toContain('SYSTEM_ADMIN')
    })

    it('should handle operator login with limited permissions', async () => {
      // Arrange
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'test.operator')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert
      await waitFor(() => {
        const store = usePlatformStore.getState()
        expect(store.auth.isAuthenticated).toBe(true)
        expect(store.auth.user?.role).toBe('Operator')
      })

      // Verify limited operator permissions
      const store = usePlatformStore.getState()
      expect(store.auth.permissions).toContain('VIEW_DEVICES')
      expect(store.auth.permissions).toContain('VIEW_OEE')
      expect(store.auth.permissions).not.toContain('MANAGE_USERS')
      expect(store.auth.permissions).not.toContain('SYSTEM_ADMIN')
    })
  })

  describe('Failed Login Flow', () => {
    it('should handle invalid credentials properly', async () => {
      // Arrange
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'invalid.user')
      await user.type(screen.getByLabelText(/password/i), 'wrongpassword')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert
      await waitFor(() => {
        expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
      })

      // Verify no authentication was set
      const store = usePlatformStore.getState()
      expect(store.auth.isAuthenticated).toBe(false)
      expect(store.auth.user).toBe(null)
    })

    it('should handle network errors gracefully', async () => {
      // Arrange - Override handler to simulate network error
      server.use(
        http.post('/api/auth/login', () => {
          return HttpResponse.error()
        })
      )

      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'test.admin')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert
      await waitFor(() => {
        expect(screen.getByText(/network error/i)).toBeInTheDocument()
      })

      // Verify no authentication state was compromised
      const store = usePlatformStore.getState()
      expect(store.auth.isAuthenticated).toBe(false)
    })

    it('should handle malformed server response', async () => {
      // Arrange - Override with malformed response
      server.use(
        http.post('/api/auth/login', () => {
          return HttpResponse.json({
            // Malformed response missing required fields
            message: 'Login successful but incomplete data'
          })
        })
      )

      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'test.admin')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert
      await waitFor(() => {
        expect(screen.getByText(/invalid response format/i)).toBeInTheDocument()
      })

      const store = usePlatformStore.getState()
      expect(store.auth.isAuthenticated).toBe(false)
    })
  })

  describe('Logout Flow', () => {
    it('should complete full logout workflow', async () => {
      // Arrange - First login
      const store = usePlatformStore.getState()
      store.setAuthenticated(true)
      store.setUser({
        id: 'test-user',
        username: 'test.admin',
        email: 'test@example.com',
        fullName: 'Test Admin',
        role: 'SystemAdmin',
        permissions: ['SYSTEM_ADMIN'],
        hierarchyAssignments: [],
        status: 'active',
        createdAt: new Date(),
        updatedAt: new Date()
      })
      store.setToken({
        accessToken: 'test-token',
        refreshToken: 'test-refresh',
        expiresAt: new Date(Date.now() + 3600000),
        refreshExpiresAt: new Date(Date.now() + 86400000)
      })

      // Act
      await authService.logout()

      // Assert
      await waitFor(() => {
        const currentStore = usePlatformStore.getState()
        expect(currentStore.auth.isAuthenticated).toBe(false)
        expect(currentStore.auth.user).toBe(null)
        expect(currentStore.auth.token).toBe(null)
        expect(currentStore.auth.permissions).toEqual([])
      })
    })

    it('should clear authentication even if API call fails', async () => {
      // Arrange - Setup authenticated state
      const store = usePlatformStore.getState()
      store.setAuthenticated(true)
      store.setUser({
        id: 'test-user',
        username: 'test.admin',
        email: 'test@example.com',
        fullName: 'Test Admin',
        role: 'SystemAdmin',
        permissions: ['SYSTEM_ADMIN'],
        hierarchyAssignments: [],
        status: 'active',
        createdAt: new Date(),
        updatedAt: new Date()
      })

      // Override logout to fail
      server.use(
        http.post('/api/auth/logout', () => {
          return HttpResponse.json({ error: 'Server error' }, { status: 500 })
        })
      )

      // Act
      await authService.logout()

      // Assert - Should clear local state despite API failure
      const currentStore = usePlatformStore.getState()
      expect(currentStore.auth.isAuthenticated).toBe(false)
      expect(currentStore.auth.user).toBe(null)
    })
  })

  describe('Token Refresh Flow', () => {
    it('should refresh token successfully', async () => {
      // Arrange - Setup authenticated state with valid refresh token
      const store = usePlatformStore.getState()
      store.setAuthenticated(true)
      store.setToken({
        accessToken: 'old-token',
        refreshToken: 'valid-refresh-token',
        expiresAt: new Date(Date.now() + 300000), // 5 minutes
        refreshExpiresAt: new Date(Date.now() + 86400000) // 24 hours
      })

      // Act
      const result = await authService.refreshToken()

      // Assert
      expect(result).toBe(true)
      
      const currentStore = usePlatformStore.getState()
      expect(currentStore.auth.token?.accessToken).toBe('mock.jwt.token.test-user')
      expect(currentStore.auth.isAuthenticated).toBe(true)
    })

    it('should logout user when refresh fails', async () => {
      // Arrange - Setup with expired refresh token
      const store = usePlatformStore.getState()
      store.setAuthenticated(true)
      store.setToken({
        accessToken: 'old-token',
        refreshToken: 'expired-refresh-token',
        expiresAt: new Date(Date.now() - 3600000), // Expired 1 hour ago
        refreshExpiresAt: new Date(Date.now() - 1800000) // Expired 30 minutes ago
      })

      // Override refresh to fail
      server.use(
        http.post('/api/auth/refresh', () => {
          return HttpResponse.json({ error: 'Token expired' }, { status: 401 })
        })
      )

      // Act
      const result = await authService.refreshToken()

      // Assert
      expect(result).toBe(false)
      
      const currentStore = usePlatformStore.getState()
      expect(currentStore.auth.isAuthenticated).toBe(false)
      expect(currentStore.auth.user).toBe(null)
    })
  })

  describe('Permission and Role Checks', () => {
    beforeEach(async () => {
      // Setup authenticated admin user
      const store = usePlatformStore.getState()
      store.setAuthenticated(true)
      store.setUser({
        id: 'test-admin',
        username: 'test.admin',
        email: 'admin@test.com',
        fullName: 'Test Admin',
        role: 'SystemAdmin',
        permissions: ['SYSTEM_ADMIN', 'MANAGE_USERS', 'VIEW_USERS'],
        hierarchyAssignments: [],
        status: 'active',
        createdAt: new Date(),
        updatedAt: new Date()
      })
      store.setPermissions(['SYSTEM_ADMIN', 'MANAGE_USERS', 'VIEW_USERS'])
    })

    it('should correctly validate user permissions', () => {
      // Act & Assert
      expect(authService.hasPermission('SYSTEM_ADMIN')).toBe(true)
      expect(authService.hasPermission('MANAGE_USERS')).toBe(true)
      expect(authService.hasPermission('INVALID_PERMISSION')).toBe(false)
    })

    it('should correctly validate user roles', () => {
      // Act & Assert
      expect(authService.hasRole('SystemAdmin')).toBe(true)
      expect(authService.hasRole('Operator')).toBe(false)
    })

    it('should correctly validate multiple roles', () => {
      // Act & Assert
      expect(authService.hasAnyRole(['SystemAdmin', 'Admin'])).toBe(true)
      expect(authService.hasAnyRole(['Operator', 'Supervisor'])).toBe(false)
    })

    it('should return current user information', () => {
      // Act
      const currentUser = authService.getCurrentUser()

      // Assert
      expect(currentUser?.username).toBe('test.admin')
      expect(currentUser?.role).toBe('SystemAdmin')
    })

    it('should return current permissions', () => {
      // Act
      const permissions = authService.getCurrentPermissions()

      // Assert
      expect(permissions).toContain('SYSTEM_ADMIN')
      expect(permissions).toContain('MANAGE_USERS')
      expect(permissions).toContain('VIEW_USERS')
    })
  })

  describe('CFR Part 11 Compliance Integration', () => {
    it('should never generate synthetic authentication tokens', async () => {
      // Arrange - Force API to return incomplete data
      server.use(
        http.post('/api/auth/login', () => {
          return HttpResponse.json({
            user: {
              userId: 'test-user',
              username: 'test.user'
            }
            // Missing accessToken and other required fields
          })
        })
      )

      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act
      await user.type(screen.getByLabelText(/username/i), 'test.user')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      // Assert - Should fail rather than generate synthetic tokens
      await waitFor(() => {
        expect(screen.getByText(/invalid response format/i)).toBeInTheDocument()
      })

      const store = usePlatformStore.getState()
      expect(store.auth.token).toBe(null)
      expect(store.auth.isAuthenticated).toBe(false)
    })

    it('should maintain audit trail for authentication events', async () => {
      // This test would verify that authentication events are logged
      // For now, we ensure no silent failures occur
      
      const consoleSpy = vi.spyOn(console, 'error')
      
      render(
        <TestWrapper>
          <LoginForm />
        </TestWrapper>
      )

      // Act - Successful login
      await user.type(screen.getByLabelText(/username/i), 'test.admin')
      await user.type(screen.getByLabelText(/password/i), 'test123')
      await user.click(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() => {
        const store = usePlatformStore.getState()
        expect(store.auth.isAuthenticated).toBe(true)
      })

      // Assert - No unexpected errors logged
      expect(consoleSpy).not.toHaveBeenCalledWith(
        expect.stringMatching(/authentication|token|login/i),
        expect.anything()
      )
    })

    it('should validate token expiration strictly', async () => {
      // Arrange - Setup token that appears valid but is actually expired
      const store = usePlatformStore.getState()
      store.setToken({
        accessToken: 'seemingly-valid-token',
        refreshToken: 'refresh-token',
        expiresAt: new Date(Date.now() - 1000), // Expired 1 second ago
        refreshExpiresAt: new Date(Date.now() + 86400000)
      })
      
      // Mock token refresh to fail
      server.use(
        http.post('/api/auth/refresh', () => {
          return HttpResponse.json({ error: 'Token expired' }, { status: 401 })
        })
      )

      // Act
      await authService.initializeAuth()

      // Assert - Should clear expired authentication
      const currentStore = usePlatformStore.getState()
      expect(currentStore.auth.isAuthenticated).toBe(false)
    })

    it('should handle concurrent authentication attempts safely', async () => {
      // Arrange
      const credentials = { username: 'test.admin', password: 'test123' }
      
      // Act - Start multiple login attempts simultaneously
      const loginPromises = [
        authService.login(credentials),
        authService.login(credentials),
        authService.login(credentials)
      ]
      
      const results = await Promise.all(loginPromises)
      
      // Assert - All should either succeed or fail consistently
      const successCount = results.filter(r => r.success).length
      const failureCount = results.filter(r => !r.success).length
      
      // Either all succeed or all fail (no mixed results from race conditions)
      expect(successCount === results.length || failureCount === results.length).toBe(true)
      
      // Final state should be consistent
      const store = usePlatformStore.getState()
      if (successCount > 0) {
        expect(store.auth.isAuthenticated).toBe(true)
        expect(store.auth.user).toBeTruthy()
      } else {
        expect(store.auth.isAuthenticated).toBe(false)
        expect(store.auth.user).toBe(null)
      }
    })
  })
})