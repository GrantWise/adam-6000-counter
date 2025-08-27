/**
 * Logger Dashboard Component
 * Main dashboard for ADAM device monitoring and counter visualization
 * Follows LOGGER-MODULE-DESIGN.md specifications
 */

import React, { useState, useEffect } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { MetricCard } from '@/components/shared/MetricCard'
import { DataQualityIndicator } from '@/components/ui/data-quality-indicator'
import { useAuth } from '@/hooks/useAuth'
import { useDeviceData } from '../hooks/useDeviceData'
import { useRealTimeCounters } from '../hooks/useRealTimeCounters'
import { DeviceStatusCard } from './DeviceStatusCard'
import { CounterDisplayWidget } from './CounterDisplayWidget'
import { SystemHealthOverview } from './SystemHealthOverview'
import { ActiveAlertsPanel } from './ActiveAlertsPanel'
import { RecentCounterUpdates } from './RecentCounterUpdates'
import { cn } from '@/lib/utils'

const LoggerDashboard: React.FC = () => {
  const { user } = useAuth()
  const [selectedTab, setSelectedTab] = useState('overview')
  
  // Role-based interface adaptation
  const isOperator = user?.role === 'Operator'
  const isSupervisor = user?.role === 'Supervisor' 
  const isAdmin = user?.role === 'Admin'

  // Data hooks
  const { 
    devices, 
    loading: devicesLoading, 
    error: devicesError, 
    summary,
    refreshDevices 
  } = useDeviceData({ 
    includeHealth: !isOperator,
    refreshInterval: isOperator ? 10000 : 30000 // Operators get faster updates
  })

  const {
    counters,
    lastUpdate,
    connectionState,
    isConnected,
    error: countersError
  } = useRealTimeCounters({
    enableUpdates: true,
    maxUpdateFrequency: 5
  })

  // Render role-specific dashboard layout
  const renderOperatorView = () => (
    <div className={cn('logger-dashboard operator-view')}>
      {/* System Status Overview - Large and prominent */}
      <div className="mb-6">
        <SystemHealthOverview 
          summary={summary}
          devices={devices}
          counters={counters}
          connectionState={connectionState}
          size="large"
          simplified
        />
      </div>

      {/* Active Alerts Panel - High visibility */}
      <div className="mb-6">
        <ActiveAlertsPanel 
          devices={devices}
          priority="high"
          maxVisible={5}
          autoRefresh
        />
      </div>

      {/* Recent Counter Updates - Expandable */}
      <div>
        <RecentCounterUpdates 
          counters={counters}
          lastUpdate={lastUpdate}
          maxVisible={10}
          showDataQuality
        />
      </div>
    </div>
  )

  const renderSupervisorView = () => (
    <div className={cn('logger-dashboard supervisor-view')}>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-full">
        {/* Left Column - Device Management */}
        <div className="lg:col-span-2 space-y-6">
          {/* Device Status Grid */}
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-neutral-900">Device Status</h3>
              <div className="flex items-center space-x-2">
                <StatusIndicator 
                  status={isConnected ? 'success' : 'error'} 
                  label={connectionState}
                />
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={refreshDevices}
                  disabled={devicesLoading}
                >
                  Refresh
                </Button>
              </div>
            </div>
            
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
              {devices.map(device => (
                <DeviceStatusCard
                  key={device.id}
                  device={device}
                  counters={counters.filter(c => c.deviceId === device.id)}
                  size="standard"
                  showActions
                />
              ))}
            </div>
          </Card>

          {/* Alert Management Panel */}
          <ActiveAlertsPanel 
            devices={devices}
            allowAcknowledgment
            showImpactMetrics
            maxVisible={8}
          />
        </div>

        {/* Right Column - Analytics */}
        <div className="space-y-6">
          {/* Counter Trends */}
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Counter Trends</h3>
            <div className="space-y-4">
              {counters.slice(0, 6).map(counter => (
                <CounterDisplayWidget
                  key={`${counter.deviceId}-${counter.channel}`}
                  counter={counter}
                  size="compact"
                  showTrend
                  compliance={{ showQualityIndicator: true }}
                />
              ))}
            </div>
          </Card>

          {/* Quick Actions */}
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Quick Actions</h3>
            <div className="space-y-2">
              <Button variant="outline" className="w-full justify-start">
                Configure Devices
              </Button>
              <Button variant="outline" className="w-full justify-start">
                Export Data
              </Button>
              <Button variant="outline" className="w-full justify-start">
                View Analytics
              </Button>
              <Button variant="outline" className="w-full justify-start">
                Generate Report
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )

  const renderAdminView = () => (
    <div className={cn('logger-dashboard admin-view')}>
      <Tabs value={selectedTab} onValueChange={setSelectedTab} className="h-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="devices">Devices</TabsTrigger>
          <TabsTrigger value="monitoring">Monitoring</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6 mt-6">
          {/* System Metrics Row */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <MetricCard
              title="Total Devices"
              value={summary?.total || 0}
              trend={summary?.total > 5 ? 'up' : 'stable'}
              format="number"
            />
            <MetricCard
              title="Online Devices"
              value={summary?.online || 0}
              total={summary?.total || 0}
              trend={summary?.online > 2 ? 'up' : 'down'}
              format="fraction"
              status={summary?.online === summary?.total ? 'success' : 'warning'}
            />
            <MetricCard
              title="Data Points Today"
              value={summary?.dataPointsToday || 0}
              trend="up"
              format="number"
            />
            <MetricCard
              title="Avg Response Time"
              value={summary?.avgResponseTime || 0}
              unit="ms"
              trend="stable"
              format="number"
            />
          </div>

          {/* Main Dashboard Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2">
              <SystemHealthOverview 
                summary={summary}
                devices={devices}
                counters={counters}
                connectionState={connectionState}
                detailed
              />
            </div>
            <div>
              <ActiveAlertsPanel 
                devices={devices}
                allowAcknowledgment
                showSystemMetrics
              />
            </div>
          </div>
        </TabsContent>

        <TabsContent value="devices" className="mt-6">
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {devices.map(device => (
              <DeviceStatusCard
                key={device.id}
                device={device}
                counters={counters.filter(c => c.deviceId === device.id)}
                size="detailed"
                showActions
                showHealth
              />
            ))}
          </div>
        </TabsContent>

        <TabsContent value="monitoring" className="mt-6">
          <RecentCounterUpdates 
            counters={counters}
            lastUpdate={lastUpdate}
            showDataQuality
            showExportOptions
            enableFiltering
          />
        </TabsContent>

        <TabsContent value="analytics" className="mt-6">
          <Card className="p-6">
            <h3 className="text-lg font-semibold text-neutral-900 mb-4">Analytics Coming Soon</h3>
            <p className="text-neutral-600">
              Advanced analytics and historical trends will be available in the next release.
            </p>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )

  // Error state
  if (devicesError) {
    return (
      <div className="logger-dashboard error-state">
        <Card className="p-8 text-center">
          <h2 className="text-xl font-semibold text-status-error mb-4">
            Logger Dashboard Error
          </h2>
          <p className="text-neutral-600 mb-6">{devicesError}</p>
          <Button onClick={refreshDevices} disabled={devicesLoading}>
            Retry
          </Button>
        </Card>
      </div>
    )
  }

  // Loading state
  if (devicesLoading && !devices.length) {
    return (
      <div className="logger-dashboard loading-state">
        <Card className="p-8 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto mb-4" />
          <p className="text-neutral-600">Loading Logger Dashboard...</p>
        </Card>
      </div>
    )
  }

  // CFR Part 11 Compliance Warning
  const showComplianceWarning = !isConnected || countersError
  
  return (
    <div className="logger-dashboard">
      {/* CFR Part 11 Compliance Header */}
      {showComplianceWarning && (
        <div className="mb-6">
          <Card className="p-4 bg-yellow-50 border-yellow-200">
            <div className="flex items-center space-x-2">
              <DataQualityIndicator quality="unavailable" size="sm" />
              <div>
                <h4 className="font-semibold text-yellow-800">Data Quality Warning</h4>
                <p className="text-sm text-yellow-700">
                  Real-time data connection unavailable. Displaying last known values only.
                  {countersError && ` Error: ${countersError}`}
                </p>
              </div>
            </div>
          </Card>
        </div>
      )}

      {/* Role-based dashboard content */}
      {isOperator && renderOperatorView()}
      {isSupervisor && renderSupervisorView()}
      {isAdmin && renderAdminView()}
    </div>
  )
}

export default LoggerDashboard