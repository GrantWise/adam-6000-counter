'use client'

import React, { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { 
  Activity, 
  AlertTriangle, 
  CheckCircle, 
  Clock, 
  Database, 
  TrendingUp,
  Zap,
  BarChart3,
  RefreshCw
} from 'lucide-react'

/**
 * Performance Dashboard Component for Phase 4
 * Displays real-time performance metrics and system health
 * 
 * Focuses on the success metrics from the plan:
 * - OEE calculation < 100ms
 * - Page load < 2 seconds  
 * - 99% uptime achieved
 */

interface PerformanceMetrics {
  averageOeeCalculationTime: number
  averageQueryDuration: number
  queryCount: number
  cacheHitRate: number
  slowQueries: number
  uptimeSeconds: number
  peakMemoryUsage?: number
  activeConnections?: number
}

interface HealthStatus {
  status: 'healthy' | 'warning' | 'error'
  database: {
    isConnected: boolean
    latencyMs?: number
    error?: string
    lastCheck: Date
  }
  dataAge: number
  version: string
  uptime: number
  performance?: PerformanceMetrics
}

interface PerformanceSummary {
  oeePerformance: 'excellent' | 'good' | 'needs-improvement'
  queryPerformance: 'excellent' | 'good' | 'needs-improvement'
  overallHealth: 'healthy' | 'warning' | 'error'
  alerts: string[]
}

export function PerformanceDashboard() {
  const [healthData, setHealthData] = useState<HealthStatus | null>(null)
  const [performanceSummary, setPerformanceSummary] = useState<PerformanceSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [lastRefresh, setLastRefresh] = useState(new Date())

  // Fetch performance data
  const fetchPerformanceData = async () => {
    try {
      setLoading(true)
      const response = await fetch('/api/health/detailed')
      const data = await response.json()
      
      setHealthData(data.overall ? data : null)
      
      // Extract performance summary if available
      if (data.performance?.summary) {
        setPerformanceSummary(data.performance.summary)
      }
      
      setLastRefresh(new Date())
    } catch (error) {
      console.error('Failed to fetch performance data:', error)
    } finally {
      setLoading(false)
    }
  }

  // Auto-refresh every 30 seconds
  useEffect(() => {
    fetchPerformanceData()
    const interval = setInterval(fetchPerformanceData, 30000)
    return () => clearInterval(interval)
  }, [])

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
      case 'excellent':
        return 'text-green-600'
      case 'warning':
      case 'good':
        return 'text-yellow-600'
      case 'error':
      case 'needs-improvement':
        return 'text-red-600'
      default:
        return 'text-gray-600'
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'healthy':
      case 'excellent':
        return <Badge variant="secondary" className="bg-green-100 text-green-700">Excellent</Badge>
      case 'warning':
      case 'good':
        return <Badge variant="secondary" className="bg-yellow-100 text-yellow-700">Good</Badge>
      case 'error':
      case 'needs-improvement':
        return <Badge variant="destructive">Needs Improvement</Badge>
      default:
        return <Badge variant="outline">Unknown</Badge>
    }
  }

  const formatDuration = (ms: number) => {
    if (ms < 1000) {
      return `${Math.round(ms)}ms`
    }
    return `${Math.round(ms / 10) / 100}s`
  }

  const formatUptime = (seconds: number) => {
    const hours = Math.floor(seconds / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)
    
    if (hours > 24) {
      const days = Math.floor(hours / 24)
      return `${days}d ${hours % 24}h`
    }
    
    return `${hours}h ${minutes}m`
  }

  if (loading && !healthData) {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <RefreshCw className="h-4 w-4 animate-spin" />
          <span>Loading performance data...</span>
        </div>
      </div>
    )
  }

  const performance = healthData?.performance
  const database = healthData?.database

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Performance Dashboard</h2>
          <p className="text-muted-foreground">
            Real-time system performance and health monitoring
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={fetchPerformanceData}
            disabled={loading}
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <span className="text-sm text-muted-foreground">
            Last updated: {lastRefresh.toLocaleTimeString()}
          </span>
        </div>
      </div>

      {/* Success Criteria Overview */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">OEE Calculation Time</CardTitle>
            <Zap className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {performance ? formatDuration(performance.averageOeeCalculationTime) : '--'}
            </div>
            <p className="text-xs text-muted-foreground">
              Target: &lt; 100ms {performance && (
                <span className={performance.averageOeeCalculationTime < 100 ? 'text-green-600' : 'text-red-600'}>
                  {performance.averageOeeCalculationTime < 100 ? '✓' : '✗'}
                </span>
              )}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Database Performance</CardTitle>
            <Database className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {performance ? formatDuration(performance.averageQueryDuration) : '--'}
            </div>
            <p className="text-xs text-muted-foreground">
              Avg query time {performance && (
                <span className={performance.averageQueryDuration < 500 ? 'text-green-600' : 'text-yellow-600'}>
                  ({performance.queryCount} queries)
                </span>
              )}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">System Uptime</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {healthData ? formatUptime(healthData.uptime) : '--'}
            </div>
            <p className="text-xs text-muted-foreground">
              Target: 99% uptime
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Performance Alerts */}
      {performanceSummary?.alerts && performanceSummary.alerts.length > 0 && (
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            <div className="space-y-1">
              {performanceSummary.alerts.map((alert, index) => (
                <div key={index}>• {alert}</div>
              ))}
            </div>
          </AlertDescription>
        </Alert>
      )}

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="database">Database</TabsTrigger>
          <TabsTrigger value="cache">Cache Performance</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Overall Health */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Activity className="h-5 w-5" />
                  Overall Health
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>System Status</span>
                  {getStatusBadge(healthData?.status || 'unknown')}
                </div>
                
                {performanceSummary && (
                  <>
                    <div className="flex items-center justify-between">
                      <span>OEE Performance</span>
                      {getStatusBadge(performanceSummary.oeePerformance)}
                    </div>
                    
                    <div className="flex items-center justify-between">
                      <span>Query Performance</span>
                      {getStatusBadge(performanceSummary.queryPerformance)}
                    </div>
                  </>
                )}
                
                <div className="flex items-center justify-between">
                  <span>Database</span>
                  {database?.isConnected ? (
                    <Badge variant="secondary" className="bg-green-100 text-green-700">
                      <CheckCircle className="h-3 w-3 mr-1" />
                      Connected
                    </Badge>
                  ) : (
                    <Badge variant="destructive">
                      <AlertTriangle className="h-3 w-3 mr-1" />
                      Disconnected
                    </Badge>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Performance Metrics */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <TrendingUp className="h-5 w-5" />
                  Performance Metrics
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {performance && (
                  <>
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span>OEE Calculation Time</span>
                        <span>{formatDuration(performance.averageOeeCalculationTime)}</span>
                      </div>
                      <Progress 
                        value={Math.min(100, (performance.averageOeeCalculationTime / 200) * 100)} 
                        className="w-full"
                      />
                    </div>
                    
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span>Query Duration</span>
                        <span>{formatDuration(performance.averageQueryDuration)}</span>
                      </div>
                      <Progress 
                        value={Math.min(100, (performance.averageQueryDuration / 1000) * 100)} 
                        className="w-full"
                      />
                    </div>
                    
                    <div className="pt-2 border-t">
                      <div className="flex justify-between text-sm">
                        <span>Total Queries</span>
                        <span>{performance.queryCount.toLocaleString()}</span>
                      </div>
                      
                      {performance.slowQueries > 0 && (
                        <div className="flex justify-between text-sm text-yellow-600">
                          <span>Slow Queries</span>
                          <span>{performance.slowQueries}</span>
                        </div>
                      )}
                    </div>
                  </>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="database" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Database Performance</CardTitle>
              <CardDescription>
                Database connection health and query performance metrics
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {database && (
                <>
                  <div className="grid gap-4 md:grid-cols-2">
                    <div>
                      <label className="text-sm font-medium">Connection Status</label>
                      <div className="mt-1">
                        {database.isConnected ? (
                          <Badge variant="secondary" className="bg-green-100 text-green-700">
                            Connected
                          </Badge>
                        ) : (
                          <Badge variant="destructive">Disconnected</Badge>
                        )}
                      </div>
                    </div>
                    
                    {database.latencyMs && (
                      <div>
                        <label className="text-sm font-medium">Connection Latency</label>
                        <div className="mt-1 text-sm">
                          {formatDuration(database.latencyMs)}
                        </div>
                      </div>
                    )}
                  </div>
                  
                  {database.error && (
                    <Alert>
                      <AlertTriangle className="h-4 w-4" />
                      <AlertDescription>{database.error}</AlertDescription>
                    </Alert>
                  )}
                </>
              )}
              
              {performance && (
                <div className="pt-4 border-t">
                  <h4 className="font-medium mb-2">Query Statistics</h4>
                  <div className="grid gap-2 text-sm">
                    <div className="flex justify-between">
                      <span>Average Duration:</span>
                      <span>{formatDuration(performance.averageQueryDuration)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Total Queries:</span>
                      <span>{performance.queryCount.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Slow Queries:</span>
                      <span className={performance.slowQueries > 0 ? 'text-yellow-600' : ''}>
                        {performance.slowQueries}
                      </span>
                    </div>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="cache" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Cache Performance</CardTitle>
              <CardDescription>
                Query caching statistics and hit rates
              </CardDescription>
            </CardHeader>
            <CardContent>
              {performance && (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                      <span>Cache Hit Rate</span>
                      <span>{Math.round(performance.cacheHitRate * 100)}%</span>
                    </div>
                    <Progress 
                      value={performance.cacheHitRate * 100} 
                      className="w-full"
                    />
                    <p className="text-xs text-muted-foreground">
                      Target: &gt; 80% {performance.cacheHitRate > 0.8 ? '✓' : '✗'}
                    </p>
                  </div>
                  
                  {performance.peakMemoryUsage && (
                    <div className="pt-4 border-t">
                      <h4 className="font-medium mb-2">Memory Usage</h4>
                      <div className="text-sm">
                        Peak: {Math.round(performance.peakMemoryUsage / 1024 / 1024)}MB
                      </div>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default PerformanceDashboard