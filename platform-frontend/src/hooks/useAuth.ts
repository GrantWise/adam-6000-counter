import { useCallback } from 'react'
import { usePlatformStore } from '@/store/platformStore'
import { authService } from '@/lib/services/authService'
import type { LoginCredentials, Permission } from '@/types'

/**
 * Authentication hook providing auth state and methods
 * Centralized authentication logic for components
 */
export const useAuth = () => {
  const auth = usePlatformStore(state => state.auth)

  const login = useCallback(async (credentials: LoginCredentials) => {
    return await authService.login(credentials)
  }, [])

  const logout = useCallback(async () => {
    await authService.logout()
  }, [])

  const hasPermission = useCallback((permission: Permission) => {
    return authService.hasPermission(permission)
  }, [auth.permissions])

  const hasRole = useCallback((role: string) => {
    return authService.hasRole(role)
  }, [auth.user?.role])

  const hasAnyRole = useCallback((roles: string[]) => {
    return authService.hasAnyRole(roles)
  }, [auth.user?.role])

  const isAdmin = useCallback(() => {
    return hasAnyRole(['Admin', 'SystemAdmin'])
  }, [hasAnyRole])

  const canAccessAdminDashboard = useCallback(() => {
    return isAdmin()
  }, [isAdmin])

  const canManageUsers = useCallback(() => {
    return hasPermission('MANAGE_USERS')
  }, [hasPermission])

  const canManageHierarchy = useCallback(() => {
    return hasPermission('MANAGE_HIERARCHY')
  }, [hasPermission])

  const canViewSecurity = useCallback(() => {
    return hasPermission('VIEW_SECURITY')
  }, [hasPermission])

  return {
    // State
    ...auth,
    
    // Actions
    login,
    logout,
    
    // Permission checks
    hasPermission,
    hasRole,
    hasAnyRole,
    isAdmin,
    canAccessAdminDashboard,
    canManageUsers,
    canManageHierarchy,
    canViewSecurity,
    
    // Computed properties
    displayName: auth.user?.fullName || auth.user?.username || 'User',
    initials: auth.user?.fullName
      ?.split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase() || 'U',
  }
}