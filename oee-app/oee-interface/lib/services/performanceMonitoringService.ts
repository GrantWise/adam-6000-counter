import { PerformanceMetrics } from '@/lib/types/oee'

/**
 * Performance Monitoring Service for Phase 4
 * Tracks key metrics for OEE calculation and query performance
 * 
 * Design principle: Pragmatic monitoring that provides clear value
 * Focuses on the success metrics from the plan:
 * - OEE calculation < 100ms
 * - Page load < 2 seconds  
 * - 99% uptime achieved
 */
export class PerformanceMonitoringService {
  private static instance: PerformanceMonitoringService | null = null
  
  // Performance metrics storage
  private oeeCalculationTimes: number[] = []
  private queryDurations: number[] = []
  private queryCount = 0
  private cacheHits = 0
  private cacheMisses = 0
  private slowQueries = 0
  private readonly slowQueryThreshold = 500 // 500ms
  private readonly oeeTargetTime = 100 // 100ms target
  private readonly maxStoredMetrics = 1000 // Keep last 1000 measurements
  
  // Service start time for uptime calculation
  private readonly startTime = Date.now()
  
  private constructor() {}
  
  /**
   * Get singleton instance
   */
  static getInstance(): PerformanceMonitoringService {
    if (!PerformanceMonitoringService.instance) {
      PerformanceMonitoringService.instance = new PerformanceMonitoringService()
    }
    return PerformanceMonitoringService.instance
  }
  
  /**
   * Record OEE calculation time (key metric from plan)
   */
  recordOeeCalculationTime(durationMs: number): void {
    this.oeeCalculationTimes.push(durationMs)
    
    // Keep only recent measurements to prevent memory bloat
    if (this.oeeCalculationTimes.length > this.maxStoredMetrics) {
      this.oeeCalculationTimes.shift()
    }
    
    // Log warning if OEE calculation exceeds target
    if (durationMs > this.oeeTargetTime) {
      console.warn(`OEE calculation took ${durationMs}ms (target: ${this.oeeTargetTime}ms)`)
    }
  }
  
  /**
   * Record database query duration
   */
  recordQueryDuration(durationMs: number): void {
    this.queryCount++
    this.queryDurations.push(durationMs)
    
    // Track slow queries
    if (durationMs > this.slowQueryThreshold) {
      this.slowQueries++
      console.warn(`Slow query detected: ${durationMs}ms (threshold: ${this.slowQueryThreshold}ms)`)
    }
    
    // Keep only recent measurements
    if (this.queryDurations.length > this.maxStoredMetrics) {
      this.queryDurations.shift()
    }
  }
  
  /**
   * Record cache hit for performance tracking
   */
  recordCacheHit(): void {
    this.cacheHits++
  }
  
  /**
   * Record cache miss
   */
  recordCacheMiss(): void {
    this.cacheMisses++
  }
  
  /**
   * Get current performance metrics
   */
  getPerformanceMetrics(): PerformanceMetrics {
    const now = Date.now()
    const uptimeSeconds = Math.floor((now - this.startTime) / 1000)
    
    // Calculate averages
    const avgOeeTime = this.oeeCalculationTimes.length > 0 
      ? this.oeeCalculationTimes.reduce((sum, time) => sum + time, 0) / this.oeeCalculationTimes.length
      : 0
      
    const avgQueryTime = this.queryDurations.length > 0
      ? this.queryDurations.reduce((sum, time) => sum + time, 0) / this.queryDurations.length
      : 0
      
    const totalCacheOperations = this.cacheHits + this.cacheMisses
    const cacheHitRate = totalCacheOperations > 0 ? this.cacheHits / totalCacheOperations : 0
    
    return {
      averageOeeCalculationTime: Math.round(avgOeeTime * 10) / 10,
      averageQueryDuration: Math.round(avgQueryTime * 10) / 10,
      queryCount: this.queryCount,
      cacheHitRate: Math.round(cacheHitRate * 1000) / 1000, // 3 decimal places
      slowQueries: this.slowQueries,
      uptimeSeconds,
      peakMemoryUsage: this.getPeakMemoryUsage(),
      activeConnections: this.getActiveConnectionCount()
    }
  }
  
