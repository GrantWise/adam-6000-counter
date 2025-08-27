/**
 * OEE Dashboard Component
 * Main dashboard for Overall Equipment Effectiveness monitoring
 * Follows OEE-MODULE-DESIGN.md specifications
 */

import React, { useState, useMemo, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { useAuth } from '@/hooks/useAuth'
import { useOeeData } from '../hooks/useOeeData'
import { OeeMetricCard } from './OeeMetricCard'
import { MachineSelector } from './MachineSelector'
import { MachineComparison } from './MachineComparison'
import { Activity, TrendingUp, AlertTriangle, RefreshCw, Monitor, Filter, BarChart3 } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { MachineInfo } from './MachineSelector'

const OeeDashboard: React.FC = () => {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [selectedTab, setSelectedTab] = useState('overview')
  const [selectedMachines, setSelectedMachines] = useState<string[]>([])
  const [viewMode, setViewMode] = useState<'all' | 'selected' | 'comparison'>('all')
  const [showMachineSelector, setShowMachineSelector] = useState(false)
  
  // Role-based interface adaptation
  const isOperator = user?.role === 'Operator'
  const isSupervisor = user?.role === 'Supervisor' 
  const isManager = user?.role === 'Manager'
  const isAdmin = user?.role === 'Admin'

  // Determine selection mode based on current view
  const selectionMode = viewMode === 'comparison' ? 'comparison' : 'multiple'

  // Data hooks
  const { 
    currentOee, 
    loading, 
    error, 
    overview,
    refreshOee 
  } = useOeeData({ 
    refreshInterval: isOperator ? 30000 : 60000, // Operators get faster updates
    autoRefresh: true 
  })

  // Helper functions for machine categorization
  const getEquipmentArea = useCallback((equipmentId: string) => {
    // In a real implementation, this would come from equipment configuration
    // For the 3 simulators, assign areas based on their names
    if (equipmentId.includes('production-line-1')) return 'Production Area A'
    if (equipmentId.includes('production-line-2')) return 'Production Area B'
    if (equipmentId.includes('packaging')) return 'Packaging Area'
    return 'General'
  }, [])

  const getEquipmentCategory = useCallback((equipmentId: string) => {
    if (equipmentId.includes('packaging')) return 'packaging'
    return 'production'
  }, [])

  // Convert OEE data to machine info format
  const machineInfoList = useMemo((): MachineInfo[] => {
    return currentOee.map(oee => ({
      equipmentId: oee.equipmentId,
      equipmentName: oee.equipmentName,
      status: (
        !oee.isRealData || oee.oee === null ? 'offline' :
        oee.oee >= 50 ? 'producing' :
        oee.oee > 0 ? 'idle' : 'offline'
      ) as 'online' | 'offline' | 'producing' | 'idle' | 'maintenance',
      oeeData: oee,
      area: getEquipmentArea(oee.equipmentId),
      category: getEquipmentCategory(oee.equipmentId) as 'production' | 'packaging' | 'quality' | 'utility'
    }))
  }, [currentOee, getEquipmentArea, getEquipmentCategory])

  // Filter data based on selected machines
  const filteredOeeData = useMemo(() => {
    if (viewMode === 'all' || selectedMachines.length === 0) {
      return currentOee
    }
    return currentOee.filter(oee => selectedMachines.includes(oee.equipmentId))
  }, [currentOee, selectedMachines, viewMode])

  // Calculate system-wide metrics
  const hasDataAvailable = filteredOeeData.some(oee => oee.isRealData && oee.oee !== null)
  const systemOeeStatus: 'healthy' | 'warning' | 'error' = 
    !hasDataAvailable ? 'error' :
    overview && overview.averageOee !== null && overview.averageOee >= 75 ? 'healthy' :
    overview && overview.averageOee !== null && overview.averageOee >= 50 ? 'warning' : 'error'

  // Machine selection handlers
  const handleMachineSelectionChange = useCallback((machineIds: string[]) => {
    setSelectedMachines(machineIds)
    if (machineIds.length > 0 && viewMode === 'all') {
      setViewMode('selected')
    } else if (machineIds.length === 0) {
      setViewMode('all')
    }
  }, [viewMode])

  const handleMachineClick = useCallback((machineId: string) => {
    navigate(`/dashboard/oee/machine/${machineId}`)
  }, [navigate])

  const handleViewModeChange = useCallback((mode: 'all' | 'selected' | 'comparison') => {
    setViewMode(mode)
    if (mode === 'all') {
      setSelectedMachines([])
    }
  }, [])

  // Prepare comparison data
  const comparisonData = useMemo(() => {
    if (viewMode !== 'comparison' || selectedMachines.length < 2) return []
    
    return selectedMachines.map(machineId => {
      const oeeData = currentOee.find(oee => oee.equipmentId === machineId)
      if (!oeeData) return null
      
      return {
        equipmentId: machineId,
        equipmentName: oeeData.equipmentName,
        oeeData
      }
    }).filter(Boolean) as any[]
  }, [currentOee, selectedMachines, viewMode])

  // Render operator view (simplified, large numbers)
  const renderOperatorView = () => (
    <div className={cn('oee-dashboard operator-view')}>
      {/* Current OEE Overview - Extra large display */}
      <div className="mb-8">
        <Card className="p-8 bg-gradient-to-r from-primary-50 to-blue-50">
          <div className="text-center">
            <div className="mb-6">
              <StatusIndicator 
                status={systemOeeStatus}
                size="xl"
                pulse={systemOeeStatus === 'warning'}
              />
            </div>
            
            <h1 className="text-4xl font-bold text-neutral-900 mb-2">
              Overall Equipment Effectiveness
            </h1>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-8">
              <div className="text-center">
                <div className={cn(
                  'text-6xl font-bold mb-2',
                  systemOeeStatus === 'healthy' ? 'text-status-success' :
                  systemOeeStatus === 'warning' ? 'text-status-warning' : 'text-status-error'
                )}>
                  {overview?.averageOee !== null 
                    ? `${overview.averageOee.toFixed(1)}%`
                    : 'N/A'
                  }
                </div>
                <div className="text-lg text-neutral-600">Average OEE</div>
                {!hasDataAvailable && (
                  <DataQualityIndicator quality="unavailable" className="mt-2" />
                )}
              </div>
              <div className="text-center">
                <div className="text-6xl font-bold text-primary-600 mb-2">
                  {overview?.activeEquipment || 0}/{overview?.totalEquipment || 0}
                </div>
                <div className="text-lg text-neutral-600">Equipment Active</div>
              </div>
            </div>
          </div>
        </Card>
      </div>

      {/* Active Equipment Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
        {filteredOeeData.map(oee => (
          <div key={oee.equipmentId} onClick={() => handleMachineClick(oee.equipmentId)} className="cursor-pointer">
            <OeeMetricCard
              oeeData={oee}
              size="large"
              showComponents
              showTarget
              auditMode={false}
            />
          </div>
        ))}
      </div>
    </div>
  )

  // Render supervisor view (balanced monitoring and analysis)
  const renderSupervisorView = () => (
    <div className={cn('oee-dashboard supervisor-view')}>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-full">
        {/* Left Column - Equipment OEE Grid */}
        <div className="lg:col-span-2 space-y-6">
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-neutral-900">Equipment OEE Status</h3>
              <div className="flex items-center space-x-2">
                <StatusIndicator status={systemOeeStatus} label={systemOeeStatus.toUpperCase()} />
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={refreshOee}
                  disabled={loading}
                >
                  <RefreshCw className={cn('w-4 h-4 mr-1', loading && 'animate-spin')} />
                  Refresh
                </Button>
              </div>
            </div>
            
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
              {filteredOeeData.map(oee => (
                <div key={oee.equipmentId} onClick={() => handleMachineClick(oee.equipmentId)} className="cursor-pointer">
                  <OeeMetricCard
                    oeeData={oee}
                    size="standard"
                    showComponents
                    showTarget
                    showTrend
                  />
                </div>
              ))}
            </div>
          </Card>

          {/* OEE Component Breakdown */}
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">OEE Component Analysis</h3>
            {overview && (
              <div className="grid grid-cols-3 gap-4">
                <div className="text-center p-4 bg-blue-50 rounded-lg">
                  <div className="text-2xl font-bold text-blue-600 mb-1">
                    {overview.breakdownByMetric.availability.avg !== null 
                      ? `${overview.breakdownByMetric.availability.avg.toFixed(1)}%`
                      : 'N/A'
                    }
                  </div>
                  <div className="text-sm text-neutral-600">Availability</div>
                  <Badge variant="outline" className="mt-1">
                    {overview.breakdownByMetric.availability.trend}
                  </Badge>
                </div>
                <div className="text-center p-4 bg-green-50 rounded-lg">
                  <div className="text-2xl font-bold text-green-600 mb-1">
                    {overview.breakdownByMetric.performance.avg !== null 
                      ? `${overview.breakdownByMetric.performance.avg.toFixed(1)}%`
                      : 'N/A'
                    }
                  </div>
                  <div className="text-sm text-neutral-600">Performance</div>
                  <Badge variant="outline" className="mt-1">
                    {overview.breakdownByMetric.performance.trend}
                  </Badge>
                </div>
                <div className="text-center p-4 bg-purple-50 rounded-lg">
                  <div className="text-2xl font-bold text-purple-600 mb-1">
                    {overview.breakdownByMetric.quality.avg !== null 
                      ? `${overview.breakdownByMetric.quality.avg.toFixed(1)}%`
                      : 'N/A'
                    }
                  </div>
                  <div className="text-sm text-neutral-600">Quality</div>
                  <Badge variant="outline" className="mt-1">
                    {overview.breakdownByMetric.quality.trend}
                  </Badge>
                </div>
              </div>
            )}
          </Card>
        </div>

        {/* Right Column - Analytics and Actions */}
        <div className="space-y-6">
          {/* Top Performers */}
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Top Performers</h3>
            <div className="space-y-3">
              {overview?.topPerformers.slice(0, 5).map((performer, index) => (
                <div key={performer.equipmentId} className="flex items-center justify-between p-3 bg-neutral-50 rounded-lg">
                  <div className="flex items-center space-x-3">
                    <div className={cn(
                      'w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold text-white',
                      index === 0 && 'bg-yellow-500',
                      index === 1 && 'bg-gray-400',
                      index === 2 && 'bg-amber-600',
                      index > 2 && 'bg-neutral-400'
                    )}>
                      {index + 1}
                    </div>
                    <div>
                      <div className="font-medium text-neutral-900">{performer.equipmentName}</div>
                      <div className="text-xs text-neutral-600">Equipment ID: {performer.equipmentId}</div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className={cn(
                      'font-bold',
                      performer.oee !== null && performer.oee >= 85 ? 'text-status-success' :
                      performer.oee !== null && performer.oee >= 65 ? 'text-primary-600' :
                      performer.oee !== null && performer.oee >= 45 ? 'text-status-warning' : 'text-status-error'
                    )}>
                      {performer.oee !== null ? `${performer.oee.toFixed(1)}%` : 'N/A'}
                    </div>
                    {performer.oee === null && (
                      <DataQualityIndicator quality="unavailable" size="xs" />
                    )}
                  </div>
                </div>
              ))}
            </div>
          </Card>

          {/* Quick Actions */}
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Quick Actions</h3>
            <div className="space-y-2">
              <Button variant="outline" className="w-full justify-start">
                <TrendingUp className="w-4 h-4 mr-2" />
                View Analytics
              </Button>
              <Button variant="outline" className="w-full justify-start">
                <Activity className="w-4 h-4 mr-2" />
                Stoppage Tracking
              </Button>
              <Button variant="outline" className="w-full justify-start">
                Export OEE Report
              </Button>
              <Button variant="outline" className="w-full justify-start">
                Manage Work Orders
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )

  // Render manager/admin view (comprehensive analytics)
  const renderManagerView = () => (
    <div className={cn('oee-dashboard manager-view')}>
      <Tabs value={selectedTab} onValueChange={setSelectedTab} className="h-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="equipment">Equipment</TabsTrigger>
          <TabsTrigger value="analysis">Analysis</TabsTrigger>
          <TabsTrigger value="reports">Reports</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6 mt-6">
          {/* Executive Metrics */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <MetricCard
              title="Average OEE"
              value={overview?.averageOee || null}
              unit="%"
              format="number"
              status={
                overview?.averageOee !== null && overview.averageOee >= 75 ? 'success' :
                overview?.averageOee !== null && overview.averageOee >= 50 ? 'warning' : 'error'
              }
            />
            <MetricCard
              title="Equipment Utilization"
              value={overview?.activeEquipment || 0}
              total={overview?.totalEquipment || 0}
              format="fraction"
              status={overview?.activeEquipment === overview?.totalEquipment ? 'success' : 'warning'}
            />
            <MetricCard
              title="Best Performer"
              value={overview?.topPerformers[0]?.oee || null}
              unit="%"
              format="number"
              trend="up"
              subtitle={overview?.topPerformers[0]?.equipmentName}
            />
            <MetricCard
              title="Improvement Opportunity"
              value={
                overview?.breakdownByMetric ? 
                Math.min(
                  overview.breakdownByMetric.availability.avg || 100,
                  overview.breakdownByMetric.performance.avg || 100,
                  overview.breakdownByMetric.quality.avg || 100
                ) : null
              }
              unit="%"
              format="number"
              status="warning"
              subtitle="Lowest Component"
            />
          </div>

          {/* Main Analytics Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card className="p-6">
              <h3 className="text-lg font-semibold text-neutral-900 mb-4">OEE Component Breakdown</h3>
              {overview && (
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-3 bg-blue-50 rounded">
                    <span className="font-medium">Availability</span>
                    <span className="text-lg font-bold text-blue-600">
                      {overview.breakdownByMetric.availability.avg !== null 
                        ? `${overview.breakdownByMetric.availability.avg.toFixed(1)}%`
                        : 'N/A'
                      }
                    </span>
                  </div>
                  <div className="flex items-center justify-between p-3 bg-green-50 rounded">
                    <span className="font-medium">Performance</span>
                    <span className="text-lg font-bold text-green-600">
                      {overview.breakdownByMetric.performance.avg !== null 
                        ? `${overview.breakdownByMetric.performance.avg.toFixed(1)}%`
                        : 'N/A'
                      }
                    </span>
                  </div>
                  <div className="flex items-center justify-between p-3 bg-purple-50 rounded">
                    <span className="font-medium">Quality</span>
                    <span className="text-lg font-bold text-purple-600">
                      {overview.breakdownByMetric.quality.avg !== null 
                        ? `${overview.breakdownByMetric.quality.avg.toFixed(1)}%`
                        : 'N/A'
                      }
                    </span>
                  </div>
                </div>
              )}
            </Card>

            <Card className="p-6">
              <h3 className="text-lg font-semibold text-neutral-900 mb-4">Equipment Performance Ranking</h3>
              <div className="space-y-2">
                {overview?.topPerformers.map((performer, index) => (
                  <div key={performer.equipmentId} className="flex items-center justify-between py-2">
                    <div className="flex items-center space-x-3">
                      <span className="text-sm font-medium text-neutral-600">#{index + 1}</span>
                      <span className="font-medium text-neutral-900">{performer.equipmentName}</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className={cn(
                        'font-bold',
                        performer.oee !== null && performer.oee >= 85 ? 'text-status-success' :
                        performer.oee !== null && performer.oee >= 65 ? 'text-primary-600' :
                        performer.oee !== null && performer.oee >= 45 ? 'text-status-warning' : 'text-status-error'
                      )}>
                        {performer.oee !== null ? `${performer.oee.toFixed(1)}%` : 'N/A'}
                      </span>
                      {performer.oee === null && (
                        <DataQualityIndicator quality="unavailable" size="xs" />
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="equipment" className="mt-6">
          <div className="space-y-6">
            {/* Machine Selection and View Controls */}
            <Card className="p-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold">Machine Management</h3>
                <div className="flex items-center space-x-2">
                  <Button
                    variant={viewMode === 'all' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleViewModeChange('all')}
                  >
                    <Monitor className="w-4 h-4 mr-1" />
                    All Machines
                  </Button>
                  <Button
                    variant={viewMode === 'selected' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleViewModeChange('selected')}
                    disabled={selectedMachines.length === 0}
                  >
                    <Filter className="w-4 h-4 mr-1" />
                    Selected ({selectedMachines.length})
                  </Button>
                  <Button
                    variant={viewMode === 'comparison' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleViewModeChange('comparison')}
                    disabled={selectedMachines.length < 2}
                  >
                    <BarChart3 className="w-4 h-4 mr-1" />
                    Compare
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowMachineSelector(!showMachineSelector)}
                  >
                    {showMachineSelector ? 'Hide' : 'Show'} Selector
                  </Button>
                </div>
              </div>

              {showMachineSelector && (
                <MachineSelector
                  machines={machineInfoList}
                  selectedMachines={selectedMachines}
                  onSelectionChange={handleMachineSelectionChange}
                  selectionMode={selectionMode}
                  showStatus
                  showOeeValues
                  enableFiltering
                  maxComparison={4}
                />
              )}
            </Card>

            {/* Machine Comparison View */}
            {viewMode === 'comparison' && selectedMachines.length >= 2 && (
              <MachineComparison
                machines={comparisonData}
                mode="side-by-side"
                showTargets
                showGaps
                loading={loading}
              />
            )}

            {/* Machine Grid View */}
            {viewMode !== 'comparison' && (
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
                {filteredOeeData.map(oee => (
                  <div key={oee.equipmentId} onClick={() => handleMachineClick(oee.equipmentId)} className="cursor-pointer">
                    <OeeMetricCard
                      oeeData={oee}
                      size="detailed"
                      showComponents
                      showTarget
                      showTrend
                      auditMode
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </TabsContent>

        <TabsContent value="analysis" className="mt-6">
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Advanced Analytics</h3>
            <p className="text-neutral-600">
              Loss analysis, trend charts, and performance optimization tools will be available here.
            </p>
          </Card>
        </TabsContent>

        <TabsContent value="reports" className="mt-6">
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">OEE Reports</h3>
            <p className="text-neutral-600">
              Automated reporting, scheduled exports, and CFR Part 11 compliant documentation.
            </p>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )

  // Error state
  if (error) {
    return (
      <div className="oee-dashboard error-state">
        <Card className="p-8 text-center">
          <AlertTriangle className="w-12 h-12 text-status-error mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-status-error mb-4">
            OEE Dashboard Error
          </h2>
          <p className="text-neutral-600 mb-6">{error}</p>
          <Button onClick={refreshOee} disabled={loading}>
            <RefreshCw className={cn('w-4 h-4 mr-2', loading && 'animate-spin')} />
            Retry
          </Button>
        </Card>
      </div>
    )
  }

  // Loading state
  if (loading && currentOee.length === 0) {
    return (
      <div className="oee-dashboard loading-state">
        <Card className="p-8 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto mb-4" />
          <p className="text-neutral-600">Loading OEE Dashboard...</p>
        </Card>
      </div>
    )
  }

  // CFR Part 11 Data Quality Warning
  if (!hasDataAvailable) {
    return (
      <div className="oee-dashboard">
        <Card className="p-8 bg-yellow-50 border-yellow-200 mb-6">
          <div className="flex items-center space-x-3">
            <DataQualityIndicator quality="unavailable" size="lg" />
            <div>
              <h2 className="text-xl font-semibold text-yellow-800 mb-2">OEE Data Unavailable</h2>
              <p className="text-yellow-700">
                No real OEE data is currently available from the OEE service. 
                CFR Part 21 compliance requires investigation before proceeding.
              </p>
              <p className="text-sm text-yellow-600 mt-2">
                No synthetic or estimated values are displayed to maintain data integrity.
              </p>
            </div>
          </div>
        </Card>

        {/* Show equipment structure with unavailable data indicators */}
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {filteredOeeData.map(oee => (
            <div key={oee.equipmentId} onClick={() => handleMachineClick(oee.equipmentId)} className="cursor-pointer">
              <OeeMetricCard
                oeeData={oee}
                size="standard"
                showComponents
                auditMode
              />
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="oee-dashboard">
      {/* Role-based dashboard content */}
      {isOperator && renderOperatorView()}
      {isSupervisor && renderSupervisorView()}
      {(isManager || isAdmin) && renderManagerView()}
    </div>
  )
}

export default OeeDashboard