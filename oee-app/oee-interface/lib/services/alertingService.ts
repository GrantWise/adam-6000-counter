import { getPerformanceMonitoring } from './performanceMonitoringService'
import { getQueryCache } from './queryCacheService'

/**
 * Alerting Service for Phase 4
 * Basic alerting for critical performance failures
 * 
 * Design principle: Only alert on things that provide clear value
 * Focus on critical issues that impact the success metrics:
 * - OEE calculation failures
 * - Database connectivity issues
 * - Severe performance degradation
 */

export enum AlertSeverity {
  INFO = 'info',
  WARNING = 'warning',
  ERROR = 'error',
  CRITICAL = 'critical'
}

export interface Alert {
  id: string
  severity: AlertSeverity
  title: string
  message: string
  timestamp: Date
  resolved: boolean
  resolvedAt?: Date
  source: string
  metadata?: Record<string, any>
}

export class AlertingService {
  private static instance: AlertingService | null = null
  
  // Active alerts storage
  private activeAlerts = new Map<string, Alert>()
  private alertHistory: Alert[] = []
  private readonly maxHistorySize = 1000
  
  // Alert thresholds
  private readonly oeeCalculationThreshold = 200 // 200ms
  private readonly queryDurationThreshold = 1000 // 1 second
  private readonly cacheHitRateThreshold = 0.5 // 50%
  private readonly slowQueryRateThreshold = 0.2 // 20%
  
  private constructor() {
    // Start monitoring for alerts every minute
    setInterval(() => this.checkForAlerts(), 60000)
  }
  
  /**
   * Get singleton instance
   */
  static getInstance(): AlertingService {
    if (!AlertingService.instance) {
      AlertingService.instance = new AlertingService()
    }
    return AlertingService.instance
  }
  
  /**
   * Check for alert conditions and fire alerts
   */
  private async checkForAlerts(): Promise<void> {
    try {
      const performanceService = getPerformanceMonitoring()
      const cache = getQueryCache()
      
      const metrics = performanceService.getPerformanceMetrics()
      const performanceSummary = performanceService.getPerformanceSummary()
      const cacheStats = cache.getStats()
      
      // Check OEE calculation performance
      if (metrics.averageOeeCalculationTime > this.oeeCalculationThreshold) {
        this.fireAlert({
          id: 'oee-calculation-slow',
          severity: AlertSeverity.WARNING,
          title: 'OEE Calculation Performance Degraded',
          message: `OEE calculations averaging ${Math.round(metrics.averageOeeCalculationTime)}ms (threshold: ${this.oeeCalculationThreshold}ms)`,
          source: 'performance-monitor',
          metadata: {
            actualTime: metrics.averageOeeCalculationTime,
            threshold: this.oeeCalculationThreshold,
            target: 100
          }
        })
      } else {
        this.resolveAlert('oee-calculation-slow')
      }
      
      // Check database query performance
      if (metrics.averageQueryDuration > this.queryDurationThreshold) {
        this.fireAlert({
          id: 'database-queries-slow',
          severity: AlertSeverity.WARNING,
          title: 'Database Query Performance Degraded',
          message: `Database queries averaging ${Math.round(metrics.averageQueryDuration)}ms (threshold: ${this.queryDurationThreshold}ms)`,
          source: 'performance-monitor',
          metadata: {
            actualTime: metrics.averageQueryDuration,
            threshold: this.queryDurationThreshold,
            queryCount: metrics.queryCount,
            slowQueries: metrics.slowQueries
          }
        })
      } else {
        this.resolveAlert('database-queries-slow')
      }
      
      // Check cache hit rate
      if (metrics.cacheHitRate < this.cacheHitRateThreshold) {
        this.fireAlert({
          id: 'cache-hit-rate-low',
          severity: AlertSeverity.WARNING,
          title: 'Low Cache Hit Rate',
          message: `Cache hit rate is ${Math.round(metrics.cacheHitRate * 100)}% (threshold: ${this.cacheHitRateThreshold * 100}%)`,
          source: 'cache-monitor',
          metadata: {
            hitRate: metrics.cacheHitRate,
            threshold: this.cacheHitRateThreshold,
            cacheStats
          }
        })
      } else {
        this.resolveAlert('cache-hit-rate-low')
      }
      
      // Check for high slow query rate
      const slowQueryRate = metrics.queryCount > 0 ? metrics.slowQueries / metrics.queryCount : 0
      if (slowQueryRate > this.slowQueryRateThreshold) {
        this.fireAlert({
          id: 'high-slow-query-rate',
          severity: AlertSeverity.ERROR,
          title: 'High Slow Query Rate',
          message: `${Math.round(slowQueryRate * 100)}% of queries are slow (${metrics.slowQueries}/${metrics.queryCount})`,
          source: 'performance-monitor',
          metadata: {
            slowQueryRate,
            threshold: this.slowQueryRateThreshold,
            slowQueries: metrics.slowQueries,
            totalQueries: metrics.queryCount
          }
        })
      } else {
        this.resolveAlert('high-slow-query-rate')
      }
      
      // Check overall performance health
      if (performanceSummary.overallHealth === 'error') {
        this.fireAlert({
          id: 'overall-performance-critical',
          severity: AlertSeverity.CRITICAL,
          title: 'Critical Performance Issues Detected',
          message: 'Multiple performance metrics are below acceptable thresholds',
          source: 'performance-monitor',
          metadata: {
            summary: performanceSummary,
            alerts: performanceSummary.alerts
          }
        })
      } else {
        this.resolveAlert('overall-performance-critical')
      }
      
    } catch (error) {
      console.error('Alert checking failed:', error)
      this.fireAlert({
        id: 'alert-system-error',
        severity: AlertSeverity.ERROR,
        title: 'Alerting System Error',
        message: 'Failed to check for alert conditions',
        source: 'alerting-service',
        metadata: {
          error: error instanceof Error ? error.message : 'Unknown error'
        }
      })
    }
  }
  
