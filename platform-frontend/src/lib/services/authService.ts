import { apiClient } from '@/lib/api/client'
import { usePlatformStore } from '@/store/platformStore'
import { validateLoginResponse, processApiResponse } from '@/lib/utils/apiValidation'
import type { LoginCredentials, LoginResult, User, AuthToken, Permission, UserRole } from '@/types'

/**
 * Authentication service for the Industrial ADAM Platform
 * Handles login, logout, token management, and user session
 */
class AuthService {
  private refreshTimeoutId: NodeJS.Timeout | null = null

  /**
   * Authenticate user with credentials
   */
  async login(credentials: LoginCredentials): Promise<LoginResult> {
    try {
      const response = await apiClient.login(credentials.username, credentials.password)
      
      // Validate the response format first
      const validation = validateLoginResponse(response.data)
      if (!validation.isValid) {
        console.error('Login response validation failed:', validation.errors)
        return {
          success: false,
          error: `Invalid response format: ${validation.errors.join(', ')}`
        }
      }
      
      const validatedData = validation.data
      
      // Handle both success wrapper and direct response formats
      const authData = validatedData.success ? validatedData : validatedData
      if (authData && authData.accessToken && authData.user) {
        const { user: backendUser, accessToken, refreshToken, expiresAt, refreshExpiresAt } = authData
        
        // Create token object in expected format
        const token = {
          accessToken,
          refreshToken,
          expiresAt: new Date(expiresAt),
          refreshExpiresAt: new Date(refreshExpiresAt)
        }
        
        // Map backend user format to frontend format
        const user = {
          id: backendUser.userId,
          username: backendUser.username,
          email: backendUser.email,
          fullName: backendUser.fullName,
          role: backendUser.roles[0] as UserRole, // Take first role
          permissions: this.mapRolesToPermissions(backendUser.roles),
          hierarchyAssignments: [], // TODO: Get from backend when implemented
          status: 'active' as const,
          lastLogin: backendUser.lastLogin ? new Date(backendUser.lastLogin) : undefined,
          createdAt: new Date(), // TODO: Get from backend when available
          updatedAt: new Date()  // TODO: Get from backend when available
        }
        
        // Store authentication data
        this.setAuthenticationData(user, token)
        
        // Setup automatic token refresh
        this.scheduleTokenRefresh(token.expiresAt)
        
        // Determine redirect based on user role
        const redirectTo = this.getRedirectPath(user)
        
        return {
          success: true,
          user,
          redirectTo
        }
      } else {
        return {
          success: false,
          error: 'Invalid response format'
        }
      }
    } catch (error: any) {
      console.error('Login error:', error)
      
      // Handle specific error scenarios
      if (error.response?.status === 401) {
        return {
          success: false,
          error: 'Invalid credentials'
        }
      }
      
      if (error.code === 'ERR_NETWORK' || error.message?.includes('Network')) {
        return {
          success: false,
          error: 'Network error'
        }
      }
      
      return {
        success: false,
        error: error.message || 'An error occurred during login'
      }
    }
  }

  /**
   * Logout current user
   */
  async logout(): Promise<void> {
    try {
      // Clear refresh timeout
      if (this.refreshTimeoutId) {
        clearTimeout(this.refreshTimeoutId)
        this.refreshTimeoutId = null
      }

      // Call logout endpoint
      await apiClient.logout()
    } catch (error) {
      console.error('Logout error:', error)
      // Continue with local logout even if API call fails
    } finally {
      // Clear authentication data
      this.clearAuthenticationData()
    }
  }

