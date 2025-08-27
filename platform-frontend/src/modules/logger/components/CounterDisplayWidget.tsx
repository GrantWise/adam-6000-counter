/**
 * Counter Display Widget Component
 * Displays individual counter values with CFR Part 11 compliance indicators
 * Follows LOGGER-MODULE-DESIGN.md specifications
 */

import React, { memo, useState, useEffect } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { TrendingUp, TrendingDown, Minus, FileText } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { CounterData } from '../types'
import type { DataQuality } from '@/types'

interface CounterDisplayWidgetProps {
  counter: CounterData
  compliance?: {
    showDataSource?: boolean
    showQualityIndicator?: boolean
    showTimestamp?: boolean
    enableAuditAccess?: boolean
  }
  size?: 'compact' | 'standard' | 'large'
  precision?: number
  showRates?: boolean
  showTrend?: boolean
  onAuditAccess?: (counter: CounterData) => void
  onAlert?: (counter: CounterData, alertType: string) => void
  className?: string
}

const formatTimestamp = (date: Date): string => {
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
  })
}

const getTrendIcon = (trend: CounterData['trend']) => {
  switch (trend) {
    case 'up':
      return <TrendingUp className="w-4 h-4" />
    case 'down':
      return <TrendingDown className="w-4 h-4" />
    case 'stable':
    default:
      return <Minus className="w-4 h-4" />
  }
}

const useUpdateFlash = () => {
  const [isFlashing, setIsFlashing] = useState(false)
  
  const triggerFlash = () => {
    setIsFlashing(true)
    setTimeout(() => setIsFlashing(false), 300)
  }
  
  return { isFlashing, triggerFlash }
}

