import React, { useState, useEffect, useCallback } from 'react'
import {
  Database,
  Wifi,
  WifiOff,
  AlertTriangle,
  Clock,
  RefreshCw,
  Settings,
  TrendingUp,
  Activity,
  CheckCircle,
  XCircle,
  AlertCircle,
  Zap,
  BarChart3,
  Signal,
  Timer
} from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Alert } from '@/components/ui/alert'
import { MetricCard } from '@/components/shared/MetricCard'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { deviceService } from '@/lib/services/deviceService'
import type { 
  ComponentStatus, 
  DeviceConfig, 
  CounterReading, 
  DataQuality 
} from '@/types'

interface DeviceMonitoringData {
  devices?: DeviceConfig[]
  summary?: {
    total: number
    online: number
    offline: number
    error: number
    dataPointsToday: number
    avgResponseTime: number
  }
  realTimeReadings?: Array<{
    deviceId: string
    deviceName: string
    channels: Array<{
      channelId: string
      channelName: string
      value: number
      rate?: number
      unit: string
      timestamp: Date
      quality: DataQuality
    }>
    status: 'online' | 'offline' | 'error'
    lastUpdate: Date
    responseTime: number
  }>
  systemHealth?: {
    overall: 'healthy' | 'warning' | 'error'
    dataFlow: {
      pointsPerSecond: number
      avgLatency: number
      errorRate: number
    }
  }
  alarms?: Array<{
    id: string
    deviceName: string
    channelName: string
    type: string
    severity: 'low' | 'medium' | 'high'
    message: string
    timestamp: Date
    resolved: boolean
  }>
}

/**
 * Device Monitoring Page - Real-time ADAM device monitoring and control
 */
