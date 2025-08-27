/**
 * Device Health Hook
 * Manages device health monitoring and alerting for Logger module
 */

import { useState, useEffect, useCallback } from 'react'
import { deviceService } from '@/lib/services/deviceService'
import type { DeviceHealth, DeviceAlarm } from '../types'

interface UseDeviceHealthOptions {
  deviceId?: string
  refreshInterval?: number
  includeAlarms?: boolean
  autoRefresh?: boolean
}

interface UseDeviceHealthReturn {
  health: DeviceHealth | null
  alarms: DeviceAlarm[]
  loading: boolean
  error: string | null
  refreshHealth: () => Promise<void>
  acknowledgeAlarm: (alarmId: string, note?: string) => Promise<void>
  testConnection: () => Promise<{ success: boolean; responseTime: number; error?: string }>
}

export const useDeviceHealth = (options: UseDeviceHealthOptions = {}): UseDeviceHealthReturn => {
  const {
    deviceId,
    refreshInterval = 60000, // 1 minute default
    includeAlarms = true,
    autoRefresh = true
  } = options

  const [health, setHealth] = useState<DeviceHealth | null>(null)
  const [alarms, setAlarms] = useState<DeviceAlarm[]>([])
  const [loading, setLoading] = useState(!!deviceId)
  const [error, setError] = useState<string | null>(null)

  // Fetch device health data
  const refreshHealth = useCallback(async () => {
    if (!deviceId) return

    try {
      setLoading(true)
      setError(null)

      // Fetch health data
      const healthResponse = await deviceService.getDeviceHealth(deviceId)
      
      if (healthResponse.success && healthResponse.data) {
        setHealth(healthResponse.data)
      } else {
        throw new Error('Failed to fetch device health')
      }

      // Fetch alarms if requested
      if (includeAlarms) {
        const alarmsResponse = await deviceService.getDeviceAlarms(deviceId)
        
        if (alarmsResponse.success && alarmsResponse.data) {
          setAlarms(alarmsResponse.data)
        }
      }

    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch device health'
      setError(errorMessage)
      console.error('Device health fetch error:', err)
    } finally {
      setLoading(false)
    }
  }, [deviceId, includeAlarms])

  // Test device connection
  const testConnection = useCallback(async (): Promise<{ success: boolean; responseTime: number; error?: string }> => {
    if (!deviceId) {
      return { success: false, responseTime: 0, error: 'No device ID specified' }
    }

    try {
      const response = await deviceService.testDeviceConnection(deviceId)
      
      if (response.success && response.data) {
        return {
          success: response.data.success,
          responseTime: response.data.responseTime,
          error: response.data.error
        }
      } else {
        return { success: false, responseTime: 0, error: 'Test failed' }
      }
    } catch (err) {
      return {
        success: false,
        responseTime: 0,
        error: err instanceof Error ? err.message : 'Connection test failed'
      }
    }
  }, [deviceId])

  // Acknowledge alarm
  const acknowledgeAlarm = useCallback(async (alarmId: string, note?: string) => {
    try {
      const response = await deviceService.acknowledgeAlarm(alarmId, note)
      
      if (response.success) {
        // Update local alarm state
        setAlarms(prevAlarms => 
          prevAlarms.map(alarm => 
            alarm.id === alarmId 
              ? { ...alarm, acknowledgedAt: new Date(), acknowledgedBy: 'Current User' }
              : alarm
          )
        )
      } else {
        throw new Error('Failed to acknowledge alarm')
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to acknowledge alarm'
      console.error('Alarm acknowledgment error:', err)
      throw new Error(errorMessage)
    }
  }, [])

  // Initial data load
  useEffect(() => {
    if (deviceId) {
      refreshHealth()
    }
  }, [deviceId, refreshHealth])

  // Auto-refresh interval
  useEffect(() => {
    if (!autoRefresh || !deviceId) return

    const interval = setInterval(refreshHealth, refreshInterval)
    return () => clearInterval(interval)
  }, [refreshHealth, refreshInterval, autoRefresh, deviceId])

  return {
    health,
    alarms,
    loading,
    error,
    refreshHealth,
    acknowledgeAlarm,
    testConnection
  }
}

export default useDeviceHealth