export const CounterDisplayWidget = memo<CounterDisplayWidgetProps>(({
  counter,
  compliance = {
    showDataSource: false,
    showQualityIndicator: true,
    showTimestamp: false,
    enableAuditAccess: false
  },
  size = 'standard',
  precision = 0,
  showRates = true,
  showTrend = true,
  onAuditAccess,
  onAlert,
  className
}) => {
  const [isUpdating, setIsUpdating] = useState(false)
  const { isFlashing, triggerFlash } = useUpdateFlash()
  
  // Flash effect for real-time updates
  useEffect(() => {
    triggerFlash()
    setIsUpdating(true)
    const timer = setTimeout(() => setIsUpdating(false), 300)
    return () => clearTimeout(timer)
  }, [counter.currentValue, triggerFlash])

  const trendIcon = showTrend ? getTrendIcon(counter.trend) : null

  return (
    <Card 
      className={cn(
        'counter-display-widget relative transition-all duration-200',
        isUpdating && 'ring-2 ring-primary-200 bg-primary-50',
        isFlashing && 'animate-pulse',
        size === 'compact' && 'p-3',
        size === 'standard' && 'p-4',
        size === 'large' && 'p-6',
        className
      )}
    >
      {/* CFR Part 11 Data Quality Header */}
      {compliance.showQualityIndicator && (
        <div className="flex items-center justify-between mb-3">
          <DataQualityIndicator 
            quality={counter.dataQuality}
            size={size === 'compact' ? 'sm' : 'md'}
            detailed
          />
          {compliance.enableAuditAccess && onAuditAccess && (
            <Button 
              size="xs" 
              variant="ghost"
              onClick={() => onAuditAccess(counter)}
              className="opacity-60 hover:opacity-100"
            >
              <FileText className="w-3 h-3 mr-1" />
              Audit
            </Button>
          )}
        </div>
      )}
      
      {/* Device and channel identification */}
      <div className="mb-2">
        <h4 className={cn(
          'font-medium text-neutral-700',
          size === 'compact' && 'text-xs',
          size === 'standard' && 'text-sm',
          size === 'large' && 'text-base'
        )}>
          {counter.deviceName}
        </h4>
        <p className={cn(
          'text-neutral-500',
          size === 'compact' && 'text-xs',
          size === 'standard' && 'text-xs',
          size === 'large' && 'text-sm'
        )}>
          Channel {counter.channel}: {counter.channelName}
        </p>
      </div>
      
      {/* Main counter value */}
      <div className="flex items-baseline space-x-2 mb-3">
        {/* Display value or "Data Not Available" for CFR Part 11 compliance */}
        {counter.dataQuality === 'unavailable' || !counter.isRealData ? (
          <div className="flex flex-col">
            <span className={cn(
              'font-mono font-bold text-status-error',
              size === 'compact' && 'text-lg',
              size === 'standard' && 'text-2xl',
              size === 'large' && 'text-4xl'
            )}>
              N/A
            </span>
            <span className="text-xs text-status-error font-medium">
              Data Not Available
            </span>
          </div>
        ) : (
          <span className={cn(
            'font-mono font-bold text-neutral-900',
            size === 'compact' && 'text-xl',
            size === 'standard' && 'text-2xl',
            size === 'large' && 'text-4xl'
          )}>
            {counter.currentValue.toLocaleString(undefined, {
              minimumFractionDigits: precision,
              maximumFractionDigits: precision
            })}
          </span>
        )}
        
        <span className={cn(
          'text-neutral-600 font-medium',
          size === 'compact' && 'text-xs',
          size === 'standard' && 'text-sm',
          size === 'large' && 'text-base'
        )}>
          {counter.unit}
        </span>
        
        {/* Trend indicator */}
        {trendIcon && counter.isRealData && (
          <span className={cn(
            'ml-2',
            counter.trend === 'up' && 'text-status-success',
            counter.trend === 'down' && 'text-status-error',
            counter.trend === 'stable' && 'text-neutral-500'
          )}>
            {trendIcon}
          </span>
        )}
      </div>
      
      {/* Rate information */}
      {showRates && counter.isRealData && counter.dataQuality !== 'unavailable' && (
        <div className="grid grid-cols-2 gap-2 mb-3 text-sm">
          <div>
            <span className="text-neutral-600">Rate/min:</span>
            <span className="ml-1 font-medium text-neutral-900">
              {counter.ratePerMinute.toFixed(1)}
            </span>
          </div>
          <div>
            <span className="text-neutral-600">Rate/hr:</span>
            <span className="ml-1 font-medium text-neutral-900">
              {counter.ratePerHour.toLocaleString()}
            </span>
          </div>
        </div>
      )}
      
      {/* CFR Part 11 Compliance footer */}
      <div className="text-xs text-neutral-500 border-t pt-2">
        {compliance.showDataSource && (
          <div>Source: {counter.deviceId} | Ch{counter.channel}</div>
        )}
        {compliance.showTimestamp && (
          <div>Updated: {formatTimestamp(counter.lastUpdate)}</div>
        )}
        
        {/* Real data indicator */}
        <div className="flex items-center justify-between mt-1">
          <span className={cn(
            'font-medium',
            counter.isRealData && counter.dataQuality === 'good' 
              ? 'text-status-success' 
              : 'text-status-error'
          )}>
            {counter.isRealData 
              ? '✓ Real Data' 
              : '⚠️ Data Unavailable'
            }
          </span>
          
          {/* Data quality badge */}
          <span className={cn(
            'px-1 py-0.5 rounded text-xs font-medium',
            counter.dataQuality === 'good' && 'bg-green-100 text-green-700',
            counter.dataQuality === 'uncertain' && 'bg-yellow-100 text-yellow-700',
            counter.dataQuality === 'bad' && 'bg-red-100 text-red-700',
            counter.dataQuality === 'unavailable' && 'bg-gray-100 text-gray-700'
          )}>
            {counter.dataQuality.toUpperCase()}
          </span>
        </div>
        
        {/* Warning for non-compliant data */}
        {!counter.isRealData && (
          <div className="text-status-error font-medium text-xs mt-1">
            ⚠️ CFR Part 11: No real data available for regulatory use
          </div>
        )}
      </div>
    </Card>
  )
})

CounterDisplayWidget.displayName = 'CounterDisplayWidget'

export default CounterDisplayWidget