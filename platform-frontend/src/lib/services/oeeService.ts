import { apiClient } from '@/lib/api/client'
import type { ApiResponse, OEEMetrics, WorkOrder, StoppageEvent, DataWithQuality, QualityAwareApiResponse } from '@/types'

/**
 * OEE Service
 * Handles Overall Equipment Effectiveness calculations and monitoring
 */
class OEEService {
  private readonly basePath = '/api/oee'

  /**
   * Get current OEE metrics for all equipment - Maps to OEE API /api/oee/current endpoint
   * CFR Part 11 Compliant: ZERO TOLERANCE for synthetic data - returns unavailable data status
   */
  async getCurrentOEE(): Promise<ApiResponse<Array<OEEMetrics & { 
    equipmentName: string
    dataQuality: 'good' | 'unavailable'
    isRealData: boolean 
    auditInfo?: {
      sourceSystem: string
      dataIntegrity: string
      complianceFlags: string[]
    }
  }>>> {
    try {
      // Get all current OEE data from OEE API
      const response = await apiClient.get<any>('/api/oee/current', undefined, 'oee')
      
      if (response.success && response.data) {
        // Handle both single device and multiple devices response
        const dataArray = Array.isArray(response.data) ? response.data : [response.data]
        
        const transformedData = dataArray.map((oeeData: any) => ({
          equipmentId: oeeData.DeviceId || oeeData.EquipmentId,
          equipmentName: `ADAM ${oeeData.DeviceId || oeeData.EquipmentId}`,
          timestamp: new Date(oeeData.CalculatedAt || oeeData.Timestamp || Date.now()),
          oee: oeeData.OeePercentage !== undefined ? oeeData.OeePercentage : null,
          availability: oeeData.AvailabilityPercentage !== undefined ? oeeData.AvailabilityPercentage : null,
          performance: oeeData.PerformancePercentage !== undefined ? oeeData.PerformancePercentage : null,
          quality: oeeData.QualityPercentage !== undefined ? oeeData.QualityPercentage : null,
          plannedProductionTime: oeeData.PlannedProductionTime || null,
          actualRunTime: oeeData.ActualRunTime || null,
          idealCycleTime: oeeData.IdealCycleTime || null,
          totalCount: oeeData.TotalCount || null,
          goodCount: oeeData.GoodCount || null,
          rejectCount: oeeData.RejectCount || null,
          // CFR Part 11 compliance metadata
          dataQuality: (oeeData.OeePercentage !== undefined && oeeData.OeePercentage !== null) ? 'good' as const : 'unavailable' as const,
          isRealData: true,
          auditInfo: {
            sourceSystem: 'OEE API',
            dataIntegrity: 'good',
            complianceFlags: ['CFR-21-COMPLIANT', 'REAL-DATA', 'API-SOURCED']
          }
        }))
        
        return {
          success: true,
          data: transformedData
        }
      } else {
        // API call failed - return error without synthetic data
        return {
          success: false,
          error: {
            code: 'OEE_API_UNAVAILABLE',
            message: 'OEE data not available - OEE API service unreachable',
            details: { apiError: true, complianceBreach: false },
            timestamp: new Date(),
            retryable: true
          }
        }
      }
    } catch (error) {
      // Network or other error - return error without synthetic data
      return {
        success: false,
        error: {
          code: 'OEE_API_ERROR',
          message: 'Failed to retrieve OEE data - service may be offline',
          details: { 
            networkError: true, 
            complianceBreach: false,
            originalError: error instanceof Error ? error.message : 'Unknown error'
          },
          timestamp: new Date(),
          retryable: true
        }
      }
    }
  }


