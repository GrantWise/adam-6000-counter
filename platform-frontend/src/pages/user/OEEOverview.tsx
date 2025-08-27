import React, { useState, useEffect, useCallback } from 'react'
import {
  BarChart3,
  TrendingUp,
  TrendingDown,
  Minus,
  Clock,
  Zap,
  Target,
  AlertTriangle,
  RefreshCw,
  Play,
  Pause,
  Square,
  CheckCircle
} from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Alert } from '@/components/ui/alert'
import { MetricCard } from '@/components/shared/MetricCard'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { oeeService } from '@/lib/services/oeeService'
import type { ComponentStatus, OEEMetrics, WorkOrder, StoppageEvent } from '@/types'

interface OEEOverviewData {
  overview?: {
    averageOEE: number
    totalEquipment: number
    activeEquipment: number
    topPerformers: Array<{
      equipmentId: string
      equipmentName: string
      oee: number
      availability: number
      performance: number
      quality: number
    }>
    breakdownByMetric: {
      availability: { avg: number; trend: 'up' | 'down' | 'stable' }
      performance: { avg: number; trend: 'up' | 'down' | 'stable' }
      quality: { avg: number; trend: 'up' | 'down' | 'stable' }
    }
  }
  currentMetrics?: Array<OEEMetrics & { equipmentName: string }>
  activeWorkOrders?: WorkOrder[]
  recentStoppages?: StoppageEvent[]
  targets?: {
    global: {
      oee: number
      availability: number
      performance: number
      quality: number
    }
  }
}

/**
 * OEE Overview Page - Real-time OEE monitoring and analysis
 */
