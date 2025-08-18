"use client"

import { useState, useEffect, useRef } from 'react'
import { HealthStatus } from '@/lib/types/oee'

export interface UseConnectionMonitorOptions {
  checkInterval?: number // milliseconds, default 10000 (10 seconds)
  enabled?: boolean
}

export interface UseConnectionMonitorReturn {
  healthStatus: HealthStatus | null
  isOnline: boolean
  lastCheck: Date | null
  error: string | null
}

/**
 * Connection monitor hook for tracking system health and database connectivity
 * Polls health endpoint to monitor overall system status
 */
export function useConnectionMonitor(options: UseConnectionMonitorOptions = {}): UseConnectionMonitorReturn {
  const {
    checkInterval = 10000, // 10 seconds
    enabled = true
  } = options

  const [healthStatus, setHealthStatus] = useState<HealthStatus | null>(null)
  const [isOnline, setIsOnline] = useState(true)
  const [lastCheck, setLastCheck] = useState<Date | null>(null)
  const [error, setError] = useState<string | null>(null)

  const intervalRef = useRef<NodeJS.Timeout | null>(null)

  const checkHealth = async (): Promise<void> => {
    try {
      const response = await fetch('/api/health', {
        method: 'GET',
        headers: {
          'Cache-Control': 'no-cache'
        }
      })

      if (!response.ok) {
        throw new Error(`Health check failed: ${response.status}`)
      }

      const health: HealthStatus = await response.json()
      
      setHealthStatus(health)
      setIsOnline(health.status === 'healthy' || health.status === 'warning')
      setError(null)
      setLastCheck(new Date())

    } catch (err) {
      console.error('Health check error:', err)
      const errorMessage = err instanceof Error ? err.message : 'Health check failed'
      
      setError(errorMessage)
      setIsOnline(false)
      setLastCheck(new Date())
    }
  }

  const startMonitoring = (): void => {
    if (!enabled) return

    // Clear existing interval
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
    }

    // Initial health check
    checkHealth()

    // Set up monitoring interval
    intervalRef.current = setInterval(checkHealth, checkInterval)
  }

  const stopMonitoring = (): void => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
  }

  // Effect to manage monitoring lifecycle
  useEffect(() => {
    if (enabled) {
      startMonitoring()
    } else {
      stopMonitoring()
    }

    return () => {
      stopMonitoring()
    }
  }, [checkInterval, enabled])

  // Listen to browser online/offline events
  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true)
      if (enabled) {
        checkHealth()
      }
    }

    const handleOffline = () => {
      setIsOnline(false)
      setError('Browser is offline')
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [enabled])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopMonitoring()
    }
  }, [])

  return {
    healthStatus,
    isOnline,
    lastCheck,
    error
  }
}