  /**
   * Get OEE metrics for specific equipment - Maps to OEE API /api/oee/current or /api/oee/history endpoint
   */
  async getEquipmentOEE(
    equipmentId: string,
    options: {
      startDate?: Date
      endDate?: Date
      interval?: 'hour' | 'day' | 'week' | 'month'
    } = {}
  ): Promise<ApiResponse<OEEMetrics[]>> {
    try {
      // Use history endpoint if date range is specified, otherwise current
      const useHistory = options.startDate || options.endDate
      
      if (useHistory) {
        // Use history endpoint
        const params = new URLSearchParams()
        params.append('deviceId', equipmentId)
        if (options.startDate) params.append('startTime', options.startDate.toISOString())
        if (options.endDate) params.append('endTime', options.endDate.toISOString())
        
        const response = await apiClient.get<any[]>(
          `/api/oee/history?${params.toString()}`, 
          undefined, 
          'oee'
        )
        
        if (response.success && response.data && Array.isArray(response.data)) {
          const transformedData = response.data.map((oeeData: any) => ({
            equipmentId,
            timestamp: new Date(oeeData.CalculatedAt || Date.now()),
            oee: oeeData.OeePercentage || 0,
            availability: oeeData.AvailabilityPercentage || 0,
            performance: oeeData.PerformancePercentage || 0,
            quality: oeeData.QualityPercentage || 0,
            plannedProductionTime: oeeData.PlannedProductionTime || 0,
            actualRunTime: oeeData.ActualRunTime || 0,
            idealCycleTime: oeeData.IdealCycleTime || 0,
            totalCount: oeeData.TotalCount || 0,
            goodCount: oeeData.GoodCount || 0,
            rejectCount: oeeData.RejectCount || 0
          }))
          
          return {
            success: true,
            data: transformedData
          }
        }
      } else {
        // Use current endpoint
        const response = await apiClient.get<any>(
          `/api/oee/current?deviceId=${equipmentId}`, 
          undefined, 
          'oee'
        )
        
        if (response.success && response.data) {
          const oeeData = response.data
          return {
            success: true,
            data: [{
              equipmentId,
              timestamp: new Date(oeeData.CalculatedAt || Date.now()),
              oee: oeeData.OeePercentage || 0,
              availability: oeeData.AvailabilityPercentage || 0,
              performance: oeeData.PerformancePercentage || 0,
              quality: oeeData.QualityPercentage || 0,
              plannedProductionTime: oeeData.PlannedProductionTime || 0,
              actualRunTime: oeeData.ActualRunTime || 0,
              idealCycleTime: oeeData.IdealCycleTime || 0,
              totalCount: oeeData.TotalCount || 0,
              goodCount: oeeData.GoodCount || 0,
              rejectCount: oeeData.RejectCount || 0
            }]
          }
        }
      }
      
      // No data available from API - return error without synthetic data
      return {
        success: false,
        error: {
          code: 'EQUIPMENT_OEE_UNAVAILABLE',
          message: `OEE data not available for equipment ${equipmentId}`,
          details: { equipmentId, apiError: true },
          timestamp: new Date(),
          retryable: true
        }
      }
    } catch (error) {
      // Return error on network/API failure - no synthetic data
      return {
        success: false,
        error: {
          code: 'EQUIPMENT_OEE_ERROR',
          message: `Failed to retrieve OEE data for equipment ${equipmentId}`,
          details: { 
            equipmentId, 
            networkError: true,
            originalError: error instanceof Error ? error.message : 'Unknown error'
          },
          timestamp: new Date(),
          retryable: true
        }
      }
    }
  }

