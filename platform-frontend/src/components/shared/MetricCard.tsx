import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { StatusIndicator } from './StatusIndicator'
import { cn, formatNumber } from '@/lib/utils'
import { TrendingUp, TrendingDown, Minus, LucideIcon } from 'lucide-react'
import type { ComponentStatus } from '@/types'

interface TrendData {
  direction: 'up' | 'down' | 'stable'
  percentage: number
  period: string
}

interface MetricCardProps {
  title: string
  value: string | number
  unit?: string
  trend?: TrendData
  status?: 'healthy' | 'warning' | 'error' | 'offline' | 'unknown'
  icon?: LucideIcon
  loading?: boolean
  className?: string
  size?: 'default' | 'compact' | 'large'
  onClick?: () => void
}

/**
 * Industrial metric display card with status indication and trend analysis
 * Optimized for dashboard use with clear visual hierarchy
 */
export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  unit = '',
  trend,
  status,
  icon: Icon,
  loading = false,
  className,
  size = 'default',
  onClick
}) => {
  const formatValue = (val: string | number): string => {
    if (typeof val === 'number') {
      return formatNumber(val, { 
        decimals: val % 1 === 0 ? 0 : 2,
        compact: size === 'compact'
      })
    }
    return val
  }

  const getTrendIcon = () => {
    if (!trend) return null
    
    switch (trend.direction) {
      case 'up':
        return <TrendingUp className="w-4 h-4" />
      case 'down':
        return <TrendingDown className="w-4 h-4" />
      case 'stable':
      default:
        return <Minus className="w-4 h-4" />
    }
  }

  const getTrendColor = () => {
    if (!trend) return 'text-muted-foreground'
    
    switch (trend.direction) {
      case 'up':
        return 'text-status-success'
      case 'down':
        return 'text-status-error'
      case 'stable':
      default:
        return 'text-muted-foreground'
    }
  }

  if (loading) {
    return (
      <Card className={cn("animate-pulse", className)}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <div className="h-4 bg-muted rounded w-24"></div>
            <div className="h-6 bg-muted rounded w-16"></div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <div className="h-8 bg-muted rounded w-20"></div>
            <div className="h-4 bg-muted rounded w-16"></div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card 
      className={cn(
        "transition-all duration-200",
        {
          "hover:shadow-md cursor-pointer hover:bg-accent/50": onClick,
          "h-32": size === 'compact',
          "h-40": size === 'default',
          "h-48": size === 'large',
        },
        className
      )}
      onClick={onClick}
    >
      <CardHeader className={cn(
        "pb-2",
        {
          "pb-1": size === 'compact',
          "pb-3": size === 'large',
        }
      )}>
        <div className="flex items-center justify-between">
          <CardTitle className={cn(
            "font-medium text-muted-foreground",
            {
              "text-xs": size === 'compact',
              "text-sm": size === 'default',
              "text-base": size === 'large',
            }
          )}>
            {title}
          </CardTitle>
          <div className="flex items-center gap-2">
            {status && (
              <StatusIndicator
                status={status}
                label=""
                size={size === 'compact' ? 'sm' : 'default'}
              />
            )}
            {Icon && (
              <Icon className={cn(
                "text-muted-foreground",
                {
                  "w-4 h-4": size === 'compact',
                  "w-5 h-5": size === 'default',
                  "w-6 h-6": size === 'large',
                }
              )} />
            )}
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        <div className="space-y-2">
          <div className="flex items-baseline">
            <span className={cn(
              "font-bold text-foreground",
              {
                "text-xl": size === 'compact',
                "text-2xl": size === 'default',
                "text-3xl": size === 'large',
              }
            )}>
              {formatValue(value)}
            </span>
            {unit && (
              <span className={cn(
                "text-muted-foreground ml-1",
                {
                  "text-sm": size === 'compact',
                  "text-base": size === 'default',
                  "text-lg": size === 'large',
                }
              )}>
                {unit}
              </span>
            )}
          </div>
          
          {trend && (
            <div className={cn(
              "flex items-center gap-1",
              getTrendColor(),
              {
                "text-xs": size === 'compact',
                "text-sm": size === 'default' || size === 'large',
              }
            )}>
              {getTrendIcon()}
              <span>
                {trend.direction === 'stable' ? 'No change' : `${trend.percentage}%`}
              </span>
              <span className="text-muted-foreground">
                vs {trend.period}
              </span>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

// Specialized metric cards for common industrial use cases
export const OEEMetricCard: React.FC<{
  title: string
  value: number
  target?: number
  className?: string
}> = ({ title, value, target, className }) => {
  const getStatus = () => {
    if (!target) return undefined
    if (value >= target * 0.95) return 'healthy'
    if (value >= target * 0.8) return 'warning'
    return 'error'
  }

  return (
    <MetricCard
      title={title}
      value={value}
      unit="%"
      status={getStatus()}
      className={className}
    />
  )
}

export const CounterMetricCard: React.FC<{
  title: string
  value: number
  rate?: number
  unit?: string
  className?: string
}> = ({ title, value, rate, unit = 'counts', className }) => {
  return (
    <Card className={className}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-1">
          <div className="flex items-baseline">
            <span className="text-2xl font-bold">
              {formatNumber(value, { decimals: 0 })}
            </span>
            <span className="text-sm text-muted-foreground ml-1">
              {unit}
            </span>
          </div>
          {rate !== undefined && (
            <div className="text-sm text-muted-foreground">
              Rate: {formatNumber(rate, { decimals: 2 })}/min
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}