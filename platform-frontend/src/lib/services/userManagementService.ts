import { apiClient } from '@/lib/api/client'
import type {
  User,
  UserRole,
  ApiResponse,
  PaginatedResponse,
  SecuritySession,
  UserActivity
} from '@/types'

/**
 * Enhanced User Management Service for Phase 2 admin features
 * Extends user management with advanced features for session monitoring,
 * activity tracking, and bulk operations
 */
class UserManagementService {
  private readonly baseUrl = '/api/admin/users'

  /**
   * Get user's active sessions
   */
  async getUserSessions(userId: string, params?: {
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<PaginatedResponse<SecuritySession>>> {
    const queryParams: any = {}
    
    if (params) {
      if (params.page) queryParams.page = params.page
      if (params.pageSize) queryParams.pageSize = params.pageSize
    }

    return apiClient.get<PaginatedResponse<SecuritySession>>(`${this.baseUrl}/${userId}/sessions`, {
      params: queryParams
    }, 'security')
  }

  /**
   * Get user activity history
   */
  async getUserActivity(userId: string, params?: {
    page?: number
    pageSize?: number
    startDate?: Date
    endDate?: Date
    module?: string
    action?: string
  }): Promise<ApiResponse<PaginatedResponse<UserActivity>>> {
    const queryParams: any = {}
    
    if (params) {
      if (params.page) queryParams.page = params.page
      if (params.pageSize) queryParams.pageSize = params.pageSize
      if (params.startDate) queryParams.startDate = params.startDate.toISOString()
      if (params.endDate) queryParams.endDate = params.endDate.toISOString()
      if (params.module) queryParams.module = params.module
      if (params.action) queryParams.action = params.action
    }

    return apiClient.get<PaginatedResponse<UserActivity>>(`${this.baseUrl}/${userId}/activity`, {
      params: queryParams
    }, 'security')
  }

  /**
   * Get user's login history
   */
  async getUserLoginHistory(userId: string, params?: {
    page?: number
    pageSize?: number
    startDate?: Date
    endDate?: Date
  }): Promise<ApiResponse<PaginatedResponse<{
    id: string
    userId: string
    username: string
    ipAddress: string
    userAgent: string
    success: boolean
    failureReason?: string
    timestamp: Date
    location?: string
    deviceFingerprint?: string
  }>>> {
    const queryParams: any = {}
    
    if (params) {
      if (params.page) queryParams.page = params.page
      if (params.pageSize) queryParams.pageSize = params.pageSize
      if (params.startDate) queryParams.startDate = params.startDate.toISOString()
      if (params.endDate) queryParams.endDate = params.endDate.toISOString()
    }

    return apiClient.get(`${this.baseUrl}/${userId}/login-history`, {
      params: queryParams
    }, 'security')
  }

  /**
   * Bulk password reset for multiple users
   */
  async bulkPasswordReset(userIds: string[], options?: {
    sendEmailNotification?: boolean
    temporaryPasswordLength?: number
    forcePasswordChange?: boolean
  }): Promise<ApiResponse<{
    successful: Array<{ userId: string; temporaryPassword?: string }>
    failed: Array<{ userId: string; error: string }>
  }>> {
    return apiClient.post(`${this.baseUrl}/bulk/reset`, {
      userIds,
      options: {
        sendEmailNotification: options?.sendEmailNotification ?? true,
        temporaryPasswordLength: options?.temporaryPasswordLength ?? 12,
        forcePasswordChange: options?.forcePasswordChange ?? true
      }
    }, {}, 'security')
  }

  /**
   * Lock user account
   */
  async lockUser(userId: string, reason?: string): Promise<ApiResponse<User>> {
    return apiClient.post(`${this.baseUrl}/${userId}/lock`, { reason }, {}, 'security')
  }

  /**
   * Unlock user account
   */
  async unlockUser(userId: string): Promise<ApiResponse<User>> {
    return apiClient.post(`${this.baseUrl}/${userId}/unlock`, {}, {}, 'security')
  }

  /**
   * Terminate all user sessions
   */
  async terminateUserSessions(userId: string): Promise<ApiResponse<{ terminatedSessions: number }>> {
    return apiClient.delete(`${this.baseUrl}/${userId}/sessions`, {}, 'security')
  }

  /**
   * Terminate specific user session
   */
  async terminateUserSession(userId: string, sessionId: string): Promise<ApiResponse<void>> {
    return apiClient.delete(`${this.baseUrl}/${userId}/sessions/${sessionId}`, {}, 'security')
  }

  /**
   * Get role permissions matrix
   */
  async getRolePermissions(): Promise<ApiResponse<{
    roles: Array<{
      role: UserRole
      displayName: string
      description: string
      permissions: Array<{
        permission: string
        category: string
        description: string
        granted: boolean
      }>
    }>
  }>> {
    return apiClient.get('/api/admin/roles/permissions', {}, 'security')
  }

  /**
   * Update role permissions
   */
  async updateRolePermissions(role: UserRole, permissions: string[]): Promise<ApiResponse<void>> {
    return apiClient.put(`/api/admin/roles/${role}/permissions`, {
      permissions
    }, {}, 'security')
  }

  /**
   * Bulk user operations
   */
  async bulkUpdateUsers(userIds: string[], operations: {
    role?: UserRole
    status?: 'active' | 'inactive' | 'locked'
    hierarchyNodeIds?: string[]
    addToHierarchy?: string[]
    removeFromHierarchy?: string[]
  }): Promise<ApiResponse<{
    successful: Array<{ userId: string; changes: string[] }>
    failed: Array<{ userId: string; error: string }>
  }>> {
    return apiClient.post(`${this.baseUrl}/bulk/update`, {
      userIds,
      operations
    }, {}, 'security')
  }

  /**
   * Get user session analytics
   */
  async getUserSessionAnalytics(userId: string, timeRange: '24h' | '7d' | '30d' = '7d'): Promise<ApiResponse<{
    totalSessions: number
    activeSessions: number
    averageSessionDuration: number
    longestSession: number
    sessionsByDay: Array<{ date: string; sessions: number }>
    deviceTypes: Array<{ device: string; count: number }>
    locations: Array<{ location: string; count: number }>
  }>> {
    return apiClient.get(`${this.baseUrl}/${userId}/session-analytics`, {
      params: { timeRange }
    }, 'security')
  }

  /**
   * Get user activity summary
   */
  async getUserActivitySummary(userId: string, timeRange: '24h' | '7d' | '30d' = '7d'): Promise<ApiResponse<{
    totalActions: number
    successfulActions: number
    failedActions: number
    topActions: Array<{ action: string; count: number }>
    moduleUsage: Array<{ module: string; actions: number }>
    timelineData: Array<{ date: string; actions: number }>
    lastActivity: Date
  }>> {
    return apiClient.get(`${this.baseUrl}/${userId}/activity-summary`, {
      params: { timeRange }
    }, 'security')
  }

  /**
   * Search users with advanced filtering
   */
  async searchUsers(params: {
    query?: string
    roles?: UserRole[]
    statuses?: ('active' | 'inactive' | 'locked')[]
    hierarchyNodeIds?: string[]
    lastLoginBefore?: Date
    lastLoginAfter?: Date
    createdBefore?: Date
    createdAfter?: Date
    hasPermissions?: string[]
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<PaginatedResponse<User>>> {
    const queryParams: any = {}

    if (params.query) queryParams.query = params.query
    if (params.roles?.length) queryParams.roles = params.roles.join(',')
    if (params.statuses?.length) queryParams.statuses = params.statuses.join(',')
    if (params.hierarchyNodeIds?.length) queryParams.hierarchyNodeIds = params.hierarchyNodeIds.join(',')
    if (params.lastLoginBefore) queryParams.lastLoginBefore = params.lastLoginBefore.toISOString()
    if (params.lastLoginAfter) queryParams.lastLoginAfter = params.lastLoginAfter.toISOString()
    if (params.createdBefore) queryParams.createdBefore = params.createdBefore.toISOString()
    if (params.createdAfter) queryParams.createdAfter = params.createdAfter.toISOString()
    if (params.hasPermissions?.length) queryParams.hasPermissions = params.hasPermissions.join(',')
    if (params.page) queryParams.page = params.page
    if (params.pageSize) queryParams.pageSize = params.pageSize

    return apiClient.get<PaginatedResponse<User>>(`${this.baseUrl}/search`, {
      params: queryParams
    }, 'security')
  }

  /**
   * Bulk assign users to hierarchy nodes
   */
  async bulkAssignHierarchy(userIds: string[], hierarchyNodeIds: string[], options?: {
    replaceExisting?: boolean
  }): Promise<ApiResponse<{
    successful: Array<{ userId: string }>
    failed: Array<{ userId: string; error: string }>
  }>> {
    return apiClient.post(`${this.baseUrl}/bulk/assign-hierarchy`, {
      userIds,
      hierarchyNodeIds,
      replaceExisting: options?.replaceExisting ?? false
    }, {}, 'security')
  }

  /**
   * Get inactive users report
   */
  async getInactiveUsersReport(params?: {
    inactiveSinceDays?: number
    neverLoggedIn?: boolean
    includeStats?: boolean
  }): Promise<ApiResponse<{
    users: Array<User & {
      lastLoginDaysAgo?: number
      totalSessions: number
      neverLoggedIn: boolean
    }>
    summary: {
      totalInactive: number
      neverLoggedIn: number
      inactiveMoreThan30Days: number
      inactiveMoreThan90Days: number
    }
  }>> {
    return apiClient.get(`${this.baseUrl}/reports/inactive`, {
      params: {
        inactiveSinceDays: params?.inactiveSinceDays ?? 30,
        neverLoggedIn: params?.neverLoggedIn ?? true,
        includeStats: params?.includeStats ?? true
      }
    }, 'security')
  }

  /**
   * Cleanup inactive users (soft delete)
   */
  async cleanupInactiveUsers(params: {
    inactiveSinceDays: number
    dryRun?: boolean
    preserveAdmins?: boolean
  }): Promise<ApiResponse<{
    affectedUsers: number
    usersToDelete: User[]
    deletedUsers?: User[]
  }>> {
    return apiClient.post(`${this.baseUrl}/cleanup/inactive`, {
      inactiveSinceDays: params.inactiveSinceDays,
      dryRun: params.dryRun ?? true,
      preserveAdmins: params.preserveAdmins ?? true
    }, {}, 'security')
  }
}

// Export singleton instance
export const userManagementService = new UserManagementService()

// Export the class for testing
export { UserManagementService }