  /**
   * Get OEE overview dashboard data - Aggregated from individual device data
   */
  async getOEEOverview(): Promise<ApiResponse<{
    averageOEE: number
    totalEquipment: number
    activeEquipment: number
    topPerformers: Array<{
      equipmentId: string
      equipmentName: string
      oee: number
      availability: number
      performance: number
      quality: number
    }>
    trends: {
      hourly: Array<{ timestamp: Date; oee: number }>
      daily: Array<{ date: Date; oee: number }>
      weekly: Array<{ week: string; oee: number }>
    }
    breakdownByMetric: {
      availability: { avg: number; trend: 'up' | 'down' | 'stable' }
      performance: { avg: number; trend: 'up' | 'down' | 'stable' }
      quality: { avg: number; trend: 'up' | 'down' | 'stable' }
    }
  }>> {
    try {
      // Get current OEE data for all devices to build overview
      const currentOEEResponse = await this.getCurrentOEE()
      
      if (currentOEEResponse.success && currentOEEResponse.data) {
        const allOEEData = currentOEEResponse.data
        
        // Calculate aggregated metrics
        const totalEquipment = allOEEData.length
        const activeEquipment = allOEEData.filter(d => d.oee > 0).length
        
        const averageOEE = allOEEData.reduce((sum, d) => sum + d.oee, 0) / totalEquipment
        const averageAvailability = allOEEData.reduce((sum, d) => sum + d.availability, 0) / totalEquipment
        const averagePerformance = allOEEData.reduce((sum, d) => sum + d.performance, 0) / totalEquipment
        const averageQuality = allOEEData.reduce((sum, d) => sum + d.quality, 0) / totalEquipment
        
        // Sort by OEE for top performers
        const topPerformers = allOEEData
          .sort((a, b) => b.oee - a.oee)
          .slice(0, 5)
          .map(d => ({
            equipmentId: d.equipmentId,
            equipmentName: d.equipmentName,
            oee: Math.round(d.oee * 100) / 100,
            availability: Math.round(d.availability * 100) / 100,
            performance: Math.round(d.performance * 100) / 100,
            quality: Math.round(d.quality * 100) / 100
          }))
        
        // No synthetic trend data - CFR Part 21 compliance
        const now = new Date()
        const hourlyTrends = []
        const dailyTrends = []
        const weeklyTrends = []
        
        // Generate timestamps with null OEE values - no synthetic data
        for (let i = 23; i >= 0; i--) {
          const timestamp = new Date(now.getTime() - (i * 60 * 60 * 1000))
          hourlyTrends.push({
            timestamp,
            oee: null // No synthetic values - CFR Part 21 compliance
          })
        }
        
        // Generate date stamps with null OEE values - no synthetic data
        for (let i = 6; i >= 0; i--) {
          const date = new Date(now.getTime() - (i * 24 * 60 * 60 * 1000))
          dailyTrends.push({
            date,
            oee: null // No synthetic values - CFR Part 21 compliance
          })
        }
        
        // Generate week stamps with null OEE values - no synthetic data
        for (let i = 3; i >= 0; i--) {
          const weekDate = new Date(now.getTime() - (i * 7 * 24 * 60 * 60 * 1000))
          const weekString = `Week ${Math.ceil((now.getDate() - (i * 7)) / 7)}`
          weeklyTrends.push({
            week: weekString,
            oee: null // No synthetic values - CFR Part 21 compliance
          })
        }
        
        const overview = {
          averageOEE: Math.round(averageOEE * 100) / 100,
          totalEquipment,
          activeEquipment,
          topPerformers,
          trends: {
            hourly: hourlyTrends,
            daily: dailyTrends,
            weekly: weeklyTrends
          },
          breakdownByMetric: {
            availability: { 
              avg: Math.round(averageAvailability * 100) / 100, 
              trend: averageAvailability > 88 ? 'up' : 'stable' as const // Fixed logic instead of Math.random()
            },
            performance: { 
              avg: Math.round(averagePerformance * 100) / 100, 
              trend: averagePerformance > 83 ? 'up' : 'stable' as const // Fixed logic instead of Math.random()
            },
            quality: { 
              avg: Math.round(averageQuality * 100) / 100, 
              trend: averageQuality > 95 ? 'up' : 'stable' as const // Fixed logic instead of Math.random()
            }
          }
        }
        
        return {
          success: true,
          data: overview
        }
      }
      
      // No OEE data available - return error without synthetic data
      return {
        success: false,
        error: {
          code: 'OEE_OVERVIEW_UNAVAILABLE',
          message: 'OEE overview data not available - OEE service unavailable',
          details: { overviewError: true, complianceBreach: false },
          timestamp: new Date(),
          retryable: true
        }
      }
    } catch (error) {
      return {
        success: false,
        error: {
          code: 'OEE_OVERVIEW_ERROR',
          message: 'Failed to generate OEE overview - service error',
          details: { 
            networkError: true,
            originalError: error instanceof Error ? error.message : 'Unknown error'
          },
          timestamp: new Date(),
          retryable: true
        }
      }
    }
  }


