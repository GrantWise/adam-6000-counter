import React, { useMemo } from 'react'
import { 
  AreaChart, 
  Area, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer,
  Legend,
  ScatterChart,
  Scatter
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { 
  Activity, 
  TrendingUp, 
  AlertTriangle, 
  CheckCircle, 
  Settings,
  Upload,
  Wifi,
  WifiOff
} from 'lucide-react'
import type { HealthTimelineProps, HealthTimelineEvent } from '@/types'

/**
 * HealthTimeline - Historical health events visualization
 * Shows system health events over time with severity indicators and service status changes
 */
const HealthTimeline: React.FC<HealthTimelineProps> = ({ 
  events, 
  height = 200,
  timeRange = '24h' 
}) => {
  // Transform events data for timeline visualization
  const timelineData = useMemo(() => {
    const sortedEvents = [...events].sort((a, b) => 
      new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    )

    // Group events by hour for better visualization
    const groupedData: Record<string, any> = {}
    
    sortedEvents.forEach(event => {
      const hourKey = new Date(event.timestamp).toISOString().slice(0, 13) + ':00:00.000Z'
      
      if (!groupedData[hourKey]) {
        groupedData[hourKey] = {
          timestamp: new Date(hourKey),
          timeLabel: new Date(hourKey).toLocaleTimeString('en-US', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false 
          }),
          events: [],
          serviceUp: 0,
          serviceDown: 0,
          errors: 0,
          maintenance: 0,
          deployments: 0,
          totalEvents: 0,
          severityScore: 0 // For visualization
        }
      }
      
      groupedData[hourKey].events.push(event)
      groupedData[hourKey].totalEvents++
      
      // Count event types
      switch (event.type) {
        case 'service_up':
          groupedData[hourKey].serviceUp++
          break
        case 'service_down':
          groupedData[hourKey].serviceDown++
          break
        case 'error':
          groupedData[hourKey].errors++
          break
        case 'maintenance':
          groupedData[hourKey].maintenance++
          break
        case 'deployment':
          groupedData[hourKey].deployments++
          break
      }
      
      // Calculate severity score for visualization
      const severityWeights = { low: 1, medium: 2, high: 3, critical: 4 }
      groupedData[hourKey].severityScore += severityWeights[event.severity] || 1
    })
    
    return Object.values(groupedData).sort((a: any, b: any) => 
      a.timestamp.getTime() - b.timestamp.getTime()
    )
  }, [events])

  // Event type configuration for icons and colors
  const eventConfig = {
    service_up: {
      icon: CheckCircle,
      color: '#10B981',
      bgColor: '#D1FAE5',
      label: 'Service Up'
    },
    service_down: {
      icon: WifiOff,
      color: '#EF4444',
      bgColor: '#FEE2E2',
      label: 'Service Down'
    },
    error: {
      icon: AlertTriangle,
      color: '#F59E0B',
      bgColor: '#FEF3C7',
      label: 'Error'
    },
    maintenance: {
      icon: Settings,
      color: '#8B5CF6',
      bgColor: '#EDE9FE',
      label: 'Maintenance'
    },
    deployment: {
      icon: Upload,
      color: '#3B82F6',
      bgColor: '#DBEAFE',
      label: 'Deployment'
    }
  }

  // Severity configuration
  const severityConfig = {
    critical: { color: '#DC2626', weight: 4 },
    high: { color: '#EA580C', weight: 3 },
    medium: { color: '#CA8A04', weight: 2 },
    low: { color: '#059669', weight: 1 }
  }

  // Custom tooltip for timeline
  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length > 0) {
      const data = payload[0].payload
      const events: HealthTimelineEvent[] = data.events || []
      
      return (
        <div className="bg-white p-4 border border-gray-200 rounded-lg shadow-lg max-w-sm">
          <h4 className="font-semibold text-sm mb-2">
            {new Date(data.timestamp).toLocaleString()}
          </h4>
          <div className="space-y-2">
            {data.serviceUp > 0 && (
              <div className="flex items-center gap-2 text-sm text-green-600">
                <CheckCircle className="w-4 h-4" />
                <span>{data.serviceUp} services came online</span>
              </div>
            )}
            {data.serviceDown > 0 && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <WifiOff className="w-4 h-4" />
                <span>{data.serviceDown} services went offline</span>
              </div>
            )}
            {data.errors > 0 && (
              <div className="flex items-center gap-2 text-sm text-yellow-600">
                <AlertTriangle className="w-4 h-4" />
                <span>{data.errors} errors occurred</span>
              </div>
            )}
            {data.maintenance > 0 && (
              <div className="flex items-center gap-2 text-sm text-purple-600">
                <Settings className="w-4 h-4" />
                <span>{data.maintenance} maintenance events</span>
              </div>
            )}
            {data.deployments > 0 && (
              <div className="flex items-center gap-2 text-sm text-blue-600">
                <Upload className="w-4 h-4" />
                <span>{data.deployments} deployments</span>
              </div>
            )}
          </div>
          {events.length > 0 && (
            <div className="mt-3 pt-2 border-t">
              <div className="text-xs text-muted-foreground mb-1">Recent Events:</div>
              {events.slice(0, 3).map((event, index) => (
                <div key={index} className="text-xs text-gray-600 truncate">
                  {event.serviceName}: {event.message}
                </div>
              ))}
              {events.length > 3 && (
                <div className="text-xs text-muted-foreground">
                  +{events.length - 3} more events
                </div>
              )}
            </div>
          )}
        </div>
      )
    }
    return null
  }

  // Get time range label
  const getTimeRangeLabel = (range: string) => {
    switch (range) {
      case '1h': return 'Last Hour'
      case '6h': return 'Last 6 Hours'
      case '24h': return 'Last 24 Hours'
      case '7d': return 'Last 7 Days'
      default: return 'Timeline'
    }
  }

  // Calculate summary stats
  const summaryStats = useMemo(() => {
    return events.reduce((acc, event) => {
      acc.total++
      acc[event.type] = (acc[event.type] || 0) + 1
      acc[event.severity] = (acc[event.severity] || 0) + 1
      return acc
    }, {} as Record<string, number>)
  }, [events])

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="w-5 h-5" />
            Health Timeline
          </CardTitle>
          <Badge variant="outline">
            {getTimeRangeLabel(timeRange)}
          </Badge>
        </div>

        {/* Summary Stats */}
        <div className="flex flex-wrap gap-4 pt-2">
          <div className="flex items-center gap-2">
            <Activity className="w-4 h-4 text-muted-foreground" />
            <span className="text-sm text-muted-foreground">
              {summaryStats.total || 0} events
            </span>
          </div>
          {Object.entries(eventConfig).map(([type, config]) => {
            const count = summaryStats[type] || 0
            if (count === 0) return null
            const Icon = config.icon
            return (
              <div key={type} className="flex items-center gap-2">
                <Icon className="w-4 h-4" style={{ color: config.color }} />
                <span className="text-sm text-muted-foreground">
                  {count} {config.label.toLowerCase()}
                </span>
              </div>
            )
          })}
        </div>
      </CardHeader>

      <CardContent>
        {timelineData.length === 0 ? (
          <div className="flex items-center justify-center py-12 text-muted-foreground">
            <div className="text-center">
              <Activity className="w-12 h-12 mx-auto mb-2 opacity-50" />
              <p>No health events in the selected time range</p>
            </div>
          </div>
        ) : (
          <div className="w-full" style={{ height }}>
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={timelineData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                <XAxis 
                  dataKey="timeLabel" 
                  tick={{ fontSize: 12 }}
                  stroke="#6B7280"
                />
                <YAxis 
                  tick={{ fontSize: 12 }}
                  stroke="#6B7280"
                  label={{ value: 'Event Count', angle: -90, position: 'insideLeft' }}
                />
                <Tooltip content={<CustomTooltip />} />
                
                {/* Service status areas */}
                <Area
                  type="monotone"
                  dataKey="serviceUp"
                  stackId="1"
                  stroke="#10B981"
                  fill="#10B981"
                  fillOpacity={0.6}
                  name="Services Up"
                />
                <Area
                  type="monotone"
                  dataKey="errors"
                  stackId="1"
                  stroke="#F59E0B"
                  fill="#F59E0B"
                  fillOpacity={0.6}
                  name="Errors"
                />
                <Area
                  type="monotone"
                  dataKey="serviceDown"
                  stackId="1"
                  stroke="#EF4444"
                  fill="#EF4444"
                  fillOpacity={0.6}
                  name="Services Down"
                />
                <Area
                  type="monotone"
                  dataKey="maintenance"
                  stackId="1"
                  stroke="#8B5CF6"
                  fill="#8B5CF6"
                  fillOpacity={0.6}
                  name="Maintenance"
                />
                <Area
                  type="monotone"
                  dataKey="deployments"
                  stackId="1"
                  stroke="#3B82F6"
                  fill="#3B82F6"
                  fillOpacity={0.6}
                  name="Deployments"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Event Legend */}
        {Object.keys(summaryStats).some(key => key !== 'total' && summaryStats[key] > 0) && (
          <div className="flex flex-wrap gap-4 mt-4 pt-4 border-t">
            {Object.entries(eventConfig).map(([type, config]) => {
              const count = summaryStats[type] || 0
              if (count === 0) return null
              const Icon = config.icon
              return (
                <div key={type} className="flex items-center gap-2">
                  <div 
                    className="w-3 h-3 rounded-full" 
                    style={{ backgroundColor: config.color }}
                  />
                  <span className="text-sm text-muted-foreground">
                    {config.label} ({count})
                  </span>
                </div>
              )
            })}
          </div>
        )}

        {/* Data Status */}
        <div className="flex items-center justify-between text-sm text-muted-foreground mt-4 pt-2 border-t">
          <span>
            {timelineData.length} time periods with events
          </span>
          <span>
            Last updated: {events.length > 0 ? 
              new Date(Math.max(...events.map(e => new Date(e.timestamp).getTime()))).toLocaleTimeString() : 
              'Never'
            }
          </span>
        </div>
      </CardContent>
    </Card>
  )
}

export default HealthTimeline