  /**
   * Refresh authentication token
   */
  async refreshToken(): Promise<boolean> {
    try {
      const store = usePlatformStore.getState()
      const currentToken = store.auth.token
      
      if (!currentToken?.refreshToken) {
        throw new Error('No refresh token available')
      }

      const response = await apiClient.refreshToken(currentToken.refreshToken)
      
      if (response.data.success) {
        const { token } = response.data
        
        // Update token in store
        store.setToken(token)
        
        // Schedule next refresh
        this.scheduleTokenRefresh(token.expiresAt)
        
        return true
      } else {
        throw new Error('Token refresh failed')
      }
    } catch (error) {
      console.error('Token refresh error:', error)
      
      // If refresh fails, logout user
      await this.logout()
      return false
    }
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(permission: Permission): boolean {
    const store = usePlatformStore.getState()
    return store.auth.permissions.includes(permission)
  }

  /**
   * Check if user has specific role
   */
  hasRole(role: string): boolean {
    const store = usePlatformStore.getState()
    return store.auth.user?.role === role
  }

  /**
   * Check if user has any of the specified roles
   */
  hasAnyRole(roles: string[]): boolean {
    const store = usePlatformStore.getState()
    const userRole = store.auth.user?.role
    return userRole ? roles.includes(userRole) : false
  }

  /**
   * Get current user
   */
  getCurrentUser(): User | null {
    const store = usePlatformStore.getState()
    return store.auth.user
  }

  /**
   * Get current user permissions
   */
  getCurrentPermissions(): Permission[] {
    const store = usePlatformStore.getState()
    return store.auth.permissions
  }

  /**
   * Get current access token for API/SignalR authentication
   */
  getToken(): string | null {
    const store = usePlatformStore.getState()
    return store.auth.token?.accessToken || null
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    const store = usePlatformStore.getState()
    return store.auth.isAuthenticated && !!store.auth.user && !!store.auth.token
  }

  /**
   * Initialize authentication state on app startup
   */
  async initializeAuth(): Promise<void> {
    const store = usePlatformStore.getState()
    
    try {
      store.setAuthLoading(true)
      
      // Check if we have stored tokens
      const token = store.auth.token
      
      if (token && token.expiresAt > new Date()) {
        // Token is still valid, assume valid session
        // Note: Logger API doesn't have a verify endpoint, so we trust unexpired tokens
        
        // We can try to make any authenticated call to test the token
        // For now, just schedule refresh and set as authenticated
        store.setAuthenticated(true)
        this.scheduleTokenRefresh(token.expiresAt)
      } else if (token?.refreshToken) {
        // Access token expired, try to refresh
        const refreshSuccess = await this.refreshToken()
        if (!refreshSuccess) {
          this.clearAuthenticationData()
        }
      } else {
        // No valid tokens, user needs to login
        this.clearAuthenticationData()
      }
    } catch (error) {
      console.error('Auth initialization error:', error)
      this.clearAuthenticationData()
    } finally {
      store.setAuthLoading(false)
    }
  }

  /**
   * Set authentication data in store
   */
  private setAuthenticationData(user: User, token: AuthToken): void {
    const store = usePlatformStore.getState()
    
    store.setUser(user)
    store.setToken(token)
    store.setPermissions(user.permissions)
    store.setAuthenticated(true)
    
    // Set dashboard layout based on user role
    const dashboardLayout = this.getDashboardLayout(user.role)
    store.setDashboardLayout(dashboardLayout)
    
    // Initialize hierarchy context if user has assignments
    if (user.hierarchyAssignments.length > 0) {
      const primaryAssignment = user.hierarchyAssignments[0]
      store.setHierarchyContext({
        currentNode: primaryAssignment.node,
        userAccess: user.hierarchyAssignments.map(a => a.node),
        breadcrumbs: this.buildBreadcrumbs(primaryAssignment.node)
      })
    }
  }

  /**
   * Clear authentication data
   */
  private clearAuthenticationData(): void {
    const store = usePlatformStore.getState()
    store.clearAuth()
  }

  /**
   * Get redirect path based on user role
   */
  private getRedirectPath(user: User): string {
    switch (user.role) {
      case 'SystemAdmin':
      case 'Admin':
        return '/admin/dashboard'
      case 'Supervisor':
      case 'Operator':
      default:
        return '/dashboard'
    }
  }

  /**
   * Get dashboard layout based on user role
   */
  private getDashboardLayout(role: string): 'admin' | 'user' {
    return (role === 'SystemAdmin' || role === 'Admin') ? 'admin' : 'user'
  }

  /**
   * Build breadcrumbs from hierarchy node
   */
  private buildBreadcrumbs(node: any): any[] {
    const breadcrumbs = []
    let current = node
    
    while (current) {
      breadcrumbs.unshift(current)
      current = current.parent
    }
    
    return breadcrumbs
  }

  /**
   * Schedule automatic token refresh
   */
  private scheduleTokenRefresh(expiresAt: Date): void {
    // Clear existing timeout
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId)
    }
    
    // Calculate refresh time (5 minutes before expiration)
    const refreshTime = new Date(expiresAt).getTime() - Date.now() - (5 * 60 * 1000)
    
    if (refreshTime > 0) {
      this.refreshTimeoutId = setTimeout(() => {
        this.refreshToken()
      }, refreshTime)
    } else {
      // Token expires soon, refresh immediately
      this.refreshToken()
    }
  }

  /**
   * Map backend roles to frontend permissions
   */
  private mapRolesToPermissions(roles: string[]): Permission[] {
    const permissions: Permission[] = []
    
    for (const role of roles) {
      switch (role) {
        case 'SystemAdmin':
          permissions.push('SYSTEM_ADMIN', 'MANAGE_USERS', 'MANAGE_HIERARCHY', 'MANAGE_SECURITY', 'MANAGE_DEVICES', 'MANAGE_OEE', 'MANAGE_SCHEDULING')
          permissions.push('VIEW_USERS', 'VIEW_HIERARCHY', 'VIEW_SECURITY', 'VIEW_DEVICES', 'VIEW_OEE', 'VIEW_SCHEDULING')
          break
        case 'Admin':
          permissions.push('MANAGE_USERS', 'MANAGE_HIERARCHY', 'MANAGE_DEVICES', 'MANAGE_OEE', 'MANAGE_SCHEDULING')
          permissions.push('VIEW_USERS', 'VIEW_HIERARCHY', 'VIEW_DEVICES', 'VIEW_OEE', 'VIEW_SCHEDULING')
          break
        case 'Supervisor':
          permissions.push('MANAGE_OEE', 'MANAGE_SCHEDULING')
          permissions.push('VIEW_DEVICES', 'VIEW_OEE', 'VIEW_SCHEDULING', 'VIEW_HIERARCHY')
          break
        case 'Operator':
          permissions.push('VIEW_DEVICES', 'VIEW_OEE')
          break
      }
    }
    
    return [...new Set(permissions)] // Remove duplicates
  }
}

// Export singleton instance
export const authService = new AuthService()

// Export the class for testing
export { AuthService }