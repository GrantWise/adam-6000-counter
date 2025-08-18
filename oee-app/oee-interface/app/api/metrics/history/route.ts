import { type NextRequest, NextResponse } from "next/server"
import { queryDatabase } from "@/lib/database/connection"
import { HistoricalMetricsPoint } from "@/lib/types/oee"
import { channels, config } from "@/config"

// GET /api/metrics/history - Get rate history for chart using real TimescaleDB data with pagination
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const deviceId = searchParams.get("deviceId") || config.app.device.defaultId
    const hours = Number(searchParams.get("hours")) || config.oee.timeRanges.chartHistory.defaultHours
    
    // Phase 4: Add pagination support for large datasets
    const page = Math.max(1, Number(searchParams.get("page")) || 1)
    const limit = Math.min(1000, Number(searchParams.get("limit")) || 500) // Max 1000 points per request
    const offset = (page - 1) * limit

    // Get total count for pagination metadata
    const countQuery = `
      SELECT COUNT(*) as total_count
      FROM (
        SELECT time_bucket('5 minutes', timestamp) AS time_bucket
        FROM counter_data
        WHERE 
          device_id = $1
          AND channel = $2
          AND timestamp >= NOW() - INTERVAL '${hours} hours'
        GROUP BY time_bucket
      ) grouped_data`

    const countResult = await queryDatabase<{ total_count: number }>(countQuery, [deviceId, channels.production()])
    const totalCount = Number(countResult[0]?.total_count) || 0

    // Get rate history from counter_data table with pagination
    const historyQuery = `
      SELECT 
        time_bucket('5 minutes', timestamp) AS time_bucket,
        AVG(rate) * 60 as avg_rate_per_minute,
        MIN(timestamp) as timestamp
      FROM counter_data
      WHERE 
        device_id = $1
        AND channel = $2
        AND timestamp >= NOW() - INTERVAL '${hours} hours'
      GROUP BY time_bucket
      ORDER BY time_bucket ASC
      LIMIT $3 OFFSET $4`

    const historyData = await queryDatabase<{
      time_bucket: string
      avg_rate_per_minute: number
      timestamp: string
    }>(historyQuery, [deviceId, channels.production(), limit, offset])

    // Get target rate from current active job
    const targetRateQuery = `
      SELECT target_rate
      FROM production_jobs
      WHERE device_id = $1 AND status = 'active'
      ORDER BY start_time DESC
      LIMIT 1`

    const targetRateResult = await queryDatabase<{ target_rate: number }>(targetRateQuery, [deviceId])
    const targetRate = targetRateResult[0]?.target_rate || 100

    // Transform data for chart
    const timestamps = historyData.map(point => new Date(point.timestamp).toISOString())
    const rates = historyData.map(point => Math.round((point.avg_rate_per_minute || 0) * 10) / 10)

    // Calculate pagination metadata
    const totalPages = Math.ceil(totalCount / limit)
    const hasNextPage = page < totalPages
    const hasPreviousPage = page > 1

    // If no data available, generate minimal response
    if (historyData.length === 0) {
      const now = new Date()
      return NextResponse.json({
        data: {
          timestamps: [now.toISOString()],
          rates: [0],
          targetRate
        },
        pagination: {
          page,
          limit,
          totalCount,
          totalPages,
          hasNextPage: false,
          hasPreviousPage: false
        }
      })
    }

    const response = {
      data: {
        timestamps,
        rates,
        targetRate
      },
      pagination: {
        page,
        limit,
        totalCount,
        totalPages,
        hasNextPage,
        hasPreviousPage
      }
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Error getting metrics history:", error)
    
    // Return fallback data on error
    const now = new Date()
    const fallbackResponse = {
      data: {
        timestamps: [now.toISOString()],
        rates: [0],
        targetRate: 100
      },
      pagination: {
        page: 1,
        limit: 500,
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
      }
    }
    
    return NextResponse.json(fallbackResponse, { status: 500 })
  }
}