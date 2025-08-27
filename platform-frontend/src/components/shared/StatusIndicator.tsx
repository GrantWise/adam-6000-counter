import React from 'react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { StatusIndicatorProps } from '@/types'

/**
 * Industrial status indicator component with high contrast colors
 * Optimized for factory floor visibility with clear status states
 */
export const StatusIndicator: React.FC<StatusIndicatorProps> = ({
  status,
  label,
  description,
  pulse = false,
  size = 'default',
}) => {
  const getStatusColor = () => {
    switch (status) {
      case 'healthy':
        return 'success'
      case 'warning':
        return 'warning'
      case 'error':
        return 'error'
      case 'offline':
        return 'neutral'
      case 'unknown':
      default:
        return 'neutral'
    }
  }

  const badgeSize = size === 'lg' ? 'industrial' : size === 'sm' ? 'sm' : 'default'

  return (
    <div className="inline-flex items-center gap-2">
      <Badge
        variant={getStatusColor()}
        size={badgeSize}
        className={cn(
          "font-semibold",
          {
            'animate-pulse': pulse,
            'text-xs': size === 'sm',
            'text-sm': size === 'default',
            'text-base': size === 'lg',
          }
        )}
      >
        {label}
      </Badge>
      {description && (
        <span className={cn(
          "text-muted-foreground",
          {
            'text-xs': size === 'sm',
            'text-sm': size === 'default' || size === 'lg',
          }
        )}>
          {description}
        </span>
      )}
    </div>
  )
}

// Color-coded dot indicator for compact displays
export const StatusDot: React.FC<{
  status: StatusIndicatorProps['status']
  size?: 'sm' | 'md' | 'lg'
  pulse?: boolean
}> = ({ status, size = 'md', pulse = false }) => {
  const sizeClasses = {
    sm: 'w-2 h-2',
    md: 'w-3 h-3', 
    lg: 'w-4 h-4'
  }

  const colorClasses = {
    healthy: 'bg-status-success',
    warning: 'bg-status-warning',
    error: 'bg-status-error',
    offline: 'bg-status-neutral',
    unknown: 'bg-status-neutral'
  }

  return (
    <div
      className={cn(
        'rounded-full',
        sizeClasses[size],
        colorClasses[status],
        {
          'animate-pulse': pulse
        }
      )}
      aria-label={`Status: ${status}`}
    />
  )
}