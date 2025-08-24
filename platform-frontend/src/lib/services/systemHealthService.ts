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
  PaginatedResponse 
} from '@/types'

/**
 * System Health Service - API client for system health monitoring
 * Integrates with the backend admin health endpoints for real-time system monitoring
 */
class SystemHealthService {
  private readonly baseUrl = '/api'
  private readonly alertsUrl = '/api/admin/alerts'

  /**
   * Get status of all microservices
   */
  async getServiceStatuses(): Promise<ApiResponse<ServiceStatus[]>> {
    try {
      // Test Logger API health
      const loggerHealthRes = await apiClient.get('/health/checks', {}, 'logger')
      
      // Mock service status data based on actual health checks
      const services: ServiceStatus[] = [
        {
          serviceName: 'Logger API',
          status: loggerHealthRes.success ? 'healthy' : 'error',
          uptime: Date.now() - 3600000, // 1 hour ago
          lastCheck: new Date(),
          responseTime: 45,
          version: '1.0.0',
          endpoint: 'http://localhost:5139',
          details: {
            database: 'TimescaleDB connected',
            devices: '3 devices connected',
            memoryUsage: '156MB'
          }
        },
        {
          serviceName: 'OEE API',
          status: 'healthy',
          uptime: Date.now() - 7200000, // 2 hours ago
          lastCheck: new Date(),
          responseTime: 32,
          version: '1.0.0',
          endpoint: 'http://localhost:5001',
          details: {
            calculations: 'Running',
            schedules: '15 active schedules',
            memoryUsage: '203MB'
          }
        },
        {
          serviceName: 'Equipment Scheduling API',
          status: 'warning',
          uptime: Date.now() - 1800000, // 30 minutes ago
          lastCheck: new Date(),
          responseTime: 89,
          version: '1.0.0',
          endpoint: 'http://localhost:5141',
          details: {
            status: 'High CPU usage detected',
            schedules: '23 scheduled jobs',
            memoryUsage: '445MB'
          }
        }
      ]
      
      // Validate the response data
      const response = { success: true, data: services }
      return processApiResponse(response, (data) => isArrayOf(data, isServiceStatus), [])
    } catch (error) {
      return { success: false, error: error as ApiError }
    }
  }

  /**
   * Get specific service status by name
   */
  async getServiceStatus(serviceName: string): Promise<ApiResponse<ServiceStatus>> {
    return apiClient.get<ServiceStatus>(`${this.baseUrl}/health/detailed`, {}, 'logger')
  }

  /**
   * Get current system metrics (CPU, RAM, disk, network)
   */
  async getSystemMetrics(): Promise<ApiResponse<SystemMetrics>> {
    try {
      // Mock system metrics - in a real implementation this would come from actual system monitoring
      const metrics: SystemMetrics = {
        timestamp: new Date(),
        cpu: {
          usage: Math.random() * 30 + 15, // 15-45%
          cores: 8,
          temperature: Math.random() * 10 + 55 // 55-65°C
        },
        memory: {
          used: Math.random() * 2048 + 2048, // 2-4GB
          total: 8192, // 8GB
          percentage: 0
        },
        disk: {
          used: Math.random() * 200 + 100, // 100-300GB
          total: 1000, // 1TB
          percentage: 0,
          readSpeed: Math.random() * 50 + 100,
          writeSpeed: Math.random() * 30 + 80
        },
        network: {
          inbound: Math.random() * 10 + 5, // 5-15 Mbps
          outbound: Math.random() * 5 + 2, // 2-7 Mbps
          latency: Math.random() * 10 + 5 // 5-15ms
        }
      }
      
      // Calculate percentages
      metrics.memory.percentage = (metrics.memory.used / metrics.memory.total) * 100
      metrics.disk.percentage = (metrics.disk.used / metrics.disk.total) * 100
      
      // Validate the response data
      const response = { success: true, data: metrics }
      return processApiResponse(response, isSystemMetrics)
    } catch (error) {
      return { success: false, error: error as ApiError }
    }
  }

  /**
   * Get historical system metrics for time range
   */
  async getMetricsHistory(
    timeRange: '1h' | '6h' | '24h' | '7d' = '24h'
  ): Promise<ApiResponse<SystemMetrics[]>> {
    // Mock historical data for now - return current metrics as array
    const currentMetrics = await this.getSystemMetrics()
    if (currentMetrics.success && currentMetrics.data) {
      return Promise.resolve({
        success: true,
        data: [currentMetrics.data]
      })
    }
    return Promise.resolve({ success: true, data: [] })
  }

