/**
 * Device Data Hook
 * Manages device data fetching and real-time updates for Logger module
 */

import { useState, useEffect, useCallback } from 'react'
import { deviceService } from '@/lib/services/deviceService'
import type { DeviceData, DeviceHealth } from '../types'
import type { ApiResponse } from '@/types'

interface UseDeviceDataOptions {
  refreshInterval?: number
  includeHealth?: boolean
  autoRefresh?: boolean
}

interface UseDeviceDataReturn {
  devices: DeviceData[]
  loading: boolean
  error: string | null
  summary: {
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
  } | null
  refreshDevices: () => Promise<void>
  getDevice: (deviceId: string) => DeviceData | undefined
  updateDevice: (deviceId: string, updates: Partial<DeviceData>) => void
}

export const useDeviceData = (options: UseDeviceDataOptions = {}): UseDeviceDataReturn => {
  const {
    refreshInterval = 30000, // 30 seconds default
    includeHealth = false,
    autoRefresh = true
  } = options

  const [devices, setDevices] = useState<DeviceData[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [summary, setSummary] = useState<any>(null)

  // Transform device config to device data format
  const transformDeviceData = useCallback(async (deviceConfigs: any[]): Promise<DeviceData[]> => {
    const deviceDataPromises = deviceConfigs.map(async (config) => {
      const baseDeviceData: DeviceData = {
        id: config.id,
        name: config.name,
        type: config.deviceType || 'ADAM-6051',
        ipAddress: config.ipAddress,
        status: config.status === 'active' ? 'online' : 'offline',
        lastSeen: config.lastSeen,
        connectionQuality: config.status === 'active' ? 'good' : 'offline',
        counters: {
          total: config.channels?.length || 2,
          active: config.channels?.filter((c: any) => c.enabled).length || 2,
          totalCounts: 0, // Will be updated with real-time data
          countsPerHour: 0 // Will be updated with real-time data
        }
      }

      // Include health data if requested
      if (includeHealth) {
        try {
          const healthResponse = await deviceService.getDeviceHealth(config.id)
          if (healthResponse.success && healthResponse.data) {
            baseDeviceData.health = healthResponse.data
            
            // Update status based on health
            baseDeviceData.status = healthResponse.data.status
            baseDeviceData.connectionQuality = 
              healthResponse.data.status === 'healthy' ? 'excellent' :
              healthResponse.data.status === 'warning' ? 'good' :
              healthResponse.data.status === 'error' ? 'poor' : 'offline'
          }
        } catch (healthError) {
          console.warn(`Failed to fetch health for device ${config.id}:`, healthError)
        }
      }

      return baseDeviceData
    })

    return Promise.all(deviceDataPromises)
  }, [includeHealth])

  // Fetch devices function
  const refreshDevices = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)

      // Fetch devices and summary in parallel
      const [devicesResponse, summaryResponse] = await Promise.all([
        deviceService.getDevices(),
        deviceService.getDeviceSummary()
      ])

      if (devicesResponse.success && devicesResponse.data) {
        const transformedDevices = await transformDeviceData(devicesResponse.data)
        setDevices(transformedDevices)
      } else {
        throw new Error('Failed to fetch devices')
      }

      if (summaryResponse.success && summaryResponse.data) {
        setSummary(summaryResponse.data)
      }

    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch device data'
      setError(errorMessage)
      console.error('Device data fetch error:', err)
    } finally {
      setLoading(false)
    }
  }, [transformDeviceData])

  // Initial data load
  useEffect(() => {
    refreshDevices()
  }, [refreshDevices])

  // Auto-refresh interval
  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(refreshDevices, refreshInterval)
    return () => clearInterval(interval)
  }, [refreshDevices, refreshInterval, autoRefresh])

  // Get specific device
  const getDevice = useCallback((deviceId: string): DeviceData | undefined => {
    return devices.find(device => device.id === deviceId)
  }, [devices])

  // Update specific device (for real-time updates)
  const updateDevice = useCallback((deviceId: string, updates: Partial<DeviceData>) => {
    setDevices(prevDevices => 
      prevDevices.map(device => 
        device.id === deviceId 
          ? { ...device, ...updates }
          : device
      )
    )
  }, [])

  return {
    devices,
    loading,
    error,
    summary,
    refreshDevices,
    getDevice,
    updateDevice
  }
}

export default useDeviceData