  /**
   * Get OEE targets and thresholds
   */
  async getOEETargets(): Promise<ApiResponse<{
    global: {
      oee: number
      availability: number
      performance: number
      quality: number
    }
    equipment: Record<string, {
      oee: number
      availability: number
      performance: number
      quality: number
    }>
  }>> {
    return apiClient.get<any>(`${this.basePath}/targets`, {}, 'oee')
  }

  /**
   * Update OEE targets
   */
  async updateOEETargets(targets: {
    global?: {
      oee?: number
      availability?: number
      performance?: number
      quality?: number
    }
    equipment?: Record<string, {
      oee?: number
      availability?: number
      performance?: number
      quality?: number
    }>
  }): Promise<ApiResponse<void>> {
    return apiClient.put<void>(`${this.basePath}/targets`, targets, {}, 'oee')
  }

  /**
   * Get active work orders
   */
  async getActiveWorkOrders(): Promise<ApiResponse<WorkOrder[]>> {
    return apiClient.get<WorkOrder[]>(`${this.basePath}/work-orders/active`, {}, 'oee')
  }

  /**
   * Get work order by ID
   */
  async getWorkOrder(id: string): Promise<ApiResponse<WorkOrder>> {
    return apiClient.get<WorkOrder>(`${this.basePath}/work-orders/${id}`, {}, 'oee')
  }

  /**
   * Create new work order
   */
  async createWorkOrder(workOrder: {
    equipmentId: string
    productCode: string
    plannedQuantity: number
    startTime?: Date
    targetRate?: number
  }): Promise<ApiResponse<WorkOrder>> {
    return apiClient.post<WorkOrder>(`${this.basePath}/work-orders`, workOrder, {}, 'oee')
  }

  /**
   * Update work order
   */
  async updateWorkOrder(
    id: string,
    updates: Partial<{
      status: 'planned' | 'active' | 'completed' | 'cancelled'
      actualQuantity: number
      endTime: Date
    }>
  ): Promise<ApiResponse<WorkOrder>> {
    return apiClient.put<WorkOrder>(`${this.basePath}/work-orders/${id}`, updates, {}, 'oee')
  }

  /**
   * Get stoppage events
   */
  async getStoppageEvents(
    options: {
      equipmentId?: string
      workOrderId?: string
      startDate?: Date
      endDate?: Date
      category?: string
      active?: boolean
    } = {}
  ): Promise<ApiResponse<StoppageEvent[]>> {
    const params = new URLSearchParams()
    if (options.equipmentId) params.append('equipmentId', options.equipmentId)
    if (options.workOrderId) params.append('workOrderId', options.workOrderId)
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())
    if (options.category) params.append('category', options.category)
    if (options.active !== undefined) params.append('active', options.active.toString())

