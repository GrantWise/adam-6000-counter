import { createOeeService } from '@/lib/services/oeeService'
import { OperationResult, OEEMetrics } from '@/lib/types/oee'
import { config, getRating, requiresAttention, fromMinutes } from '@/config'

/**
 * Use case for calculating OEE metrics
 * Encapsulates business logic for OEE calculation and validation
 */
export class CalculateOeeUseCase {
  async execute(request: {
    deviceId: string
    jobId?: string
    timeRange?: {
      startTime: Date
      endTime: Date
    }
  }): Promise<OperationResult<OEEMetrics>> {
    try {
      // Create OEE service for the device
      const oeeService = createOeeService(request.deviceId)
      
      // Additional business logic validations
      const validationResult = await this.validateCalculationRequest(request)
      if (!validationResult.isSuccess) {
        return validationResult
      }

      // Calculate OEE metrics
      const result = request.timeRange 
        ? await oeeService.calculateOeeForTimeRange(
            request.deviceId, 
            request.timeRange.startTime, 
            request.timeRange.endTime
          )
        : await oeeService.getCurrentOee(request.deviceId, request.jobId)
      
      if (!result.isSuccess) {
        return {
          isSuccess: false,
          value: {} as OEEMetrics,
          errorMessage: result.errorMessage
        }
      }

      // Apply business rules and transformations
      const enhancedMetrics = await this.enhanceMetricsWithBusinessLogic(result.value)

      return {
        isSuccess: true,
        value: enhancedMetrics,
        errorMessage: null
      }
    } catch (error) {
      return {
        isSuccess: false,
        value: {} as OEEMetrics,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }

  private async validateCalculationRequest(request: {
    deviceId: string
    jobId?: string
    timeRange?: {
      startTime: Date
      endTime: Date
    }
  }): Promise<OperationResult<void>> {
    // Business rule: Device ID is required
    if (!request.deviceId || request.deviceId.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Device ID is required for OEE calculation'
      }
    }

    // Business rule: Time range validation if provided
    if (request.timeRange) {
      const { startTime, endTime } = request.timeRange
      
      if (startTime >= endTime) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Start time must be before end time'
        }
      }

      // Business rule: Time range cannot exceed configured max for performance
      const maxRangeMs = fromMinutes(config.oee.timeRanges.maxQueryRange.minutes)
      if (endTime.getTime() - startTime.getTime() > maxRangeMs) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: `Time range cannot exceed ${config.oee.timeRanges.maxQueryRange.days} days`
        }
      }

      // Business rule: Cannot calculate OEE for future dates
      if (endTime > new Date()) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Cannot calculate OEE for future dates'
        }
      }
    }

    return {
      isSuccess: true,
      value: undefined,
      errorMessage: null
    }
  }

  private async enhanceMetricsWithBusinessLogic(rawMetrics: OEEMetrics): Promise<OEEMetrics> {
    // Business rule: Apply thresholds and warnings
    const enhancedMetrics = { ...rawMetrics }

    // Set performance indicators based on configured thresholds
    enhancedMetrics.availabilityRating = getRating(rawMetrics.availability * 100, 'availability')
    enhancedMetrics.performanceRating = getRating(rawMetrics.performance * 100, 'performance')
    enhancedMetrics.qualityRating = getRating(rawMetrics.quality * 100, 'quality')
    enhancedMetrics.oeeRating = getRating(rawMetrics.oee * 100, 'oee')

    // Business rule: Flag metrics requiring attention based on config
    enhancedMetrics.requiresAttention = requiresAttention({
      oee: rawMetrics.oee * 100,
      availability: rawMetrics.availability * 100,
      performance: rawMetrics.performance * 100,
      quality: rawMetrics.quality * 100
    })

    // Business rule: Calculate trend indicators (simplified)
    enhancedMetrics.trend = this.calculateTrend(rawMetrics)

    return enhancedMetrics
  }

  private calculateTrend(metrics: OEEMetrics): 'improving' | 'stable' | 'declining' {
    // In a real implementation, this would compare with historical data
    // For now, return stable as default
    return 'stable'
  }
}

// Singleton instance
export const calculateOeeUseCase = new CalculateOeeUseCase()