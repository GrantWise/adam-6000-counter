import { apiClient } from '@/lib/api/client'
import type { ApiResponse, DeviceConfig, CounterReading, DataQuality, PaginatedResponse } from '@/types'

/**
 * Device Service
 * Handles Logger API integration for ADAM device monitoring and control
 */
class DeviceService {
  private readonly basePath = '/api/logger'

  /**
   * Get all devices - Maps to Logger API /devices endpoint
   */
  async getDevices(): Promise<ApiResponse<DeviceConfig[]>> {
    // Call Logger API and transform response
    const response = await apiClient.get<Record<string, any>>(`${this.basePath}/devices`, undefined, 'logger')
    
    if (response.success && response.data) {
      // Transform Logger API device health format to DeviceConfig format
      const transformedDevices = Object.entries(response.data).map(([deviceId, health]: [string, any]) => ({
        id: deviceId,
        name: health.deviceId || deviceId,
        description: `ADAM Device ${deviceId}`,
        ipAddress: health.ipAddress || 'Unknown',
        port: health.port || 502,
        deviceType: 'ADAM-6051',
        status: health.isConnected ? 'active' : 'inactive',
        lastSeen: health.lastSeen ? new Date(health.lastSeen) : new Date(),
        hierarchyNodeId: undefined,
        channels: [
          {
            id: '0',
            name: 'Counter 1',
            address: 1,
            dataType: 'Counter' as const,
            unit: 'counts',
            scalingFactor: 1,
            enabled: true
          },
          {
            id: '1', 
            name: 'Counter 2',
            address: 2,
            dataType: 'Counter' as const,
            unit: 'counts',
            scalingFactor: 1,
            enabled: true
          }
        ],
        configuration: {
          pollingInterval: 1000,
          timeout: 5000,
          retryCount: 3,
          logging: {
            enabled: true,
            level: 'Info'
          }
        },
        createdAt: new Date(),
        updatedAt: new Date()
      }))
      
      return {
        success: true,
        data: transformedDevices
      }
    }
    
    return response as ApiResponse<DeviceConfig[]>
  }

  /**
   * Get device by ID - Maps to Logger API /devices/{deviceId} endpoint
   */
  async getDevice(id: string): Promise<ApiResponse<DeviceConfig>> {
    const response = await apiClient.get<any>(`${this.basePath}/devices/${id}`, undefined, 'logger')
    
    if (response.success && response.data) {
      const deviceHealth = response.data
      
      // Transform to DeviceConfig format
      const transformedDevice: DeviceConfig = {
        id,
        name: deviceHealth.deviceId || id,
        description: `ADAM Device ${id}`,
        ipAddress: deviceHealth.ipAddress || 'Unknown',
        port: deviceHealth.port || 502,
        deviceType: 'ADAM-6051',
        status: deviceHealth.isConnected ? 'active' : 'inactive',
        lastSeen: deviceHealth.lastSeen ? new Date(deviceHealth.lastSeen) : new Date(),
        hierarchyNodeId: undefined,
        channels: [
          {
            id: '0',
            name: 'Counter 1',
            address: 1,
            dataType: 'Counter' as const,
            unit: 'counts',
            scalingFactor: 1,
            enabled: true
          },
          {
            id: '1', 
            name: 'Counter 2',
            address: 2,
            dataType: 'Counter' as const,
            unit: 'counts',
            scalingFactor: 1,
            enabled: true
          }
        ],
        configuration: {
          pollingInterval: 1000,
          timeout: 5000,
          retryCount: 3,
          logging: {
            enabled: true,
            level: 'Info'
          }
        },
        createdAt: new Date(),
        updatedAt: new Date()
      }
      
      return {
        success: true,
        data: transformedDevice
      }
    }
    
    return response as ApiResponse<DeviceConfig>
  }

