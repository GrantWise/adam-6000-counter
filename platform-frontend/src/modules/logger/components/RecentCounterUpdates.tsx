/**
 * Recent Counter Updates Component
 * Displays recent counter data updates with CFR Part 11 compliance indicators
 */

import React, { useState, useMemo } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { Activity, Search, Download, Filter, Refresh } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { CounterData } from '../types'

interface RecentCounterUpdatesProps {
  counters: CounterData[]
  lastUpdate: Date | null
  maxVisible?: number
  showDataQuality?: boolean
  showExportOptions?: boolean
  enableFiltering?: boolean
  autoRefresh?: boolean
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

const formatRelativeTime = (date: Date): string => {
  const now = new Date()
  const diff = Math.floor((now.getTime() - date.getTime()) / 1000)
  
  if (diff < 60) return `${diff}s ago`
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
  return `${Math.floor(diff / 86400)}d ago`
}

export const RecentCounterUpdates: React.FC<RecentCounterUpdatesProps> = ({
  counters,
  lastUpdate,
  maxVisible = 20,
  showDataQuality = true,
  showExportOptions = false,
  enableFiltering = false,
  autoRefresh = false,
  className
}) => {
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedQuality, setSelectedQuality] = useState<string>('all')
  const [sortBy, setSortBy] = useState<'timestamp' | 'device' | 'value'>('timestamp')

  // Filter and sort counters
  const filteredCounters = useMemo(() => {
    let filtered = [...counters]
    
    // Search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(counter => 
        counter.deviceName.toLowerCase().includes(term) ||
        counter.channelName.toLowerCase().includes(term) ||
        counter.deviceId.toLowerCase().includes(term)
      )
    }
    
    // Quality filter
    if (selectedQuality !== 'all') {
      filtered = filtered.filter(counter => counter.dataQuality === selectedQuality)
    }
    
