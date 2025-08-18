import { checkConnectionHealth, dbCircuitBreaker } from '../database/connectionResilience'
import { ErrorHandler, safeAsync } from '../utils/errorHandler'
import { HealthStatus, DatabaseStatus } from '../types/oee'
import { getPerformanceMonitoring } from './performanceMonitoringService'
import { getAlertingService, AlertSeverity } from './alertingService'

/**
 * Monitoring Service - System health monitoring and automatic recovery
 * Implements health checks, alerting, and automatic recovery patterns
 */
export class MonitoringService {
  private isMonitoring = false
  private monitoringInterval: NodeJS.Timeout | null = null
  private healthCheckInterval = 30000 // 30 seconds
  private lastHealthStatus: HealthStatus | null = null
  private consecutiveFailures = 0
  private maxConsecutiveFailures = 3

  constructor(healthCheckIntervalMs?: number) {
    if (healthCheckIntervalMs) {
      this.healthCheckInterval = healthCheckIntervalMs
    }
  }

  /**
   * Start continuous health monitoring
   */
  startMonitoring(): void {
    if (this.isMonitoring) {
      console.warn('Monitoring is already running')
      return
    }

    this.isMonitoring = true
    console.log(`Starting health monitoring with ${this.healthCheckInterval}ms interval`)

    // Initial health check
    this.performHealthCheck()

    // Set up periodic health checks
    this.monitoringInterval = setInterval(() => {
      this.performHealthCheck()
    }, this.healthCheckInterval)
  }

  /**
   * Stop health monitoring
   */
  stopMonitoring(): void {
    if (this.monitoringInterval) {
      clearInterval(this.monitoringInterval)
      this.monitoringInterval = null
    }
    this.isMonitoring = false
    console.log('Health monitoring stopped')
  }

  /**
   * Perform a comprehensive health check
   */
  async performHealthCheck(): Promise<HealthStatus> {
    const startTime = Date.now()

    try {
      // Test database connectivity
      const dbHealth = await this.checkDatabaseHealth()
      
      // Check data freshness
      const dataAge = await this.checkDataAge()
      
      // Determine overall status
      let status: 'healthy' | 'warning' | 'error' = 'healthy'
      
      if (!dbHealth.isConnected) {
        status = 'error'
      } else if (dbHealth.latencyMs && dbHealth.latencyMs > 5000) {
        status = 'warning'
      } else if (dataAge > 300000) { // 5 minutes
        status = 'warning'
      }

      // Get performance metrics
      const performanceService = getPerformanceMonitoring()
      const performanceMetrics = performanceService.getPerformanceMetrics()
      const performanceSummary = performanceService.getPerformanceSummary()
      
      // Update status based on performance
      if (performanceSummary.overallHealth === 'error') {
        status = 'error'
      } else if (performanceSummary.overallHealth === 'warning' && status === 'healthy') {
        status = 'warning'
      }

      const healthStatus: HealthStatus = {
        status,
        database: dbHealth,
        dataAge,
        version: process.env.npm_package_version || '1.0.0',
        uptime: process.uptime(),
        performance: performanceMetrics,
      }

      // Handle status changes
      this.handleHealthStatusChange(healthStatus)

      this.lastHealthStatus = healthStatus
      this.consecutiveFailures = status === 'error' ? this.consecutiveFailures + 1 : 0

      // Attempt automatic recovery if needed
      if (this.consecutiveFailures >= this.maxConsecutiveFailures) {
        await this.attemptAutomaticRecovery()
      }

      return healthStatus
    } catch (error) {
      console.error('Health check failed:', error)
      
      const errorHealthStatus: HealthStatus = {
        status: 'error',
        database: {
          isConnected: false,
          error: error instanceof Error ? error.message : 'Health check failed',
          lastCheck: new Date(),
        },
        dataAge: 0,
        version: process.env.npm_package_version || '1.0.0',
        uptime: process.uptime(),
        performance: getPerformanceMonitoring().getPerformanceMetrics(),
      }

      this.consecutiveFailures++
      this.lastHealthStatus = errorHealthStatus

      return errorHealthStatus
    }
  }

  /**
   * Check database health with detailed diagnostics
   */
  private async checkDatabaseHealth(): Promise<DatabaseStatus> {
    return safeAsync(
      async (): Promise<DatabaseStatus> => {
        const dbTest = await checkConnectionHealth()
        
        return {
          isConnected: dbTest.isHealthy,
          latencyMs: dbTest.latencyMs,
          error: dbTest.error,
          lastCheck: new Date(),
        }
      },
      {
        isConnected: false,
        error: 'Database health check failed',
        lastCheck: new Date(),
      },
      (error) => {
        const appError = ErrorHandler.handleDatabaseError(error)
        console.error('Database health check error:', appError)
      }
    )
  }

  /**
   * Check data freshness by looking at latest counter data
   */
  private async checkDataAge(): Promise<number> {
    return safeAsync(
      async (): Promise<number> => {
        // This would query the latest counter data timestamp
        // For now, return a mock value
        return Date.now() - (2 * 60 * 1000) // 2 minutes ago
      },
      300000, // 5 minutes default
      (error) => {
        console.error('Data age check failed:', error)
      }
    )
  }

