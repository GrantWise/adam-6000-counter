import { type NextRequest, NextResponse } from "next/server"
import { createOeeCalculator, formatOeeMetrics } from "@/lib/calculations/oeeCalculator"
import { queryDatabase, queryDatabaseSingle } from "@/lib/database/connection"
import { CurrentMetricsResponse, ProductionJob, StoppageInfo } from "@/lib/types/oee"
import { validateQuery, getCurrentMetricsSchema } from "@/lib/validation/schemas"
import { withAuth } from "@/lib/auth/middleware"
import { calculateOeeUseCase } from "@/lib/usecases/CalculateOeeUseCase"
import { channels } from "@/config"

// GET /api/metrics/current - Get current performance metrics using real OEE calculator
export const GET = withAuth(async (request: NextRequest, user) => {
  try {
    const { searchParams } = new URL(request.url)
    
    // Validate query parameters
    const queryParams = Object.fromEntries(searchParams.entries())
    const { deviceId: validatedDeviceId } = validateQuery(getCurrentMetricsSchema)(queryParams)
    const deviceId = validatedDeviceId || process.env.DEFAULT_DEVICE_ID || 'Device001'

    // Calculate OEE using domain models
    const calculator = createOeeCalculator({ deviceId })
    const oeeCalculation = await calculator.calculateCurrentOEE(deviceId)
    
    // Get formatted metrics from domain model
    const formattedMetrics = {
      availabilityPercent: Math.round(oeeCalculation.availability_percentage * 10) / 10,
      performancePercent: Math.round(oeeCalculation.performance_percentage * 10) / 10,
      qualityPercent: Math.round(oeeCalculation.quality_percentage * 10) / 10,
      oeePercent: Math.round(oeeCalculation.oee_percentage * 10) / 10
    }

    // Get current job information
    const currentJob = await queryDatabaseSingle<ProductionJob>(
      `SELECT 
        job_id,
        job_number,
        part_number,
        device_id,
        target_rate,
        start_time,
        operator_id
      FROM production_jobs
      WHERE device_id = $1 AND status = 'active'
      ORDER BY start_time DESC
      LIMIT 1`,
      [deviceId]
    )

    // Get current rate from latest counter data
    const currentRateResult = await queryDatabaseSingle<{ units_per_minute: number; timestamp: string }>(
      `SELECT 
        rate * 60 as units_per_minute,
        timestamp
      FROM counter_data 
      WHERE 
        device_id = $1 
        AND channel = $2
        AND timestamp > NOW() - INTERVAL '2 minutes'
      ORDER BY timestamp DESC 
      LIMIT 1`,
      [deviceId, channels.production()]
    )

    // Check for active stoppage
    const activeStoppage = await calculator.detectCurrentStoppage(deviceId)
    
    // Determine machine status
    const currentRate = currentRateResult?.units_per_minute || 0
    const targetRate = currentJob?.target_rate || 100
    let status: 'running' | 'stopped' | 'error' = 'stopped'
    
    if (currentRate > 0.1) {
      status = 'running'
    } else if (activeStoppage) {
      status = 'stopped'
    }

    const response: CurrentMetricsResponse = {
      currentRate: Math.round(currentRate * 10) / 10,
      targetRate,
      performancePercent: formattedMetrics.performancePercent,
      qualityPercent: formattedMetrics.qualityPercent,
      availabilityPercent: formattedMetrics.availabilityPercent,
      oeePercent: formattedMetrics.oeePercent,
      status,
      lastUpdate: new Date().toISOString(),
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Error calculating current metrics:", error)
    
    // Handle validation errors specifically
    if (error instanceof Error && error.message.includes('validation error')) {
      return NextResponse.json({ error: error.message }, { status: 400 })
    }
    
    // Return error response with fallback metrics
    const fallbackResponse: CurrentMetricsResponse = {
      currentRate: 0,
      targetRate: 100,
      performancePercent: 0,
      qualityPercent: 100,
      availabilityPercent: 0,
      oeePercent: 0,
      status: 'error',
      lastUpdate: new Date().toISOString(),
    }
    
    return NextResponse.json(fallbackResponse, { status: 500 })
  }
})