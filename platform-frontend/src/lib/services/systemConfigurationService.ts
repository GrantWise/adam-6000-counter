import { apiClient } from '@/lib/api/client'
import type { ApiResponse } from '@/types'

/**
 * System Configuration Service for Phase 2 admin features
 * Handles system settings, feature flags, and maintenance configuration
 */
class SystemConfigurationService {
  private readonly baseUrl = '/api/admin/config'
  private readonly maintenanceUrl = '/api/admin/maintenance'

  /**
   * Configuration setting data structure
   */
  interface ConfigurationSetting {
    key: string
    value: any
    category: string
    description: string
    dataType: 'string' | 'number' | 'boolean' | 'object' | 'array'
    isEncrypted: boolean
    isRequired: boolean
    validationRules?: string
    updatedBy?: string
    updatedAt?: Date
  }

  interface FeatureFlag {
    name: string
    enabled: boolean
    description: string
    conditions?: {
      userRoles?: string[]
      hierarchyNodes?: string[]
      startDate?: Date
      endDate?: Date
      percentage?: number
    }
    updatedBy?: string
    updatedAt?: Date
  }

  interface MaintenanceWindow {
    id: string
    title: string
    description: string
    scheduledStart: Date
    scheduledEnd: Date
    actualStart?: Date
    actualEnd?: Date
    status: 'scheduled' | 'active' | 'completed' | 'cancelled'
    affectedServices: string[]
    notificationsSent: boolean
    createdBy: string
    createdAt: Date
  }

  interface NotificationSettings {
    email: {
      enabled: boolean
      smtpServer: string
      smtpPort: number
      useSSL: boolean
      username: string
      password?: string
      fromAddress: string
      fromName: string
    }
    webhook: {
      enabled: boolean
      endpoints: Array<{
        name: string
        url: string
        events: string[]
        headers?: Record<string, string>
        authToken?: string
      }>
    }
    inApp: {
      enabled: boolean
      retentionDays: number
    }
  }

  /**
   * Get all configuration settings
   */
  async getConfiguration(category?: string): Promise<ApiResponse<{
    settings: ConfigurationSetting[]
    categories: string[]
  }>> {
    return apiClient.get(this.baseUrl, {
      params: category ? { category } : undefined
    })
  }

  /**
   * Update configuration settings
   */
  async updateConfiguration(settings: Array<{
    key: string
    value: any
  }>): Promise<ApiResponse<{
    updated: string[]
    failed: Array<{ key: string; error: string }>
  }>> {
    return apiClient.put(this.baseUrl, { settings })
  }

  /**
   * Get single configuration setting
   */
  async getConfigurationSetting(key: string): Promise<ApiResponse<ConfigurationSetting>> {
    return apiClient.get(`${this.baseUrl}/${encodeURIComponent(key)}`)
  }

  /**
   * Update single configuration setting
   */
  async updateConfigurationSetting(key: string, value: any): Promise<ApiResponse<ConfigurationSetting>> {
    return apiClient.put(`${this.baseUrl}/${encodeURIComponent(key)}`, { value })
  }

  /**
   * Reset configuration setting to default
   */
  async resetConfigurationSetting(key: string): Promise<ApiResponse<ConfigurationSetting>> {
    return apiClient.post(`${this.baseUrl}/${encodeURIComponent(key)}/reset`)
  }

  /**
   * Get all feature flags
   */
  async getFeatureFlags(): Promise<ApiResponse<{
    flags: FeatureFlag[]
    categories: string[]
  }>> {
    return apiClient.get(`${this.baseUrl}/features`)
  }

  /**
   * Update feature flags
   */
  async updateFeatureFlags(flags: Array<{
    name: string
    enabled: boolean
    conditions?: FeatureFlag['conditions']
  }>): Promise<ApiResponse<{
    updated: string[]
    failed: Array<{ name: string; error: string }>
  }>> {
    return apiClient.put(`${this.baseUrl}/features`, { flags })
  }

