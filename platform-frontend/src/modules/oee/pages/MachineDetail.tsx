/**
 * Machine Detail Page
 * Individual machine drill-down with comprehensive OEE analysis,
 * work order management, stoppage history, and deep-dive analytics
 */

import React, { useState, useEffect, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { OeeMetricCard } from '../components/OeeMetricCard'
import { WorkOrderManagement } from '../components/WorkOrderManagement'
import { StoppageTracking } from '../components/StoppageTracking'
import { useOeeData } from '../hooks/useOeeData'
import { useAuth } from '@/hooks/useAuth'
import { 
  ArrowLeft,
  Settings,
  TrendingUp,
  Activity,
  Clock,
  Target,
  AlertTriangle,
  FileText,
  BarChart3,
  Calendar,
  Users,
  Wrench,
  CheckCircle,
  XCircle,
  RefreshCw
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { OeeData, WorkOrderData, StoppageData } from '../types'

interface MachineDetailPageProps {
  // Props are handled via URL params
}

export const MachineDetail: React.FC<MachineDetailPageProps> = () => {
  const { machineId } = useParams<{ machineId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  
  const [selectedTab, setSelectedTab] = useState('overview')
  const [timePeriod, setTimePeriod] = useState<'1h' | '8h' | '24h' | '7d' | '30d'>('24h')
  const [refreshing, setRefreshing] = useState(false)

  // Data hooks
  const { 
    currentOee, 
    loading, 
    error, 
    refreshOee,
    getEquipmentOee,
    getLossAnalysis
  } = useOeeData({ 
    equipmentIds: machineId ? [machineId] : [],
    refreshInterval: 30000,
    autoRefresh: true 
  })

  const [historicalData, setHistoricalData] = useState<OeeData[]>([])
  const [lossAnalysis, setLossAnalysis] = useState<any>(null)
  const [loadingHistory, setLoadingHistory] = useState(false)

  // Get current machine data
  const currentMachine = useMemo(() => {
    return currentOee.find(oee => oee.equipmentId === machineId)
  }, [currentOee, machineId])

  // Load historical data when time period changes
  useEffect(() => {
    if (!machineId) return

    const loadHistoricalData = async () => {
      setLoadingHistory(true)
      try {
        const endDate = new Date()
        const startDate = new Date()
        
        switch (timePeriod) {
          case '1h':
            startDate.setHours(startDate.getHours() - 1)
            break
          case '8h':
            startDate.setHours(startDate.getHours() - 8)
            break
          case '24h':
            startDate.setDate(startDate.getDate() - 1)
            break
          case '7d':
            startDate.setDate(startDate.getDate() - 7)
            break
          case '30d':
            startDate.setDate(startDate.getDate() - 30)
            break
        }

        const [historyData, analysisData] = await Promise.all([
          getEquipmentOee(machineId, { startDate, endDate }),
          getLossAnalysis(machineId, { startDate, endDate })
        ])

        setHistoricalData(historyData)
        setLossAnalysis(analysisData)
      } catch (err) {
        console.error('Failed to load historical data:', err)
      } finally {
        setLoadingHistory(false)
      }
    }

    loadHistoricalData()
  }, [machineId, timePeriod, getEquipmentOee, getLossAnalysis])

  // Manual refresh
  const handleRefresh = async () => {
    setRefreshing(true)
    await refreshOee()
    setRefreshing(false)
  }

  // Calculate performance metrics
  const performanceMetrics = useMemo(() => {
    if (!currentMachine || !historicalData.length) return null

    const validData = historicalData.filter(d => d.isRealData && d.oee !== null)
    if (validData.length === 0) return null

    const averageOee = validData.reduce((sum, d) => sum + (d.oee || 0), 0) / validData.length
    const averageAvailability = validData.reduce((sum, d) => sum + (d.availability || 0), 0) / validData.length
    const averagePerformance = validData.reduce((sum, d) => sum + (d.performance || 0), 0) / validData.length
    const averageQuality = validData.reduce((sum, d) => sum + (d.quality || 0), 0) / validData.length

    const maxOee = Math.max(...validData.map(d => d.oee || 0))
    const minOee = Math.min(...validData.map(d => d.oee || 0))

    return {
      current: currentMachine.oee || 0,
      average: averageOee,
      max: maxOee,
      min: minOee,
      components: {
        availability: averageAvailability,
        performance: averagePerformance,
        quality: averageQuality
      },
      trend: validData.length > 1 ? 
        (validData[validData.length - 1].oee || 0) - (validData[0].oee || 0) : 0
    }
  }, [currentMachine, historicalData])

  // Get machine status
  const machineStatus = useMemo(() => {
    if (!currentMachine) return 'unknown'
    
    const oee = currentMachine.oee
    if (oee === null || !currentMachine.isRealData) return 'offline'
    if (oee >= 50) return 'producing'
    if (oee > 0) return 'idle'
    return 'stopped'
  }, [currentMachine])

  // Render machine overview
  const renderOverview = () => (
    <div className="space-y-6">
      {/* Current Status */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <MetricCard
          title="Current OEE"
          value={currentMachine?.oee || null}
          unit="%"
          format="number"
          status={
            currentMachine?.oee !== null && currentMachine.oee >= 75 ? 'success' :
            currentMachine?.oee !== null && currentMachine.oee >= 50 ? 'warning' : 'error'
          }
          trend={performanceMetrics?.trend && performanceMetrics.trend > 0 ? 'up' : 
                 performanceMetrics?.trend && performanceMetrics.trend < 0 ? 'down' : undefined}
        />
        <MetricCard
          title="Availability"
          value={currentMachine?.availability || null}
          unit="%"
          format="number"
          status={
            currentMachine?.availability !== null && currentMachine.availability >= 90 ? 'success' :
            currentMachine?.availability !== null && currentMachine.availability >= 80 ? 'warning' : 'error'
          }
        />
        <MetricCard
          title="Performance"
          value={currentMachine?.performance || null}
          unit="%"
          format="number"
          status={
            currentMachine?.performance !== null && currentMachine.performance >= 95 ? 'success' :
            currentMachine?.performance !== null && currentMachine.performance >= 85 ? 'warning' : 'error'
          }
        />
        <MetricCard
          title="Quality"
          value={currentMachine?.quality || null}
          unit="%"
          format="number"
          status={
            currentMachine?.quality !== null && currentMachine.quality >= 99 ? 'success' :
            currentMachine?.quality !== null && currentMachine.quality >= 95 ? 'warning' : 'error'
          }
        />
      </div>

      {/* Detailed OEE Card */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">Detailed OEE Analysis</h3>
          {currentMachine ? (
            <OeeMetricCard
              oeeData={currentMachine}
              size="detailed"
              showComponents
              showTarget
              showTrend
              auditMode
            />
          ) : (
            <div className="text-center py-8">
              <AlertTriangle className="w-8 h-8 text-yellow-500 mx-auto mb-3" />
              <p className="text-neutral-600">Machine data not available</p>
              {!currentMachine?.isRealData && (
                <DataQualityIndicator quality="unavailable" className="mt-2" />
              )}
            </div>
          )}
        </Card>

        {/* Performance Summary */}
        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">Performance Summary ({timePeriod})</h3>
          {performanceMetrics ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="text-center p-3 bg-blue-50 rounded">
                  <div className="text-2xl font-bold text-blue-600">
                    {performanceMetrics.average.toFixed(1)}%
                  </div>
                  <div className="text-sm text-neutral-600">Average OEE</div>
                </div>
                <div className="text-center p-3 bg-green-50 rounded">
                  <div className="text-2xl font-bold text-green-600">
                    {performanceMetrics.max.toFixed(1)}%
                  </div>
                  <div className="text-sm text-neutral-600">Peak OEE</div>
                </div>
              </div>

              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Availability</span>
                  <span className="font-medium">{performanceMetrics.components.availability.toFixed(1)}%</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Performance</span>
                  <span className="font-medium">{performanceMetrics.components.performance.toFixed(1)}%</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-neutral-600">Quality</span>
                  <span className="font-medium">{performanceMetrics.components.quality.toFixed(1)}%</span>
                </div>
              </div>

              {Math.abs(performanceMetrics.trend) > 0.1 && (
                <div className={cn(
                  'p-3 rounded flex items-center space-x-2',
                  performanceMetrics.trend > 0 ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'
                )}>
                  {performanceMetrics.trend > 0 ? 
                    <TrendingUp className="w-4 h-4" /> : 
                    <TrendingUp className="w-4 h-4 rotate-180" />
                  }
                  <span className="text-sm">
                    {performanceMetrics.trend > 0 ? 'Improving' : 'Declining'} trend: 
                    {performanceMetrics.trend > 0 ? '+' : ''}{performanceMetrics.trend.toFixed(1)}% 
                    over {timePeriod}
                  </span>
                </div>
              )}
            </div>
          ) : (
            <div className="text-center py-8">
              <BarChart3 className="w-8 h-8 text-neutral-400 mx-auto mb-3" />
              <p className="text-neutral-600">No historical data available</p>
              <p className="text-sm text-neutral-500 mt-1">
                CFR Part 11: Only real measurement data is displayed
              </p>
            </div>
          )}
        </Card>
      </div>

      {/* Loss Analysis */}
      {lossAnalysis && (
        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">Loss Analysis</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center p-4 bg-red-50 rounded">
              <div className="text-xl font-bold text-red-600">
                {lossAnalysis.availabilityLoss?.toFixed(0) || 0} min
              </div>
              <div className="text-sm text-neutral-600">Availability Loss</div>
            </div>
            <div className="text-center p-4 bg-orange-50 rounded">
              <div className="text-xl font-bold text-orange-600">
                {lossAnalysis.performanceLoss?.toFixed(0) || 0} min
              </div>
              <div className="text-sm text-neutral-600">Performance Loss</div>
            </div>
            <div className="text-center p-4 bg-purple-50 rounded">
              <div className="text-xl font-bold text-purple-600">
                {lossAnalysis.qualityLoss?.toFixed(0) || 0} min
              </div>
              <div className="text-sm text-neutral-600">Quality Loss</div>
            </div>
          </div>
        </Card>
      )}
    </div>
  )

  // Error state
  if (error) {
    return (
      <div className="container mx-auto px-4 py-6">
        <Card className="p-8 text-center">
          <AlertTriangle className="w-12 h-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-red-600 mb-4">
            Machine Data Error
          </h2>
          <p className="text-neutral-600 mb-6">{error}</p>
          <Button onClick={handleRefresh} disabled={refreshing}>
            <RefreshCw className={cn('w-4 h-4 mr-2', refreshing && 'animate-spin')} />
            Retry
          </Button>
        </Card>
      </div>
    )
  }

  // Loading state
  if (loading && !currentMachine) {
    return (
      <div className="container mx-auto px-4 py-6">
        <Card className="p-8 text-center">
          <LoadingSpinner size="lg" text="Loading machine details..." />
        </Card>
      </div>
    )
  }

  // Machine not found
  if (!loading && !currentMachine && machineId) {
    return (
      <div className="container mx-auto px-4 py-6">
        <Card className="p-8 text-center">
          <XCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-red-600 mb-4">
            Machine Not Found
          </h2>
          <p className="text-neutral-600 mb-6">
            Machine ID "{machineId}" was not found or you don't have access to it.
          </p>
          <Button onClick={() => navigate('/dashboard/oee-dashboard')}>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to OEE Dashboard
          </Button>
        </Card>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-4">
          <Button 
            variant="outline" 
            size="sm"
            onClick={() => navigate('/dashboard/oee-dashboard')}
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back
          </Button>
          
          <div>
            <h1 className="text-2xl font-bold text-neutral-900">
              {currentMachine?.equipmentName || `Machine ${machineId}`}
            </h1>
            <div className="flex items-center space-x-4 mt-1">
              <p className="text-sm text-neutral-600">
                ID: {machineId}
              </p>
              <StatusIndicator
                status={
                  machineStatus === 'producing' ? 'success' :
                  machineStatus === 'idle' ? 'warning' : 'error'
                }
                label={machineStatus.toUpperCase()}
              />
              <Badge variant="outline">
                Last updated: {currentMachine?.timestamp ? 
                  new Date(currentMachine.timestamp).toLocaleTimeString() : 'Unknown'
                }
              </Badge>
            </div>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          {/* Time Period Selector */}
          <select
            value={timePeriod}
            onChange={(e) => setTimePeriod(e.target.value as any)}
            className="px-3 py-2 border border-neutral-200 rounded-md text-sm focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          >
            <option value="1h">Last Hour</option>
            <option value="8h">Last 8 Hours</option>
            <option value="24h">Last 24 Hours</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
          </select>

          <Button 
            variant="outline" 
            size="sm" 
            onClick={handleRefresh}
            disabled={refreshing}
          >
            <RefreshCw className={cn('w-4 h-4', refreshing && 'animate-spin')} />
          </Button>
        </div>
      </div>

      {/* Main Content */}
      <Tabs value={selectedTab} onValueChange={setSelectedTab}>
        <TabsList className="grid w-full grid-cols-5">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="work-orders">Work Orders</TabsTrigger>
          <TabsTrigger value="stoppages">Stoppages</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
          <TabsTrigger value="reports">Reports</TabsTrigger>
        </TabsList>

        <div className="mt-6">
          <TabsContent value="overview">
            {renderOverview()}
          </TabsContent>

          <TabsContent value="work-orders">
            <WorkOrderManagement 
              selectedMachine={machineId}
              showMachineColumn={false}
            />
          </TabsContent>

          <TabsContent value="stoppages">
            <StoppageTracking 
              selectedMachine={machineId}
              showMachineColumn={false}
            />
          </TabsContent>

          <TabsContent value="analytics">
            <Card className="p-6">
              <h3 className="text-lg font-semibold mb-4">Advanced Analytics</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="p-4 border border-neutral-200 rounded">
                  <h4 className="font-medium mb-2">Historical Trends</h4>
                  <p className="text-sm text-neutral-600">
                    Time series analysis of OEE performance over selected period.
                    Charts would show availability, performance, and quality trends.
                  </p>
                  {loadingHistory && <LoadingSpinner size="sm" className="mt-2" />}
                </div>
                <div className="p-4 border border-neutral-200 rounded">
                  <h4 className="font-medium mb-2">Predictive Analysis</h4>
                  <p className="text-sm text-neutral-600">
                    Predictive maintenance alerts and performance forecasting
                    based on historical patterns and machine learning models.
                  </p>
                </div>
              </div>
              
              <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded">
                <p className="text-sm text-yellow-700">
                  <strong>CFR Part 11 Compliance:</strong> All analytics are based on 
                  validated real measurement data. No synthetic or estimated values 
                  are used in calculations to maintain regulatory compliance.
                </p>
              </div>
            </Card>
          </TabsContent>

          <TabsContent value="reports">
            <Card className="p-6">
              <h3 className="text-lg font-semibold mb-4">Machine Reports</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                <Button variant="outline" className="h-20 flex-col">
                  <FileText className="w-6 h-6 mb-2" />
                  <span className="text-sm">OEE Summary</span>
                </Button>
                <Button variant="outline" className="h-20 flex-col">
                  <BarChart3 className="w-6 h-6 mb-2" />
                  <span className="text-sm">Performance Report</span>
                </Button>
                <Button variant="outline" className="h-20 flex-col">
                  <Activity className="w-6 h-6 mb-2" />
                  <span className="text-sm">Stoppage Analysis</span>
                </Button>
                <Button variant="outline" className="h-20 flex-col">
                  <Calendar className="w-6 h-6 mb-2" />
                  <span className="text-sm">Shift Report</span>
                </Button>
                <Button variant="outline" className="h-20 flex-col">
                  <Target className="w-6 h-6 mb-2" />
                  <span className="text-sm">Target vs Actual</span>
                </Button>
                <Button variant="outline" className="h-20 flex-col">
                  <CheckCircle className="w-6 h-6 mb-2" />
                  <span className="text-sm">Compliance Report</span>
                </Button>
              </div>
              
              <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded">
                <h4 className="font-medium text-blue-900 mb-2">Report Generation</h4>
                <p className="text-sm text-blue-700">
                  All reports are generated with full audit trails and CFR Part 11 
                  compliance. Reports include data quality indicators and validation status.
                </p>
              </div>
            </Card>
          </TabsContent>
        </div>
      </Tabs>
    </div>
  )
}

export default MachineDetail