const OEEOverview: React.FC = () => {
  const [data, setData] = useState<OEEOverviewData>({})
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [autoRefresh, setAutoRefresh] = useState(true)

  // Load OEE data
  const loadOEEData = useCallback(async (showLoading = true) => {
    if (showLoading) setLoading('loading')
    setError(null)

    try {
      // Load data from multiple endpoints in parallel
      const [overviewResult, currentResult, workOrdersResult, stoppagesResult, targetsResult] = await Promise.allSettled([
        oeeService.getOEEOverview(),
        oeeService.getCurrentOEE(),
        oeeService.getActiveWorkOrders(),
        oeeService.getStoppageEvents({ active: true }),
        oeeService.getOEETargets()
      ])

      const newData: OEEOverviewData = {}

      // Process overview data
      if (overviewResult.status === 'fulfilled' && overviewResult.value.success) {
        newData.overview = overviewResult.value.data
      } else {
        // Mock data for development
        newData.overview = {
          averageOEE: 78.5,
          totalEquipment: 8,
          activeEquipment: 6,
          topPerformers: [
            {
              equipmentId: '1',
              equipmentName: 'Line 1 - Assembly',
              oee: 89.2,
              availability: 94.5,
              performance: 96.8,
              quality: 97.5
            },
            {
              equipmentId: '2', 
              equipmentName: 'Line 2 - Packaging',
              oee: 82.1,
              availability: 91.2,
              performance: 94.3,
              quality: 95.4
            },
            {
              equipmentId: '3',
              equipmentName: 'Station A - Counter',
              oee: 76.8,
              availability: 88.9,
              performance: 92.1,
              quality: 93.7
            }
          ],
          breakdownByMetric: {
            availability: { avg: 91.5, trend: 'up' },
            performance: { avg: 94.4, trend: 'stable' },
            quality: { avg: 95.5, trend: 'down' }
          }
        }
      }

      // Process current metrics
      if (currentResult.status === 'fulfilled' && currentResult.value.success) {
        newData.currentMetrics = currentResult.value.data
      } else {
        newData.currentMetrics = [
          {
            equipmentId: '1',
            equipmentName: 'Line 1 - Assembly',
            timestamp: new Date(),
            availability: 94.5,
            performance: 96.8,
            quality: 97.5,
            oee: 89.2,
            plannedProductionTime: 480,
            actualProductionTime: 453,
            idealRunRate: 100,
            actualRunRate: 97,
            goodCount: 1850,
            totalCount: 1897
          }
        ]
      }

      // Process active work orders
      if (workOrdersResult.status === 'fulfilled' && workOrdersResult.value.success) {
        newData.activeWorkOrders = workOrdersResult.value.data
      } else {
        newData.activeWorkOrders = [
          {
            id: '1',
            equipmentId: '1',
            productCode: 'PROD-001',
            plannedQuantity: 2000,
            actualQuantity: 1850,
            startTime: new Date(Date.now() - 4 * 60 * 60 * 1000), // 4 hours ago
            status: 'active'
          }
        ]
      }

      // Process recent stoppages
      if (stoppagesResult.status === 'fulfilled' && stoppagesResult.value.success) {
        newData.recentStoppages = stoppagesResult.value.data
      } else {
        newData.recentStoppages = [
          {
            id: '1',
            equipmentId: '3',
            workOrderId: '1',
            startTime: new Date(Date.now() - 15 * 60 * 1000), // 15 minutes ago
            category: 'Technical',
            reason: 'Counter calibration',
            description: 'Recalibrating counter sensor readings'
          }
        ]
      }

      // Process targets
      if (targetsResult.status === 'fulfilled' && targetsResult.value.success) {
        newData.targets = targetsResult.value.data
      } else {
        newData.targets = {
          global: {
            oee: 85.0,
            availability: 90.0,
            performance: 95.0,
            quality: 99.0
          }
        }
      }

      setData(newData)
      setLastUpdated(new Date())
      setLoading('success')
    } catch (err) {
      console.error('OEE data load error:', err)
      setError(err instanceof Error ? err.message : 'Failed to load OEE data')
      setLoading('error')
    }
  }, [])

  // Load data on mount
  useEffect(() => {
    loadOEEData()
  }, [loadOEEData])

  // Auto-refresh data every 30 seconds
  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(() => {
      loadOEEData(false)
    }, 30000)

    return () => clearInterval(interval)
  }, [loadOEEData, autoRefresh])

  // Format percentage
  const formatPercent = (value: number) => `${value.toFixed(1)}%`

  // Get OEE status color
  const getOEEStatus = (oee: number, target?: number) => {
    const threshold = target || 85
    if (oee >= threshold) return 'healthy'
    if (oee >= threshold * 0.8) return 'warning'
    return 'error'
  }

  // Get trend icon
  const getTrendIcon = (trend: 'up' | 'down' | 'stable') => {
    switch (trend) {
      case 'up': return TrendingUp
      case 'down': return TrendingDown
      default: return Minus
    }
  }

  // Format duration
  const formatDuration = (startTime: Date, endTime?: Date) => {
    const end = endTime || new Date()
    const diffMs = end.getTime() - startTime.getTime()
    const hours = Math.floor(diffMs / 3600000)
    const minutes = Math.floor((diffMs % 3600000) / 60000)
    
    if (hours > 0) {
      return `${hours}h ${minutes}m`
    }
    return `${minutes}m`
  }

  if (loading === 'loading') {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">OEE Overview</h1>
          <p className="text-muted-foreground mt-2">
            Overall Equipment Effectiveness monitoring and analysis
          </p>
        </div>
        <div className="flex justify-center py-12">
          <LoadingSpinner size="lg" />
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">OEE Overview</h1>
          <p className="text-muted-foreground mt-2">
            Overall Equipment Effectiveness monitoring and analysis
          </p>
        </div>
        <div className="flex items-center gap-3">
          {lastUpdated && (
            <div className="text-sm text-muted-foreground flex items-center gap-1">
              <Clock className="h-4 w-4" />
              Last updated: {lastUpdated.toLocaleTimeString()}
            </div>
          )}
          <Button 
            variant="outline" 
            size="sm" 
            onClick={() => setAutoRefresh(!autoRefresh)}
            className={autoRefresh ? 'text-green-600' : 'text-muted-foreground'}
          >
            <RefreshCw className={`h-4 w-4 ${autoRefresh ? 'animate-spin' : ''}`} />
            Live
          </Button>
          <Button variant="outline" size="sm" onClick={() => loadOEEData()}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <div>{error}</div>
        </Alert>
      )}

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <MetricCard
          title="Average OEE"
          value={formatPercent(data.overview?.averageOEE || 0)}
          status={getOEEStatus(data.overview?.averageOEE || 0, data.targets?.global?.oee)}
          description={`Target: ${formatPercent(data.targets?.global?.oee || 85)}`}
          icon={BarChart3}
        />
        <MetricCard
          title="Availability"
          value={formatPercent(data.overview?.breakdownByMetric?.availability?.avg || 0)}
          trend={{ 
            direction: data.overview?.breakdownByMetric?.availability?.trend || 'stable',
            percentage: 2.1,
            period: '24h'
          }}
          icon={getTrendIcon(data.overview?.breakdownByMetric?.availability?.trend || 'stable')}
        />
        <MetricCard
          title="Performance"
          value={formatPercent(data.overview?.breakdownByMetric?.performance?.avg || 0)}
          trend={{ 
            direction: data.overview?.breakdownByMetric?.performance?.trend || 'stable',
            percentage: 0.8,
            period: '24h'
          }}
          icon={getTrendIcon(data.overview?.breakdownByMetric?.performance?.trend || 'stable')}
        />
        <MetricCard
          title="Quality"
          value={formatPercent(data.overview?.breakdownByMetric?.quality?.avg || 0)}
          trend={{ 
            direction: data.overview?.breakdownByMetric?.quality?.trend || 'stable',
            percentage: -1.2,
            period: '24h'
          }}
          icon={getTrendIcon(data.overview?.breakdownByMetric?.quality?.trend || 'stable')}
        />
      </div>

      {/* Equipment Status */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Target className="h-5 w-5" />
              Top Performers
            </CardTitle>
            <CardDescription>
              Equipment with highest OEE scores
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {data.overview?.topPerformers?.map((equipment, index) => (
                <div key={equipment.equipmentId} className="flex items-center justify-between p-3 border rounded-md">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 bg-primary rounded-full flex items-center justify-center text-primary-foreground font-bold text-sm">
                      {index + 1}
                    </div>
                    <div>
                      <div className="font-medium">{equipment.equipmentName}</div>
                      <div className="text-sm text-muted-foreground">
                        A: {formatPercent(equipment.availability)} | 
                        P: {formatPercent(equipment.performance)} | 
                        Q: {formatPercent(equipment.quality)}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className={`text-lg font-bold ${
                      getOEEStatus(equipment.oee, data.targets?.global?.oee) === 'healthy' ? 'text-green-600' :
                      getOEEStatus(equipment.oee, data.targets?.global?.oee) === 'warning' ? 'text-yellow-600' :
                      'text-red-600'
                    }`}>
                      {formatPercent(equipment.oee)}
                    </div>
                    <Badge variant={getOEEStatus(equipment.oee, data.targets?.global?.oee) === 'healthy' ? 'default' : 'secondary'}>
                      OEE
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Play className="h-5 w-5" />
              Active Production ({data.activeWorkOrders?.length || 0})
            </CardTitle>
            <CardDescription>
              Current work orders in progress
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {data.activeWorkOrders && data.activeWorkOrders.length > 0 ? (
                data.activeWorkOrders.map((workOrder) => {
                  const progress = (workOrder.actualQuantity / workOrder.plannedQuantity) * 100
                  return (
                    <div key={workOrder.id} className="p-3 border rounded-md space-y-2">
                      <div className="flex items-center justify-between">
                        <div>
                          <div className="font-medium">{workOrder.productCode}</div>
                          <div className="text-sm text-muted-foreground">
                            {workOrder.actualQuantity} / {workOrder.plannedQuantity} units
                          </div>
                        </div>
                        <Badge variant={workOrder.status === 'active' ? 'default' : 'secondary'}>
                          {workOrder.status}
                        </Badge>
                      </div>
                      <div className="w-full bg-muted rounded-full h-2">
                        <div 
                          className="bg-primary h-2 rounded-full transition-all duration-300"
                          style={{ width: `${Math.min(100, progress)}%` }}
                        />
                      </div>
                      <div className="flex justify-between text-xs text-muted-foreground">
                        <span>Started: {workOrder.startTime ? formatDuration(new Date(workOrder.startTime)) : 'N/A'} ago</span>
                        <span>{progress.toFixed(1)}% complete</span>
                      </div>
                    </div>
                  )
                })
              ) : (
                <div className="text-center py-4 text-muted-foreground">
                  <Square className="h-8 w-8 mx-auto mb-2" />
                  No active production
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Stoppages */}
      {data.recentStoppages && data.recentStoppages.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-600" />
              Active Stoppages ({data.recentStoppages.length})
            </CardTitle>
            <CardDescription>
              Equipment currently stopped or in maintenance
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {data.recentStoppages.map((stoppage) => (
                <div key={stoppage.id} className="p-3 border-l-4 border-l-yellow-500 bg-yellow-50 dark:bg-yellow-950/20 rounded-r-md">
                  <div className="flex items-center justify-between mb-2">
                    <div className="font-medium">Equipment {stoppage.equipmentId}</div>
                    <Badge variant="outline" className="text-yellow-700">{stoppage.category}</Badge>
                  </div>
                  <div className="text-sm text-muted-foreground mb-1">
                    <strong>Reason:</strong> {stoppage.reason}
                  </div>
                  {stoppage.description && (
                    <div className="text-sm text-muted-foreground mb-2">{stoppage.description}</div>
                  )}
                  <div className="text-xs text-muted-foreground">
                    Duration: {formatDuration(new Date(stoppage.startTime), stoppage.endTime ? new Date(stoppage.endTime) : undefined)}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Equipment Status Summary */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="h-5 w-5" />
            Equipment Status Summary
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">{data.overview?.activeEquipment || 0}</div>
              <div className="text-sm text-muted-foreground">Active Equipment</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-yellow-600">{(data.overview?.totalEquipment || 0) - (data.overview?.activeEquipment || 0)}</div>
              <div className="text-sm text-muted-foreground">Idle/Down Equipment</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">{data.overview?.totalEquipment || 0}</div>
              <div className="text-sm text-muted-foreground">Total Equipment</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default OEEOverview