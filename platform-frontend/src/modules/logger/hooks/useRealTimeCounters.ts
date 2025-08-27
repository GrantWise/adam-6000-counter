/**
 * Real-Time Counters Hook
 * Manages SignalR integration for real-time counter data updates
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { authService } from '@/lib/services/authService'
import type { CounterData, CounterUpdate } from '../types'
import type { DataQuality } from '@/types'

interface UseRealTimeCountersOptions {
  deviceIds?: string[]
  channels?: number[]
  enableUpdates?: boolean
  maxUpdateFrequency?: number // Updates per second
  hubUrl?: string
}

interface UseRealTimeCountersReturn {
  counters: CounterData[]
  countersMap: Map<string, CounterData>
  lastUpdate: Date | null
  connectionState: 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'
  error: string | null
  isConnected: boolean
  getCounter: (deviceId: string, channel: number) => CounterData | undefined
  getDeviceCounters: (deviceId: string) => CounterData[]
  startConnection: () => Promise<void>
  stopConnection: () => Promise<void>
}

const calculateTrend = (previousValue?: number, currentValue?: number): 'up' | 'down' | 'stable' => {
  if (!previousValue || !currentValue) return 'stable'
  if (currentValue > previousValue) return 'up'
  if (currentValue < previousValue) return 'down'
  return 'stable'
}

export const useRealTimeCounters = (options: UseRealTimeCountersOptions = {}): UseRealTimeCountersReturn => {
  const {
    deviceIds = [],
    channels = [],
    enableUpdates = true,
    maxUpdateFrequency = 10,
    hubUrl = '/hubs/counter-hub' // Logger API SignalR hub for counter updates
  } = options

  const [counters, setCounters] = useState<Map<string, CounterData>>(new Map())
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null)
  const [connectionState, setConnectionState] = useState<'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'>('Disconnected')
  const [error, setError] = useState<string | null>(null)
  
  const connectionRef = useRef<HubConnection | null>(null)
  const updateQueueRef = useRef<CounterUpdate[]>([])
  const throttleTimeoutRef = useRef<NodeJS.Timeout>()

  // Throttled update processing to prevent UI flooding
  const processUpdates = useCallback(() => {
    if (updateQueueRef.current.length === 0) return

    const updates = [...updateQueueRef.current]
    updateQueueRef.current = []

    setCounters(prev => {
      const newCounters = new Map(prev)

      updates.forEach(update => {
        const key = `${update.deviceId}-${update.channel}`
        const existingCounter = newCounters.get(key)

        // Only update if timestamp is newer or no existing data
        if (!existingCounter || update.timestamp > existingCounter.lastUpdate) {
          newCounters.set(key, {
            deviceId: update.deviceId,
            deviceName: update.deviceName,
            channel: update.channel,
            channelName: update.channelName || `Channel ${update.channel}`,
            currentValue: update.value,
            unit: update.unit || 'counts',
            timestamp: update.timestamp,
            dataQuality: update.quality,
            ratePerMinute: update.ratePerMinute || 0,
            ratePerHour: update.ratePerHour || 0,
            trend: calculateTrend(existingCounter?.currentValue, update.value),
            isRealData: true,
            lastUpdate: update.timestamp
          })
        }
      })

      return newCounters
    })

    setLastUpdate(new Date())
  }, [])

  // Create and configure SignalR connection
  const createConnection = useCallback(() => {
    // Use Logger API port (5139) for SignalR hub
    const loggerApiUrl = import.meta.env.VITE_LOGGER_API_PORT 
      ? `${window.location.protocol}//${window.location.hostname}:${import.meta.env.VITE_LOGGER_API_PORT}`
      : `${window.location.protocol}//${window.location.host}:5139`
    
    const connection = new HubConnectionBuilder()
      .withUrl(`${loggerApiUrl}${hubUrl}`, {
        accessTokenFactory: () => authService.getToken() || ''
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(
        process.env.NODE_ENV === 'development' 
          ? LogLevel.Information 
          : LogLevel.Warning
      )
      .build()

    // Connection event handlers
    connection.onclose((error) => {
      setConnectionState('Disconnected')
      setError(error?.message || 'Connection closed')
      console.warn('SignalR connection closed:', error)
    })

    connection.onreconnecting((error) => {
      setConnectionState('Reconnecting')
      setError(error?.message || 'Reconnecting...')
      console.info('SignalR reconnecting:', error)
    })

    connection.onreconnected((connectionId) => {
      setConnectionState('Connected')
      setError(null)
      console.info('SignalR reconnected:', connectionId)
    })

    return connection
  }, [hubUrl])

  // Start SignalR connection
  const startConnection = useCallback(async () => {
    if (!enableUpdates) return

    try {
      setConnectionState('Connecting')
      setError(null)

      const connection = createConnection()
      connectionRef.current = connection

      await connection.start()
      
      setConnectionState('Connected')
      console.info('SignalR connection established')

      // Subscribe to counter updates
      connection.on('CounterUpdated', (update: CounterUpdate) => {
        // Filter by device and channel if specified
        if (deviceIds.length > 0 && !deviceIds.includes(update.deviceId)) return
        if (channels.length > 0 && !channels.includes(update.channel)) return

        // Add to update queue
        updateQueueRef.current.push(update)

        // Throttle updates to prevent UI flooding
        if (throttleTimeoutRef.current) {
          clearTimeout(throttleTimeoutRef.current)
        }

        throttleTimeoutRef.current = setTimeout(
          processUpdates,
          1000 / maxUpdateFrequency
        )
      })

      // Subscribe to device status changes
      connection.on('DeviceStatusChanged', (update: { deviceId: string; status: string; timestamp: Date }) => {
        if (update.status === 'offline') {
          // Mark all counters from this device as unavailable
          setCounters(prev => {
            const newCounters = new Map(prev)

            for (const [key, counter] of newCounters.entries()) {
              if (counter.deviceId === update.deviceId) {
                newCounters.set(key, {
                  ...counter,
                  dataQuality: 'unavailable' as DataQuality,
                  lastUpdate: new Date(update.timestamp)
                })
              }
            }

            return newCounters
          })
        }
      })

    } catch (err) {
      setConnectionState('Disconnected')
      const errorMessage = err instanceof Error ? err.message : 'Failed to connect'
      setError(errorMessage)
      console.error('SignalR connection error:', err)
    }
  }, [enableUpdates, createConnection, deviceIds, channels, processUpdates, maxUpdateFrequency])

  // Stop SignalR connection
  const stopConnection = useCallback(async () => {
    try {
      if (connectionRef.current) {
        await connectionRef.current.stop()
        connectionRef.current = null
      }
      setConnectionState('Disconnected')
      setError(null)
    } catch (err) {
      console.error('Error stopping SignalR connection:', err)
    }
  }, [])

  // Start connection on mount and when dependencies change
  useEffect(() => {
    if (enableUpdates) {
      startConnection()
    }

    return () => {
      stopConnection()
      if (throttleTimeoutRef.current) {
        clearTimeout(throttleTimeoutRef.current)
      }
    }
  }, [startConnection, stopConnection, enableUpdates])

  // Helper functions
  const getCounter = useCallback((deviceId: string, channel: number): CounterData | undefined => {
    return counters.get(`${deviceId}-${channel}`)
  }, [counters])

  const getDeviceCounters = useCallback((deviceId: string): CounterData[] => {
    return Array.from(counters.values()).filter(c => c.deviceId === deviceId)
  }, [counters])

  return {
    counters: Array.from(counters.values()),
    countersMap: counters,
    lastUpdate,
    connectionState,
    error,
    isConnected: connectionState === 'Connected',
    getCounter,
    getDeviceCounters,
    startConnection,
    stopConnection
  }
}

export default useRealTimeCounters