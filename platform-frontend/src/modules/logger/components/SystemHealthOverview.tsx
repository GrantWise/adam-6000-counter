/**
 * System Health Overview Component
 * Displays system-wide health metrics and status indicators
 */

import React from 'react'
import { Card } from '@/components/ui/card'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { Activity, Database, Wifi, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { DeviceData, CounterData } from '../types'

interface SystemHealthOverviewProps {
  summary?: {
    total: number
    online: number
    offline: number
    error: number
    lastSync: Date
    dataPointsToday: number
    avgResponseTime: number
  } | null
  devices: DeviceData[]
  counters: CounterData[]
  connectionState: 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'
  size?: 'standard' | 'large'
  simplified?: boolean
  detailed?: boolean
  className?: string
}

const formatRelativeTime = (date: Date): string => {
  const now = new Date()
  const diff = Math.floor((now.getTime() - date.getTime()) / 1000)
  
  if (diff < 60) return `${diff}s ago`
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
  return `${Math.floor(diff / 86400)}d ago`
}

export const SystemHealthOverview: React.FC<SystemHealthOverviewProps> = ({
  summary,
  devices,
  counters,
  connectionState,
  size = 'standard',
  simplified = false,
  detailed = false,
  className
}) => {
  // Calculate real-time metrics
  const onlineDevices = devices.filter(d => d.status === 'online').length
  const totalCounters = counters.length
  const goodQualityCounters = counters.filter(c => c.dataQuality === 'good').length
  const unavailableCounters = counters.filter(c => c.dataQuality === 'unavailable').length
  
  // System status determination
  const isConnected = connectionState === 'Connected'
  const hasOfflineDevices = devices.some(d => d.status === 'offline')
  const hasDataQualityIssues = unavailableCounters > 0
  
  const systemStatus: 'healthy' | 'warning' | 'error' = 
    !isConnected || devices.length === 0 ? 'error' :
    hasOfflineDevices || hasDataQualityIssues ? 'warning' : 'healthy'

  if (simplified) {
    return (
      <Card className={cn('system-health-overview p-6 bg-gradient-to-r from-neutral-50 to-primary-50', className)}>
        <div className="text-center">
          <div className="mb-4">
            <StatusIndicator 
              status={systemStatus === 'healthy' ? 'success' : systemStatus === 'warning' ? 'warning' : 'error'}
              size="xl"
            />
          </div>
          
          <h2 className={cn(
            'font-bold text-neutral-900 mb-2',
            size === 'large' ? 'text-3xl' : 'text-2xl'
          )}>
            System Status: {systemStatus === 'healthy' ? 'Healthy' : systemStatus === 'warning' ? 'Warning' : 'Error'}
          </h2>
          
          <div className="grid grid-cols-2 gap-4 mt-6">
            <div className="text-center">
              <div className={cn(
                'font-bold text-neutral-900',
                size === 'large' ? 'text-4xl' : 'text-3xl'
              )}>
                {onlineDevices}/{devices.length}
              </div>
              <div className="text-sm text-neutral-600">Devices Online</div>
            </div>
            <div className="text-center">
              <div className={cn(
                'font-bold',
                goodQualityCounters === totalCounters ? 'text-status-success' : 'text-status-warning',
                size === 'large' ? 'text-4xl' : 'text-3xl'
              )}>
                {goodQualityCounters}/{totalCounters}
              </div>
              <div className="text-sm text-neutral-600">Good Data Quality</div>
            </div>
          </div>

          {/* Connection status */}
          <div className="mt-4 flex items-center justify-center space-x-2">
            <Wifi className={cn(
              'w-4 h-4',
              isConnected ? 'text-status-success' : 'text-status-error'
            )} />
            <span className="text-sm text-neutral-600">
              Real-time: {connectionState}
            </span>
          </div>
        </div>
      </Card>
    )
  }

  return (
    <Card className={cn('system-health-overview', className)}>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-3">
            <Activity className="w-6 h-6 text-primary-600" />
            <h3 className="text-xl font-semibold text-neutral-900">System Health Overview</h3>
          </div>
          
          <div className="flex items-center space-x-4">
            <StatusIndicator 
              status={systemStatus === 'healthy' ? 'success' : systemStatus === 'warning' ? 'warning' : 'error'}
              label={systemStatus.toUpperCase()}
            />
            <div className="text-sm text-neutral-600">
              Last sync: {summary?.lastSync ? formatRelativeTime(summary.lastSync) : 'Never'}
            </div>
          </div>
        </div>

        {/* Key Metrics Grid */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <MetricCard
            title="Online Devices"
            value={onlineDevices}
            total={devices.length}
            format="fraction"
            status={onlineDevices === devices.length ? 'success' : 'warning'}
            icon={<Database className="w-4 h-4" />}
          />
          
          <MetricCard
            title="Good Data Quality"
            value={goodQualityCounters}
            total={totalCounters}
            format="fraction" 
            status={goodQualityCounters === totalCounters ? 'success' : 'warning'}
            icon={<DataQualityIndicator quality="good" size="sm" showTooltip={false} />}
          />
          
          <MetricCard
            title="Data Points Today"
            value={summary?.dataPointsToday || 0}
            format="number"
            trend="up"
            icon={<Activity className="w-4 h-4" />}
          />
          
          <MetricCard
            title="Avg Response Time"
            value={summary?.avgResponseTime || 0}
            unit="ms"
            format="number"
            status={summary && summary.avgResponseTime < 100 ? 'success' : 'warning'}
            icon={<Wifi className="w-4 h-4" />}
          />
        </div>

        {/* Connection Status Banner */}
        <div className={cn(
          'p-4 rounded-lg mb-4',
          isConnected ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'
        )}>
          <div className="flex items-center space-x-3">
            <Wifi className={cn(
              'w-5 h-5',
              isConnected ? 'text-status-success' : 'text-status-error'
            )} />
            <div>
              <div className={cn(
                'font-semibold',
                isConnected ? 'text-green-800' : 'text-red-800'
              )}>
                Real-time Connection: {connectionState}
              </div>
              <div className={cn(
                'text-sm',
                isConnected ? 'text-green-600' : 'text-red-600'
              )}>
                {isConnected 
                  ? 'Live data updates active'
                  : 'Real-time updates unavailable - displaying last known values'
                }
              </div>
            </div>
          </div>
        </div>

        {/* Data Quality Issues */}
        {hasDataQualityIssues && (
          <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
            <div className="flex items-start space-x-3">
              <AlertTriangle className="w-5 h-5 text-yellow-600 flex-shrink-0 mt-0.5" />
              <div>
                <div className="font-semibold text-yellow-800">Data Quality Alert</div>
                <div className="text-sm text-yellow-700">
                  {unavailableCounters} counter(s) have unavailable data. CFR Part 11 compliance requires investigation.
                </div>
                <div className="mt-2 text-xs text-yellow-600">
                  Affected counters are marked with data quality indicators. No synthetic data is displayed.
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Detailed System Information */}
        {detailed && summary && (
          <div className="mt-6 grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <h4 className="font-semibold text-neutral-900 mb-3">Device Status Distribution</h4>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Online</span>
                  <span className="text-sm font-medium text-status-success">{summary.online}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Offline</span>
                  <span className="text-sm font-medium text-status-error">{summary.offline}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Error</span>
                  <span className="text-sm font-medium text-status-error">{summary.error}</span>
                </div>
              </div>
            </div>
            
            <div>
              <h4 className="font-semibold text-neutral-900 mb-3">Data Quality Distribution</h4>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Good Quality</span>
                  <span className="text-sm font-medium text-status-success">{goodQualityCounters}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Uncertain</span>
                  <span className="text-sm font-medium text-status-warning">
                    {counters.filter(c => c.dataQuality === 'uncertain').length}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Bad/Unavailable</span>
                  <span className="text-sm font-medium text-status-error">
                    {counters.filter(c => c.dataQuality === 'bad' || c.dataQuality === 'unavailable').length}
                  </span>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </Card>
  )
}

export default SystemHealthOverview