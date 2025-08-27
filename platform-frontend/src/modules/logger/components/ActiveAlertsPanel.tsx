/**
 * Active Alerts Panel Component
 * Displays active device alerts and alarms with acknowledgment capabilities
 */

import React, { useState, useEffect } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { StatusIndicator } from '@/components/shared/StatusIndicator'
import { AlertTriangle, CheckCircle, Bell, BellOff, Eye } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { DeviceData } from '../types'

interface AlertItem {
  id: string
  deviceId: string
  deviceName: string
  type: 'device_offline' | 'data_quality' | 'communication_error' | 'configuration_warning'
  severity: 'low' | 'medium' | 'high' | 'critical'
  message: string
  timestamp: Date
  acknowledged: boolean
  acknowledgedBy?: string
  acknowledgedAt?: Date
}

interface ActiveAlertsPanelProps {
  devices: DeviceData[]
  priority?: 'low' | 'medium' | 'high' | 'critical'
  maxVisible?: number
  autoRefresh?: boolean
  allowAcknowledgment?: boolean
  showImpactMetrics?: boolean
  showSystemMetrics?: boolean
  className?: string
}

const generateAlertsFromDevices = (devices: DeviceData[]): AlertItem[] => {
  const alerts: AlertItem[] = []
  
  devices.forEach(device => {
    // Device offline alerts
    if (device.status === 'offline') {
      alerts.push({
        id: `${device.id}-offline`,
        deviceId: device.id,
        deviceName: device.name,
        type: 'device_offline',
        severity: 'high',
        message: `Device ${device.name} is offline`,
        timestamp: device.lastSeen,
        acknowledged: false
      })
    }
    
    // Connection quality alerts
    if (device.connectionQuality === 'poor') {
      alerts.push({
        id: `${device.id}-connection`,
        deviceId: device.id,
        deviceName: device.name,
        type: 'communication_error',
        severity: 'medium',
        message: `Poor connection quality to ${device.name}`,
        timestamp: new Date(),
        acknowledged: false
      })
    }
    
    // Data quality alerts based on health
    if (device.health && device.health.dataQuality.bad > 10) {
      alerts.push({
        id: `${device.id}-data-quality`,
        deviceId: device.id,
        deviceName: device.name,
        type: 'data_quality',
        severity: 'medium',
        message: `High percentage of bad data quality from ${device.name}`,
        timestamp: new Date(),
        acknowledged: false
      })
    }
  })
  
  return alerts.sort((a, b) => {
    const severityOrder = { critical: 4, high: 3, medium: 2, low: 1 }
    const severityDiff = severityOrder[b.severity] - severityOrder[a.severity]
    if (severityDiff !== 0) return severityDiff
    return b.timestamp.getTime() - a.timestamp.getTime()
  })
}

const getSeverityColor = (severity: AlertItem['severity']): string => {
  switch (severity) {
    case 'critical': return 'bg-red-600 text-white'
    case 'high': return 'bg-red-500 text-white'
    case 'medium': return 'bg-yellow-500 text-white'
    case 'low': return 'bg-blue-500 text-white'
    default: return 'bg-gray-500 text-white'
  }
}

const formatRelativeTime = (date: Date): string => {
  const now = new Date()
  const diff = Math.floor((now.getTime() - date.getTime()) / 1000)
  
  if (diff < 60) return `${diff}s ago`
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
  return `${Math.floor(diff / 86400)}d ago`
}