  /**
   * Toggle single feature flag
   */
  async toggleFeatureFlag(name: string, enabled: boolean, conditions?: FeatureFlag['conditions']): Promise<ApiResponse<FeatureFlag>> {
    return apiClient.put(`${this.baseUrl}/features/${encodeURIComponent(name)}`, {
      enabled,
      conditions
    })
  }

  /**
   * Get feature flag usage analytics
   */
  async getFeatureFlagAnalytics(timeRange: '24h' | '7d' | '30d' = '7d'): Promise<ApiResponse<{
    flagUsage: Array<{
      flagName: string
      enabled: boolean
      totalChecks: number
      uniqueUsers: number
      enabledFor: number
      disabledFor: number
    }>
    topFlags: Array<{ name: string; checks: number }>
    usageByTime: Array<{ timestamp: Date; checks: number }>
  }>> {
    return apiClient.get(`${this.baseUrl}/features/analytics`, {
      params: { timeRange }
    })
  }

  /**
   * Schedule maintenance window
   */
  async scheduleMaintenanceWindow(maintenance: {
    title: string
    description: string
    scheduledStart: Date
    scheduledEnd: Date
    affectedServices: string[]
    notifyUsers?: boolean
    notifyExternalSystems?: boolean
  }): Promise<ApiResponse<MaintenanceWindow>> {
    return apiClient.post(`${this.maintenanceUrl}/schedule`, maintenance)
  }

