/**
 * Counter Trend Chart Component
 * Time-series visualization for counter data with CFR Part 11 compliance
 */

import React, { useMemo } from 'react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { AlertTriangle, TrendingUp, TrendingDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { DataQuality } from '@/types'

interface DataPoint {
  timestamp: Date
  value: number
  rate?: number
  quality: DataQuality
  isRealData: boolean
}

interface CounterTrendChartProps {
  data: DataPoint[]
  deviceName: string
  channelName: string
  unit?: string
  timeRange: string
  height?: number
  showDataQuality?: boolean
  showGaps?: boolean
  className?: string
}

const formatTimestamp = (date: Date, timeRange: string): string => {
  if (timeRange.includes('h') || timeRange.includes('min')) {
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
  } else {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }
}

export const CounterTrendChart: React.FC<CounterTrendChartProps> = ({
  data,
  deviceName,
  channelName,
  unit = 'counts',
  timeRange,
  height = 300,
  showDataQuality = true,
  showGaps = true,
  className
}) => {
  const chartMetrics = useMemo(() => {
    if (data.length === 0) return null

    const realData = data.filter(d => d.isRealData)
    const goodQualityData = data.filter(d => d.quality === 'good')
    
    if (realData.length === 0) return null

    const values = realData.map(d => d.value)
    const min = Math.min(...values)
    const max = Math.max(...values)
    const latest = realData[realData.length - 1]
    const previous = realData.length > 1 ? realData[realData.length - 2] : null
    
    const trend = previous ? (latest.value > previous.value ? 'up' : latest.value < previous.value ? 'down' : 'stable') : 'stable'
    const dataQualityPercentage = (goodQualityData.length / data.length) * 100

    return {
      min,
      max,
      latest: latest.value,
      trend,
      dataQualityPercentage,
      totalPoints: data.length,
      realDataPoints: realData.length,
      goodQualityPoints: goodQualityData.length
    }
  }, [data])

  // Simple SVG-based chart implementation
  const renderChart = () => {
    if (!chartMetrics || data.length === 0) {
      return (
        <div 
          className="flex items-center justify-center bg-neutral-50 rounded border-2 border-dashed border-neutral-300"
          style={{ height }}
        >
          <div className="text-center">
            <AlertTriangle className="w-8 h-8 text-neutral-400 mx-auto mb-2" />
            <p className="text-sm text-neutral-600">No chart data available</p>
            <p className="text-xs text-neutral-500 mt-1">CFR Part 11: No synthetic data generated</p>
          </div>
        </div>
      )
    }

    const realData = data.filter(d => d.isRealData)
    const width = 100 // percentage
    const chartHeight = height - 60 // Account for axis labels
    
    const yScale = (value: number) => {
      const range = chartMetrics.max - chartMetrics.min
      if (range === 0) return chartHeight / 2
      return chartHeight - ((value - chartMetrics.min) / range) * chartHeight
    }

    const xScale = (index: number) => {
      return (index / Math.max(realData.length - 1, 1)) * width
    }

    // Generate SVG path for the trend line
    const pathData = realData.map((point, index) => {
      const x = xScale(index)
      const y = yScale(point.value)
      return `${index === 0 ? 'M' : 'L'} ${x} ${y}`
    }).join(' ')

    return (
      <div className="relative" style={{ height }}>
        <svg width="100%" height={height} viewBox={`0 0 100 ${height}`} className="overflow-visible">
          {/* Grid lines */}
          {[0, 25, 50, 75, 100].map(y => (
            <line
              key={y}
              x1="0"
              y1={y * (chartHeight / 100)}
              x2="100"
              y2={y * (chartHeight / 100)}
              stroke="#f3f4f6"
              strokeWidth="0.5"
            />
          ))}
          
          {/* Trend line */}
          <path
            d={pathData}
            fill="none"
            stroke="#3b82f6"
            strokeWidth="1.5"
            className="drop-shadow-sm"
          />
          
          {/* Data points */}
          {realData.map((point, index) => (
            <g key={index}>
              <circle
                cx={xScale(index)}
                cy={yScale(point.value)}
                r="2"
                fill={
                  point.quality === 'good' ? '#10b981' :
                  point.quality === 'uncertain' ? '#f59e0b' : '#ef4444'
                }
                stroke="white"
                strokeWidth="1"
              />
              
              {/* Data quality indicators */}
              {showDataQuality && point.quality !== 'good' && (
                <circle
                  cx={xScale(index)}
                  cy={yScale(point.value) - 8}
                  r="1.5"
                  fill={point.quality === 'uncertain' ? '#f59e0b' : '#ef4444'}
                />
              )}
            </g>
          ))}
          
          {/* Y-axis labels */}
          <text x="2" y="8" fontSize="8" fill="#6b7280">
            {chartMetrics.max.toLocaleString()} {unit}
          </text>
          <text x="2" y={chartHeight - 2} fontSize="8" fill="#6b7280">
            {chartMetrics.min.toLocaleString()} {unit}
          </text>
          
          {/* Time range labels */}
          {realData.length > 0 && (
            <>
              <text x="2" y={height - 5} fontSize="8" fill="#6b7280">
                {formatTimestamp(realData[0].timestamp, timeRange)}
              </text>
              <text x="85" y={height - 5} fontSize="8" fill="#6b7280">
                {formatTimestamp(realData[realData.length - 1].timestamp, timeRange)}
              </text>
            </>
          )}
        </svg>
        
        {/* Current value overlay */}
        <div className="absolute top-2 right-2 bg-white/90 backdrop-blur-sm rounded px-2 py-1 border">
          <div className="flex items-center space-x-2">
            <span className="text-lg font-bold text-neutral-900">
              {chartMetrics.latest.toLocaleString()}
            </span>
            <span className="text-sm text-neutral-600">{unit}</span>
            {chartMetrics.trend !== 'stable' && (
              <span className={cn(
                chartMetrics.trend === 'up' ? 'text-status-success' : 'text-status-error'
              )}>
                {chartMetrics.trend === 'up' ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
              </span>
            )}
          </div>
        </div>
      </div>
    )
  }

  return (
    <Card className={cn('counter-trend-chart', className)}>
      {/* Header */}
      <div className="p-4 border-b">
        <div className="flex items-center justify-between mb-2">
          <div>
            <h3 className="font-semibold text-neutral-900">{deviceName}</h3>
            <p className="text-sm text-neutral-600">{channelName} • {timeRange}</p>
          </div>
          
          {showDataQuality && chartMetrics && (
            <div className="flex items-center space-x-2">
              <DataQualityIndicator 
                quality={chartMetrics.dataQualityPercentage >= 95 ? 'good' : chartMetrics.dataQualityPercentage >= 80 ? 'uncertain' : 'bad'}
                showTooltip={false}
              />
              <Badge variant="outline" className="text-xs">
                {chartMetrics.goodQualityPoints}/{chartMetrics.totalPoints} Good
              </Badge>
            </div>
          )}
        </div>
      </div>
      
      {/* Chart */}
      <div className="p-4">
        {renderChart()}
      </div>
      
      {/* CFR Part 11 Compliance Footer */}
      <div className="px-4 pb-4">
        <div className="text-xs text-neutral-500 border-t pt-2">
          <div className="flex items-center justify-between">
            <span>
              Data Points: {chartMetrics?.realDataPoints || 0} real, {data.filter(d => !d.isRealData).length} unavailable
            </span>
            <span>
              Quality: {chartMetrics?.dataQualityPercentage.toFixed(1) || 'N/A'}% good
            </span>
          </div>
          
          {chartMetrics && chartMetrics.realDataPoints !== chartMetrics.totalPoints && (
            <div className="text-status-warning font-medium mt-1">
              ⚠️ CFR Part 11: Chart contains data gaps - no interpolation applied
            </div>
          )}
          
          {(!chartMetrics || chartMetrics.realDataPoints === 0) && (
            <div className="text-status-error font-medium mt-1">
              ⚠️ CFR Part 11: No real data available for charting
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}

export default CounterTrendChart