  /**
   * Get TimescaleDB health and performance metrics
   */
  async getDatabaseHealth(): Promise<ApiResponse<DatabaseHealth>> {
    try {
      // Test actual database connection through Logger API
      const healthRes = await apiClient.get('/health/checks', {}, 'logger')
      
      // Mock database health data
      const dbHealth: DatabaseHealth = {
        connected: healthRes.success,
        connectionString: 'TimescaleDB on localhost:5433',
        responseTime: Math.random() * 20 + 10, // 10-30ms
        activeConnections: Math.floor(Math.random() * 5) + 3, // 3-8 connections
        maxConnections: 100,
        database: 'adam_counters',
        version: '14.9',
        uptime: Date.now() - 86400000, // 24 hours ago
        stats: {
          totalQueries: Math.floor(Math.random() * 1000000) + 500000,
          queriesPerSecond: Math.random() * 50 + 25,
          dataSize: '2.3GB',
          indexSize: '456MB'
        }
      }
      
      // Validate the response data
      const response = { success: true, data: dbHealth }
      return processApiResponse(response, isDatabaseHealth)
    } catch (error) {
      return { success: false, error: error as ApiError }
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
   * Get active system alerts with pagination and filtering
   */
  async getAlerts(params?: {
    page?: number
    pageSize?: number
    severity?: 'low' | 'medium' | 'high' | 'critical'
    type?: 'error' | 'warning' | 'info'
    acknowledged?: boolean
    resolved?: boolean
  }): Promise<ApiResponse<PaginatedResponse<SystemAlert>>> {
    // Mock alerts for now since backend doesn't have alert system yet
    return Promise.resolve({
      success: true,
      data: {
        data: [],
        total: 0,
        page: params?.page || 1,
        pageSize: params?.pageSize || 10
      }
    })
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
    // Mock acknowledgment for now
    return Promise.resolve({ success: true, data: undefined })
  }

  /**
   * Resolve an alert
   */
  async resolveAlert(alertId: string, resolution?: string): Promise<ApiResponse<void>> {
    // Mock resolution for now
    return Promise.resolve({ success: true, data: undefined })
  }

  /**
   * Delete an alert (admin only)
   */
  async deleteAlert(alertId: string): Promise<ApiResponse<void>> {
    return apiClient.delete(`${this.alertsUrl}/${alertId}`)
  }

  /**
   * Get health timeline events for visualization
   */
  async getHealthTimeline(
    timeRange: '1h' | '6h' | '24h' | '7d' = '24h'
  ): Promise<ApiResponse<HealthTimelineEvent[]>> {
    // Mock timeline events for now
    return Promise.resolve({ success: true, data: [] })
  }

  /**
   * Create a test alert (for testing purposes)
   */
  async createTestAlert(severity: 'low' | 'medium' | 'high' | 'critical' = 'medium'): Promise<ApiResponse<SystemAlert>> {
    // Mock test alert creation
    const testAlert: SystemAlert = {
      id: Math.random().toString(36).substr(2, 9),
      title: 'Test Alert',
      description: 'This is a test alert for development purposes',
      severity: severity,
      status: 'active',
      source: 'test',
      timestamp: new Date(),
      acknowledged: false,
      resolved: false
    }
    
    return Promise.resolve({ success: true, data: testAlert })
  }

  /**
   * Get system health statistics summary
   */
  async getHealthStats(): Promise<ApiResponse<{
    uptime: number
    totalServices: number
    healthyServices: number
    warningServices: number
    errorServices: number
    totalAlerts: number
    criticalAlerts: number
    avgResponseTime: number
  }>> {
    // Mock stats for now
    return Promise.resolve({
      success: true,
      data: {
        uptime: Date.now() - 86400000, // 24 hours ago
        totalServices: 3,
        healthyServices: 3,
        warningServices: 0,
        errorServices: 0,
        totalAlerts: 0,
        criticalAlerts: 0,
        avgResponseTime: 150
      }
    })
  }

  /**
   * Export health data for analysis
   */
  async exportHealthData(format: 'csv' | 'json' = 'json', timeRange: '1h' | '6h' | '24h' | '7d' = '24h'): Promise<ApiResponse<Blob>> {
    // Mock export for now
    const data = JSON.stringify({ message: 'Export not yet implemented' })
    return Promise.resolve({ success: true, data: new Blob([data], { type: 'application/json' }) })
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
   * Get system resource utilization predictions
   */
  async getResourcePredictions(): Promise<ApiResponse<{
    cpu: { next1h: number; next6h: number; next24h: number }
    memory: { next1h: number; next6h: number; next24h: number }
    disk: { next1h: number; next6h: number; next24h: number }
  }>> {
    // Mock predictions for now
    return Promise.resolve({
      success: true,
      data: {
        cpu: { next1h: 45, next6h: 50, next24h: 55 },
        memory: { next1h: 60, next6h: 65, next24h: 70 },
        disk: { next1h: 25, next6h: 25, next24h: 26 }
      }
    })
  }

  /**
   * Get maintenance windows affecting system health
   */
  async getMaintenanceWindows(): Promise<ApiResponse<{
    id: string
    title: string
    description: string
    startTime: Date
    endTime: Date
    affectedServices: string[]
    status: 'scheduled' | 'active' | 'completed' | 'cancelled'
  }[]>> {
    // Mock maintenance windows for now
    return Promise.resolve({ success: true, data: [] })
  }

  /**
   * Test connectivity to all external dependencies
   */
  async testConnectivity(): Promise<ApiResponse<{
    database: { connected: boolean; responseTime: number }
    externalApis: { name: string; connected: boolean; responseTime: number }[]
    networkDrives: { name: string; accessible: boolean }[]
    timestamp: Date
  }>> {
    // Test actual database connectivity
    const dbHealth = await this.getDatabaseHealth()
    
    return Promise.resolve({
      success: true,
      data: {
        database: { connected: dbHealth.success, responseTime: 50 },
        externalApis: [],
        networkDrives: [],
        timestamp: new Date()
      }
    })
  }
}

// Export singleton instance
export const systemHealthService = new SystemHealthService()

// Export the class for testing
export { SystemHealthService }