const DeviceMonitoring: React.FC = () => {
  const [data, setData] = useState<DeviceMonitoringData>({})
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [autoRefresh, setAutoRefresh] = useState(true)
  const [selectedDevice, setSelectedDevice] = useState<string | null>(null)

  // Load device monitoring data
  const loadDeviceData = useCallback(async (showLoading = true) => {
    if (showLoading) setLoading('loading')
    setError(null)

    try {
      // Load data from multiple device endpoints in parallel
      const [devicesResult, summaryResult, realTimeResult, healthResult, alarmsResult] = await Promise.allSettled([
        deviceService.getDevices(),
        deviceService.getDeviceSummary(),
        deviceService.getRealTimeReadings(),
        deviceService.getSystemHealth(),
        deviceService.getDeviceAlarms({ active: true })
      ])

      const newData: DeviceMonitoringData = {}

      // Process devices
      if (devicesResult.status === 'fulfilled' && devicesResult.value.success) {
        newData.devices = devicesResult.value.data
      } else {
        // Mock devices for development
        newData.devices = [
          {
            id: '1',
            name: 'ADAM-6051 Counter #1',
            ipAddress: '192.168.1.100',
            port: 502,
            deviceType: 'ADAM-6051',
            channels: [
              {
                id: '1',
                name: 'Production Counter',
                address: 1,
                dataType: 'Counter',
                unit: 'pcs',
                scalingFactor: 1,
                enabled: true
              },
              {
                id: '2',
                name: 'Reject Counter',
                address: 2,
                dataType: 'Counter',
                unit: 'pcs',
                scalingFactor: 1,
                enabled: true
              }
            ],
            status: 'online',
            hierarchyNodeId: '5',
            createdAt: new Date(),
            updatedAt: new Date()
          },
          {
            id: '2',
            name: 'ADAM-6052 Digital I/O',
            ipAddress: '192.168.1.101',
            port: 502,
            deviceType: 'ADAM-6052',
            channels: [
              {
                id: '3',
                name: 'Status Input',
                address: 1,
                dataType: 'Digital',
                enabled: true
              }
            ],
            status: 'offline',
            hierarchyNodeId: '5',
            createdAt: new Date(),
            updatedAt: new Date()
          }
        ]
      }

      // Process summary
      if (summaryResult.status === 'fulfilled' && summaryResult.value.success) {
        newData.summary = summaryResult.value.data
      } else {
        newData.summary = {
          total: 2,
          online: 1,
          offline: 1,
          error: 0,
          dataPointsToday: 28400,
          avgResponseTime: 45
        }
      }

      // Process real-time readings
      if (realTimeResult.status === 'fulfilled' && realTimeResult.value.success) {
        newData.realTimeReadings = realTimeResult.value.data
      } else {
        newData.realTimeReadings = [
          {
            deviceId: '1',
            deviceName: 'ADAM-6051 Counter #1',
            channels: [
              {
                channelId: '1',
                channelName: 'Production Counter',
                value: 15847,
                rate: 125.3,
                unit: 'pcs',
                timestamp: new Date(),
                quality: 'good'
              },
              {
                channelId: '2',
                channelName: 'Reject Counter',
                value: 73,
                rate: 0.8,
                unit: 'pcs',
                timestamp: new Date(),
                quality: 'good'
              }
            ],
            status: 'online',
            lastUpdate: new Date(),
            responseTime: 42
          }
        ]
      }

      // Process system health
      if (healthResult.status === 'fulfilled' && healthResult.value.success) {
        newData.systemHealth = healthResult.value.data
      } else {
        newData.systemHealth = {
          overall: 'healthy',
          dataFlow: {
            pointsPerSecond: 12.5,
            avgLatency: 45,
            errorRate: 0.2
          }
        }
      }

      // Process alarms
      if (alarmsResult.status === 'fulfilled' && alarmsResult.value.success) {
        newData.alarms = alarmsResult.value.data
      } else {
        newData.alarms = [
          {
            id: '1',
            deviceName: 'ADAM-6052 Digital I/O',
            channelName: 'Status Input',
            type: 'communication_error',
            severity: 'high',
            message: 'Device not responding to Modbus requests',
            timestamp: new Date(Date.now() - 5 * 60 * 1000),
            resolved: false
          }
        ]
      }

      setData(newData)
      setLastUpdated(new Date())
      setLoading('success')
    } catch (err) {
      console.error('Device monitoring load error:', err)
      setError(err instanceof Error ? err.message : 'Failed to load device data')
      setLoading('error')
    }
  }, [])

  // Load data on mount
  useEffect(() => {
    loadDeviceData()
  }, [loadDeviceData])

  // Auto-refresh data every 5 seconds for real-time monitoring
  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(() => {
      loadDeviceData(false)
    }, 5000)

    return () => clearInterval(interval)
  }, [loadDeviceData, autoRefresh])

  // Get device status icon
  const getDeviceStatusIcon = (status: string) => {
    switch (status) {
      case 'online': return CheckCircle
      case 'offline': return XCircle
      case 'error': return AlertCircle
      default: return AlertTriangle
    }
  }

  // Get data quality color
  const getQualityColor = (quality: DataQuality) => {
    switch (quality) {
      case 'good': return 'text-green-600'
      case 'uncertain': return 'text-yellow-600'
      case 'bad': return 'text-red-600'
      default: return 'text-gray-600'
    }
  }

  // Format large numbers
  const formatNumber = (num: number) => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`
    return num.toString()
  }

  // Format time ago
  const formatTimeAgo = (date: Date) => {
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    
    if (diffMins < 1) return 'Just now'
    if (diffMins < 60) return `${diffMins}m ago`
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`
    return `${Math.floor(diffMins / 1440)}d ago`
  }

  if (loading === 'loading') {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Device Monitoring</h1>
          <p className="text-muted-foreground mt-2">
            Real-time device status and counter monitoring
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
          <h1 className="text-3xl font-bold">Device Monitoring</h1>
          <p className="text-muted-foreground mt-2">
            Real-time device status and counter monitoring
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
            Auto Refresh
          </Button>
          <Button variant="outline" size="sm" onClick={() => loadDeviceData()}>
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

      {/* System Overview */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-6">
        <MetricCard
          title="Total Devices"
          value={data.summary?.total || 0}
          icon={Database}
        />
        <MetricCard
          title="Online"
          value={data.summary?.online || 0}
          status="healthy"
          icon={Wifi}
        />
        <MetricCard
          title="Offline"
          value={data.summary?.offline || 0}
          status={data.summary && data.summary.offline > 0 ? 'warning' : 'healthy'}
          icon={WifiOff}
        />
        <MetricCard
          title="Data Points Today"
          value={formatNumber(data.summary?.dataPointsToday || 0)}
          icon={BarChart3}
        />
        <MetricCard
          title="Avg Response"
          value={`${data.summary?.avgResponseTime || 0}ms`}
          status={data.summary && data.summary.avgResponseTime > 100 ? 'warning' : 'healthy'}
          icon={Timer}
        />
      </div>

      {/* Active Alarms */}
      {data.alarms && data.alarms.length > 0 && (
        <Card className="border-l-4 border-l-red-500">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-red-600">
              <AlertTriangle className="h-5 w-5" />
              Active Alarms ({data.alarms.filter(a => !a.resolved).length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {data.alarms.filter(a => !a.resolved).map((alarm) => (
                <div key={alarm.id} className="flex items-center justify-between p-3 bg-red-50 dark:bg-red-950/20 rounded-md">
                  <div>
                    <div className="font-medium">{alarm.deviceName} - {alarm.channelName}</div>
                    <div className="text-sm text-muted-foreground">{alarm.message}</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={alarm.severity === 'high' ? 'destructive' : 'secondary'}>
                      {alarm.severity}
                    </Badge>
                    <div className="text-xs text-muted-foreground">
                      {formatTimeAgo(new Date(alarm.timestamp))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Real-time Device Readings */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {data.realTimeReadings?.map((device) => {
          const StatusIcon = getDeviceStatusIcon(device.status)
          return (
            <Card key={device.deviceId} className={`transition-all duration-200 ${
              selectedDevice === device.deviceId ? 'ring-2 ring-primary' : ''
            }`}>
              <CardHeader>
                <CardTitle className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <StatusIcon className={`h-5 w-5 ${
                      device.status === 'online' ? 'text-green-600' :
                      device.status === 'offline' ? 'text-red-600' :
                      'text-yellow-600'
                    }`} />
                    {device.deviceName}
                  </div>
                  <Badge variant={device.status === 'online' ? 'default' : 'secondary'}>
                    {device.status}
                  </Badge>
                </CardTitle>
                <CardDescription>
                  Response: {device.responseTime}ms | Updated: {formatTimeAgo(new Date(device.lastUpdate))}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {device.channels.map((channel) => (
                    <div key={channel.channelId} className="flex items-center justify-between p-3 border rounded-md">
                      <div>
                        <div className="font-medium">{channel.channelName}</div>
                        <div className="text-sm text-muted-foreground">
                          Quality: <span className={getQualityColor(channel.quality)}>{channel.quality}</span>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-2xl font-bold">
                          {formatNumber(channel.value)} {channel.unit}
                        </div>
                        {channel.rate && (
                          <div className="text-sm text-muted-foreground flex items-center gap-1">
                            <TrendingUp className="h-3 w-3" />
                            {channel.rate.toFixed(1)}/min
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* Device List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Database className="h-5 w-5" />
            All Devices ({data.devices?.length || 0})
          </CardTitle>
          <CardDescription>
            Complete list of configured ADAM devices
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.devices?.map((device) => {
              const StatusIcon = getDeviceStatusIcon(device.status)
              return (
                <div 
                  key={device.id} 
                  className="p-4 border rounded-md hover:bg-muted/50 cursor-pointer transition-colors"
                  onClick={() => setSelectedDevice(selectedDevice === device.id ? null : device.id)}
                >
                  <div className="flex items-center justify-between mb-2">
                    <div className="font-medium">{device.name}</div>
                    <StatusIcon className={`h-4 w-4 ${
                      device.status === 'online' ? 'text-green-600' :
                      device.status === 'offline' ? 'text-red-600' :
                      'text-yellow-600'
                    }`} />
                  </div>
                  <div className="text-sm text-muted-foreground space-y-1">
                    <div>IP: {device.ipAddress}:{device.port}</div>
                    <div>Type: {device.deviceType}</div>
                    <div>Channels: {device.channels.length}</div>
                  </div>
                  <div className="mt-3 pt-2 border-t">
                    <Badge variant={device.status === 'online' ? 'default' : 'secondary'}>
                      {device.status}
                    </Badge>
                  </div>
                </div>
              )
            })}
          </div>
        </CardContent>
      </Card>

      {/* System Health Summary */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            System Health
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-2xl font-bold">{data.systemHealth?.dataFlow?.pointsPerSecond?.toFixed(1) || '0.0'}</div>
              <div className="text-sm text-muted-foreground">Data Points/sec</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">{data.systemHealth?.dataFlow?.avgLatency || 0}ms</div>
              <div className="text-sm text-muted-foreground">Average Latency</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">{((1 - (data.systemHealth?.dataFlow?.errorRate || 0) / 100) * 100).toFixed(1)}%</div>
              <div className="text-sm text-muted-foreground">Success Rate</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default DeviceMonitoring