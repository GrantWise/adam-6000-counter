import { NextResponse } from "next/server"
import { testDatabaseConnection, getDatabaseCircuitBreakerStatus } from "@/lib/database/connection"
import { checkConnectionHealth, getApiCircuitBreakerStatus, dbCircuitBreaker } from "@/lib/database/connectionResilience"
import { DatabaseFallbackStrategies } from "@/lib/database/fallbackStrategies"
import { getPerformanceMonitoring } from "@/lib/services/performanceMonitoringService"

// GET /api/health/detailed - Comprehensive health check with circuit breaker details
export async function GET() {
  try {
    const startTime = Date.now();
    
    // Test database connectivity
    const dbConnectionTest = await testDatabaseConnection();
    const dbHealthCheck = await checkConnectionHealth();
    
    // Get circuit breaker statuses
    const dbCircuitBreakerStatus = getDatabaseCircuitBreakerStatus();
    const apiCircuitBreakerStatus = getApiCircuitBreakerStatus();
    
    // Get fallback cache status
    const fallbackCacheStatus = DatabaseFallbackStrategies.getFallbackCacheStatus();
    
    // Get performance metrics
    const performanceService = getPerformanceMonitoring()
    const performanceMetrics = performanceService.getPerformanceMetrics()
    const performanceSummary = performanceService.getPerformanceSummary()
    const successCriteria = performanceService.checkSuccessCriteria()
    
    // Determine overall system health including performance
    const isSystemHealthy = dbConnectionTest.isConnected && 
                          dbCircuitBreakerStatus.isHealthy && 
                          apiCircuitBreakerStatus.isHealthy &&
                          performanceSummary.overallHealth !== 'error';
    
    const healthCheckDuration = Date.now() - startTime;
    
    const detailedHealth = {
      timestamp: new Date().toISOString(),
      overall: {
        status: isSystemHealthy ? 'healthy' : 'degraded',
        healthCheckDuration: `${healthCheckDuration}ms`,
        fallbackMode: !isSystemHealthy
      },
      database: {
        connectivity: {
          isConnected: dbConnectionTest.isConnected,
          latencyMs: dbConnectionTest.latencyMs,
          error: dbConnectionTest.error,
          circuitBreakerState: dbConnectionTest.circuitBreakerState
        },
        resilience: {
          healthCheck: dbHealthCheck,
          circuitBreaker: {
            ...dbCircuitBreakerStatus,
            description: dbCircuitBreakerStatus.isHealthy 
              ? 'Circuit breaker is closed - database operations are normal'
              : 'Circuit breaker is open - database operations are blocked, using fallback strategies'
          }
        }
      },
      api: {
        circuitBreaker: {
          ...apiCircuitBreakerStatus,
          description: apiCircuitBreakerStatus.isHealthy
            ? 'API circuit breaker is closed - external API calls are normal'
            : 'API circuit breaker is open - external API calls are blocked, using fallback strategies'
        }
      },
      fallback: {
        cache: fallbackCacheStatus,
        strategies: {
          available: true,
          description: 'Fallback strategies are available for degraded operation when circuit breakers are open'
        }
      },
      performance: {
        metrics: performanceMetrics,
        summary: performanceSummary,
        successCriteria: {
          oeeCalculationUnder100ms: successCriteria.oeeCalculationTarget,
          allCriteriaMet: successCriteria.criteriasMet
        },
        targets: {
          oeeCalculationTime: '< 100ms',
          pageLoadTime: '< 2 seconds',
          uptime: '99%'
        }
      },
      industrialReliability: {
        designedFor: 'Industrial environments with network instability',
        features: [
          'Circuit breaker protection for database and API calls',
          'Automatic retry with exponential backoff',
          'In-memory fallback cache for critical operations',
          'Graceful degradation when services are unavailable',
          'Health monitoring with automatic recovery attempts',
          'Resilient WebSocket connections with auto-reconnection',
          'Real-time data fallback mechanisms',
          'Performance monitoring and alerting'
        ],
        configuration: {
          databaseCircuitBreaker: {
            failureThreshold: 5,
            recoveryTimeout: '30 seconds'
          },
          apiCircuitBreaker: {
            failureThreshold: 3,
            recoveryTimeout: '60 seconds'
          },
          retryPolicy: {
            maxAttempts: 3,
            baseDelay: '1 second',
            exponentialBackoff: true
          },
          performanceTargets: {
            oeeCalculation: '100ms',
            slowQueryThreshold: '500ms',
            cacheHitRateTarget: '80%'
          }
        }
      }
    };
    
    // Determine HTTP status code
    let httpStatus = 200;
    if (!isSystemHealthy) {
      // If we have fallback capabilities, it's still a 200 but degraded
      // If no fallback, it would be 503
      httpStatus = fallbackCacheStatus.activeJobsCount > 0 || fallbackCacheStatus.readingsCount > 0 ? 200 : 503;
    }
    
    return NextResponse.json(detailedHealth, { 
      status: httpStatus,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      }
    });
    
  } catch (error) {
    console.error("Detailed health check error:", error);
    
    // Return comprehensive error information
    const errorHealth = {
      timestamp: new Date().toISOString(),
      overall: {
        status: 'error',
        error: error instanceof Error ? error.message : 'Health check failed'
      },
      database: {
        connectivity: {
          isConnected: false,
          error: 'Health check failed'
        },
        resilience: {
          circuitBreaker: getDatabaseCircuitBreakerStatus()
        }
      },
      api: {
        circuitBreaker: getApiCircuitBreakerStatus()
      },
      fallback: {
        cache: DatabaseFallbackStrategies.getFallbackCacheStatus(),
        strategies: {
          available: true,
          active: true,
          description: 'System running in full fallback mode due to health check failure'
        }
      }
    };
    
    return NextResponse.json(errorHealth, { 
      status: 503,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      }
    });
  }
}

// POST /api/health/detailed/test - Test circuit breaker functionality  
export async function POST(request: Request) {
  try {
    const { searchParams } = new URL(request.url);
    const action = searchParams.get('action');
    
    switch (action) {
      case 'reset-db-circuit-breaker':
        dbCircuitBreaker.reset();
        return NextResponse.json({
          message: 'Database circuit breaker reset successfully',
          status: getDatabaseCircuitBreakerStatus()
        });
        
      case 'clear-fallback-cache':
        DatabaseFallbackStrategies.clearFallbackCache();
        return NextResponse.json({
          message: 'Fallback cache cleared successfully',
          status: DatabaseFallbackStrategies.getFallbackCacheStatus()
        });
        
      case 'test-database-connection':
        const dbTest = await testDatabaseConnection();
        return NextResponse.json({
          message: 'Database connection test completed',
          result: dbTest
        });
        
      default:
        return NextResponse.json({
          error: 'Invalid action. Available actions: reset-db-circuit-breaker, clear-fallback-cache, test-database-connection'
        }, { status: 400 });
    }
    
  } catch (error) {
    console.error("Health test error:", error);
    return NextResponse.json(
      { error: "Health test failed", details: error instanceof Error ? error.message : 'Unknown error' }, 
      { status: 500 }
    );
  }
}