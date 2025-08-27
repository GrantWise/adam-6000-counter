import React from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { MetricCard, OEEMetricCard, CounterMetricCard } from '@/components/shared/MetricCard'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { 
  Activity,
  Database,
  BarChart3,
  Clock,
  TrendingUp,
  AlertCircle
} from 'lucide-react'

/**
 * User Dashboard - Operational overview for factory floor personnel
 */
const UserDashboard: React.FC = () => {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Production Dashboard</h1>
          <p className="text-muted-foreground mt-1">
            Real-time overview of your production line
          </p>
        </div>
        <div className="mt-4 md:mt-0">
          <StatusIndicator
            status="healthy"
            label="Line Status: Running"
            description="All systems operational"
            size="lg"
          />
        </div>
      </div>

      {/* Key Performance Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <OEEMetricCard
          title="Overall OEE"
          value={87.5}
          target={85}
        />
        <MetricCard
          title="Availability"
          value={92.1}
          unit="%"
          status="healthy"
          trend={{ direction: 'up', percentage: 2.3, period: '24h' }}
          icon={Activity}
        />
        <MetricCard
          title="Performance"
          value={94.8}
          unit="%"
          status="healthy"
          trend={{ direction: 'stable', percentage: 0, period: '24h' }}
          icon={TrendingUp}
        />
        <MetricCard
          title="Quality"
          value={96.2}
          unit="%"
          status="warning"
          trend={{ direction: 'down', percentage: 1.2, period: '24h' }}
          icon={BarChart3}
        />
      </div>

      {/* Production Counters */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <CounterMetricCard
          title="Total Production"
          value={15420}
          rate={42.3}
          unit="parts"
        />
        <CounterMetricCard
          title="Good Parts"
          value={14834}
          rate={40.7}
          unit="parts"
        />
        <CounterMetricCard
          title="Rejected Parts"
          value={586}
          rate={1.6}
          unit="parts"
        />
      </div>

      {/* Equipment Status and Recent Events */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Database className="h-5 w-5" />
              Equipment Status
            </CardTitle>
            <CardDescription>
              Current status of connected devices
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center">
                    <Database className="w-5 h-5 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium">ADAM-6051-01</p>
                    <p className="text-sm text-muted-foreground">Counter Input Device</p>
                  </div>
                </div>
                <StatusIndicator
                  status="healthy"
                  label="Online"
                  size="sm"
                />
              </div>
              
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center">
                    <Database className="w-5 h-5 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium">ADAM-6051-02</p>
                    <p className="text-sm text-muted-foreground">Quality Scanner</p>
                  </div>
                </div>
                <StatusIndicator
                  status="healthy"
                  label="Online"
                  size="sm"
                />
              </div>
              
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center">
                    <Database className="w-5 h-5 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium">ADAM-6051-03</p>
                    <p className="text-sm text-muted-foreground">Reject Counter</p>
                  </div>
                </div>
                <StatusIndicator
                  status="warning"
                  label="Slow Response"
                  size="sm"
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5" />
              Recent Events
            </CardTitle>
            <CardDescription>
              Latest production events and alerts
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-start space-x-3 p-3 border rounded-md">
                <AlertCircle className="w-4 h-4 text-status-warning mt-1 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">Quality Alert</p>
                  <p className="text-xs text-muted-foreground">
                    Reject rate increased to 3.8% on Line 3
                  </p>
                  <p className="text-xs text-muted-foreground">5 minutes ago</p>
                </div>
              </div>
              
              <div className="flex items-start space-x-3 p-3 border rounded-md">
                <TrendingUp className="w-4 h-4 text-status-success mt-1 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">Production Milestone</p>
                  <p className="text-xs text-muted-foreground">
                    Daily target of 15,000 parts reached
                  </p>
                  <p className="text-xs text-muted-foreground">1 hour ago</p>
                </div>
              </div>
              
              <div className="flex items-start space-x-3 p-3 border rounded-md">
                <Database className="w-4 h-4 text-status-info mt-1 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">Device Maintenance</p>
                  <p className="text-xs text-muted-foreground">
                    ADAM-6051-02 completed scheduled maintenance
                  </p>
                  <p className="text-xs text-muted-foreground">2 hours ago</p>
                </div>
              </div>
              
              <div className="flex items-start space-x-3 p-3 border rounded-md">
                <Activity className="w-4 h-4 text-status-success mt-1 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">Shift Started</p>
                  <p className="text-xs text-muted-foreground">
                    Day shift began - all systems nominal
                  </p>
                  <p className="text-xs text-muted-foreground">8 hours ago</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Production Schedule */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            Today's Schedule
          </CardTitle>
          <CardDescription>
            Production schedule and work orders
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center justify-between p-4 border rounded-md bg-status-success/10">
              <div className="flex items-center space-x-3">
                <div className="w-3 h-3 bg-status-success rounded-full" />
                <div>
                  <p className="font-medium">Work Order #WO-2025-0234</p>
                  <p className="text-sm text-muted-foreground">Product A - Target: 15,000 units</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-status-success">97% Complete</p>
                <p className="text-xs text-muted-foreground">14,550 / 15,000</p>
              </div>
            </div>
            
            <div className="flex items-center justify-between p-4 border rounded-md">
              <div className="flex items-center space-x-3">
                <div className="w-3 h-3 bg-muted rounded-full" />
                <div>
                  <p className="font-medium">Work Order #WO-2025-0235</p>
                  <p className="text-sm text-muted-foreground">Product B - Target: 8,000 units</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-muted-foreground">Scheduled</p>
                <p className="text-xs text-muted-foreground">Starts at 4:00 PM</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default UserDashboard