  /**
   * Get maintenance windows
   */
  async getMaintenanceWindows(params?: {
    status?: MaintenanceWindow['status']
    startDate?: Date
    endDate?: Date
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<{
    windows: MaintenanceWindow[]
    total: number
    upcoming: number
    active: number
    completed: number
  }>> {
    const queryParams: any = {}
    
    if (params) {
      if (params.status) queryParams.status = params.status
      if (params.startDate) queryParams.startDate = params.startDate.toISOString()
      if (params.endDate) queryParams.endDate = params.endDate.toISOString()
      if (params.page) queryParams.page = params.page
      if (params.pageSize) queryParams.pageSize = params.pageSize
    }

    return apiClient.get(`${this.maintenanceUrl}/windows`, {
      params: queryParams
    })
  }

  /**
   * Update maintenance window
   */
  async updateMaintenanceWindow(id: string, updates: Partial<{
    title: string
    description: string
    scheduledStart: Date
    scheduledEnd: Date
    affectedServices: string[]
    status: MaintenanceWindow['status']
  }>): Promise<ApiResponse<MaintenanceWindow>> {
    return apiClient.put(`${this.maintenanceUrl}/windows/${id}`, updates)
  }

  /**
   * Cancel maintenance window
   */
  async cancelMaintenanceWindow(id: string, reason: string): Promise<ApiResponse<MaintenanceWindow>> {
    return apiClient.post(`${this.maintenanceUrl}/windows/${id}/cancel`, { reason })
  }

  /**
   * Start maintenance window early
   */
  async startMaintenanceWindow(id: string): Promise<ApiResponse<MaintenanceWindow>> {
    return apiClient.post(`${this.maintenanceUrl}/windows/${id}/start`)
  }

  /**
   * End maintenance window early
   */
  async endMaintenanceWindow(id: string): Promise<ApiResponse<MaintenanceWindow>> {
    return apiClient.post(`${this.maintenanceUrl}/windows/${id}/end`)
  }

  /**
   * Get notification settings
   */
  async getNotificationSettings(): Promise<ApiResponse<NotificationSettings>> {
    return apiClient.get(`${this.baseUrl}/notifications`)
  }

  /**
   * Update notification settings
   */
  async updateNotificationSettings(settings: Partial<NotificationSettings>): Promise<ApiResponse<NotificationSettings>> {
    return apiClient.put(`${this.baseUrl}/notifications`, settings)
  }

  /**
   * Test email notification configuration
   */
  async testEmailNotification(testEmail: string): Promise<ApiResponse<{
    success: boolean
    message: string
    testId: string
  }>> {
    return apiClient.post(`${this.baseUrl}/notifications/email/test`, {
      testEmail
    })
  }

  /**
   * Test webhook notification
   */
  async testWebhook(webhookName: string, testPayload?: any): Promise<ApiResponse<{
    success: boolean
    responseCode: number
    responseBody: string
    responseTime: number
    testId: string
  }>> {
    return apiClient.post(`${this.baseUrl}/notifications/webhook/test`, {
      webhookName,
      testPayload: testPayload || { test: true, timestamp: new Date().toISOString() }
    })
  }

  /**
   * Get system status for configuration
   */
  async getSystemStatus(): Promise<ApiResponse<{
    maintenanceMode: boolean
    readOnlyMode: boolean
    version: string
    uptime: number
    lastConfigUpdate: Date
    pendingRestarts: string[]
    requiresRestart: boolean
  }>> {
    return apiClient.get(`${this.baseUrl}/system-status`)
  }

  /**
   * Enable/disable maintenance mode
   */
  async setMaintenanceMode(enabled: boolean, message?: string): Promise<ApiResponse<{
    maintenanceMode: boolean
    message?: string
    enabledBy: string
    enabledAt: Date
  }>> {
    return apiClient.post(`${this.baseUrl}/maintenance-mode`, {
      enabled,
      message
    })
  }

  /**
   * Restart system services that require restart after configuration changes
   */
  async restartServices(services?: string[]): Promise<ApiResponse<{
    restartInitiated: boolean
    affectedServices: string[]
    estimatedDowntime: number
    restartId: string
  }>> {
    return apiClient.post(`${this.baseUrl}/restart-services`, {
      services
    })
  }

  /**
   * Export configuration settings
   */
  async exportConfiguration(options?: {
    categories?: string[]
    includeEncrypted?: boolean
    format?: 'json' | 'yaml'
  }): Promise<ApiResponse<Blob>> {
    return apiClient.post(`${this.baseUrl}/export`, {
      categories: options?.categories,
      includeEncrypted: options?.includeEncrypted ?? false,
      format: options?.format ?? 'json'
    }, {
      responseType: 'blob'
    })
  }

  /**
   * Import configuration settings
   */
  async importConfiguration(file: File, options?: {
    overwriteExisting?: boolean
    validateOnly?: boolean
    skipEncrypted?: boolean
  }): Promise<ApiResponse<{
    imported: number
    skipped: number
    errors: Array<{ key: string; error: string }>
    warnings: Array<{ key: string; warning: string }>
    requiresRestart: boolean
  }>> {
    const formData = new FormData()
    formData.append('file', file)
    if (options) {
      formData.append('options', JSON.stringify(options))
    }

    return apiClient.post(`${this.baseUrl}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  }

  /**
   * Get configuration change history
   */
  async getConfigurationHistory(params?: {
    key?: string
    category?: string
    changedBy?: string
    startDate?: Date
    endDate?: Date
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<{
    changes: Array<{
      id: string
      key: string
      category: string
      oldValue: any
      newValue: any
      changedBy: string
      changedAt: Date
      reason?: string
    }>
    total: number
  }>> {
    const queryParams: any = {}
    
    if (params) {
      if (params.key) queryParams.key = params.key
      if (params.category) queryParams.category = params.category
      if (params.changedBy) queryParams.changedBy = params.changedBy
      if (params.startDate) queryParams.startDate = params.startDate.toISOString()
      if (params.endDate) queryParams.endDate = params.endDate.toISOString()
      if (params.page) queryParams.page = params.page
      if (params.pageSize) queryParams.pageSize = params.pageSize
    }

    return apiClient.get(`${this.baseUrl}/history`, {
      params: queryParams
    })
  }

  /**
   * Validate configuration settings
   */
  async validateConfiguration(settings?: Array<{ key: string; value: any }>): Promise<ApiResponse<{
    valid: boolean
    errors: Array<{ key: string; error: string }>
    warnings: Array<{ key: string; warning: string }>
    affectedFeatures: string[]
    requiresRestart: boolean
  }>> {
    return apiClient.post(`${this.baseUrl}/validate`, {
      settings
    })
  }
}

// Export singleton instance
export const systemConfigurationService = new SystemConfigurationService()

// Export the class for testing
export { SystemConfigurationService }