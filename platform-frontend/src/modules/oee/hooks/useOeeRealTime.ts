/**
 * Real-Time OEE Hook
 * Manages SignalR integration for real-time OEE updates and stoppage events
 * CFR Part 11 Compliant: ZERO TOLERANCE for synthetic data
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { authService } from '@/lib/services/authService'
import type { OeeData, StoppageEvent } from '../types'

interface UseOeeRealTimeOptions {
  equipmentIds?: string[]
  enableUpdates?: boolean
  hubUrl?: string
}

interface OeeUpdate {
  equipmentId: string
  equipmentName: string
  timestamp: Date
  oee: number | null
  availability: number | null
  performance: number | null
  quality: number | null
  status: 'running' | 'idle' | 'down'
  currentWorkOrder?: {
    id: string
    productCode: string
    progress: number
  }
}

interface UseOeeRealTimeReturn {
  liveOeeData: Map<string, OeeUpdate>
  activeStoppages: StoppageEvent[]
  lastUpdate: Date | null
  connectionState: 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'
  error: string | null
  isConnected: boolean
  getEquipmentOee: (equipmentId: string) => OeeUpdate | undefined
  startConnection: () => Promise<void>
  stopConnection: () => Promise<void>
}

export const useOeeRealTime = (options: UseOeeRealTimeOptions = {}): UseOeeRealTimeReturn => {
  const {
    equipmentIds = [],
    enableUpdates = true,
    hubUrl = '/hubs/oee-hub' // OEE API SignalR hub for real-time OEE updates
  } = options

  const [liveOeeData, setLiveOeeData] = useState<Map<string, OeeUpdate>>(new Map())
  const [activeStoppages, setActiveStoppages] = useState<StoppageEvent[]>([])
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null)
  const [connectionState, setConnectionState] = useState<'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'>('Disconnected')
  const [error, setError] = useState<string | null>(null)
  
  const connectionRef = useRef<HubConnection | null>(null)

  // Create and configure SignalR connection
  const createConnection = useCallback(() => {
    // Use OEE API port (5140) for SignalR hub
    const oeeApiUrl = import.meta.env.VITE_OEE_API_PORT 
      ? `${window.location.protocol}//${window.location.hostname}:${import.meta.env.VITE_OEE_API_PORT}`
      : `${window.location.protocol}//${window.location.host}:5140`
    
    const connection = new HubConnectionBuilder()
      .withUrl(`${oeeApiUrl}${hubUrl}`, {
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
      setError(error?.message || 'OEE connection closed')
      console.warn('OEE SignalR connection closed:', error)
    })

    connection.onreconnecting((error) => {
      setConnectionState('Reconnecting')
      setError(error?.message || 'OEE reconnecting...')
      console.info('OEE SignalR reconnecting:', error)
    })

    connection.onreconnected((connectionId) => {
      setConnectionState('Connected')
      setError(null)
      console.info('OEE SignalR reconnected:', connectionId)
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
      console.info('OEE SignalR connection established')

      // Subscribe to OEE updates - REAL DATA ONLY
      connection.on('OeeUpdated', (update: OeeUpdate) => {
        // Filter by equipment IDs if specified
        if (equipmentIds.length > 0 && !equipmentIds.includes(update.equipmentId)) return

        // Only process real data - CFR Part 11 compliance
        if (update.oee !== null || update.availability !== null || update.performance !== null || update.quality !== null) {
          setLiveOeeData(prev => {
            const newData = new Map(prev)
            newData.set(update.equipmentId, {
              ...update,
              timestamp: new Date(update.timestamp)
            })
            return newData
          })
          
          setLastUpdate(new Date())
        }
      })

      // Subscribe to stoppage events - REAL DATA ONLY
      connection.on('StoppageStarted', (stoppage: StoppageEvent) => {
        if (equipmentIds.length > 0 && !equipmentIds.includes(stoppage.equipmentId)) return

        setActiveStoppages(prev => [...prev, {
          ...stoppage,
          startTime: new Date(stoppage.startTime)
        }])
      })

      connection.on('StoppageEnded', (stoppageUpdate: { id: string; endTime: Date; resolution?: string }) => {
        setActiveStoppages(prev => 
          prev.filter(s => s.id !== stoppageUpdate.id)
        )
      })

      // Subscribe to equipment status changes
      connection.on('EquipmentStatusChanged', (update: { equipmentId: string; status: string; timestamp: Date }) => {
        if (equipmentIds.length > 0 && !equipmentIds.includes(update.equipmentId)) return

        setLiveOeeData(prev => {
          const newData = new Map(prev)
          const existing = newData.get(update.equipmentId)
          if (existing) {
            newData.set(update.equipmentId, {
              ...existing,
              status: update.status as 'running' | 'idle' | 'down',
              timestamp: new Date(update.timestamp)
            })
          }
          return newData
        })
      })

    } catch (err) {
      setConnectionState('Disconnected')
      const errorMessage = err instanceof Error ? err.message : 'Failed to connect to OEE hub'
      setError(errorMessage)
      console.error('OEE SignalR connection error:', err)
    }
  }, [enableUpdates, createConnection, equipmentIds])

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
      console.error('Error stopping OEE SignalR connection:', err)
    }
  }, [])

  // Start connection on mount and when dependencies change
  useEffect(() => {
    if (enableUpdates) {
      startConnection()
    }

    return () => {
      stopConnection()
    }
  }, [startConnection, stopConnection, enableUpdates])

  // Helper function
  const getEquipmentOee = useCallback((equipmentId: string): OeeUpdate | undefined => {
    return liveOeeData.get(equipmentId)
  }, [liveOeeData])

  return {
    liveOeeData,
    activeStoppages,
    lastUpdate,
    connectionState,
    error,
    isConnected: connectionState === 'Connected',
    getEquipmentOee,
    startConnection,
    stopConnection
  }
}

export default useOeeRealTime