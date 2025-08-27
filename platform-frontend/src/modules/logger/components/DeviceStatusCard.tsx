/**
 * Device Status Card Component
 * Displays device status, counters, and health metrics with CFR Part 11 compliance
 * Follows LOGGER-MODULE-DESIGN.md specifications
 */

import React, { memo } from 'react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { Settings, Zap, Eye, Activity, Wifi, WifiOff } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { DeviceData, CounterData } from '../types'

interface DeviceStatusCardProps {
  device: DeviceData
  counters?: CounterData[]
  size?: 'compact' | 'standard' | 'detailed'
  showActions?: boolean
  showHealth?: boolean
  selectable?: boolean
  onSelect?: (device: DeviceData) => void
  onConfigure?: (device: DeviceData) => void
  onTest?: (device: DeviceData) => void
  onViewDetails?: (device: DeviceData) => void
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

const getConnectionQualityIcon = (quality: DeviceData['connectionQuality']) => {
  switch (quality) {
    case 'excellent':
      return <Wifi className="w-4 h-4 text-status-success" />
    case 'good':
      return <Wifi className="w-4 h-4 text-primary-500" />
    case 'poor':
      return <Wifi className="w-4 h-4 text-status-warning" />
    case 'offline':
    default:
      return <WifiOff className="w-4 h-4 text-status-error" />
  }
}

const getStatusColor = (status: DeviceData['status']): string => {
  switch (status) {
    case 'online': return 'status-success'
    case 'warning': return 'status-warning'
    case 'error': return 'status-error'
    case 'offline':
    default: return 'status-error'
  }
}

export const DeviceStatusCard = memo<DeviceStatusCardProps>(({
  device,
  counters = [],
  size = 'standard',
  showActions = false,
  showHealth = false,
  selectable = false,
  onSelect,
  onConfigure,
  onTest,
  onViewDetails,
  className
}) => {
  const statusColor = getStatusColor(device.status)
  const qualityIcon = getConnectionQualityIcon(device.connectionQuality)
  
  // Calculate real-time counter metrics from provided counter data
  const totalCounts = counters.reduce((sum, counter) => sum + counter.currentValue, 0)
  const avgRate = counters.length > 0 
    ? counters.reduce((sum, counter) => sum + counter.ratePerHour, 0) / counters.length 
    : 0

  // Determine data quality from counters
  const hasGoodData = counters.some(c => c.dataQuality === 'good')
  const hasUnavailableData = counters.some(c => c.dataQuality === 'unavailable')
  
  return (
    <Card 
      className={cn(
        'device-status-card relative transition-all duration-200',
        'border-l-4',
        device.status === 'online' && 'border-l-status-success',
        device.status === 'offline' && 'border-l-status-error',
        device.status === 'warning' && 'border-l-status-warning',
        device.status === 'error' && 'border-l-status-error',
        size === 'compact' && 'p-4',
        size === 'standard' && 'p-6',
        size === 'detailed' && 'p-8',
        selectable && 'hover:bg-primary-50 cursor-pointer hover:shadow-lg',
        className
      )}
      onClick={() => onSelect?.(device)}
    >
      {/* Header with status and device info */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center space-x-3">
          <StatusIndicator 
            status={device.status === 'online' ? 'success' : 'error'} 
            size={size === 'compact' ? 'sm' : 'lg'}
            pulse={device.status === 'warning'}
          />
          <div>
            <h3 className={cn(
              'font-semibold text-neutral-900',
              size === 'compact' && 'text-sm',
              size === 'standard' && 'text-lg',
              size === 'detailed' && 'text-xl'
            )}>
              {device.name}
            </h3>
            <p className={cn(
              'text-neutral-600',
              size === 'compact' && 'text-xs',
              size === 'standard' && 'text-sm',
              size === 'detailed' && 'text-base'
            )}>
              {device.type} • {device.ipAddress}
            </p>
          </div>
        </div>
        
        <div className="flex items-center space-x-2">
          {qualityIcon}
          <Badge 
            variant={device.status === 'online' ? 'default' : 'secondary'}
            className={cn(
              device.status === 'online' && 'bg-status-success text-white',
              device.status === 'offline' && 'bg-status-error text-white',
              device.status === 'warning' && 'bg-status-warning text-white'
            )}
          >
            {device.status.toUpperCase()}
          </Badge>
        </div>
      </div>
      
      {/* Counter metrics with CFR Part 11 compliance */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <div className="text-sm text-neutral-600">Active Counters</div>
          <div className={cn(
            'font-semibold text-neutral-900',
            size === 'compact' && 'text-base',
            size === 'standard' && 'text-lg',
            size === 'detailed' && 'text-xl'
          )}>
            {device.counters.active}/{device.counters.total}
          </div>
        </div>
        
        <div>
          <div className="text-sm text-neutral-600">Total Counts</div>
          <div className={cn(
            'font-semibold text-neutral-900',
            size === 'compact' && 'text-base',
            size === 'standard' && 'text-lg',
            size === 'detailed' && 'text-xl'
          )}>
            {totalCounts > 0 ? totalCounts.toLocaleString() : 'N/A'}
          </div>
          {hasUnavailableData && (
            <DataQualityIndicator quality="unavailable" size="xs" className="mt-1" />
          )}
        </div>
        
        <div>
          <div className="text-sm text-neutral-600">Counts/Hour</div>
          <div className={cn(
            'font-semibold text-neutral-900',
            size === 'compact' && 'text-base',
            size === 'standard' && 'text-lg',
            size === 'detailed' && 'text-xl'
          )}>
            {avgRate > 0 ? Math.round(avgRate).toLocaleString() : 'N/A'}
          </div>
        </div>
        
        <div>
          <div className="text-sm text-neutral-600">Last Seen</div>
          <div className={cn(
            'font-semibold text-neutral-900',
            size === 'compact' && 'text-base',
            size === 'standard' && 'text-lg',
            size === 'detailed' && 'text-xl'
          )}>
            {formatRelativeTime(device.lastSeen)}
          </div>
        </div>
      </div>

      {/* Health metrics (detailed view only) */}
      {size === 'detailed' && showHealth && device.health && (
        <div className="mb-4 p-3 bg-neutral-50 rounded-md">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2">Health Metrics</h4>
          <div className="grid grid-cols-3 gap-3 text-sm">
            <div>
              <span className="text-neutral-600">Uptime:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {device.health.uptime.toFixed(1)}%
              </span>
            </div>
            <div>
              <span className="text-neutral-600">Response:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {device.health.responseTime.current}ms
              </span>
            </div>
            <div>
              <span className="text-neutral-600">Errors (24h):</span>
              <span className="ml-1 font-medium text-neutral-900">
                {device.health.errorCount.last24h}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Counter data quality indicators */}
      {counters.length > 0 && (
        <div className="mb-4">
          <div className="text-sm text-neutral-600 mb-2">Channel Data Quality</div>
          <div className="flex flex-wrap gap-2">
            {counters.map(counter => (
              <div key={`${counter.deviceId}-${counter.channel}`} className="flex items-center space-x-1">
                <span className="text-xs text-neutral-500">Ch{counter.channel}:</span>
                <DataQualityIndicator 
                  quality={counter.dataQuality} 
                  size="xs"
                  showTooltip={false}
                />
              </div>
            ))}
          </div>
        </div>
      )}
      
      {/* Quick actions */}
      {showActions && (
        <div className="flex space-x-2 pt-4 border-t border-neutral-200">
          {onConfigure && (
            <Button 
              size="sm" 
              variant="outline"
              onClick={(e) => {
                e.stopPropagation()
                onConfigure(device)
              }}
              className="flex-1"
            >
              <Settings className="w-4 h-4 mr-1" />
              Configure
            </Button>
          )}
          
          {onTest && (
            <Button 
              size="sm" 
              variant="outline"
              onClick={(e) => {
                e.stopPropagation()
                onTest(device)
              }}
              className="flex-1"
            >
              <Zap className="w-4 h-4 mr-1" />
              Test
            </Button>
          )}

          {onViewDetails && (
            <Button 
              size="sm" 
              variant="outline"
              onClick={(e) => {
                e.stopPropagation()
                onViewDetails(device)
              }}
              className="flex-1"
            >
              <Eye className="w-4 h-4 mr-1" />
              Details
            </Button>
          )}
        </div>
      )}

      {/* CFR Part 11 Data Source Footer */}
      <div className="text-xs text-neutral-500 border-t pt-2 mt-4">
        <div>Device ID: {device.id}</div>
        <div>Last Update: {formatRelativeTime(device.lastSeen)}</div>
        {!hasGoodData && counters.length > 0 && (
          <div className="text-status-warning font-medium mt-1">
            ⚠️ No good quality data available
          </div>
        )}
      </div>
    </Card>
  )
})

DeviceStatusCard.displayName = 'DeviceStatusCard'

export default DeviceStatusCard