export const ActiveAlertsPanel: React.FC<ActiveAlertsPanelProps> = ({
  devices,
  priority,
  maxVisible = 10,
  autoRefresh = false,
  allowAcknowledgment = false,
  showImpactMetrics = false,
  showSystemMetrics = false,
  className
}) => {
  const [alerts, setAlerts] = useState<AlertItem[]>([])
  const [mutedAlerts, setMutedAlerts] = useState<Set<string>>(new Set())

  // Generate alerts from device data
  useEffect(() => {
    const generatedAlerts = generateAlertsFromDevices(devices)
    
    // Filter by priority if specified
    const filteredAlerts = priority 
      ? generatedAlerts.filter(alert => alert.severity === priority)
      : generatedAlerts
    
    setAlerts(filteredAlerts.slice(0, maxVisible))
  }, [devices, priority, maxVisible])

  // Auto refresh
  useEffect(() => {
    if (!autoRefresh) return
    
    const interval = setInterval(() => {
      const refreshedAlerts = generateAlertsFromDevices(devices)
      const filteredAlerts = priority 
        ? refreshedAlerts.filter(alert => alert.severity === priority)
        : refreshedAlerts
      setAlerts(filteredAlerts.slice(0, maxVisible))
    }, 30000) // Refresh every 30 seconds
    
    return () => clearInterval(interval)
  }, [devices, priority, maxVisible, autoRefresh])

  const handleAcknowledgeAlert = (alertId: string) => {
    setAlerts(prev => prev.map(alert => 
      alert.id === alertId 
        ? { 
            ...alert, 
            acknowledged: true, 
            acknowledgedBy: 'Current User',
            acknowledgedAt: new Date()
          }
        : alert
    ))
  }

  const handleMuteAlert = (alertId: string) => {
    setMutedAlerts(prev => new Set([...prev, alertId]))
  }

  const activeAlerts = alerts.filter(alert => !alert.acknowledged && !mutedAlerts.has(alert.id))
  const acknowledgedAlerts = alerts.filter(alert => alert.acknowledged)

  return (
    <Card className={cn('active-alerts-panel', className)}>
      <div className="p-4 border-b">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <Bell className="w-5 h-5 text-neutral-600" />
            <h3 className="text-lg font-semibold text-neutral-900">Active Alerts</h3>
            {activeAlerts.length > 0 && (
              <Badge className={getSeverityColor('high')}>
                {activeAlerts.length}
              </Badge>
            )}
          </div>
          
          {showSystemMetrics && (
            <div className="flex items-center space-x-4 text-sm text-neutral-600">
              <span>Critical: {alerts.filter(a => a.severity === 'critical').length}</span>
              <span>High: {alerts.filter(a => a.severity === 'high').length}</span>
              <span>Medium: {alerts.filter(a => a.severity === 'medium').length}</span>
            </div>
          )}
        </div>
      </div>
      
      <div className="max-h-96 overflow-auto">
        {activeAlerts.length === 0 ? (
          <div className="p-8 text-center">
            <CheckCircle className="w-12 h-12 text-status-success mx-auto mb-4" />
            <h4 className="text-lg font-medium text-neutral-900 mb-2">
              All Clear
            </h4>
            <p className="text-neutral-600">
              No active alerts at this time
            </p>
          </div>
        ) : (
          <div className="space-y-2 p-4">
            {activeAlerts.map(alert => (
              <div
                key={alert.id}
                className={cn(
                  'p-3 rounded-lg border-l-4 bg-white',
                  alert.severity === 'critical' && 'border-l-red-600 bg-red-50',
                  alert.severity === 'high' && 'border-l-red-500 bg-red-50',
                  alert.severity === 'medium' && 'border-l-yellow-500 bg-yellow-50',
                  alert.severity === 'low' && 'border-l-blue-500 bg-blue-50'
                )}
              >
                <div className="flex items-start space-x-3">
                  <StatusIndicator
                    status={alert.severity === 'critical' || alert.severity === 'high' ? 'error' : 'warning'}
                    size="sm"
                  />
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-2 mb-1">
                      <Badge className={getSeverityColor(alert.severity)}>
                        {alert.severity.toUpperCase()}
                      </Badge>
                      <span className="text-sm font-medium text-neutral-900">
                        {alert.deviceName}
                      </span>
                      <span className="text-xs text-neutral-500">
                        {formatRelativeTime(alert.timestamp)}
                      </span>
                    </div>
                    
                    <p className="text-sm text-neutral-700 mb-2">
                      {alert.message}
                    </p>
                    
                    {showImpactMetrics && (
                      <div className="text-xs text-neutral-600">
                        Impact: Device offline affects {alert.deviceName} counter readings
                      </div>
                    )}
                  </div>
                  
                  <div className="flex space-x-1 flex-shrink-0">
                    {allowAcknowledgment && (
                      <Button
                        size="xs"
                        variant="outline"
                        onClick={() => handleAcknowledgeAlert(alert.id)}
                        className="text-status-success"
                      >
                        <CheckCircle className="w-3 h-3 mr-1" />
                        ACK
                      </Button>
                    )}
                    
                    <Button
                      size="xs"
                      variant="outline"
                      onClick={() => handleMuteAlert(alert.id)}
                    >
                      <BellOff className="w-3 h-3" />
                    </Button>
                    
                    <Button
                      size="xs"
                      variant="outline"
                    >
                      <Eye className="w-3 h-3" />
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
        
        {/* Acknowledged alerts section */}
        {acknowledgedAlerts.length > 0 && (
          <div className="border-t bg-neutral-50">
            <div className="p-3">
              <h4 className="text-sm font-semibold text-neutral-700 mb-2">
                Recently Acknowledged ({acknowledgedAlerts.length})
              </h4>
              <div className="space-y-1">
                {acknowledgedAlerts.slice(0, 3).map(alert => (
                  <div key={alert.id} className="flex items-center space-x-2 text-xs text-neutral-600">
                    <CheckCircle className="w-3 h-3 text-status-success" />
                    <span>{alert.deviceName}: {alert.message}</span>
                    <span>by {alert.acknowledgedBy}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
      
      {/* Alert summary footer */}
      {(showImpactMetrics || showSystemMetrics) && (
        <div className="border-t p-4 bg-neutral-50">
          <div className="grid grid-cols-3 gap-4 text-center text-sm">
            <div>
              <div className="font-semibold text-status-error">
                {activeAlerts.filter(a => a.severity === 'critical' || a.severity === 'high').length}
              </div>
              <div className="text-neutral-600">Critical/High</div>
            </div>
            <div>
              <div className="font-semibold text-status-warning">
                {activeAlerts.filter(a => a.severity === 'medium').length}
              </div>
              <div className="text-neutral-600">Medium</div>
            </div>
            <div>
              <div className="font-semibold text-neutral-700">
                {acknowledgedAlerts.length}
              </div>
              <div className="text-neutral-600">Acknowledged</div>
            </div>
          </div>
        </div>
      )}
    </Card>
  )
}

export default ActiveAlertsPanel