  /**
   * Fire an alert
   */
  private fireAlert(alertData: Omit<Alert, 'timestamp' | 'resolved'>): void {
    const existingAlert = this.activeAlerts.get(alertData.id)
    
    // Only fire if not already active
    if (!existingAlert) {
      const alert: Alert = {
        ...alertData,
        timestamp: new Date(),
        resolved: false
      }
      
      this.activeAlerts.set(alert.id, alert)
      this.alertHistory.unshift(alert)
      
      // Trim history if too large
      if (this.alertHistory.length > this.maxHistorySize) {
        this.alertHistory = this.alertHistory.slice(0, this.maxHistorySize)
      }
      
      // Log the alert
      this.logAlert(alert)
      
      // In production, this could send to external systems
      this.sendAlert(alert)
    }
  }
  
  /**
   * Resolve an alert
   */
  private resolveAlert(alertId: string): void {
    const alert = this.activeAlerts.get(alertId)
    if (alert && !alert.resolved) {
      alert.resolved = true
      alert.resolvedAt = new Date()
      this.activeAlerts.delete(alertId)
      
      console.log(`Alert resolved: ${alert.title}`)
    }
  }
  
  /**
   * Log alert to console (in production, would use proper logging)
   */
  private logAlert(alert: Alert): void {
    const logLevel = alert.severity === AlertSeverity.CRITICAL ? 'error' : 
                    alert.severity === AlertSeverity.ERROR ? 'error' :
                    alert.severity === AlertSeverity.WARNING ? 'warn' : 'info'
    
    console[logLevel](`[ALERT] ${alert.severity.toUpperCase()}: ${alert.title}`, {
      message: alert.message,
      source: alert.source,
      timestamp: alert.timestamp.toISOString(),
      metadata: alert.metadata
    })
  }
  
  /**
   * Send alert to external systems (placeholder for production implementation)
   */
  private sendAlert(alert: Alert): void {
    // In production, this might send to:
    // - Slack/Teams webhook
    // - Email notifications
    // - PagerDuty/OpsGenie
    // - SNMP traps
    // - Custom webhook endpoints
    
    // For now, just log that we would send
    if (alert.severity === AlertSeverity.CRITICAL || alert.severity === AlertSeverity.ERROR) {
      console.warn(`[ALERT NOTIFICATION] Would send ${alert.severity} alert: ${alert.title}`)
    }
  }
  
  /**
   * Get active alerts
   */
  getActiveAlerts(): Alert[] {
    return Array.from(this.activeAlerts.values())
  }
  
  /**
   * Get alert history
   */
  getAlertHistory(limit: number = 100): Alert[] {
    return this.alertHistory.slice(0, limit)
  }
  
  /**
   * Get alerts by severity
   */
  getAlertsBySeverity(severity: AlertSeverity): Alert[] {
    return this.getActiveAlerts().filter(alert => alert.severity === severity)
  }
  
  /**
   * Check if system has critical alerts
   */
  hasCriticalAlerts(): boolean {
    return this.getAlertsBySeverity(AlertSeverity.CRITICAL).length > 0
  }
  
  /**
   * Get alert statistics
   */
  getAlertStats(): {
    active: number
    byseverity: Record<AlertSeverity, number>
    totalFired: number
    resolved: number
  } {
    const active = this.activeAlerts.size
    const byStatus: Record<AlertSeverity, number> = {
      [AlertSeverity.INFO]: 0,
      [AlertSeverity.WARNING]: 0,
      [AlertSeverity.ERROR]: 0,
      [AlertSeverity.CRITICAL]: 0
    }
    
    // Count active alerts by severity
    for (const alert of this.activeAlerts.values()) {
      byStatus[alert.severity]++
    }
    
    const totalFired = this.alertHistory.length
    const resolved = this.alertHistory.filter(alert => alert.resolved).length
    
    return {
      active,
      byStatus,
      totalFired,
      resolved
    }
  }
  
  /**
   * Manually fire a custom alert
   */
  fireCustomAlert(
    id: string,
    severity: AlertSeverity,
    title: string,
    message: string,
    source: string = 'manual',
    metadata?: Record<string, any>
  ): void {
    this.fireAlert({
      id,
      severity,
      title,
      message,
      source,
      metadata
    })
  }
  
  /**
   * Manually resolve an alert
   */
  manuallyResolveAlert(alertId: string): boolean {
    if (this.activeAlerts.has(alertId)) {
      this.resolveAlert(alertId)
      return true
    }
    return false
  }
  
  /**
   * Clear all resolved alerts from history
   */
  clearResolvedAlerts(): void {
    this.alertHistory = this.alertHistory.filter(alert => !alert.resolved)
  }
}

/**
 * Global helper functions
 */

/**
 * Get global alerting service instance
 */
export function getAlertingService(): AlertingService {
  return AlertingService.getInstance()
}

/**
 * Quick function to fire a custom alert
 */
export function fireAlert(
  id: string,
  severity: AlertSeverity,
  title: string,
  message: string,
  metadata?: Record<string, any>
): void {
  getAlertingService().fireCustomAlert(id, severity, title, message, 'application', metadata)
}

/**
 * Check if system has critical issues
 */
export function hasCriticalIssues(): boolean {
  return getAlertingService().hasCriticalAlerts()
}