import { NextResponse } from "next/server"
import { getPerformanceMonitoring } from "@/lib/services/performanceMonitoringService"
import { getQueryCache } from "@/lib/services/queryCacheService"

/**
 * Performance API Endpoint for Phase 4
 * Provides access to performance metrics and statistics
 */

// GET /api/performance - Get current performance metrics
export async function GET() {
  try {
    const performanceService = getPerformanceMonitoring()
    const cache = getQueryCache()
    
    // Get performance metrics
    const metrics = performanceService.getPerformanceMetrics()
    const summary = performanceService.getPerformanceSummary()
    const successCriteria = performanceService.checkSuccessCriteria()
    const cacheStats = cache.getStats()
    
    const response = {
      timestamp: new Date().toISOString(),
      metrics,
      summary,
      successCriteria: {
        oeeCalculationUnder100ms: successCriteria.oeeCalculationTarget,
        allCriteriaMet: successCriteria.criteriasMet,
        targets: {
          oeeCalculationTime: '< 100ms',
          pageLoadTime: '< 2 seconds',
          uptime: '99%'
        }
      },
      cache: cacheStats,
      recentTrends: {
        oeeCalculationTimes: performanceService.getRecentOeeCalculationTimes(20),
        queryTimes: performanceService.getRecentQueryTimes(20)
      }
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Performance API error:", error)
    
    const errorResponse = {
      timestamp: new Date().toISOString(),
      error: "Failed to retrieve performance metrics",
      details: error instanceof Error ? error.message : 'Unknown error'
    }
    
    return NextResponse.json(errorResponse, { status: 500 })
  }
}

// POST /api/performance/reset - Reset performance counters (for admin use)
export async function POST() {
  try {
    const performanceService = getPerformanceMonitoring()
    const cache = getQueryCache()
    
    // Reset counters
    performanceService.resetCounters()
    cache.clear()
    
    const response = {
      message: 'Performance counters and cache cleared successfully',
      timestamp: new Date().toISOString()
    }
    
    return NextResponse.json(response)
  } catch (error) {
    console.error("Performance reset error:", error)
    return NextResponse.json(
      { error: "Failed to reset performance counters" }, 
      { status: 500 }
    )
  }
}