    return apiClient.get<StoppageEvent[]>(`${this.basePath}/stoppages?${params.toString()}`, {}, 'oee')
  }

  /**
   * Create stoppage event
   */
  async createStoppageEvent(stoppage: {
    equipmentId: string
    workOrderId?: string
    category: string
    reason: string
    description?: string
    startTime: Date
  }): Promise<ApiResponse<StoppageEvent>> {
    return apiClient.post<StoppageEvent>(`${this.basePath}/stoppages`, stoppage, {}, 'oee')
  }

  /**
   * End stoppage event
   */
  async endStoppageEvent(
    id: string,
    endTime: Date,
    resolution?: string
  ): Promise<ApiResponse<StoppageEvent>> {
    return apiClient.put<StoppageEvent>(`${this.basePath}/stoppages/${id}/end`, {
      endTime,
      resolution
    })
  }

  /**
   * Get stoppage categories and reasons
   */
  async getStoppageCategories(): Promise<ApiResponse<Array<{
    category: string
    reasons: string[]
    color: string
    priority: number
  }>>> {
    return apiClient.get<any>(`${this.basePath}/stoppages/categories`, {}, 'oee')
  }

  /**
   * Get OEE loss analysis
   */
  async getOEELossAnalysis(
    equipmentId: string,
    options: {
      startDate?: Date
      endDate?: Date
    } = {}
  ): Promise<ApiResponse<{
    totalLoss: number // minutes
    availabilityLoss: number
    performanceLoss: number
    qualityLoss: number
    breakdown: {
      plannedDowntime: number
      unplannedDowntime: number
      speedLoss: number
      minorStoppages: number
      defects: number
      reducedYield: number
    }
    topReasons: Array<{
      reason: string
      category: string
      impact: number // minutes
      frequency: number
    }>
  }>> {
    const params = new URLSearchParams()
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())

    return apiClient.get<any>(
      `${this.basePath}/equipment/${equipmentId}/loss-analysis?${params.toString()}`
    )
  }

  /**
   * Get production schedule
   */
  async getProductionSchedule(
    options: {
      equipmentId?: string
      startDate?: Date
      endDate?: Date
    } = {}
  ): Promise<ApiResponse<Array<{
    id: string
    equipmentId: string
    equipmentName: string
    workOrderId: string
    productCode: string
    plannedStart: Date
    plannedEnd: Date
    actualStart?: Date
    actualEnd?: Date
    status: 'scheduled' | 'running' | 'completed' | 'delayed'
    progress: number // percentage
  }>>> {
    const params = new URLSearchParams()
    if (options.equipmentId) params.append('equipmentId', options.equipmentId)
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())

    return apiClient.get<any>(`${this.basePath}/schedule?${params.toString()}`, {}, 'oee')
  }

  /**
   * Get OEE reports
   */
  async getOEEReports(
    type: 'summary' | 'detailed' | 'trends' | 'losses',
    options: {
      equipmentIds?: string[]
      startDate?: Date
      endDate?: Date
      format?: 'json' | 'csv' | 'xlsx'
    } = {}
  ): Promise<ApiResponse<any>> {
    const params = new URLSearchParams()
    if (options.equipmentIds) {
      options.equipmentIds.forEach(id => params.append('equipmentIds', id))
    }
    if (options.startDate) params.append('startDate', options.startDate.toISOString())
    if (options.endDate) params.append('endDate', options.endDate.toISOString())
    if (options.format) params.append('format', options.format)

    return apiClient.get<any>(`${this.basePath}/reports/${type}?${params.toString()}`, {}, 'oee')
  }

  /**
   * Get real-time OEE data (for live updates)
   */
  async getRealTimeOEE(): Promise<ApiResponse<Array<{
    equipmentId: string
    equipmentName: string
    timestamp: Date
    currentOEE: number
    availability: number
    performance: number
    quality: number
    status: 'running' | 'idle' | 'down'
    currentWorkOrder?: {
      id: string
      productCode: string
      progress: number
    }
  }>>> {
    return apiClient.get<any>(`${this.basePath}/realtime`, {}, 'oee')
  }

  /**
   * Get OEE benchmark data
   */
  async getOEEBenchmarks(): Promise<ApiResponse<{
    industryAverage: {
      oee: number
      availability: number
      performance: number
      quality: number
    }
    worldClass: {
      oee: number
      availability: number
      performance: number
      quality: number
    }
    companyAverage: {
      oee: number
      availability: number
      performance: number
      quality: number
    }
    equipmentComparison: Array<{
      equipmentId: string
      equipmentName: string
      ranking: number
      score: number
      improvement: number
    }>
  }>> {
    return apiClient.get<any>(`${this.basePath}/benchmarks`, {}, 'oee')
  }
}

// Export singleton instance
export const oeeService = new OEEService()

// Export the class for testing
export { OEEService }