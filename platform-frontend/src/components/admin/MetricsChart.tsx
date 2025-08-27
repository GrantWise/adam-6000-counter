import React, { useMemo } from 'react'
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  Legend, 
  ResponsiveContainer,
  ReferenceLine 
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Activity, Cpu, HardDrive, Network, Database } from 'lucide-react'
import type { MetricsChartProps, SystemMetrics } from '@/types'

/**
 * MetricsChart - Real-time performance visualization using Recharts
 * Displays system metrics (CPU, Memory, Disk, Network) with time-series data
 */
const MetricsChart: React.FC<MetricsChartProps> = ({ 
  metrics, 
  height = 300,
  timeRange = '24h' 
}) => {
  // Transform metrics data for Recharts
  const chartData = useMemo(() => {
    return metrics.map(metric => ({
      timestamp: new Date(metric.timestamp).toLocaleTimeString('en-US', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit'
      }),
      fullTimestamp: metric.timestamp,
      cpu: Math.round(metric.cpuUsage * 10) / 10,
      memory: Math.round(metric.memoryUsage * 10) / 10,
      disk: Math.round(metric.diskUsage * 10) / 10,
      network: Math.round((metric.networkIn + metric.networkOut) / 1024 / 1024 * 10) / 10, // Convert to MB/s
      connections: metric.activeConnections,
      dbConnections: metric.databaseConnections
    })).slice(-50) // Show last 50 data points for performance
  }, [metrics])

  // Get latest metrics for current values display
  const latestMetric = useMemo(() => {
    return metrics[metrics.length - 1]
  }, [metrics])

  // Custom tooltip for better data display
  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload
      return (
        <div className="bg-white p-3 border border-gray-200 rounded-lg shadow-lg">
          <p className="text-sm font-medium text-gray-900 mb-2">
            {new Date(data.fullTimestamp).toLocaleString()}
          </p>
          {payload.map((entry: any, index: number) => (
            <div key={index} className="flex items-center gap-2 text-sm">
              <div 
                className="w-3 h-3 rounded-full" 
                style={{ backgroundColor: entry.color }}
              />
              <span className="text-gray-700">
                {entry.name}: {entry.value}
                {entry.dataKey === 'network' ? ' MB/s' : 
                 ['cpu', 'memory', 'disk'].includes(entry.dataKey) ? '%' : ''}
              </span>
            </div>
          ))}
        </div>
      )
    }
    return null
  }

  // Get status color based on value
  const getStatusColor = (value: number, thresholds: { warning: number; critical: number }) => {
    if (value >= thresholds.critical) return 'text-red-600'
    if (value >= thresholds.warning) return 'text-yellow-600'
    return 'text-green-600'
  }

  // Format time range display
  const getTimeRangeLabel = (range: string) => {
    switch (range) {
      case '1h': return 'Last Hour'
      case '6h': return 'Last 6 Hours'
      case '24h': return 'Last 24 Hours'
      case '7d': return 'Last 7 Days'
      default: return 'Live Data'
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Activity className="w-5 h-5" />
            System Performance Metrics
          </CardTitle>
          <Badge variant="outline">
            {getTimeRangeLabel(timeRange)}
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Current Values Display */}
        {latestMetric && (
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            {/* CPU Usage */}
            <div className="flex items-center gap-3 p-3 bg-blue-50 rounded-lg">
              <Cpu className="w-8 h-8 text-blue-600" />
              <div>
                <div className="text-xs text-muted-foreground">CPU Usage</div>
                <div className={`text-lg font-bold ${getStatusColor(latestMetric.cpuUsage, { warning: 70, critical: 90 })}`}>
                  {latestMetric.cpuUsage.toFixed(1)}%
                </div>
              </div>
            </div>

            {/* Memory Usage */}
            <div className="flex items-center gap-3 p-3 bg-purple-50 rounded-lg">
              <Database className="w-8 h-8 text-purple-600" />
              <div>
                <div className="text-xs text-muted-foreground">Memory Usage</div>
                <div className={`text-lg font-bold ${getStatusColor(latestMetric.memoryUsage, { warning: 80, critical: 95 })}`}>
                  {latestMetric.memoryUsage.toFixed(1)}%
                </div>
              </div>
            </div>

            {/* Disk Usage */}
            <div className="flex items-center gap-3 p-3 bg-orange-50 rounded-lg">
              <HardDrive className="w-8 h-8 text-orange-600" />
              <div>
                <div className="text-xs text-muted-foreground">Disk Usage</div>
                <div className={`text-lg font-bold ${getStatusColor(latestMetric.diskUsage, { warning: 85, critical: 95 })}`}>
                  {latestMetric.diskUsage.toFixed(1)}%
                </div>
              </div>
            </div>

            {/* Network Activity */}
            <div className="flex items-center gap-3 p-3 bg-green-50 rounded-lg">
              <Network className="w-8 h-8 text-green-600" />
              <div>
                <div className="text-xs text-muted-foreground">Network I/O</div>
                <div className="text-lg font-bold text-green-600">
                  {((latestMetric.networkIn + latestMetric.networkOut) / 1024 / 1024).toFixed(1)} MB/s
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Performance Chart */}
        <div className="w-full" style={{ height }}>
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
              <XAxis 
                dataKey="timestamp" 
                tick={{ fontSize: 12 }}
                stroke="#6B7280"
              />
              <YAxis 
                domain={[0, 100]}
                tick={{ fontSize: 12 }}
                stroke="#6B7280"
                label={{ value: 'Usage (%)', angle: -90, position: 'insideLeft' }}
              />
              <Tooltip content={<CustomTooltip />} />
              <Legend />
              
              {/* Warning and Critical Reference Lines */}
              <ReferenceLine y={70} stroke="#F59E0B" strokeDasharray="5 5" strokeOpacity={0.5} />
              <ReferenceLine y={90} stroke="#EF4444" strokeDasharray="5 5" strokeOpacity={0.5} />

              {/* Performance Metrics Lines */}
              <Line
                type="monotone"
                dataKey="cpu"
                stroke="#3B82F6"
                strokeWidth={2}
                name="CPU %"
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
              />
              <Line
                type="monotone"
                dataKey="memory"
                stroke="#8B5CF6"
                strokeWidth={2}
                name="Memory %"
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
              />
              <Line
                type="monotone"
                dataKey="disk"
                stroke="#F59E0B"
                strokeWidth={2}
                name="Disk %"
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Connection Metrics */}
        <div className="grid grid-cols-2 gap-4 pt-4 border-t">
          <div className="text-center">
            <div className="text-2xl font-bold text-blue-600">
              {latestMetric?.activeConnections || 0}
            </div>
            <div className="text-sm text-muted-foreground">Active Connections</div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-purple-600">
              {latestMetric?.databaseConnections || 0}
            </div>
            <div className="text-sm text-muted-foreground">Database Connections</div>
          </div>
        </div>

        {/* Data Status */}
        <div className="flex items-center justify-between text-sm text-muted-foreground pt-2 border-t">
          <span>
            Showing {chartData.length} data points
          </span>
          <span>
            Last updated: {latestMetric ? new Date(latestMetric.timestamp).toLocaleTimeString() : 'Never'}
          </span>
        </div>
      </CardContent>
    </Card>
  )
}

export default MetricsChart