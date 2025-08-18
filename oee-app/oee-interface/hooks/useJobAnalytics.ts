"use client"

import { useState, useEffect, useRef, useCallback } from 'react'
import { JobAnalytics, createJobAnalyticsService } from '@/lib/services/jobAnalyticsService'

export interface UseJobAnalyticsOptions {
  jobId: number | null
  deviceId?: string
  updateInterval?: number // milliseconds, default 30000 (30 seconds)
  enabled?: boolean
}

export interface UseJobAnalyticsReturn {
  analytics: JobAnalytics | null
  isLoading: boolean
  error: string | null
  lastUpdated: Date | null
  refresh: () => Promise<void>
  isStale: boolean // Data is older than 2 minutes
}

/**
 * Hook for real-time job analytics with progress tracking and completion estimates
 * Provides comprehensive job metrics with automatic updates
 */
export function useJobAnalytics(options: UseJobAnalyticsOptions): UseJobAnalyticsReturn {
  const {
    jobId,
    deviceId = 'Device001',
    updateInterval = 30000, // 30 seconds
    enabled = true
  } = options

  const [analytics, setAnalytics] = useState<JobAnalytics | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)

  const intervalRef = useRef<NodeJS.Timeout | null>(null)
  const analyticsServiceRef = useRef(createJobAnalyticsService(deviceId))

  // Check if data is stale (older than 2 minutes)
  const isStale = lastUpdated ? (Date.now() - lastUpdated.getTime()) > 120000 : false

  const fetchAnalytics = useCallback(async (): Promise<void> => {
    if (!jobId) {
      setAnalytics(null)
      setError(null)
      setIsLoading(false)
      return
    }

    try {
      setIsLoading(true)
      const result = await analyticsServiceRef.current.getRealTimeJobAnalytics(jobId)

      if (result.isSuccess && result.value) {
        setAnalytics(result.value)
        setError(null)
        setLastUpdated(new Date())
      } else {
        setError(result.errorMessage || 'Failed to fetch job analytics')
        if (result.errorMessage?.includes('not found')) {
          setAnalytics(null) // Clear analytics if job not found
        }
      }
    } catch (err) {
      console.error('Error fetching job analytics:', err)
      setError(err instanceof Error ? err.message : 'Unknown error occurred')
    } finally {
      setIsLoading(false)
    }
  }, [jobId])

  const startUpdates = useCallback((): void => {
    if (!enabled || !jobId) return

    // Clear existing interval
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
    }

    // Initial fetch
    fetchAnalytics()

    // Set up periodic updates
    intervalRef.current = setInterval(() => {
      fetchAnalytics()
    }, updateInterval)
  }, [enabled, jobId, updateInterval, fetchAnalytics])

  const stopUpdates = useCallback((): void => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
  }, [])

  // Manual refresh function
  const refresh = useCallback(async (): Promise<void> => {
    await fetchAnalytics()
  }, [fetchAnalytics])

  // Effect to manage analytics updates
  useEffect(() => {
    if (enabled && jobId) {
      startUpdates()
    } else {
      stopUpdates()
      if (!jobId) {
        setAnalytics(null)
        setError(null)
      }
    }

    return () => {
      stopUpdates()
    }
  }, [enabled, jobId, startUpdates, stopUpdates])

  // Update analytics service when deviceId changes
  useEffect(() => {
    analyticsServiceRef.current = createJobAnalyticsService(deviceId)
  }, [deviceId])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopUpdates()
    }
  }, [stopUpdates])

  return {
    analytics,
    isLoading,
    error,
    lastUpdated,
    refresh,
    isStale
  }
}

/**
 * Hook for getting job completion estimates without full analytics
 * Lightweight version for components that only need completion data
 */
export function useJobCompletion(jobId: number | null, deviceId: string = 'Device001') {
  const { analytics, isLoading, error } = useJobAnalytics({
    jobId,
    deviceId,
    updateInterval: 60000, // 1 minute updates for completion estimates
    enabled: !!jobId
  })

  return {
    completion: analytics?.completion || null,
    isLoading,
    error,
    progressPercent: analytics?.completion.progressPercent || 0,
    estimatedCompletion: analytics?.completion.estimatedCompletionTime,
    onSchedule: analytics?.completion.onSchedule ?? true,
    hoursRemaining: analytics?.completion.hoursRemaining || 0
  }
}

/**
 * Hook for getting job efficiency metrics
 * Focused on performance and efficiency data
 */
export function useJobEfficiency(jobId: number | null, deviceId: string = 'Device001') {
  const { analytics, isLoading, error, lastUpdated } = useJobAnalytics({
    jobId,
    deviceId,
    updateInterval: 15000, // 15 second updates for efficiency
    enabled: !!jobId
  })

  return {
    efficiency: analytics?.efficiency || null,
    production: analytics?.production || null,
    benchmarks: analytics?.benchmarks || null,
    isLoading,
    error,
    lastUpdated,
    currentEfficiency: analytics?.efficiency.currentEfficiency || 0,
    averageEfficiency: analytics?.efficiency.averageEfficiency || 0,
    trend: analytics?.efficiency.overallTrend || 'stable',
    consistencyScore: analytics?.efficiency.consistencyScore || 0
  }
}

/**
 * Hook for quality metrics monitoring
 * Specialized for quality and defect tracking
 */
export function useJobQuality(jobId: number | null, deviceId: string = 'Device001') {
  const { analytics, isLoading, error } = useJobAnalytics({
    jobId,
    deviceId,
    updateInterval: 45000, // 45 second updates for quality
    enabled: !!jobId
  })

  return {
    quality: analytics?.quality || null,
    isLoading,
    error,
    qualityPercent: analytics?.quality.qualityPercent || 100,
    defectRate: analytics?.quality.defectRate || 0,
    hasQualityData: analytics?.quality.isQualityDataAvailable || false,
    qualityTrend: analytics?.quality.trendDirection || 'stable',
    hourlyQualityData: analytics?.quality.hourlyTrend || []
  }
}