  /**
   * Get device summary statistics - Maps to Logger API /data/stats endpoint
   */
  async getDeviceSummary(): Promise<ApiResponse<{
    total: number
    online: number
    offline: number
    error: number
    lastSync: Date
    dataPointsToday: number
    avgResponseTime: number
    topDevicesByActivity: Array<{
      id: string
      name: string
      dataPoints: number
      lastReading: Date
    }>
  }>> {
    const response = await apiClient.get<any>(`${this.basePath}/data/stats`, undefined, 'logger')
    
    if (response.success && response.data) {
      const stats = response.data
      
      // Transform Logger API stats to expected format
      const transformedSummary = {
        total: stats.Summary?.TotalDevices || 0,
        online: stats.Summary?.ConnectedDevices || 0,
        offline: Math.max(0, (stats.Summary?.TotalDevices || 0) - (stats.Summary?.ConnectedDevices || 0)),
        error: 0, // Not available in Logger API stats
        lastSync: stats.Summary?.LastDataUpdate ? new Date(stats.Summary.LastDataUpdate) : new Date(),
        dataPointsToday: stats.Summary?.TotalReadings || 0,
        avgResponseTime: 0, // Not available in current stats
        topDevicesByActivity: stats.DeviceStatistics?.map((deviceStat: any) => ({
          id: deviceStat.DeviceId,
          name: deviceStat.DeviceId,
          dataPoints: deviceStat.ChannelCount || 0,
          lastReading: deviceStat.LastUpdate ? new Date(deviceStat.LastUpdate) : new Date()
        })) || []
      }
      
      return {
        success: true,
        data: transformedSummary
      }
    }
    
    return response
  }

  /**
   * Get real-time device readings - Maps to Logger API /data/latest endpoint
   */
  async getRealTimeReadings(): Promise<ApiResponse<Array<{
    deviceId: string
    deviceName: string
    channels: Array<{
      channelId: string
      channelName: string
      value: number
      rate?: number
      unit: string
      timestamp: Date
      quality: DataQuality
    }>
    status: 'online' | 'offline' | 'error'
    lastUpdate: Date
    responseTime: number
  }>>> {
    const response = await apiClient.get<any>(`${this.basePath}/data/latest`, undefined, 'logger')
    
    if (response.success && response.data) {
      const latestData = response.data
      
      // Group readings by device and transform to expected format
      const readings = latestData.Readings || []
      const deviceGroups = new Map()
      
      readings.forEach((reading: any) => {
        const deviceId = reading.DeviceId
        if (!deviceGroups.has(deviceId)) {
          deviceGroups.set(deviceId, {
            deviceId,
            deviceName: deviceId,
            channels: [],
            status: 'online' as const,
            lastUpdate: new Date(),
            responseTime: 0
          })
        }
        
        const device = deviceGroups.get(deviceId)
        device.channels.push({
          channelId: reading.Channel.toString(),
          channelName: `Channel ${reading.Channel}`,
          value: reading.Value || 0,
          rate: reading.Rate,
          unit: 'counts',
          timestamp: new Date(reading.Timestamp),
          quality: reading.Quality === 'Good' ? 'good' : 
                  reading.Quality === 'Bad' ? 'bad' : 'uncertain'
        })
        
        // Update last update time to most recent reading
        const readingTime = new Date(reading.Timestamp)
        if (readingTime > device.lastUpdate) {
          device.lastUpdate = readingTime
        }
      })
      
      const transformedData = Array.from(deviceGroups.values())
      
      return {
        success: true,
        data: transformedData
      }
    }
    
    return response
  }

  /**
   * Get historical counter readings
   */
  async getCounterReadings(
    deviceId?: string,
    options: {
      channelId?: string
      startDate?: Date
      endDate?: Date
      interval?: 'minute' | 'hour' | 'day'
      limit?: number
      includeRates?: boolean
    } = {}
  ): Promise<ApiResponse<CounterReading[]>> {
    const params = new URLSearchParams()
    if (deviceId) params.append('deviceId', deviceId)
    if (options.channelId) params.append('channelId', options.channelId)
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())
    if (options.interval) params.append('interval', options.interval)
    if (options.limit) params.append('limit', options.limit.toString())
    if (options.includeRates) params.append('includeRates', 'true')

