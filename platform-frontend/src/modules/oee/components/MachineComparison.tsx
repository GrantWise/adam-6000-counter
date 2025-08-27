/**
 * Machine Comparison Component
 * Provides side-by-side OEE comparison, ranking, and performance gap analysis
 * Supports multiple comparison views and CFR Part 11 compliance
 */

import React, { useState, useMemo, useCallback } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { 
  TrendingUp, 
  TrendingDown, 
  Minus,
  Trophy,
  BarChart3,
  Target,
  AlertTriangle,
  ArrowUp,
  ArrowDown,
  Equal,
  Crown,
  Medal,
  Award
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { OeeData } from '../types'

interface MachineComparisonData {
  equipmentId: string
  equipmentName: string
  oeeData: OeeData
  ranking?: number
  comparisonMetrics?: {
    vsAverage: number
    vsBest: number
    improvement: number
    trend: 'up' | 'down' | 'stable'
  }
}

interface MachineComparisonProps {
  /** Machines to compare with their OEE data */
  machines: MachineComparisonData[]
  
  /** Comparison mode */
  mode?: 'side-by-side' | 'ranking' | 'gap-analysis' | 'trends'
  
  /** Show targets for comparison */
  showTargets?: boolean
  
  /** Show performance gaps */
  showGaps?: boolean
  
  /** Time period for trends */
  timePeriod?: '1h' | '8h' | '24h' | '7d' | '30d'
  
  /** Loading state */
  loading?: boolean
  
  /** Additional CSS classes */
  className?: string
}

export const MachineComparison: React.FC<MachineComparisonProps> = ({
  machines = [],
  mode = 'side-by-side',
  showTargets = true,
  showGaps = true,
  timePeriod = '24h',
  loading = false,
  className
}) => {
  const [selectedTab, setSelectedTab] = useState(mode)
  const [sortBy, setSortBy] = useState<'oee' | 'availability' | 'performance' | 'quality'>('oee')
  const [sortOrder, setSortOrder] = useState<'desc' | 'asc'>('desc')

  // Calculate ranking and comparison metrics
  const rankedMachines = useMemo(() => {
    if (!machines.length) return []

    // Sort machines by selected metric
    const sorted = [...machines].sort((a, b) => {
      const aValue = a.oeeData[sortBy] || 0
      const bValue = b.oeeData[sortBy] || 0
      return sortOrder === 'desc' ? bValue - aValue : aValue - bValue
    })

    // Calculate averages for comparison
    const validData = machines.filter(m => m.oeeData.isRealData && m.oeeData.oee !== null)
    const averages = {
      oee: validData.reduce((sum, m) => sum + (m.oeeData.oee || 0), 0) / validData.length,
      availability: validData.reduce((sum, m) => sum + (m.oeeData.availability || 0), 0) / validData.length,
      performance: validData.reduce((sum, m) => sum + (m.oeeData.performance || 0), 0) / validData.length,
      quality: validData.reduce((sum, m) => sum + (m.oeeData.quality || 0), 0) / validData.length
    }

    const bestPerformer = sorted[0]

    // Add ranking and comparison metrics
    return sorted.map((machine, index) => ({
      ...machine,
      ranking: index + 1,
      comparisonMetrics: {
        vsAverage: (machine.oeeData[sortBy] || 0) - averages[sortBy],
        vsBest: (machine.oeeData[sortBy] || 0) - (bestPerformer.oeeData[sortBy] || 0),
        improvement: Math.max(0, averages[sortBy] - (machine.oeeData[sortBy] || 0)),
        trend: 'stable' as const // Would be calculated from historical data
      }
    }))
  }, [machines, sortBy, sortOrder])

  // Get ranking badge properties
  const getRankingBadgeProps = useCallback((ranking: number) => {
    switch (ranking) {
      case 1:
        return { icon: <Crown className="w-3 h-3" />, color: 'bg-yellow-500', text: '1st' }
      case 2:
        return { icon: <Medal className="w-3 h-3" />, color: 'bg-gray-400', text: '2nd' }
      case 3:
        return { icon: <Award className="w-3 h-3" />, color: 'bg-amber-600', text: '3rd' }
      default:
        return { icon: null, color: 'bg-neutral-400', text: `${ranking}th` }
    }
  }, [])

  // Get performance status
  const getPerformanceStatus = useCallback((oee: number | null): 'excellent' | 'good' | 'needs-improvement' | 'poor' => {
    if (oee === null) return 'poor'
    if (oee >= 85) return 'excellent'
    if (oee >= 75) return 'good'
    if (oee >= 50) return 'needs-improvement'
    return 'poor'
  }, [])

  // Get trend icon
  const getTrendIcon = useCallback((trend: 'up' | 'down' | 'stable', value: number) => {
    if (trend === 'up') return <ArrowUp className="w-4 h-4 text-green-500" />
    if (trend === 'down') return <ArrowDown className="w-4 h-4 text-red-500" />
    return <Equal className="w-4 h-4 text-gray-500" />
  }, [])

  // Render side-by-side comparison
  const renderSideBySideComparison = () => (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {rankedMachines.map((machine) => {
        const oee = machine.oeeData.oee
        const performance = getPerformanceStatus(oee)
        const rankingProps = getRankingBadgeProps(machine.ranking || 0)
        
        return (
          <Card key={machine.equipmentId} className="p-6 relative">
            {/* Ranking Badge */}
            {machine.ranking && machine.ranking <= 3 && (
              <div className="absolute -top-2 -right-2">
                <div className={cn(
                  'w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold',
                  rankingProps.color
                )}>
                  {rankingProps.icon || machine.ranking}
                </div>
              </div>
            )}

            {/* Machine Header */}
            <div className="text-center mb-4">
              <h3 className="font-semibold text-neutral-900 text-lg">
                {machine.equipmentName}
              </h3>
              <p className="text-sm text-neutral-500">
                ID: {machine.equipmentId}
              </p>
              {machine.ranking && (
                <Badge variant="outline" className="mt-1">
                  #{machine.ranking} of {rankedMachines.length}
                </Badge>
              )}
            </div>

            {/* OEE Value */}
            <div className="text-center mb-6">
              <div className={cn(
                'text-4xl font-bold mb-2',
                performance === 'excellent' && 'text-green-600',
                performance === 'good' && 'text-blue-600',
                performance === 'needs-improvement' && 'text-yellow-600',
                performance === 'poor' && 'text-red-600'
              )}>
                {oee !== null ? `${oee.toFixed(1)}%` : 'N/A'}
              </div>
              <StatusIndicator 
                status={
                  performance === 'excellent' || performance === 'good' ? 'success' :
                  performance === 'needs-improvement' ? 'warning' : 'error'
                }
                label={performance.replace('-', ' ').toUpperCase()}
              />
              {!machine.oeeData.isRealData && (
                <DataQualityIndicator quality="unavailable" className="mt-2" />
              )}
            </div>

            {/* OEE Components */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm text-neutral-600">Availability</span>
                <span className="font-medium">
                  {machine.oeeData.availability?.toFixed(1) || 'N/A'}%
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-neutral-600">Performance</span>
                <span className="font-medium">
                  {machine.oeeData.performance?.toFixed(1) || 'N/A'}%
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-neutral-600">Quality</span>
                <span className="font-medium">
                  {machine.oeeData.quality?.toFixed(1) || 'N/A'}%
                </span>
              </div>
            </div>

            {/* Comparison Metrics */}
            {showGaps && machine.comparisonMetrics && (
              <div className="mt-4 pt-4 border-t border-neutral-200">
                <div className="space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-neutral-600">vs Average</span>
                    <div className="flex items-center space-x-1">
                      {getTrendIcon(
                        machine.comparisonMetrics.vsAverage > 0 ? 'up' : 
                        machine.comparisonMetrics.vsAverage < 0 ? 'down' : 'stable',
                        machine.comparisonMetrics.vsAverage
                      )}
                      <span className={cn(
                        'font-medium',
                        machine.comparisonMetrics.vsAverage > 0 ? 'text-green-600' :
                        machine.comparisonMetrics.vsAverage < 0 ? 'text-red-600' : 'text-neutral-600'
                      )}>
                        {machine.comparisonMetrics.vsAverage > 0 ? '+' : ''}
                        {machine.comparisonMetrics.vsAverage.toFixed(1)}%
                      </span>
                    </div>
                  </div>
                  
                  {machine.comparisonMetrics.improvement > 0 && (
                    <div className="text-xs text-neutral-500">
                      {machine.comparisonMetrics.improvement.toFixed(1)}% improvement potential
                    </div>
                  )}
                </div>
              </div>
            )}
          </Card>
        )
      })}
    </div>
  )

  // Render ranking view
  const renderRankingView = () => (
    <div className="space-y-4">
      {/* Sorting Controls */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <span className="text-sm font-medium text-neutral-700">Sort by:</span>
          <div className="flex items-center space-x-2">
            {(['oee', 'availability', 'performance', 'quality'] as const).map((metric) => (
              <Button
                key={metric}
                variant={sortBy === metric ? "default" : "outline"}
                size="sm"
                onClick={() => setSortBy(metric)}
                className="capitalize"
              >
                {metric === 'oee' ? 'OEE' : metric}
              </Button>
            ))}
          </div>
        </div>
        
        <Button
          variant="outline"
          size="sm"
          onClick={() => setSortOrder(sortOrder === 'desc' ? 'asc' : 'desc')}
        >
          {sortOrder === 'desc' ? '↓' : '↑'} {sortOrder === 'desc' ? 'High to Low' : 'Low to High'}
        </Button>
      </div>

      {/* Ranking List */}
      <Card className="divide-y divide-neutral-200">
        {rankedMachines.map((machine, index) => {
          const rankingProps = getRankingBadgeProps(machine.ranking || 0)
          const value = machine.oeeData[sortBy]
          
          return (
            <div key={machine.equipmentId} className="p-4 flex items-center justify-between">
              <div className="flex items-center space-x-4">
                {/* Ranking */}
                <div className={cn(
                  'w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-bold',
                  rankingProps.color
                )}>
                  {rankingProps.icon || machine.ranking}
                </div>

                {/* Machine Info */}
                <div>
                  <h4 className="font-medium text-neutral-900">
                    {machine.equipmentName}
                  </h4>
                  <p className="text-sm text-neutral-500">
                    {machine.equipmentId}
                  </p>
                </div>

                {/* Performance Indicators */}
                {machine.ranking === 1 && (
                  <Badge className="bg-yellow-100 text-yellow-800 border-yellow-300">
                    <Trophy className="w-3 h-3 mr-1" />
                    Top Performer
                  </Badge>
                )}
              </div>

              {/* Metrics */}
              <div className="flex items-center space-x-6">
                <div className="text-right">
                  <div className="text-lg font-bold text-neutral-900">
                    {value !== null ? `${value.toFixed(1)}%` : 'N/A'}
                  </div>
                  <div className="text-xs text-neutral-500 capitalize">
                    {sortBy === 'oee' ? 'OEE' : sortBy}
                  </div>
                </div>

                {machine.comparisonMetrics && (
                  <div className="text-right">
                    <div className={cn(
                      'text-sm font-medium flex items-center',
                      machine.comparisonMetrics.vsAverage > 0 ? 'text-green-600' :
                      machine.comparisonMetrics.vsAverage < 0 ? 'text-red-600' : 'text-neutral-600'
                    )}>
                      {getTrendIcon(
                        machine.comparisonMetrics.vsAverage > 0 ? 'up' :
                        machine.comparisonMetrics.vsAverage < 0 ? 'down' : 'stable',
                        machine.comparisonMetrics.vsAverage
                      )}
                      <span className="ml-1">
                        {machine.comparisonMetrics.vsAverage > 0 ? '+' : ''}
                        {machine.comparisonMetrics.vsAverage.toFixed(1)}%
                      </span>
                    </div>
                    <div className="text-xs text-neutral-500">vs Avg</div>
                  </div>
                )}

                {!machine.oeeData.isRealData && (
                  <DataQualityIndicator quality="unavailable" size="sm" />
                )}
              </div>
            </div>
          )
        })}
      </Card>
    </div>
  )

  // Render gap analysis view
  const renderGapAnalysis = () => {
    const bestOee = rankedMachines[0]?.oeeData.oee || 0
    
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <MetricCard
            title="Best Performer"
            value={bestOee}
            unit="%"
            format="number"
            status="success"
            subtitle={rankedMachines[0]?.equipmentName}
          />
          <MetricCard
            title="Average OEE"
            value={rankedMachines.reduce((sum, m) => sum + (m.oeeData.oee || 0), 0) / rankedMachines.length}
            unit="%"
            format="number"
            status="warning"
          />
          <MetricCard
            title="Improvement Potential"
            value={Math.max(0, bestOee - (rankedMachines[rankedMachines.length - 1]?.oeeData.oee || 0))}
            unit="%"
            format="number"
            status="info"
            subtitle="Gap to close"
          />
        </div>

        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">Performance Gap Analysis</h3>
          <div className="space-y-4">
            {rankedMachines.map((machine) => {
              const gap = bestOee - (machine.oeeData.oee || 0)
              const gapPercentage = bestOee > 0 ? (gap / bestOee) * 100 : 0
              
              return (
                <div key={machine.equipmentId} className="flex items-center justify-between p-3 border border-neutral-200 rounded">
                  <div className="flex items-center space-x-3">
                    <div className="text-sm font-medium text-neutral-900">
                      {machine.equipmentName}
                    </div>
                    <Badge variant={gap === 0 ? "default" : gap <= 5 ? "secondary" : "outline"}>
                      {gap === 0 ? 'Leader' : `${gap.toFixed(1)}% gap`}
                    </Badge>
                  </div>
                  
                  <div className="flex items-center space-x-4">
                    <div className="text-right">
                      <div className="text-sm font-semibold">
                        {machine.oeeData.oee?.toFixed(1) || 'N/A'}%
                      </div>
                      <div className="text-xs text-neutral-500">Current</div>
                    </div>
                    
                    {gap > 0 && (
                      <div className="w-24 bg-neutral-200 rounded-full h-2">
                        <div 
                          className="bg-red-500 h-2 rounded-full"
                          style={{ width: `${Math.min(100, gapPercentage)}%` }}
                        />
                      </div>
                    )}
                    
                    <div className="text-right">
                      <div className="text-sm font-semibold text-green-600">
                        {bestOee.toFixed(1)}%
                      </div>
                      <div className="text-xs text-neutral-500">Target</div>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        </Card>
      </div>
    )
  }

  if (loading) {
    return (
      <Card className={cn('p-6', className)}>
        <div className="animate-pulse space-y-4">
          <div className="h-6 bg-neutral-200 rounded w-48" />
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-48 bg-neutral-200 rounded" />
            ))}
          </div>
        </div>
      </Card>
    )
  }

  if (machines.length === 0) {
    return (
      <Card className={cn('p-6', className)}>
        <div className="text-center py-12">
          <BarChart3 className="w-12 h-12 text-neutral-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-neutral-600 mb-2">
            No machines to compare
          </h3>
          <p className="text-neutral-500">
            Select at least 2 machines to enable comparison features.
          </p>
        </div>
      </Card>
    )
  }

  if (machines.length === 1) {
    return (
      <Card className={cn('p-6', className)}>
        <div className="text-center py-8">
          <AlertTriangle className="w-8 h-8 text-yellow-500 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-neutral-600 mb-2">
            Single machine selected
          </h3>
          <p className="text-neutral-500">
            Select additional machines to enable comparison analysis.
          </p>
        </div>
      </Card>
    )
  }

  return (
    <div className={className}>
      <Tabs value={selectedTab} onValueChange={setSelectedTab} className="w-full">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-xl font-semibold text-neutral-900">
              Machine Performance Comparison
            </h2>
            <p className="text-sm text-neutral-600">
              Compare OEE performance across {machines.length} selected machines
            </p>
          </div>

          <TabsList className="grid w-auto grid-cols-4">
            <TabsTrigger value="side-by-side">Side by Side</TabsTrigger>
            <TabsTrigger value="ranking">Ranking</TabsTrigger>
            <TabsTrigger value="gap-analysis">Gap Analysis</TabsTrigger>
            <TabsTrigger value="trends">Trends</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="side-by-side" className="mt-0">
          {renderSideBySideComparison()}
        </TabsContent>

        <TabsContent value="ranking" className="mt-0">
          {renderRankingView()}
        </TabsContent>

        <TabsContent value="gap-analysis" className="mt-0">
          {renderGapAnalysis()}
        </TabsContent>

        <TabsContent value="trends" className="mt-0">
          <Card className="p-6">
            <h3 className="text-lg font-semibold mb-4">Trend Comparison</h3>
            <p className="text-neutral-600">
              Historical trend comparison charts will be implemented here.
              This would show OEE performance over time for each selected machine.
            </p>
            <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded">
              <p className="text-sm text-yellow-700">
                <strong>CFR Part 11 Compliance:</strong> Trend data requires historical 
                real measurements. No synthetic or interpolated values will be displayed 
                to maintain data integrity.
              </p>
            </div>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default MachineComparison