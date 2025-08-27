import { apiClient } from '@/lib/api/client'
import type {
  User,
  UserRole,
  ApiResponse,
  PaginatedResponse,
  SortConfig,
  FilterConfig,
  HierarchyNode
} from '@/types'

/**
 * User Management Service
 * Handles all user-related API operations including CRUD operations,
 * role assignments, and hierarchy management
 */
class UserService {
  private readonly basePath = '/api/admin/users'

  /**
   * Get paginated list of users with filtering and sorting
   */
  async getUsers(options: {
    page?: number
    pageSize?: number
    search?: string
    role?: UserRole
    status?: 'active' | 'inactive' | 'locked'
    sort?: SortConfig
    filters?: FilterConfig[]
  } = {}): Promise<ApiResponse<PaginatedResponse<User>>> {
    const params = new URLSearchParams()
    
    if (options.page) params.append('page', options.page.toString())
    if (options.pageSize) params.append('pageSize', options.pageSize.toString())
    if (options.search) params.append('search', options.search)
    if (options.role) params.append('role', options.role)
    if (options.status) params.append('status', options.status)
    
    if (options.sort) {
      params.append('sortBy', options.sort.field)
      params.append('sortDirection', options.sort.direction)
    }
    
    if (options.filters) {
      options.filters.forEach((filter, index) => {
        params.append(`filters[${index}].field`, filter.field)
        params.append(`filters[${index}].operator`, filter.operator)
        params.append(`filters[${index}].value`, String(filter.value))
      })
    }

    return apiClient.get<PaginatedResponse<User>>(
      `${this.basePath}?${params.toString()}`, {}, 'security'
    )
  }

  /**
   * Get single user by ID
   */
  async getUser(id: string): Promise<ApiResponse<User>> {
    return apiClient.get<User>(`${this.basePath}/${id}`, {}, 'security')
  }

  /**
   * Create new user
   */
  async createUser(userData: {
    username: string
    email: string
    fullName: string
    password: string
    role: UserRole
    hierarchyNodeIds?: string[]
    status?: 'active' | 'inactive'
  }): Promise<ApiResponse<User>> {
    return apiClient.post<User>(this.basePath, userData, {}, 'security')
  }

  /**
   * Update existing user
   */
  async updateUser(
    id: string,
    userData: Partial<{
      username: string
      email: string
      fullName: string
      role: UserRole
      status: 'active' | 'inactive' | 'locked'
      hierarchyNodeIds: string[]
    }>
  ): Promise<ApiResponse<User>> {
    return apiClient.put<User>(`${this.basePath}/${id}`, userData, {}, 'security')
  }

  /**
   * Delete user (soft delete)
   */
  async deleteUser(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`${this.basePath}/${id}`, {}, 'security')
  }

  /**
   * Reset user password
   */
  async resetPassword(id: string, newPassword?: string): Promise<ApiResponse<{ temporaryPassword?: string }>> {
    return apiClient.post<{ temporaryPassword?: string }>(
      `${this.basePath}/${id}/reset-password`,
      { newPassword },
      {},
      'security'
    )
  }

  /**
   * Lock/unlock user account
   */
  async setUserStatus(id: string, status: 'active' | 'inactive' | 'locked'): Promise<ApiResponse<User>> {
    return apiClient.patch<User>(`${this.basePath}/${id}/status`, { status }, {}, 'security')
  }

  /**
   * Assign user to hierarchy nodes
   */
  async assignHierarchy(id: string, hierarchyNodeIds: string[]): Promise<ApiResponse<User>> {
    return apiClient.put<User>(
      `${this.basePath}/${id}/hierarchy`,
      { hierarchyNodeIds },
      {},
      'security'
    )
  }

  /**
   * Remove user from hierarchy nodes
   */
  async removeHierarchyAssignment(id: string, hierarchyNodeId: string): Promise<ApiResponse<User>> {
    return apiClient.delete<User>(
      `${this.basePath}/${id}/hierarchy/${hierarchyNodeId}`,
      {},
      'security'
    )
  }

  /**
   * Get available roles
   */
  async getRoles(): Promise<ApiResponse<Array<{ value: UserRole; label: string; description: string }>>> {
    return apiClient.get<Array<{ value: UserRole; label: string; description: string }>>(
      `${this.basePath}/roles`,
      {},
      'security'
    )
  }

  /**
   * Get user activity log
   */
  async getUserActivity(
    id: string,
    options: {
      page?: number
      pageSize?: number
      startDate?: Date
      endDate?: Date
    } = {}
  ): Promise<ApiResponse<PaginatedResponse<{
    id: string
    userId: string
    action: string
    details: string
    ipAddress: string
    userAgent: string
    timestamp: Date
  }>>> {
    const params = new URLSearchParams()
    
    if (options.page) params.append('page', options.page.toString())
    if (options.pageSize) params.append('pageSize', options.pageSize.toString())
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())

    return apiClient.get<PaginatedResponse<any>>(
      `${this.basePath}/${id}/activity?${params.toString()}`,
      {},
      'security'
    )
  }

  /**
   * Bulk operations
   */
  async bulkUpdateUsers(
    userIds: string[],
    updates: {
      role?: UserRole
      status?: 'active' | 'inactive' | 'locked'
      hierarchyNodeIds?: string[]
    }
  ): Promise<ApiResponse<{ updated: number; failed: Array<{ id: string; error: string }> }>> {
    return apiClient.post<any>(`${this.basePath}/bulk-update`, {
      userIds,
      updates
    }, {}, 'security')
  }

  /**
   * Export users data
   */
  async exportUsers(options: {
    format: 'csv' | 'xlsx'
    filters?: FilterConfig[]
    includeInactive?: boolean
  }): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.post<{ downloadUrl: string }>(`${this.basePath}/export`, options, {}, 'security')
  }

  /**
   * Import users from file
   */
  async importUsers(file: File, options: {
    updateExisting?: boolean
    defaultRole?: UserRole
    defaultStatus?: 'active' | 'inactive'
  }): Promise<ApiResponse<{
    imported: number
    updated: number
    failed: Array<{ row: number; error: string }>
  }>> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('options', JSON.stringify(options))

    return apiClient.post<any>(`${this.basePath}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    }, 'security')
  }

  /**
   * Validate username availability
   */
  async validateUsername(username: string, excludeUserId?: string): Promise<ApiResponse<{ available: boolean }>> {
    const params = new URLSearchParams()
    params.append('username', username)
    if (excludeUserId) params.append('excludeUserId', excludeUserId)

    return apiClient.get<{ available: boolean }>(
      `${this.basePath}/validate-username?${params.toString()}`,
      {},
      'security'
    )
  }

  /**
   * Validate email availability
   */
  async validateEmail(email: string, excludeUserId?: string): Promise<ApiResponse<{ available: boolean }>> {
    const params = new URLSearchParams()
    params.append('email', email)
    if (excludeUserId) params.append('excludeUserId', excludeUserId)

    return apiClient.get<{ available: boolean }>(
      `${this.basePath}/validate-email?${params.toString()}`,
      {},
      'security'
    )
  }

  /**
   * Get user statistics
   */
  async getUserStats(): Promise<ApiResponse<{
    total: number
    active: number
    inactive: number
    locked: number
    byRole: Record<UserRole, number>
    recentActivity: number
  }>> {
    return apiClient.get<any>(`${this.basePath}/stats`, {}, 'security')
  }

  // All methods now use apiClient directly with 'security' service
}

// Export singleton instance
export const userService = new UserService()

// Export the class for testing
export { UserService }