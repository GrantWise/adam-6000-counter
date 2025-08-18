"use client"

import { useState, useEffect, useRef } from 'react'
import { CurrentMetricsResponse } from '@/lib/types/oee'

export interface UseRealTimeMetricsOptions {
  deviceId?: string
  pollingInterval?: number // milliseconds, default 5000 (5 seconds)
  enabled?: boolean
}

export interface UseRealTimeMetricsReturn {
  metrics: CurrentMetricsResponse | null
  isLoading: boolean
  error: string | null
  lastUpdated: Date | null
  connectionStatus: 'connected' | 'disconnected' | 'error'
  retryCount: number
}

/**
 * Real-time metrics hook using polling strategy (5-second intervals)
 * Provides current OEE metrics with proper error handling and retry logic
 */
export function useRealTimeMetrics(options: UseRealTimeMetricsOptions = {}): UseRealTimeMetricsReturn {
  const {
    deviceId = 'Device001',
    pollingInterval = 5000,
    enabled = true
  } = options

  const [metrics, setMetrics] = useState<CurrentMetricsResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [connectionStatus, setConnectionStatus] = useState<'connected' | 'disconnected' | 'error'>('disconnected')
  const [retryCount, setRetryCount] = useState(0)

  const intervalRef = useRef<NodeJS.Timeout | null>(null)
  const retryTimeoutRef = useRef<NodeJS.Timeout | null>(null)
  const maxRetries = 5
  const baseRetryDelay = 1000 // 1 second

  const fetchMetrics = async (): Promise<void> => {
    try {
      const response = await fetch(`/api/metrics/current?deviceId=${encodeURIComponent(deviceId)}`, {
        method: 'GET',
        headers: {
          'Cache-Control': 'no-cache'
        },
        credentials: 'include'
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }

      const data: CurrentMetricsResponse = await response.json()
      
      // Update state on successful fetch
      setMetrics(data)
      setError(null)
      setLastUpdated(new Date())
      setConnectionStatus(data.status === 'error' ? 'error' : 'connected')
      setRetryCount(0)
      setIsLoading(false)

    } catch (err) {
      console.error('Error fetching metrics:', err)
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred'
      
      setError(errorMessage)
      setConnectionStatus('error')
      
      // Implement exponential backoff retry logic
      if (retryCount < maxRetries) {
        const delay = baseRetryDelay * Math.pow(2, retryCount)
        setRetryCount(prev => prev + 1)
        
        retryTimeoutRef.current = setTimeout(() => {
          fetchMetrics()
        }, delay)
      } else {
        setConnectionStatus('disconnected')
        setIsLoading(false)
      }
    }
  }

  const startPolling = (): void => {
    if (!enabled) return

    // Clear existing intervals
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
    }

    // Initial fetch
    fetchMetrics()

    // Set up polling interval
    intervalRef.current = setInterval(() => {
      fetchMetrics()
    }, pollingInterval)
  }

  const stopPolling = (): void => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
    if (retryTimeoutRef.current) {
      clearTimeout(retryTimeoutRef.current)
      retryTimeoutRef.current = null
    }
  }

  // Effect to manage polling lifecycle
  useEffect(() => {
    if (enabled) {
      startPolling()
    } else {
      stopPolling()
      setConnectionStatus('disconnected')
    }

    return () => {
      stopPolling()
    }
  }, [deviceId, pollingInterval, enabled])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopPolling()
    }
  }, [])

  return {
    metrics,
    isLoading,
    error,
    lastUpdated,
    connectionStatus,
    retryCount
  }
}