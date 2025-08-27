import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tooltip } from '@/components/ui/tooltip'
import { Activity, Clock, AlertTriangle, CheckCircle, XCircle, Wifi } from 'lucide-react'
import type { ServiceStatusCardProps } from '@/types'

/**
 * ServiceStatusCard - Display individual service health with traffic light indicators
 * Shows service status, uptime, response time, and error count with visual indicators
 */
const ServiceStatusCard: React.FC<ServiceStatusCardProps> = ({ service, onClick }) => {
  // Status configuration for visual indicators
  const statusConfig = {
    healthy: {
      icon: CheckCircle,
      color: 'bg-green-500',
      textColor: 'text-green-700',
      bgColor: 'bg-green-50',
      borderColor: 'border-green-200',
      badge: 'bg-green-100 text-green-800'
    },
    warning: {
      icon: AlertTriangle,
      color: 'bg-yellow-500',
      textColor: 'text-yellow-700',
      bgColor: 'bg-yellow-50',
      borderColor: 'border-yellow-200',
      badge: 'bg-yellow-100 text-yellow-800'
    },
    error: {
      icon: XCircle,
      color: 'bg-red-500',
      textColor: 'text-red-700',
      bgColor: 'bg-red-50',
      borderColor: 'border-red-200',
      badge: 'bg-red-100 text-red-800'
    },
    offline: {
      icon: Wifi,
      color: 'bg-gray-500',
      textColor: 'text-gray-700',
      bgColor: 'bg-gray-50',
      borderColor: 'border-gray-200',
      badge: 'bg-gray-100 text-gray-800'
    }
  }

  const config = statusConfig[service.status]
  const StatusIcon = config.icon

  // Format uptime as human readable
  const formatUptime = (uptimeMs: number): string => {
    const seconds = Math.floor(uptimeMs / 1000)
    const minutes = Math.floor(seconds / 60)
    const hours = Math.floor(minutes / 60)
    const days = Math.floor(hours / 24)

    if (days > 0) return `${days}d ${hours % 24}h`
    if (hours > 0) return `${hours}h ${minutes % 60}m`
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`
    return `${seconds}s`
  }

  // Format response time with appropriate units
  const formatResponseTime = (timeMs: number): string => {
    if (timeMs >= 1000) {
      return `${(timeMs / 1000).toFixed(2)}s`
    }
    return `${Math.round(timeMs)}ms`
  }

  // Get status display text
  const getStatusText = (): string => {
    switch (service.status) {
      case 'healthy': return 'Healthy'
      case 'warning': return 'Warning'
      case 'error': return 'Error'
      case 'offline': return 'Offline'
      default: return 'Unknown'
    }
  }

  return (
    <Card 
      className={`
        transition-all duration-200 hover:shadow-md cursor-pointer
        ${config.borderColor} border-2
        ${onClick ? 'hover:scale-[1.02]' : ''}
      `}
      onClick={onClick}
    >
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center justify-between text-base">
          <div className="flex items-center gap-2">
            <div className={`w-3 h-3 rounded-full ${config.color} animate-pulse`} />
            <span className="truncate">{service.serviceName}</span>
          </div>
          <Badge variant="outline" className={config.badge}>
            {getStatusText()}
          </Badge>
        </CardTitle>
      </CardHeader>

      <CardContent className="space-y-3">
        {/* Status Icon and Main Info */}
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${config.bgColor}`}>
            <StatusIcon className={`w-5 h-5 ${config.textColor}`} />
          </div>
          <div className="flex-1 min-w-0">
            <div className="text-sm text-muted-foreground">
              Endpoint: <span className="font-mono text-xs">{service.endpoint}</span>
            </div>
            {service.version && (
              <div className="text-xs text-muted-foreground">
                Version: {service.version}
              </div>
            )}
          </div>
        </div>

        {/* Metrics Grid */}
        <div className="grid grid-cols-2 gap-4">
          {/* Uptime */}
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 text-muted-foreground" />
            <div className="min-w-0">
              <div className="text-xs text-muted-foreground">Uptime</div>
              <div className="text-sm font-medium truncate">
                {formatUptime(service.uptime)}
              </div>
            </div>
          </div>

          {/* Response Time */}
          <div className="flex items-center gap-2">
            <Activity className="w-4 h-4 text-muted-foreground" />
            <div className="min-w-0">
              <div className="text-xs text-muted-foreground">Response</div>
              <div className={`text-sm font-medium ${
                service.responseTimeMs > 1000 ? 'text-red-600' : 
                service.responseTimeMs > 500 ? 'text-yellow-600' : 
                'text-green-600'
              }`}>
                {formatResponseTime(service.responseTimeMs)}
              </div>
            </div>
          </div>
        </div>

        {/* Error Count */}
        {service.errorCount > 0 && (
          <div className="flex items-center justify-between p-2 bg-red-50 rounded-md">
            <span className="text-sm text-red-700">Errors (24h)</span>
            <Badge variant="destructive" className="text-xs">
              {service.errorCount.toLocaleString()}
            </Badge>
          </div>
        )}

        {/* Last Check */}
        <div className="text-xs text-muted-foreground text-center pt-2 border-t">
          Last checked: {new Date(service.lastCheck).toLocaleTimeString()}
        </div>
      </CardContent>
    </Card>
  )
}

export default ServiceStatusCard