  /**
   * Handle health status changes and trigger alerts
   */
  private handleHealthStatusChange(currentStatus: HealthStatus): void {
    const previousStatus = this.lastHealthStatus?.status
    const currentStatusValue = currentStatus.status
    const alerting = getAlertingService()

    if (previousStatus && previousStatus !== currentStatusValue) {
      console.log(`Health status changed: ${previousStatus} → ${currentStatusValue}`)
      
      // Fire health alerts based on status changes
      if (currentStatusValue === 'error') {
        alerting.fireCustomAlert(
          'system-health-error',
          AlertSeverity.CRITICAL,
          'System Health Critical',
          `System health changed from ${previousStatus} to ${currentStatusValue}`,
          'monitoring-service',
          {
            previousStatus,
            currentStatus: currentStatusValue,
            database: currentStatus.database,
            performance: currentStatus.performance
          }
        )
      } else if (currentStatusValue === 'warning') {
        alerting.fireCustomAlert(
          'system-health-warning',
          AlertSeverity.WARNING,
          'System Health Warning',
          `System health degraded from ${previousStatus} to ${currentStatusValue}`,
          'monitoring-service',
          {
            previousStatus,
            currentStatus: currentStatusValue,
            database: currentStatus.database,
            performance: currentStatus.performance
          }
        )
      } else if (currentStatusValue === 'healthy' && previousStatus !== 'healthy') {
        // Resolve health alerts when system recovers
        alerting.manuallyResolveAlert('system-health-error')
        alerting.manuallyResolveAlert('system-health-warning')
      }
      
      // Database specific alerts
      if (!currentStatus.database.isConnected) {
        alerting.fireCustomAlert(
          'database-disconnected',
          AlertSeverity.CRITICAL,
          'Database Connection Lost',
          'Database connection is not available',
          'monitoring-service',
          {
            error: currentStatus.database.error,
            latency: currentStatus.database.latencyMs
          }
        )
      } else {
        alerting.manuallyResolveAlert('database-disconnected')
      }
      
      // Data age alerts
      if (currentStatus.dataAge > 600000) { // 10 minutes
        alerting.fireCustomAlert(
          'data-age-critical',
          AlertSeverity.ERROR,
          'Data Age Critical',
          `No fresh data received for ${Math.round(currentStatus.dataAge / 60000)} minutes`,
          'monitoring-service',
          {
            dataAgeMinutes: Math.round(currentStatus.dataAge / 60000)
          }
        )
      } else {
        alerting.manuallyResolveAlert('data-age-critical')
      }
      
      // In a production environment, you might send alerts here
      this.sendHealthAlert(currentStatus, previousStatus)
    }

    // Log periodic status updates
    if (!previousStatus || Date.now() % (5 * 60 * 1000) < this.healthCheckInterval) {
      console.log('Health Status:', {
        status: currentStatus.status,
        database: currentStatus.database.isConnected ? 'connected' : 'disconnected',
        dbLatency: currentStatus.database.latencyMs ? `${currentStatus.database.latencyMs}ms` : 'unknown',
        dataAge: `${Math.round(currentStatus.dataAge / 1000)}s`,
        uptime: `${Math.round(currentStatus.uptime)}s`
      })
    }
  }

  /**
   * Send health alerts (placeholder for production alerting)
   */
  private sendHealthAlert(currentStatus: HealthStatus, previousStatus: string): void {
    const alert = {
      timestamp: new Date().toISOString(),
      previousStatus,
      currentStatus: currentStatus.status,
      details: {
        database: currentStatus.database,
        dataAge: currentStatus.dataAge,
        uptime: currentStatus.uptime
      }
    }

    // In production, this might send to Slack, email, PagerDuty, etc.
    console.warn('HEALTH ALERT:', alert)
  }

  /**
   * Attempt automatic recovery procedures
   */
  private async attemptAutomaticRecovery(): Promise<void> {
    console.log('Attempting automatic recovery procedures...')

    try {
      // Reset database circuit breaker
      dbCircuitBreaker.reset()
      console.log('Database circuit breaker reset')

      // Test connection after reset
      const dbTest = await checkConnectionHealth()
      
      if (dbTest.isHealthy) {
        console.log('Automatic recovery successful - database connection restored')
        this.consecutiveFailures = 0
      } else {
        console.error('Automatic recovery failed - database still unavailable')
      }

      // Additional recovery procedures could be added here:
      // - Restart connection pools
      // - Clear caches
      // - Restart services
      // - Failover to backup systems

    } catch (error) {
      console.error('Automatic recovery failed:', error)
    }
  }

  /**
   * Get current health status
   */
  getCurrentHealthStatus(): HealthStatus | null {
    return this.lastHealthStatus
  }

  /**
   * Get monitoring statistics
   */
  getMonitoringStats(): {
    isMonitoring: boolean
    healthCheckInterval: number
    consecutiveFailures: number
    lastCheckTime: Date | null
  } {
    return {
      isMonitoring: this.isMonitoring,
      healthCheckInterval: this.healthCheckInterval,
      consecutiveFailures: this.consecutiveFailures,
      lastCheckTime: this.lastHealthStatus?.database.lastCheck || null
    }
  }

  /**
   * Force a health check and return results
   */
  async forceHealthCheck(): Promise<HealthStatus> {
    console.log('Forcing immediate health check...')
    return this.performHealthCheck()
  }

  /**
   * Reset failure counters (useful for manual recovery)
   */
  resetFailureCounters(): void {
    this.consecutiveFailures = 0
    console.log('Failure counters reset')
  }
}

// Singleton instance
let monitoringService: MonitoringService | null = null

/**
 * Get or create monitoring service singleton
 */
export function getMonitoringService(healthCheckIntervalMs?: number): MonitoringService {
  if (!monitoringService) {
    monitoringService = new MonitoringService(healthCheckIntervalMs)
  }
  return monitoringService
}

/**
 * Start system-wide health monitoring
 */
export function startSystemMonitoring(healthCheckIntervalMs?: number): MonitoringService {
  const service = getMonitoringService(healthCheckIntervalMs)
  service.startMonitoring()
  return service
}

/**
 * Stop system-wide health monitoring
 */
export function stopSystemMonitoring(): void {
  if (monitoringService) {
    monitoringService.stopMonitoring()
  }
}