    return apiClient.get<CounterReading[]>(`${this.basePath}/readings?${params.toString()}`)
  }

  /**
   * Get device health metrics - Maps to Logger API /devices/{deviceId} endpoint
   */
  async getDeviceHealth(deviceId: string): Promise<ApiResponse<{
    deviceId: string
    status: 'healthy' | 'warning' | 'error' | 'offline'
    lastSeen: Date
    uptime: number // percentage
    responseTime: {
      current: number
      average: number
      max: number
    }
    errorCount: {
      last24h: number
      lastWeek: number
      lastMonth: number
    }
    dataQuality: {
      good: number // percentage
      uncertain: number
      bad: number
    }
    communicationErrors: Array<{
      timestamp: Date
      error: string
      severity: 'low' | 'medium' | 'high'
    }>
    trends: {
      responseTime: Array<{ timestamp: Date; value: number }>
      errorRate: Array<{ timestamp: Date; count: number }>
    }
  }>> {
    const response = await apiClient.get<any>(`${this.basePath}/devices/${deviceId}`, undefined, 'logger')
    
    if (response.success && response.data) {
      const deviceHealth = response.data
      
      // Transform Logger API device health to expected format
      const transformedHealth = {
        deviceId,
        status: deviceHealth.isConnected ? 'healthy' : 'offline' as const,
        lastSeen: deviceHealth.lastSeen ? new Date(deviceHealth.lastSeen) : new Date(),
        uptime: deviceHealth.successRate || 0, // Use success rate as uptime percentage
        responseTime: {
          current: deviceHealth.averageResponseTime || 0,
          average: deviceHealth.averageResponseTime || 0,
          max: deviceHealth.maxResponseTime || 0
        },
        errorCount: {
          last24h: deviceHealth.errorCount || 0,
          lastWeek: deviceHealth.errorCount || 0,
          lastMonth: deviceHealth.errorCount || 0
        },
        dataQuality: {
          good: 95, // Default values since not available in Logger API
          uncertain: 3,
          bad: 2
        },
        communicationErrors: [],
        trends: {
          responseTime: [],
          errorRate: []
        }
      }
      
      return {
        success: true,
        data: transformedHealth
      }
    }
    
    return response
  }

  /**
   * Get device configuration
   */
  async getDeviceConfiguration(deviceId: string): Promise<ApiResponse<{
    device: DeviceConfig
    modbus: {
      address: number
      baudRate: number
      parity: string
      stopBits: number
      dataBits: number
    }
    channels: Array<{
      id: string
      name: string
      address: number
      dataType: 'Counter' | 'Digital' | 'Analog'
      unit: string
      scalingFactor: number
      enabled: boolean
      alarmConfig?: {
        highLimit?: number
        lowLimit?: number
        enabled: boolean
      }
    }>
    logging: {
      interval: number // seconds
      bufferSize: number
      compression: boolean
      retention: number // days
    }
  }>> {
    return apiClient.get<any>(`${this.basePath}/${deviceId}/configuration`)
  }

  /**
   * Update device configuration
   */
  async updateDeviceConfiguration(
    deviceId: string,
    config: {
      name?: string
      ipAddress?: string
      port?: number
      channels?: Array<{
        id: string
        name?: string
        enabled?: boolean
        scalingFactor?: number
        alarmConfig?: {
          highLimit?: number
          lowLimit?: number
          enabled: boolean
        }
      }>
      logging?: {
        interval?: number
        bufferSize?: number
        compression?: boolean
        retention?: number
      }
    }
  ): Promise<ApiResponse<DeviceConfig>> {
    return apiClient.put<DeviceConfig>(`${this.basePath}/${deviceId}/configuration`, config)
  }

  /**
   * Test device connection
   */
  async testDeviceConnection(deviceId: string): Promise<ApiResponse<{
    success: boolean
    responseTime: number
    error?: string
    diagnostics: {
      tcpConnection: boolean
      modbusResponse: boolean
      dataValidation: boolean
      timestamp: Date
    }
  }>> {
    return apiClient.post<any>(`${this.basePath}/${deviceId}/test`)
  }

  /**
   * Reset device counters
   */
  async resetDeviceCounters(
    deviceId: string,
    channelIds?: string[]
  ): Promise<ApiResponse<{ resetChannels: string[] }>> {
    return apiClient.post<any>(`${this.basePath}/${deviceId}/reset`, { channelIds })
  }

  /**
   * Calibrate device
   */
  async calibrateDevice(
    deviceId: string,
    channelId: string,
    calibrationData: {
      referenceValue: number
      measuredValue: number
      description?: string
    }
  ): Promise<ApiResponse<{
    success: boolean
    oldScalingFactor: number
    newScalingFactor: number
    calibrationId: string
  }>> {
    return apiClient.post<any>(
      `${this.basePath}/${deviceId}/channels/${channelId}/calibrate`,
      calibrationData
    )
  }

  /**
   * Get device alarms
   */
  async getDeviceAlarms(
    deviceId?: string,
    options: {
      active?: boolean
      severity?: 'low' | 'medium' | 'high'
      startDate?: Date
      endDate?: Date
    } = {}
  ): Promise<ApiResponse<Array<{
    id: string
    deviceId: string
    deviceName: string
    channelId: string
    channelName: string
    type: 'high_limit' | 'low_limit' | 'communication_error' | 'data_quality'
    severity: 'low' | 'medium' | 'high'
    message: string
    value?: number
    threshold?: number
    timestamp: Date
    acknowledgedAt?: Date
    acknowledgedBy?: string
    resolved: boolean
    resolvedAt?: Date
  }>>> {
    const params = new URLSearchParams()
    if (deviceId) params.append('deviceId', deviceId)
    if (options.active !== undefined) params.append('active', options.active.toString())
    if (options.severity) params.append('severity', options.severity)
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())

    return apiClient.get<any>(`${this.basePath}/alarms?${params.toString()}`)
  }

  /**
   * Acknowledge device alarm
   */
  async acknowledgeAlarm(alarmId: string, note?: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>(`${this.basePath}/alarms/${alarmId}/acknowledge`, { note })
  }

  /**
   * Get device statistics
   */
  async getDeviceStatistics(
    deviceId: string,
    period: 'hour' | 'day' | 'week' | 'month' = 'day'
  ): Promise<ApiResponse<{
    period: string
    totalReadings: number
    avgCounterValue: number
    maxCounterValue: number
    avgRate: number
    maxRate: number
    dataQualityStats: {
      good: number
      uncertain: number
      bad: number
    }
    uptimePercent: number
    trends: Array<{
      timestamp: Date
      count: number
      rate: number
      quality: DataQuality
    }>
  }>> {
    return apiClient.get<any>(`${this.basePath}/${deviceId}/statistics?period=${period}`)
  }

  /**
   * Export device data
   */
  async exportDeviceData(
    deviceId: string,
    options: {
      format: 'csv' | 'xlsx' | 'json'
      startDate: Date
      endDate: Date
      channels?: string[]
      includeMetadata?: boolean
    }
  ): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.post<{ downloadUrl: string }>(
      `${this.basePath}/${deviceId}/export`,
      options
    )
  }

  /**
   * Get system health overview
   */
  async getSystemHealth(): Promise<ApiResponse<{
    overall: 'healthy' | 'warning' | 'error'
    devices: {
      total: number
      online: number
      offline: number
      error: number
    }
    dataFlow: {
      pointsPerSecond: number
      avgLatency: number
      errorRate: number
    }
    storage: {
      used: number // GB
      available: number // GB
      retentionCompliance: number // percentage
    }
    services: Array<{
      name: string
      status: 'running' | 'stopped' | 'error'
      uptime: number // hours
      lastCheck: Date
    }>
  }>> {
    return apiClient.get<any>(`${this.basePath}/health/overview`, undefined, 'logger')
  }

  /**
   * Discover devices on network
   */
  async discoverDevices(
    networkRange?: string
  ): Promise<ApiResponse<Array<{
    ipAddress: string
    port: number
    deviceType: string
    serialNumber?: string
    firmware?: string
    model?: string
    configured: boolean
  }>>> {
    const params = new URLSearchParams()
    if (networkRange) params.append('network', networkRange)

    return apiClient.post<any>(`${this.basePath}/discover?${params.toString()}`)
  }

  /**
   * Add discovered device
   */
  async addDiscoveredDevice(discoveredDevice: {
    name: string
    ipAddress: string
    port: number
    deviceType: string
    hierarchyNodeId?: string
    channels: Array<{
      name: string
      address: number
      dataType: 'Counter' | 'Digital' | 'Analog'
      unit?: string
      enabled: boolean
    }>
  }): Promise<ApiResponse<DeviceConfig>> {
    return apiClient.post<DeviceConfig>(`${this.basePath}/add`, discoveredDevice)
  }

  /**
   * Remove device
   */
  async removeDevice(deviceId: string, options: {
    deleteData?: boolean
    backupFirst?: boolean
  } = {}): Promise<ApiResponse<void>> {
    const params = new URLSearchParams()
    if (options.deleteData) params.append('deleteData', 'true')
    if (options.backupFirst) params.append('backup', 'true')

    return apiClient.delete<void>(`${this.basePath}/${deviceId}?${params.toString()}`)
  }
}

// Export singleton instance
export const deviceService = new DeviceService()

// Export the class for testing
export { DeviceService }