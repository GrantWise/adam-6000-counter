import { NextResponse } from "next/server"
import { getMonitoringService } from "@/lib/services/monitoringService"
import { HealthStatus } from "@/lib/types/oee"
import { getDatabaseCircuitBreakerStatus } from "@/lib/database/connection"
import { getApiCircuitBreakerStatus, dbCircuitBreaker } from "@/lib/database/connectionResilience"
import { DatabaseFallbackStrategies } from "@/lib/database/fallbackStrategies"

// GET /api/health - Enhanced health check with monitoring service
export async function GET() {
  try {
    const monitoringService = getMonitoringService()
    
    // Perform comprehensive health check
    const healthStatus: HealthStatus = await monitoringService.performHealthCheck()
    
    // Determine appropriate HTTP status code
    let httpStatus = 200
    if (healthStatus.status === 'warning') {
      httpStatus = 200 // Still OK but with warnings
    } else if (healthStatus.status === 'error') {
      httpStatus = 503 // Service Unavailable
    }

    // Add comprehensive monitoring metadata including circuit breakers and fallback status
    const dbCircuitBreakerStatus = getDatabaseCircuitBreakerStatus();
    const apiCircuitBreakerStatus = getApiCircuitBreakerStatus();
    const fallbackCacheStatus = DatabaseFallbackStrategies.getFallbackCacheStatus();
    
    const response = {
      ...healthStatus,
      timestamp: new Date().toISOString(),
      monitoring: monitoringService.getMonitoringStats(),
      circuitBreakers: {
        database: dbCircuitBreakerStatus,
        api: apiCircuitBreakerStatus
      },
      fallbackCache: fallbackCacheStatus,
      resilience: {
        databaseResilience: dbCircuitBreakerStatus.isHealthy ? 'operational' : 'degraded',
        apiResilience: apiCircuitBreakerStatus.isHealthy ? 'operational' : 'degraded',
        fallbackMode: !dbCircuitBreakerStatus.isHealthy || !apiCircuitBreakerStatus.isHealthy
      }
    }

    return NextResponse.json(response, { 
      status: httpStatus,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      }
    })
  } catch (error) {
    console.error("Health check error:", error)
    
    // Return minimal error response
    const errorResponse: HealthStatus = {
      status: 'error',
      database: {
        isConnected: false,
        error: error instanceof Error ? error.message : 'Health check failed',
        lastCheck: new Date(),
      },
      dataAge: 0,
      version: process.env.npm_package_version || '1.0.0',
      uptime: process.uptime(),
    }
    
    return NextResponse.json({
      ...errorResponse,
      timestamp: new Date().toISOString(),
    }, { 
      status: 503,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      }
    })
  }
}

// POST /api/health/reset - Reset monitoring service (for admin use)
export async function POST() {
  try {
    const monitoringService = getMonitoringService()
    
    // Reset failure counters and circuit breakers
    monitoringService.resetFailureCounters()
    
    // Reset both circuit breakers
    dbCircuitBreaker.reset()
    
    // Clear fallback cache
    DatabaseFallbackStrategies.clearFallbackCache()
    
    // Force a new health check
    const healthStatus = await monitoringService.forceHealthCheck()
    
    return NextResponse.json({
      message: 'Monitoring service reset successfully',
      healthStatus,
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error("Health reset error:", error)
    return NextResponse.json(
      { error: "Failed to reset monitoring service" }, 
      { status: 500 }
    )
  }
}