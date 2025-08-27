/**
 * OEE Metric Card Component
 * Displays OEE metrics with component breakdown and CFR Part 11 compliance
 * Implementation based on OEE-MODULE-DESIGN.md specifications
 */

import React, { memo } from 'react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { Shield, TrendingUp, Download } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { OeeData, OeeLevel } from '../types'

interface OeeMetricCardProps {
  oeeData: OeeData
  size?: 'compact' | 'standard' | 'large' | 'executive'
  showComponents?: boolean
  showTrend?: boolean
  showTarget?: boolean
  trendPeriod?: '1h' | '4h' | '24h' | '7d'
  auditMode?: boolean
  onDrillDown?: (component: 'availability' | 'performance' | 'quality') => void
  onExport?: () => void
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

const getOeeLevel = (oee: number | null): OeeLevel => {
  if (oee === null) return 'poor'
  if (oee >= 85) return 'excellent'
  if (oee >= 65) return 'good'
  if (oee >= 45) return 'needs-improvement'
  return 'poor'
}

const getOeeLevelClasses = (level: OeeLevel) => {
  const classes = {
    excellent: {
      bg: 'bg-status-success-light',
      text: 'text-status-success',
      border: 'border-l-status-success'
    },
    good: {
      bg: 'bg-primary-100',
      text: 'text-primary-700',
      border: 'border-l-primary-500'
    },
    'needs-improvement': {
      bg: 'bg-status-warning-light',
      text: 'text-status-warning',
      border: 'border-l-status-warning'
    },
    poor: {
      bg: 'bg-status-error-light',
      text: 'text-status-error',
      border: 'border-l-status-error'
    }
  }
  
  return classes[level]
}

const getOeeLevelLabel = (level: OeeLevel): string => {
  const labels = {
    excellent: 'Excellent',
    good: 'Good',
    'needs-improvement': 'Needs Improvement',
    poor: 'Poor'
  }
  
  return labels[level]
}

export const OeeMetricCard = memo<OeeMetricCardProps>(({
  oeeData,
  size = 'standard',
  showComponents = true,
  showTrend = true,
  showTarget = true,
  trendPeriod = '24h',
  auditMode = false,
  onDrillDown,
  onExport,
  className
}) => {
  const oeeLevel = getOeeLevel(oeeData.oee)
  const variance = oeeData.target && oeeData.oee !== null 
    ? oeeData.oee - oeeData.target 
    : null
  
  const hasRealData = oeeData.isRealData && oeeData.oee !== null
  
  return (
    <Card className={cn(
      'oee-metric-card relative overflow-hidden',
      'border-l-4',
      getOeeLevelClasses(oeeLevel).border,
      size === 'compact' && 'p-4',
      size === 'standard' && 'p-6',
      size === 'large' && 'p-8',
      size === 'executive' && 'p-10',
      className
    )}>
      {/* CFR Part 11 Audit Indicator */}
      {auditMode && (
        <div className="absolute top-2 right-2">
          <Badge variant="outline" className="text-xs">
            <Shield className="w-3 h-3 mr-1" />
            Audited
          </Badge>
        </div>
      )}
      
      {/* Equipment Header */}
      <div className="mb-4">
        <h3 className={cn(
          'font-semibold text-neutral-900',
          size === 'compact' && 'text-sm',
          size === 'standard' && 'text-base',
          size === 'large' && 'text-lg',
          size === 'executive' && 'text-xl'
        )}>
          {oeeData.equipmentName}
        </h3>
        <p className="text-xs text-neutral-600">
          Last Updated: {formatRelativeTime(oeeData.timestamp)}
        </p>
      </div>
      
      {/* Data Quality Warning for unavailable data */}
      {!hasRealData && (
        <div className="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded">
          <div className="flex items-center space-x-2">
            <DataQualityIndicator quality={oeeData.dataQuality || 'unavailable'} size="sm" />
            <div>
              <div className="text-sm font-semibold text-yellow-800">OEE Data Not Available</div>
              <div className="text-xs text-yellow-700">
                CFR Part 11: No real data available - cannot calculate OEE metrics
              </div>
            </div>
          </div>
        </div>
      )}
      
      {/* Main OEE Display */}
      <div className="flex items-baseline space-x-2 mb-6">
        <span className={cn(
          'font-bold',
          hasRealData ? getOeeLevelClasses(oeeLevel).text : 'text-status-error',
          size === 'compact' && 'text-2xl',
          size === 'standard' && 'text-4xl',
          size === 'large' && 'text-5xl',
          size === 'executive' && 'text-6xl'
        )}>
          {hasRealData ? oeeData.oee!.toFixed(1) : 'N/A'}
        </span>
        <span className={cn(
          'text-neutral-600 font-medium',
          size === 'compact' && 'text-sm',
          size === 'standard' && 'text-base',
          size === 'large' && 'text-lg',
          size === 'executive' && 'text-xl'
        )}>
          {hasRealData ? '%' : ''}
        </span>
        
        {/* OEE Level Badge */}
        {hasRealData && (
          <Badge 
            variant="secondary" 
            className={cn(
              getOeeLevelClasses(oeeLevel).bg,
              getOeeLevelClasses(oeeLevel).text,
              'font-semibold'
            )}
          >
            {getOeeLevelLabel(oeeLevel)}
          </Badge>
        )}
      </div>
      
      {/* Target Comparison */}
      {showTarget && oeeData.target && hasRealData && variance !== null && (
        <div className="mb-4">
          <div className="flex items-center justify-between text-sm mb-1">
            <span className="text-neutral-600">vs Target ({oeeData.target.toFixed(1)}%)</span>
            <span className={cn(
              'font-semibold',
              variance >= 0 ? 'text-status-success' : 'text-status-error'
            )}>
              {variance >= 0 ? '+' : ''}{variance.toFixed(1)}%
            </span>
          </div>
          <Progress 
            value={(oeeData.oee! / oeeData.target) * 100} 
            className="h-2"
            indicatorClassName={variance >= 0 ? 'bg-status-success' : 'bg-status-error'}
          />
        </div>
      )}
      
      {/* OEE Components Breakdown */}
      {showComponents && (
        <div className="grid grid-cols-3 gap-4 mb-4">
          <div 
            className="text-center cursor-pointer hover:bg-neutral-50 p-2 rounded"
            onClick={() => onDrillDown?.('availability')}
          >
            <div className="text-lg font-bold text-neutral-900">
              {hasRealData && oeeData.availability !== null ? `${oeeData.availability.toFixed(1)}%` : 'N/A'}
            </div>
            <div className="text-xs text-neutral-600">Availability</div>
            {hasRealData && oeeData.availability !== null && (
              <div className="w-full bg-neutral-200 rounded-full h-1 mt-1">
                <div 
                  className="bg-blue-500 h-1 rounded-full"
                  style={{ width: `${Math.min(oeeData.availability, 100)}%` }}
                />
              </div>
            )}
            {!hasRealData && (
              <DataQualityIndicator quality="unavailable" size="xs" className="mt-1" />
            )}
          </div>
          
          <div 
            className="text-center cursor-pointer hover:bg-neutral-50 p-2 rounded"
            onClick={() => onDrillDown?.('performance')}
          >
            <div className="text-lg font-bold text-neutral-900">
              {hasRealData && oeeData.performance !== null ? `${oeeData.performance.toFixed(1)}%` : 'N/A'}
            </div>
            <div className="text-xs text-neutral-600">Performance</div>
            {hasRealData && oeeData.performance !== null && (
              <div className="w-full bg-neutral-200 rounded-full h-1 mt-1">
                <div 
                  className="bg-green-500 h-1 rounded-full"
                  style={{ width: `${Math.min(oeeData.performance, 100)}%` }}
                />
              </div>
            )}
            {!hasRealData && (
              <DataQualityIndicator quality="unavailable" size="xs" className="mt-1" />
            )}
          </div>
          
          <div 
            className="text-center cursor-pointer hover:bg-neutral-50 p-2 rounded"
            onClick={() => onDrillDown?.('quality')}
          >
            <div className="text-lg font-bold text-neutral-900">
              {hasRealData && oeeData.quality !== null ? `${oeeData.quality.toFixed(1)}%` : 'N/A'}
            </div>
            <div className="text-xs text-neutral-600">Quality</div>
            {hasRealData && oeeData.quality !== null && (
              <div className="w-full bg-neutral-200 rounded-full h-1 mt-1">
                <div 
                  className="bg-purple-500 h-1 rounded-full"
                  style={{ width: `${Math.min(oeeData.quality, 100)}%` }}
                />
              </div>
            )}
            {!hasRealData && (
              <DataQualityIndicator quality="unavailable" size="xs" className="mt-1" />
            )}
          </div>
        </div>
      )}
      
      {/* Production Metrics (detailed view only) */}
      {size === 'large' || size === 'executive' ? (
        <div className="mb-4 p-3 bg-neutral-50 rounded">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2">Production Metrics</h4>
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <span className="text-neutral-600">Total Count:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {hasRealData && oeeData.totalCount !== null ? oeeData.totalCount.toLocaleString() : 'N/A'}
              </span>
            </div>
            <div>
              <span className="text-neutral-600">Good Count:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {hasRealData && oeeData.goodCount !== null ? oeeData.goodCount.toLocaleString() : 'N/A'}
              </span>
            </div>
            <div>
              <span className="text-neutral-600">Run Time:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {hasRealData && oeeData.actualRunTime !== null ? `${oeeData.actualRunTime}min` : 'N/A'}
              </span>
            </div>
            <div>
              <span className="text-neutral-600">Rejects:</span>
              <span className="ml-1 font-medium text-neutral-900">
                {hasRealData && oeeData.rejectCount !== null ? oeeData.rejectCount.toLocaleString() : 'N/A'}
              </span>
            </div>
          </div>
        </div>
      ) : null}
      
      {/* Action Footer */}
      <div className="flex items-center justify-between pt-3 border-t border-neutral-200">
        <div className="flex space-x-2">
          <Button size="sm" variant="outline">
            <TrendingUp className="w-4 h-4 mr-1" />
            Analyze
          </Button>
          {onExport && (
            <Button size="sm" variant="outline" onClick={onExport}>
              <Download className="w-4 h-4 mr-1" />
              Export
            </Button>
          )}
        </div>
        
        <div className="text-xs text-neutral-500">
          Equipment ID: {oeeData.equipmentId}
        </div>
      </div>
      
      {/* CFR Part 11 Compliance Footer */}
      <div className="mt-3 text-xs text-neutral-500 border-t pt-2">
        <div className="flex items-center justify-between">
          <span>
            Data Quality: {oeeData.dataQuality || 'Unknown'}
          </span>
          <span className={cn(
            'font-medium',
            hasRealData ? 'text-status-success' : 'text-status-error'
          )}>
            {hasRealData ? '✓ Real OEE Data' : '⚠️ No Real Data'}
          </span>
        </div>
        
        {oeeData.auditInfo && (
          <div className="mt-1">
            Source: {oeeData.auditInfo.sourceSystem} | 
            Integrity: {oeeData.auditInfo.dataIntegrity}
          </div>
        )}
        
        {!hasRealData && (
          <div className="text-status-error font-medium mt-1">
            ⚠️ CFR Part 11: OEE calculations require real production data
          </div>
        )}
      </div>
    </Card>
  )
})

OeeMetricCard.displayName = 'OeeMetricCard'

export default OeeMetricCard