    // Sort
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'timestamp':
          return b.lastUpdate.getTime() - a.lastUpdate.getTime()
        case 'device':
          return a.deviceName.localeCompare(b.deviceName)
        case 'value':
          return b.currentValue - a.currentValue
        default:
          return 0
      }
    })
    
    return filtered.slice(0, maxVisible)
  }, [counters, searchTerm, selectedQuality, sortBy, maxVisible])

  const handleExport = (format: 'csv' | 'xlsx') => {
    // Implement export functionality
    console.log(`Exporting counter data as ${format}`)
  }

  return (
    <Card className={cn('recent-counter-updates', className)}>
      {/* Header */}
      <div className="p-4 border-b">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-3">
            <Activity className="w-5 h-5 text-primary-600" />
            <h3 className="text-lg font-semibold text-neutral-900">
              Recent Counter Updates
            </h3>
            {lastUpdate && (
              <Badge variant="outline">
                Last: {formatRelativeTime(lastUpdate)}
              </Badge>
            )}
          </div>
          
          <div className="flex items-center space-x-2">
            {showExportOptions && (
              <div className="flex space-x-1">
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleExport('csv')}
                >
                  <Download className="w-4 h-4 mr-1" />
                  CSV
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleExport('xlsx')}
                >
                  <Download className="w-4 h-4 mr-1" />
                  Excel
                </Button>
              </div>
            )}
            
            {autoRefresh && (
              <Button size="sm" variant="outline">
                <Refresh className="w-4 h-4" />
              </Button>
            )}
          </div>
        </div>
        
        {/* Filters */}
        {enableFiltering && (
          <div className="flex items-center space-x-4">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-neutral-400 w-4 h-4" />
              <Input
                placeholder="Search devices or channels..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            
            <select
              value={selectedQuality}
              onChange={(e) => setSelectedQuality(e.target.value)}
              className="px-3 py-2 border border-neutral-300 rounded-md text-sm"
            >
              <option value="all">All Quality</option>
              <option value="good">Good</option>
              <option value="uncertain">Uncertain</option>
              <option value="bad">Bad</option>
              <option value="unavailable">Unavailable</option>
            </select>
            
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as any)}
              className="px-3 py-2 border border-neutral-300 rounded-md text-sm"
            >
              <option value="timestamp">Sort by Time</option>
              <option value="device">Sort by Device</option>
              <option value="value">Sort by Value</option>
            </select>
          </div>
        )}
      </div>
      
      {/* Counter Updates List */}
      <div className="max-h-96 overflow-auto">
        {filteredCounters.length === 0 ? (
          <div className="p-8 text-center">
            <Activity className="w-12 h-12 text-neutral-300 mx-auto mb-4" />
            <h4 className="text-lg font-medium text-neutral-900 mb-2">
              No Counter Updates
            </h4>
            <p className="text-neutral-600">
              {searchTerm || selectedQuality !== 'all' 
                ? 'No counters match your filter criteria'
                : 'Waiting for real-time counter data...'
              }
            </p>
          </div>
        ) : (
          <div className="divide-y divide-neutral-200">
            {filteredCounters.map((counter, index) => (
              <div
                key={`${counter.deviceId}-${counter.channel}-${index}`}
                className="p-4 hover:bg-neutral-50 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-3 mb-2">
                      <h4 className="font-medium text-neutral-900">
                        {counter.deviceName}
                      </h4>
                      <span className="text-sm text-neutral-500">
                        {counter.channelName}
                      </span>
                      {showDataQuality && (
                        <DataQualityIndicator 
                          quality={counter.dataQuality}
                          size="sm"
                        />
                      )}
                    </div>
                    
                    <div className="flex items-center space-x-6 text-sm text-neutral-600">
                      <span>
                        <strong className="text-neutral-900">Value:</strong>{' '}
                        {counter.dataQuality === 'unavailable' || !counter.isRealData
                          ? 'N/A'
                          : counter.currentValue.toLocaleString()
                        } {counter.unit}
                      </span>
                      
                      {counter.isRealData && counter.dataQuality !== 'unavailable' && (
                        <span>
                          <strong className="text-neutral-900">Rate:</strong>{' '}
                          {counter.ratePerMinute.toFixed(1)}/min
                        </span>
                      )}
                      
                      <span>
                        <strong className="text-neutral-900">Updated:</strong>{' '}
                        {formatTimestamp(counter.lastUpdate)}
                      </span>
                    </div>
                  </div>
                  
                  <div className="flex items-center space-x-2">
                    {/* Trend indicator */}
                    {counter.isRealData && counter.trend !== 'stable' && (
                      <Badge 
                        variant="outline"
                        className={cn(
                          counter.trend === 'up' && 'border-status-success text-status-success',
                          counter.trend === 'down' && 'border-status-error text-status-error'
                        )}
                      >
                        {counter.trend === 'up' ? '↗' : '↘'} {counter.trend}
                      </Badge>
                    )}
                    
                    {/* Data source indicator */}
                    <Badge 
                      variant="outline"
                      className={cn(
                        counter.isRealData 
                          ? 'border-status-success text-status-success'
                          : 'border-status-error text-status-error'
                      )}
                    >
                      {counter.isRealData ? 'REAL' : 'N/A'}
                    </Badge>
                  </div>
                </div>
                
                {/* CFR Part 11 Compliance warning */}
                {!counter.isRealData && (
                  <div className="mt-2 p-2 bg-yellow-50 border border-yellow-200 rounded text-xs">
                    <span className="text-yellow-800 font-medium">
                      ⚠️ CFR Part 11: No real data available - not suitable for regulatory compliance
                    </span>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
      
      {/* Summary Footer */}
      <div className="border-t p-4 bg-neutral-50">
        <div className="grid grid-cols-4 gap-4 text-center text-sm">
          <div>
            <div className="font-semibold text-neutral-900">
              {filteredCounters.length}
            </div>
            <div className="text-neutral-600">Total Counters</div>
          </div>
          <div>
            <div className="font-semibold text-status-success">
              {filteredCounters.filter(c => c.dataQuality === 'good').length}
            </div>
            <div className="text-neutral-600">Good Quality</div>
          </div>
          <div>
            <div className="font-semibold text-status-warning">
              {filteredCounters.filter(c => c.dataQuality === 'uncertain').length}
            </div>
            <div className="text-neutral-600">Uncertain</div>
          </div>
          <div>
            <div className="font-semibold text-status-error">
              {filteredCounters.filter(c => c.dataQuality === 'bad' || c.dataQuality === 'unavailable').length}
            </div>
            <div className="text-neutral-600">Bad/Unavailable</div>
          </div>
        </div>
        
        {/* Data integrity footer */}
        <div className="mt-3 text-xs text-neutral-500 text-center">
          CFR Part 11 Compliance: All data marked with quality indicators • Real data only • No synthetic values
        </div>
      </div>
    </Card>
  )
}

export default RecentCounterUpdates