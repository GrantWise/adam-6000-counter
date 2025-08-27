import { apiClient } from '@/lib/api/client'
import { 
  processApiResponse, 
  isServiceStatus, 
  isSystemMetrics, 
  isDatabaseHealth, 
  isSystemAlert,
  isArrayOf 
} from '@/lib/utils/validation'
import type { 
  ApiResponse, 
  ServiceStatus, 
  SystemMetrics, 
  SystemAlert, 
  DatabaseHealth,
  HealthTimelineEvent,
  PaginatedResponse,
  DataQuality,
  DataWithQuality
} from '@/types'

/**
 * System Health Service - API client for system health monitoring
 * Integrates with the backend admin health endpoints for real-time system monitoring
 */
class SystemHealthService {
  private readonly baseUrl = '/api'
  private readonly alertsUrl = '/api/admin/alerts'

  /**
   * Get status of all microservices - Uses real health endpoints
   */
  async getServiceStatuses(): Promise<ApiResponse<ServiceStatus[]>> {
    try {
      // Get health status from all services in parallel
      const [loggerHealth, oeeHealth, securityHealth, schedulingHealth] = await Promise.allSettled([
        apiClient.get('/health', {}, 'logger'),
        apiClient.get('/health', {}, 'oee'),
        apiClient.get('/health', {}, 'security'),
        apiClient.get('/health', {}, 'scheduling')
      ])

      const services: ServiceStatus[] = [
        {
          serviceName: 'Logger API',
          status: loggerHealth.status === 'fulfilled' && loggerHealth.value.success ? 'healthy' : 'error',
          uptime: loggerHealth.status === 'fulfilled' && loggerHealth.value.data?.uptime ? loggerHealth.value.data.uptime : 0,
          lastCheck: new Date(),
          responseTimeMs: loggerHealth.status === 'fulfilled' ? loggerHealth.value.data?.responseTime || 0 : 0,
          errorCount: 0,
          version: loggerHealth.status === 'fulfilled' ? loggerHealth.value.data?.version || '1.0.0' : 'Unknown',
          endpoint: 'http://localhost:5139',
          details: loggerHealth.status === 'fulfilled' && loggerHealth.value.data ? loggerHealth.value.data.details : { error: 'Health check failed' }
        },
        {
          serviceName: 'OEE API',
          status: oeeHealth.status === 'fulfilled' && oeeHealth.value.success ? 'healthy' : 'error',
          uptime: oeeHealth.status === 'fulfilled' && oeeHealth.value.data?.uptime ? oeeHealth.value.data.uptime : 0,
          lastCheck: new Date(),
          responseTimeMs: oeeHealth.status === 'fulfilled' ? oeeHealth.value.data?.responseTime || 0 : 0,
          errorCount: 0,
          version: oeeHealth.status === 'fulfilled' ? oeeHealth.value.data?.version || '1.0.0' : 'Unknown',
          endpoint: 'http://localhost:5140',
          details: oeeHealth.status === 'fulfilled' && oeeHealth.value.data ? oeeHealth.value.data.details : { error: 'Health check failed' }
        },
        {
          serviceName: 'Security API',
          status: securityHealth.status === 'fulfilled' && securityHealth.value.success ? 'healthy' : 'error',
          uptime: securityHealth.status === 'fulfilled' && securityHealth.value.data?.uptime ? securityHealth.value.data.uptime : 0,
          lastCheck: new Date(),
          responseTimeMs: securityHealth.status === 'fulfilled' ? securityHealth.value.data?.responseTime || 0 : 0,
          errorCount: 0,
          version: securityHealth.status === 'fulfilled' ? securityHealth.value.data?.version || '1.0.0' : 'Unknown',
          endpoint: 'http://localhost:5139',
          details: securityHealth.status === 'fulfilled' && securityHealth.value.data ? securityHealth.value.data.details : { error: 'Health check failed' }
        },
        {
          serviceName: 'Equipment Scheduling API',
          status: schedulingHealth.status === 'fulfilled' && schedulingHealth.value.success ? 'healthy' : 'error',
          uptime: schedulingHealth.status === 'fulfilled' && schedulingHealth.value.data?.uptime ? schedulingHealth.value.data.uptime : 0,
          lastCheck: new Date(),
          responseTimeMs: schedulingHealth.status === 'fulfilled' ? schedulingHealth.value.data?.responseTime || 0 : 0,
          errorCount: 0,
          version: schedulingHealth.status === 'fulfilled' ? schedulingHealth.value.data?.version || '1.0.0' : 'Unknown',
          endpoint: 'http://localhost:5141',
          details: schedulingHealth.status === 'fulfilled' && schedulingHealth.value.data ? schedulingHealth.value.data.details : { error: 'Health check failed' }
        }
      ]
      
      return { success: true, data: services }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'HEALTH_CHECK_FAILED',
          message: 'Failed to retrieve service health status',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get specific service status by name
   */
  async getServiceStatus(serviceName: string): Promise<ApiResponse<ServiceStatus>> {
    const serviceMap: Record<string, 'logger' | 'oee' | 'security' | 'scheduling'> = {
      'Logger API': 'logger',
      'OEE API': 'oee',
      'Security API': 'security', 
      'Equipment Scheduling API': 'scheduling'
    }
    
    const service = serviceMap[serviceName]
    if (!service) {
      return {
        success: false,
        error: {
          code: 'SERVICE_NOT_FOUND',
          message: `Service ${serviceName} not found`,
          timestamp: new Date(),
          retryable: false
        }
      }
    }
    
    return apiClient.get<ServiceStatus>('/health', {}, service)
  }

  /**
   * Get current system metrics from Admin Dashboard API - NO SYNTHETIC DATA
   */
  async getSystemMetrics(): Promise<ApiResponse<DataWithQuality<SystemMetrics>>> {
    try {
      // Try to get real metrics from Admin Dashboard API
      const response = await apiClient.get<SystemMetrics>('/api/admin/system/metrics', {}, 'security')
      
      if (response.success && response.data) {
        return {
          success: true,
          data: {
            value: response.data,
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        // Return unavailable data instead of synthetic
        return {
          success: true,
          data: {
            value: null,
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'System metrics unavailable - Admin Dashboard API not responding',
            auditInfo: {
              sourceSystem: 'Admin Dashboard API',
              dataIntegrity: 'synthetic',
              complianceFlags: ['CFR21Part11', 'DATA_UNAVAILABLE']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'METRICS_UNAVAILABLE',
          message: 'System metrics data not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get historical system metrics for time range - NO SYNTHETIC DATA
   */
  async getMetricsHistory(
    timeRange: '1h' | '6h' | '24h' | '7d' = '24h'
  ): Promise<ApiResponse<DataWithQuality<SystemMetrics[]>>> {
    try {
      const response = await apiClient.get<SystemMetrics[]>(`/api/admin/system/metrics/history?timeRange=${timeRange}`, {}, 'security')
      
      if (response.success && response.data) {
        return {
          success: true,
          data: {
            value: response.data,
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        return {
          success: true,
          data: {
            value: [],
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'Historical metrics data not available',
            auditInfo: {
              sourceSystem: 'Admin Dashboard API',
              dataIntegrity: 'synthetic',
              complianceFlags: ['CFR21Part11', 'DATA_UNAVAILABLE']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'METRICS_HISTORY_UNAVAILABLE',
          message: 'Historical metrics data not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get TimescaleDB health from real database endpoint - NO SYNTHETIC DATA
   */
  async getDatabaseHealth(): Promise<ApiResponse<DataWithQuality<DatabaseHealth>>> {
    try {
      // Get real database health from Logger API
      const response = await apiClient.get<DatabaseHealth>('/api/database/health', {}, 'logger')
      
      if (response.success && response.data) {
        return {
          success: true,
          data: {
            value: response.data,
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Logger API Database Health',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        // Check basic connectivity as fallback
        const basicHealthRes = await apiClient.get('/health', {}, 'logger')
        
        return {
          success: true,
          data: {
            value: {
              connected: basicHealthRes.success,
              responseTimeMs: 0,
              connectionCount: 0,
              maxConnections: 0,
              version: 'Unknown',
              diskUsage: 0,
              queryPerformance: {
                slowQueries: 0,
                avgQueryTime: 0
              }
            },
            quality: basicHealthRes.success ? 'uncertain' : 'bad',
            timestamp: new Date(),
            warning: 'Detailed database metrics unavailable - only basic connectivity verified',
            auditInfo: {
              sourceSystem: 'Logger API Basic Health',
              dataIntegrity: 'partial',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'DATABASE_HEALTH_UNAVAILABLE',
          message: 'Database health information not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get system overview - combined health status
   */
  async getSystemOverview(): Promise<ApiResponse<{
    services: ServiceStatus[]
    metrics: SystemMetrics
    database: DatabaseHealth
    alertCount: number
  }>> {
    // Get combined data from available endpoints
    const [servicesRes, metricsRes, databaseRes, alertsRes] = await Promise.allSettled([
      this.getServiceStatuses(),
      this.getSystemMetrics(), 
      this.getDatabaseHealth(),
      this.getAlerts()
    ])
    
    return {
      success: true,
      data: {
        services: servicesRes.status === 'fulfilled' && servicesRes.value.success ? servicesRes.value.data || [] : [],
        metrics: metricsRes.status === 'fulfilled' && metricsRes.value.success ? metricsRes.value.data || {} : {} as SystemMetrics,
        database: databaseRes.status === 'fulfilled' && databaseRes.value.success ? databaseRes.value.data || {} : {} as DatabaseHealth,
        alertCount: alertsRes.status === 'fulfilled' && alertsRes.value.success ? alertsRes.value.data?.total || 0 : 0
      }
    }
  }

  /**
   * Get active system alerts from Admin Dashboard API - NO SYNTHETIC DATA
   */
  async getAlerts(params?: {
    page?: number
    pageSize?: number
    severity?: 'low' | 'medium' | 'high' | 'critical'
    type?: 'error' | 'warning' | 'info'
    acknowledged?: boolean
    resolved?: boolean
  }): Promise<ApiResponse<DataWithQuality<PaginatedResponse<SystemAlert>>>> {
    try {
      const queryParams = new URLSearchParams()
      if (params?.page) queryParams.append('page', params.page.toString())
      if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString())
      if (params?.severity) queryParams.append('severity', params.severity)
      if (params?.type) queryParams.append('type', params.type)
      if (params?.acknowledged !== undefined) queryParams.append('acknowledged', params.acknowledged.toString())
      if (params?.resolved !== undefined) queryParams.append('resolved', params.resolved.toString())
      
      const response = await apiClient.get<PaginatedResponse<SystemAlert>>(`${this.alertsUrl}?${queryParams.toString()}`, {}, 'security')
      
      if (response.success) {
        return {
          success: true,
          data: {
            value: response.data || {
              data: [],
              total: 0,
              page: params?.page || 1,
              pageSize: params?.pageSize || 10
            },
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Alerts API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        return {
          success: true,
          data: {
            value: {
              data: [],
              total: 0,
              page: params?.page || 1,
              pageSize: params?.pageSize || 10
            },
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'Alert system data not available',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Alerts API',
              dataIntegrity: 'unavailable',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'ALERTS_UNAVAILABLE',
          message: 'System alerts data not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get specific alert by ID
   */
  async getAlert(alertId: string): Promise<ApiResponse<SystemAlert>> {
    return apiClient.get<SystemAlert>(`${this.alertsUrl}/${alertId}`)
  }

  /**
   * Acknowledge an alert
   */
  async acknowledgeAlert(alertId: string): Promise<ApiResponse<void>> {
    return apiClient.patch<void>(`${this.alertsUrl}/${alertId}/acknowledge`, {}, {}, 'security')
  }

  /**
   * Resolve an alert
   */
  async resolveAlert(alertId: string, resolution?: string): Promise<ApiResponse<void>> {
    return apiClient.patch<void>(`${this.alertsUrl}/${alertId}/resolve`, { resolution }, {}, 'security')
  }

  /**
   * Delete an alert (admin only)
   */
  async deleteAlert(alertId: string): Promise<ApiResponse<void>> {
    return apiClient.delete(`${this.alertsUrl}/${alertId}`)
  }

  /**
   * Get health timeline events for visualization - NO SYNTHETIC DATA
   */
  async getHealthTimeline(
    timeRange: '1h' | '6h' | '24h' | '7d' = '24h'
  ): Promise<ApiResponse<DataWithQuality<HealthTimelineEvent[]>>> {
    try {
      const response = await apiClient.get<HealthTimelineEvent[]>(
        `/api/admin/system/timeline?timeRange=${timeRange}`,
        {},
        'security'
      )
      
      if (response.success) {
        return {
          success: true,
          data: {
            value: response.data || [],
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Timeline API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        return {
          success: true,
          data: {
            value: [],
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'Health timeline data not available',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Timeline API',
              dataIntegrity: 'unavailable',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'TIMELINE_UNAVAILABLE',
          message: 'Health timeline data not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Create a test alert (for testing purposes) - Uses real API
   */
  async createTestAlert(severity: 'low' | 'medium' | 'high' | 'critical' = 'medium'): Promise<ApiResponse<SystemAlert>> {
    return apiClient.post<SystemAlert>(
      `${this.alertsUrl}/test`,
      { 
        severity,
        title: 'Test Alert',
        description: 'Test alert generated from frontend for development purposes'
      },
      {},
      'security'
    )
  }

  /**
   * Get system health statistics summary from real API - NO SYNTHETIC DATA
   */
  async getHealthStats(): Promise<ApiResponse<DataWithQuality<{
    uptime: number | null
    totalServices: number
    healthyServices: number
    warningServices: number
    errorServices: number
    totalAlerts: number
    criticalAlerts: number
    avgResponseTime: number | null
  }>>> {
    try {
      const response = await apiClient.get<any>('/api/admin/system/stats', {}, 'security')
      
      if (response.success && response.data) {
        return {
          success: true,
          data: {
            value: response.data,
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Stats API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        // Calculate basic stats from service status as fallback
        const servicesResult = await this.getServiceStatuses()
        const services = servicesResult.data || []
        
        const stats = {
          uptime: null,
          totalServices: services.length,
          healthyServices: services.filter(s => s.status === 'healthy').length,
          warningServices: services.filter(s => s.status === 'warning').length,
          errorServices: services.filter(s => s.status === 'error').length,
          totalAlerts: 0, // Cannot determine without alert API
          criticalAlerts: 0, // Cannot determine without alert API
          avgResponseTime: null
        }
        
        return {
          success: true,
          data: {
            value: stats,
            quality: 'uncertain',
            timestamp: new Date(),
            isRealData: false,
            source: 'estimated',
            warning: 'Limited health statistics - calculated from basic service status only',
            auditInfo: {
              sourceSystem: 'Calculated from Service Status',
              dataIntegrity: 'partial',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'HEALTH_STATS_UNAVAILABLE',
          message: 'Health statistics not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Export health data for analysis
   */
  async exportHealthData(format: 'csv' | 'json' = 'json', timeRange: '1h' | '6h' | '24h' | '7d' = '24h'): Promise<ApiResponse<Blob>> {
    try {
      const response = await apiClient.getPublicInstance('security').get(
        `/api/admin/system/export?format=${format}&timeRange=${timeRange}`,
        { responseType: 'blob' }
      )
      
      return { success: true, data: response.data }
    } catch (error) {
      // Return error blob instead of synthetic data
      const errorData = JSON.stringify({ 
        error: 'Health data export not available', 
        timestamp: new Date().toISOString(),
        compliance: 'CFR21Part11 - No synthetic data generated'
      })
      return { 
        success: false, 
        error: { 
          code: 'EXPORT_UNAVAILABLE',
          message: 'Health data export not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError
      }
    }
  }

  /**
   * Perform health check on all services
   */
  async performHealthCheck(): Promise<ApiResponse<{
    services: ServiceStatus[]
    timestamp: Date
    duration: number
  }>> {
    const startTime = Date.now()
    const services = await this.getServiceStatuses()
    const duration = Date.now() - startTime
    
    return {
      success: services.success,
      data: {
        services: services.data || [],
        timestamp: new Date(),
        duration
      }
    }
  }

  /**
   * Get system resource utilization predictions - NO SYNTHETIC DATA
   */
  async getResourcePredictions(): Promise<ApiResponse<DataWithQuality<{
    cpu: { next1h: number | null; next6h: number | null; next24h: number | null } | null
    memory: { next1h: number | null; next6h: number | null; next24h: number | null } | null
    disk: { next1h: number | null; next6h: number | null; next24h: number | null } | null
  }>>> {
    try {
      const response = await apiClient.get<any>('/api/admin/system/predictions', {}, 'security')
      
      if (response.success && response.data) {
        return {
          success: true,
          data: {
            value: response.data,
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Predictions API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        return {
          success: true,
          data: {
            value: {
              cpu: null,
              memory: null,
              disk: null
            },
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'Resource prediction data not available - prediction service not implemented',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Predictions API',
              dataIntegrity: 'unavailable',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'PREDICTIONS_UNAVAILABLE',
          message: 'Resource predictions not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Get maintenance windows affecting system health - NO SYNTHETIC DATA
   */
  async getMaintenanceWindows(): Promise<ApiResponse<DataWithQuality<{
    id: string
    title: string
    description: string
    startTime: Date
    endTime: Date
    affectedServices: string[]
    status: 'scheduled' | 'active' | 'completed' | 'cancelled'
  }[]>>> {
    try {
      const response = await apiClient.get<any>('/api/admin/system/maintenance', {}, 'security')
      
      if (response.success) {
        return {
          success: true,
          data: {
            value: response.data || [],
            quality: 'good',
            timestamp: new Date(),
            isRealData: true,
            source: 'api',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Maintenance API',
              dataIntegrity: 'verified',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      } else {
        return {
          success: true,
          data: {
            value: [],
            quality: 'unavailable',
            timestamp: new Date(),
            isRealData: false,
            source: 'fallback',
            warning: 'Maintenance window data not available',
            auditInfo: {
              sourceSystem: 'Admin Dashboard Maintenance API',
              dataIntegrity: 'unavailable',
              complianceFlags: ['CFR21Part11']
            }
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'MAINTENANCE_UNAVAILABLE',
          message: 'Maintenance window data not available',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }

  /**
   * Test connectivity to all external dependencies - REAL CONNECTIVITY ONLY
   */
  async testConnectivity(): Promise<ApiResponse<DataWithQuality<{
    database: { connected: boolean; responseTime: number | null }
    externalApis: { name: string; connected: boolean; responseTime: number | null }[]
    networkDrives: { name: string; accessible: boolean }[]
    timestamp: Date
  }>>> {
    try {
      // Test real connectivity
      const [dbResult, servicesResult] = await Promise.allSettled([
        this.getDatabaseHealth(),
        this.getServiceStatuses()
      ])
      
      const dbHealth = dbResult.status === 'fulfilled' ? dbResult.value : null
      const services = servicesResult.status === 'fulfilled' ? servicesResult.value.data || [] : []
      
      const connectivityData = {
        database: {
          connected: dbHealth?.success && dbHealth.data?.value?.connected === true,
          responseTime: dbHealth?.data?.value?.responseTime || null
        },
        externalApis: services.map(service => ({
          name: service.serviceName,
          connected: service.status === 'healthy',
          responseTime: service.responseTime || null
        })),
        networkDrives: [], // No network drives in current system
        timestamp: new Date()
      }
      
      return {
        success: true,
        data: {
          value: connectivityData,
          quality: 'good',
          timestamp: new Date(),
          auditInfo: {
            sourceSystem: 'Real Connectivity Tests',
            dataIntegrity: 'verified',
            complianceLevel: 'CFR21Part11'
          }
        }
      }
    } catch (error) {
      return { 
        success: false, 
        error: { 
          code: 'CONNECTIVITY_TEST_FAILED',
          message: 'Connectivity test failed',
          timestamp: new Date(),
          retryable: true
        } as ApiError 
      }
    }
  }
}

// Export singleton instance
export const systemHealthService = new SystemHealthService()

// Export the class for testing
export { SystemHealthService }