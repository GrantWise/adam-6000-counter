/**
 * OEE Data Hook
 * Manages OEE metrics fetching and real-time updates
 */

import { useState, useEffect, useCallback } from 'react'
import { oeeService } from '@/lib/services/oeeService'
import type { OeeData, OeeLossAnalysis } from '../types'
import type { ApiResponse } from '@/types'

interface UseOeeDataOptions {
  equipmentIds?: string[]
  refreshInterval?: number
  includeTargets?: boolean
  autoRefresh?: boolean
}

interface UseOeeDataReturn {
  currentOee: OeeData[]
  loading: boolean
  error: string | null
  overview: {
    averageOee: number | null
    totalEquipment: number
    activeEquipment: number
    topPerformers: Array<{
      equipmentId: string
      equipmentName: string
      oee: number | null
      availability: number | null
      performance: number | null
      quality: number | null
    }>
    breakdownByMetric: {
      availability: { avg: number | null; trend: 'up' | 'down' | 'stable' }
      performance: { avg: number | null; trend: 'up' | 'down' | 'stable' }
      quality: { avg: number | null; trend: 'up' | 'down' | 'stable' }
    }
  } | null
  refreshOee: () => Promise<void>
  getEquipmentOee: (equipmentId: string, options?: {
    startDate?: Date
    endDate?: Date
  }) => Promise<OeeData[]>
  getLossAnalysis: (equipmentId: string, options?: {
    startDate?: Date
    endDate?: Date
  }) => Promise<OeeLossAnalysis | null>
}

export const useOeeData = (options: UseOeeDataOptions = {}): UseOeeDataReturn => {
  const {
    equipmentIds = [],
    refreshInterval = 60000, // 1 minute default
    includeTargets = true,
    autoRefresh = true
  } = options

  const [currentOee, setCurrentOee] = useState<OeeData[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [overview, setOverview] = useState<any>(null)

  // Fetch current OEE data
  const refreshOee = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)

      // Fetch current OEE and overview in parallel
      const [currentResponse, overviewResponse] = await Promise.all([
        oeeService.getCurrentOEE(),
        oeeService.getOEEOverview()
      ])

      // Process current OEE data - CFR Part 11 compliant
      if (currentResponse.success && currentResponse.data) {
        let oeeData = currentResponse.data

        // Filter by equipment IDs if specified
        if (equipmentIds.length > 0) {
          oeeData = oeeData.filter(oee => equipmentIds.includes(oee.equipmentId))
        }

        setCurrentOee(oeeData)
      } else {
        // API failed - set empty array and display error (no synthetic data)
        setCurrentOee([])
        const errorMsg = currentResponse.error?.message || 'OEE data service unavailable'
        throw new Error(errorMsg)
      }

      // Process overview data - CFR Part 11 compliant
      if (overviewResponse.success && overviewResponse.data) {
        setOverview(overviewResponse.data)
      } else {
        // Overview API failed - set null (no synthetic data)
        setOverview(null)
        console.warn('OEE overview data unavailable:', overviewResponse.error?.message)
      }

    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch OEE data'
      setError(errorMessage)
      console.error('OEE data fetch error:', err)
    } finally {
      setLoading(false)
    }
  }, [equipmentIds])

  // Get equipment-specific OEE data with history
  const getEquipmentOee = useCallback(async (
    equipmentId: string, 
    options: { startDate?: Date; endDate?: Date } = {}
  ): Promise<OeeData[]> => {
    try {
      const response = await oeeService.getEquipmentOEE(equipmentId, {
        startDate: options.startDate,
        endDate: options.endDate,
        interval: 'hour'
      })

      if (response.success && response.data) {
        // Return real data - already includes CFR Part 11 compliance metadata from service
        return response.data
      }

      return []
    } catch (error) {
      console.error('Failed to fetch equipment OEE:', error)
      return []
    }
  }, [])

  // Get loss analysis for equipment
  const getLossAnalysis = useCallback(async (
    equipmentId: string,
    options: { startDate?: Date; endDate?: Date } = {}
  ): Promise<OeeLossAnalysis | null> => {
    try {
      const response = await oeeService.getOEELossAnalysis(equipmentId, {
        startDate: options.startDate,
        endDate: options.endDate
      })

      if (response.success && response.data) {
        return response.data
      }

      return null
    } catch (error) {
      console.error('Failed to fetch loss analysis:', error)
      return null
    }
  }, [])

  // Initial data load
  useEffect(() => {
    refreshOee()
  }, [refreshOee])

  // Auto-refresh interval
  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(refreshOee, refreshInterval)
    return () => clearInterval(interval)
  }, [refreshOee, refreshInterval, autoRefresh])

  return {
    currentOee,
    loading,
    error,
    overview,
    refreshOee,
    getEquipmentOee,
    getLossAnalysis
  }
}

export default useOeeData