  /**
   * Get performance summary for health checks
   */
  getPerformanceSummary(): {
    oeePerformance: 'excellent' | 'good' | 'needs-improvement'
    queryPerformance: 'excellent' | 'good' | 'needs-improvement'
    overallHealth: 'healthy' | 'warning' | 'error'
    alerts: string[]
  } {
    const metrics = this.getPerformanceMetrics()
    const alerts: string[] = []
    
    // Evaluate OEE calculation performance against target (100ms)
    let oeePerformance: 'excellent' | 'good' | 'needs-improvement' = 'excellent'
    if (metrics.averageOeeCalculationTime > this.oeeTargetTime * 1.5) {
      oeePerformance = 'needs-improvement'
      alerts.push(`OEE calculations averaging ${metrics.averageOeeCalculationTime}ms (target: ${this.oeeTargetTime}ms)`)
    } else if (metrics.averageOeeCalculationTime > this.oeeTargetTime) {
      oeePerformance = 'good'
    }
    
    // Evaluate query performance
    let queryPerformance: 'excellent' | 'good' | 'needs-improvement' = 'excellent'
    if (metrics.averageQueryDuration > this.slowQueryThreshold) {
      queryPerformance = 'needs-improvement'
      alerts.push(`Database queries averaging ${metrics.averageQueryDuration}ms (threshold: ${this.slowQueryThreshold}ms)`)
    } else if (metrics.averageQueryDuration > this.slowQueryThreshold * 0.7) {
      queryPerformance = 'good'
    }
    
    // Check for slow query count
    const slowQueryRate = metrics.queryCount > 0 ? metrics.slowQueries / metrics.queryCount : 0
    if (slowQueryRate > 0.1) { // More than 10% slow queries
      alerts.push(`${Math.round(slowQueryRate * 100)}% of queries are slow (${metrics.slowQueries}/${metrics.queryCount})`)
    }
    
    // Check cache performance
    if (metrics.cacheHitRate < 0.8) { // Less than 80% cache hit rate
      alerts.push(`Low cache hit rate: ${Math.round(metrics.cacheHitRate * 100)}%`)
    }
    
    // Determine overall health
    let overallHealth: 'healthy' | 'warning' | 'error' = 'healthy'
    if (oeePerformance === 'needs-improvement' || queryPerformance === 'needs-improvement') {
      overallHealth = 'error'
    } else if (oeePerformance === 'good' || queryPerformance === 'good' || alerts.length > 0) {
      overallHealth = 'warning'
    }
    
    return {
      oeePerformance,
      queryPerformance,
      overallHealth,
      alerts
    }
  }
  
  /**
   * Get recent OEE calculation times for trend analysis
   */
  getRecentOeeCalculationTimes(count: number = 10): number[] {
    return this.oeeCalculationTimes.slice(-count)
  }
  
  /**
   * Get recent query times for analysis
   */
  getRecentQueryTimes(count: number = 10): number[] {
    return this.queryDurations.slice(-count)
  }
  
  /**
   * Check if performance meets plan success criteria
   */
  checkSuccessCriteria(): {
    oeeCalculationTarget: boolean // < 100ms
    criteriasMet: boolean
    performance: PerformanceMetrics
  } {
    const metrics = this.getPerformanceMetrics()
    
    const oeeCalculationTarget = metrics.averageOeeCalculationTime < this.oeeTargetTime
    
    return {
      oeeCalculationTarget,
      criteriasMet: oeeCalculationTarget,
      performance: metrics
    }
  }
  
  /**
   * Reset performance counters (for testing or maintenance)
   */
  resetCounters(): void {
    this.oeeCalculationTimes = []
    this.queryDurations = []
    this.queryCount = 0
    this.cacheHits = 0
    this.cacheMisses = 0
    this.slowQueries = 0
    console.log('Performance monitoring counters reset')
  }
  
  /**
   * Get peak memory usage (Node.js specific)
   */
  private getPeakMemoryUsage(): number | undefined {
    try {
      const memUsage = process.memoryUsage()
      return memUsage.heapUsed
    } catch (error) {
      return undefined
    }
  }
  
  /**
   * Get active connection count (placeholder for actual implementation)
   */
  private getActiveConnectionCount(): number | undefined {
    // In a real implementation, this would query the database pool
    // or connection manager for active connection count
    return undefined
  }
}

/**
 * Global helper functions for easy integration
 */

/**
 * Time an OEE calculation and record the duration
 */
export async function timeOeeCalculation<T>(
  operation: () => Promise<T>
): Promise<T> {
  const start = Date.now()
  try {
    const result = await operation()
    const duration = Date.now() - start
    PerformanceMonitoringService.getInstance().recordOeeCalculationTime(duration)
    return result
  } catch (error) {
    const duration = Date.now() - start
    PerformanceMonitoringService.getInstance().recordOeeCalculationTime(duration)
    throw error
  }
}

/**
 * Time a database query and record the duration
 */
export async function timeQueryExecution<T>(
  operation: () => Promise<T>
): Promise<T> {
  const start = Date.now()
  try {
    const result = await operation()
    const duration = Date.now() - start
    PerformanceMonitoringService.getInstance().recordQueryDuration(duration)
    return result
  } catch (error) {
    const duration = Date.now() - start
    PerformanceMonitoringService.getInstance().recordQueryDuration(duration)
    throw error
  }
}

/**
 * Get global performance monitoring instance
 */
export function getPerformanceMonitoring(): PerformanceMonitoringService {
  return PerformanceMonitoringService.getInstance()
}