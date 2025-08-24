import React, { useState, useEffect, useCallback } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Alert, AlertDescription } from '@/components/ui/alert'
import LoadingSpinner from '@/components/shared/LoadingSpinner'
import ServiceStatusCard from '@/components/admin/ServiceStatusCard'
import MetricsChart from '@/components/admin/MetricsChart'
import AlertsTable from '@/components/admin/AlertsTable'
import HealthTimeline from '@/components/admin/HealthTimeline'
import { 
  Activity, 
  RefreshCw, 
  Settings, 
  AlertTriangle,
  CheckCircle,
  Database,
  Wifi,
  WifiOff 
} from 'lucide-react'
import { systemHealthService } from '@/lib/services/systemHealthService'
import { webSocketHealthService } from '@/lib/services/webSocketHealthService'
import type { 
  ServiceStatus, 
  SystemMetrics, 
  SystemAlert, 
  DatabaseHealth,
  HealthTimelineEvent,
  ConnectionStatus 
} from '@/types'

/**
 * System Health Page - Complete real-time system monitoring dashboard
 * Displays service status, performance metrics, alerts, and health timeline
 * Integrates with backend APIs and WebSocket for real-time updates
 */
const SystemHealth: React.FC = () => {
  // State for all health data
  const [services, setServices] = useState<ServiceStatus[]>([])
  const [metrics, setMetrics] = useState<SystemMetrics[]>([])
  const [currentMetrics, setCurrentMetrics] = useState<SystemMetrics | null>(null)
  const [alerts, setAlerts] = useState<SystemAlert[]>([])
  const [healthEvents, setHealthEvents] = useState<HealthTimelineEvent[]>([])
  const [databaseHealth, setDatabaseHealth] = useState<DatabaseHealth | null>(null)
  
  // Loading and error states
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshing, setRefreshing] = useState(false)
  
  // WebSocket connection state
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>({
    connected: false,
    reconnecting: false
  })
  
  // Alert pagination state
  const [alertPagination, setAlertPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0
  })

  // Load initial data with proper cancellation support
  const loadInitialData = useCallback(async (abortSignal?: AbortSignal) => {
    try {
      setLoading(true)
      setError(null)
      
      // Check if cancelled before starting
      if (abortSignal?.aborted) {
        return
      }

      // Load all health data in parallel with timeout
      const timeout = new Promise((_, reject) => 
        setTimeout(() => reject(new Error('Request timeout')), 10000)
      )
      
      const dataPromise = Promise.allSettled([
        systemHealthService.getServiceStatuses(),
        systemHealthService.getMetricsHistory('24h'),
        systemHealthService.getAlerts({ page: 1, pageSize: 10 }),
        systemHealthService.getHealthTimeline('24h'),
        systemHealthService.getDatabaseHealth()
      ])
      
      const [
        servicesRes,
        metricsRes,
        alertsRes,
        timelineRes,
        databaseRes
      ] = await Promise.race([dataPromise, timeout]) as PromiseSettledResult<any>[]

      // Check if cancelled after data fetch
      if (abortSignal?.aborted) {
        return
      }
      
      // Process results safely
      if (servicesRes.status === 'fulfilled' && servicesRes.value?.success) {
        setServices(servicesRes.value.data || [])
      } else if (servicesRes.status === 'rejected') {
        console.warn('Failed to load services:', servicesRes.reason)
      }

      if (metricsRes.status === 'fulfilled' && metricsRes.value?.success) {
        const metricsData = metricsRes.value.data || []
        setMetrics(metricsData)
        if (metricsData.length > 0) {
          setCurrentMetrics(metricsData[metricsData.length - 1])
        }
      } else if (metricsRes.status === 'rejected') {
        console.warn('Failed to load metrics:', metricsRes.reason)
      }

      if (alertsRes.status === 'fulfilled' && alertsRes.value?.success) {
        const alertData = alertsRes.value.data
        if (alertData) {
          setAlerts(alertData.data || [])
          setAlertPagination({
            current: alertData.page,
            pageSize: alertData.pageSize,
            total: alertData.total
          })
        }
      } else if (alertsRes.status === 'rejected') {
        console.warn('Failed to load alerts:', alertsRes.reason)
      }

      if (timelineRes.status === 'fulfilled' && timelineRes.value?.success) {
        setHealthEvents(timelineRes.value.data || [])
      } else if (timelineRes.status === 'rejected') {
        console.warn('Failed to load timeline:', timelineRes.reason)
      }

      if (databaseRes.status === 'fulfilled' && databaseRes.value?.success) {
        setDatabaseHealth(databaseRes.value.data || null)
      } else if (databaseRes.status === 'rejected') {
        console.warn('Failed to load database health:', databaseRes.reason)
      }

    } catch (err) {
      // Only set error if not cancelled
      if (!abortSignal?.aborted) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to load system health data'
        setError(errorMessage)
        console.error('Error loading system health data:', err)
      }
    } finally {
      // Only update loading state if not cancelled
      if (!abortSignal?.aborted) {
        setLoading(false)
      }
    }
  }, [])

  // Manual refresh with error handling
  const handleRefresh = async () => {
    try {
      setRefreshing(true)
      await loadInitialData()
    } catch (err) {
      console.error('Refresh failed:', err)
      setError('Refresh failed. Please try again.')
    } finally {
      setRefreshing(false)
    }
  }

  // Handle alert acknowledgment with proper error handling
  const handleAcknowledgeAlert = async (alertId: string) => {
    try {
      const result = await systemHealthService.acknowledgeAlert(alertId)
      if (result.success) {
        // Update local state
        setAlerts(alerts.map(alert => 
          alert.id === alertId 
            ? { ...alert, acknowledged: true, acknowledgedAt: new Date() }
            : alert
        ))
      } else {
        setError('Failed to acknowledge alert. Please try again.')
      }
    } catch (err) {
      console.error('Failed to acknowledge alert:', err)
      setError('Failed to acknowledge alert. Please try again.')
    }
  }

  // Handle alert resolution with proper error handling
  const handleResolveAlert = async (alertId: string) => {
    try {
      const result = await systemHealthService.resolveAlert(alertId)
      if (result.success) {
        // Update local state
        setAlerts(alerts.map(alert => 
          alert.id === alertId 
            ? { ...alert, resolved: true, resolvedAt: new Date() }
            : alert
        ))
      } else {
        setError('Failed to resolve alert. Please try again.')
      }
    } catch (err) {
      console.error('Failed to resolve alert:', err)
      setError('Failed to resolve alert. Please try again.')
    }
  }

  // Handle alert pagination with proper error handling
  const handleAlertPaginationChange = async (page: number, pageSize: number) => {
    try {
      const result = await systemHealthService.getAlerts({ page, pageSize })
      if (result.success && result.data) {
        setAlerts(result.data.data || [])
        setAlertPagination({
          current: result.data.page,
          pageSize: result.data.pageSize,
          total: result.data.total
        })
      } else {
        setError('Failed to load alerts page. Please try again.')
      }
    } catch (err) {
      console.error('Failed to load alerts page:', err)
      setError('Failed to load alerts page. Please try again.')
    }
  }

  // Setup WebSocket connection and event handlers with proper cleanup
  useEffect(() => {
    let isActive = true
    
    // Set up WebSocket event handlers only if component is active
    const unsubscribeStatus = webSocketHealthService.onConnectionStatusChange((status) => {
      if (isActive) {
        setConnectionStatus(status)
      }
    })
    
    const unsubscribeServiceStatus = webSocketHealthService.onServiceStatusUpdate((status: ServiceStatus) => {
      if (isActive) {
        setServices(prev => {
          const index = prev.findIndex(s => s.serviceName === status.serviceName)
          if (index >= 0) {
            const updated = [...prev]
            updated[index] = status
            return updated
          } else {
            return [...prev, status]
          }
        })
      }
    })
    
    const unsubscribeMetrics = webSocketHealthService.onMetricsUpdate((newMetrics: SystemMetrics) => {
      if (isActive) {
        setCurrentMetrics(newMetrics)
        setMetrics(prev => [...prev.slice(-49), newMetrics]) // Keep last 50 data points
      }
    })
    
    const unsubscribeAlerts = webSocketHealthService.onAlertUpdate((alert: SystemAlert) => {
      if (isActive) {
        setAlerts(prev => [alert, ...prev].slice(0, 100)) // Keep last 100 alerts
      }
    })
    
    const unsubscribeDatabaseHealth = webSocketHealthService.onDatabaseHealthUpdate((health) => {
      if (isActive) {
        setDatabaseHealth(health)
      }
    })
    
    // Connect to WebSocket only if active
    if (isActive) {
      webSocketHealthService.connect().catch(err => {
        console.warn('Failed to connect to WebSocket:', err)
        if (isActive) {
          setConnectionStatus({ connected: false, reconnecting: false, error: 'Connection failed' })
        }
      })
    }
    
    // Cleanup on unmount
    return () => {
      isActive = false
      try {
        unsubscribeStatus()
        unsubscribeServiceStatus()
        unsubscribeMetrics()
        unsubscribeAlerts()
        unsubscribeDatabaseHealth()
        webSocketHealthService.disconnect()
      } catch (err) {
        console.warn('Cleanup error:', err)
      }
    }
  }, [])

  // Load initial data on mount with proper cancellation
  useEffect(() => {
    const abortController = new AbortController()
    
    loadInitialData(abortController.signal).catch(err => {
      if (!abortController.signal.aborted) {
        console.error('Initial data loading failed:', err)
      }
    })
    
    // Cleanup on unmount
    return () => {
      abortController.abort()
    }
  }, [loadInitialData])

  // Calculate overall system status
  const systemStatus = React.useMemo(() => {
    if (services.length === 0) return 'unknown'
    const healthyCount = services.filter(s => s.status === 'healthy').length
    const errorCount = services.filter(s => s.status === 'error' || s.status === 'offline').length
    
    if (errorCount > 0) return 'error'
    if (healthyCount === services.length) return 'healthy'
    return 'warning'
  }, [services])

  // Get system status configuration
  const getSystemStatusConfig = (status: string) => {
    switch (status) {
      case 'healthy':
        return {
          icon: CheckCircle,
          color: 'text-green-600',
          bgColor: 'bg-green-50',
          borderColor: 'border-green-200',
          badge: 'bg-green-100 text-green-800'
        }
      case 'warning':
        return {
          icon: AlertTriangle,
          color: 'text-yellow-600',
          bgColor: 'bg-yellow-50',
          borderColor: 'border-yellow-200',
          badge: 'bg-yellow-100 text-yellow-800'
        }
      case 'error':
        return {
          icon: AlertTriangle,
          color: 'text-red-600',
          bgColor: 'bg-red-50',
          borderColor: 'border-red-200',
          badge: 'bg-red-100 text-red-800'
        }
      default:
        return {
          icon: Activity,
          color: 'text-gray-600',
          bgColor: 'bg-gray-50',
          borderColor: 'border-gray-200',
          badge: 'bg-gray-100 text-gray-800'
        }
    }
  }

  const statusConfig = getSystemStatusConfig(systemStatus)
  const StatusIcon = statusConfig.icon

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">System Health</h1>
          <p className="text-muted-foreground mt-2">
            Monitor system performance and health metrics
          </p>
        </div>
        <div className="flex items-center justify-center py-12">
          <LoadingSpinner />
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">System Health</h1>
          <p className="text-muted-foreground mt-2">
            Real-time system performance and health monitoring
          </p>
        </div>
        <div className="flex items-center gap-4">
          {/* WebSocket Connection Status */}
          <div className="flex items-center gap-2">
            {connectionStatus.connected ? (
              <Wifi className="w-4 h-4 text-green-600" />
            ) : (
              <WifiOff className="w-4 h-4 text-red-600" />
            )}
            <span className="text-sm text-muted-foreground">
              {connectionStatus.connected ? 'Live' : connectionStatus.reconnecting ? 'Reconnecting...' : 'Offline'}
            </span>
          </div>
          
          {/* Refresh Button */}
          <Button 
            variant="outline" 
            size="sm" 
            onClick={handleRefresh}
            disabled={refreshing}
          >
            <RefreshCw className={`w-4 h-4 mr-2 ${refreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* System Overview */}
      <Card className={`${statusConfig.borderColor} border-2`}>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <StatusIcon className={`w-5 h-5 ${statusConfig.color}`} />
            System Status
            <Badge className={statusConfig.badge}>
              {systemStatus.toUpperCase()}
            </Badge>
          </CardTitle>
          <CardDescription>
            {services.length} services monitored, {services.filter(s => s.status === 'healthy').length} healthy
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Service Status Summary */}
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg ${statusConfig.bgColor}`}>
                <CheckCircle className="w-5 h-5 text-green-600" />
              </div>
              <div>
                <div className="text-2xl font-bold text-green-600">
                  {services.filter(s => s.status === 'healthy').length}
                </div>
                <div className="text-sm text-muted-foreground">Healthy</div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-yellow-50">
                <AlertTriangle className="w-5 h-5 text-yellow-600" />
              </div>
              <div>
                <div className="text-2xl font-bold text-yellow-600">
                  {services.filter(s => s.status === 'warning').length}
                </div>
                <div className="text-sm text-muted-foreground">Warning</div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-red-50">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <div>
                <div className="text-2xl font-bold text-red-600">
                  {services.filter(s => s.status === 'error' || s.status === 'offline').length}
                </div>
                <div className="text-sm text-muted-foreground">Critical</div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-blue-50">
                <Database className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <div className="text-2xl font-bold text-blue-600">
                  {databaseHealth?.connected ? 'OK' : 'ERR'}
                </div>
                <div className="text-sm text-muted-foreground">Database</div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabs for different views */}
      <Tabs defaultValue="services" className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="services">Services</TabsTrigger>
          <TabsTrigger value="metrics">Metrics</TabsTrigger>
          <TabsTrigger value="alerts">Alerts</TabsTrigger>
          <TabsTrigger value="timeline">Timeline</TabsTrigger>
        </TabsList>

        {/* Services Tab */}
        <TabsContent value="services" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {services.map((service) => (
              <ServiceStatusCard
                key={service.serviceName}
                service={service}
                onClick={() => {
                  // Future: Navigate to detailed service view
                  console.log('Service clicked:', service.serviceName)
                }}
              />
            ))}
          </div>
          {services.length === 0 && (
            <div className="text-center py-12 text-muted-foreground">
              No services configured
            </div>
          )}
        </TabsContent>

        {/* Metrics Tab */}
        <TabsContent value="metrics" className="space-y-6">
          {metrics.length > 0 ? (
            <MetricsChart 
              metrics={metrics} 
              height={400}
              timeRange="24h"
            />
          ) : (
            <Card>
              <CardContent className="flex items-center justify-center py-12">
                <div className="text-center text-muted-foreground">
                  <Activity className="w-12 h-12 mx-auto mb-2 opacity-50" />
                  <p>No metrics data available</p>
                </div>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        {/* Alerts Tab */}
        <TabsContent value="alerts" className="space-y-6">
          <AlertsTable
            alerts={alerts}
            loading={loading}
            onAcknowledge={handleAcknowledgeAlert}
            onResolve={handleResolveAlert}
            pagination={{
              current: alertPagination.current,
              pageSize: alertPagination.pageSize,
              total: alertPagination.total,
              onChange: handleAlertPaginationChange
            }}
          />
        </TabsContent>

        {/* Timeline Tab */}
        <TabsContent value="timeline" className="space-y-6">
          <HealthTimeline
            events={healthEvents}
            height={300}
            timeRange="24h"
          />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default SystemHealth