/**
 * Logger Module Types
 * Type definitions for ADAM device management and counter monitoring
 */

import type { DataQuality } from '@/types'

export interface DeviceData {
  id: string
  name: string
  type: string
  ipAddress: string
  status: 'online' | 'offline' | 'warning' | 'error'
  lastSeen: Date
  connectionQuality: 'excellent' | 'good' | 'poor' | 'offline'
  counters: {
    total: number
    active: number
    totalCounts: number
    countsPerHour: number
  }
  health?: DeviceHealth
}

export interface CounterData {
  deviceId: string
  deviceName: string
  channel: number
  channelName: string
  currentValue: number
  unit: string
  ratePerMinute: number
  ratePerHour: number
  lastUpdate: Date
  dataQuality: DataQuality
  trend: 'up' | 'down' | 'stable'
  isRealData: boolean
}

export interface CounterUpdate {
  deviceId: string
  deviceName: string
  channel: number
  channelName?: string
  value: number
  unit?: string
  timestamp: Date
  quality: DataQuality
  ratePerMinute?: number
  ratePerHour?: number
}

export interface DeviceHealth {
  deviceId: string
  status: 'healthy' | 'warning' | 'error' | 'offline'
  lastSeen: Date
  uptime: number
  responseTime: {
    current: number
    average: number
    max: number
  }
  errorCount: {
    last24h: number
    lastWeek: number
    lastMonth: number
  }
  dataQuality: {
    good: number
    uncertain: number
    bad: number
  }
  communicationErrors: Array<{
    timestamp: Date
    error: string
    severity: 'low' | 'medium' | 'high'
  }>
}

export interface DeviceConfiguration {
  device: {
    id: string
    name: string
    type: string
    ipAddress: string
    port: number
    location?: string
    description?: string
  }
  modbus: {
    slaveId: number
    baudRate: number
    parity: string
    stopBits: number
    dataBits: number
  }
  channels: Array<{
    id: string
    name: string
    address: number
    dataType: 'Counter' | 'Digital' | 'Analog'
    unit: string
    scalingFactor: number
    enabled: boolean
    alarmConfig?: {
      highLimit?: number
      lowLimit?: number
      enabled: boolean
    }
  }>
  logging: {
    interval: number
    bufferSize: number
    compression: boolean
    retention: number
  }
}

export interface CounterDataPoint {
  deviceId: string
  channel: number
  value: number
  rate?: number
  timestamp: Date
  quality: DataQuality
}

export interface TimeRange {
  preset: TimeRangePreset
  start: Date
  end: Date
}

export type TimeRangePreset = '1h' | '4h' | '24h' | '7d' | '30d' | 'custom'

export interface ValidationResult {
  success: boolean
  error?: string
  details?: {
    tcpConnection: boolean
    modbusResponse: boolean
    dataValidation: boolean
    timestamp: Date
  }
}

export interface DeviceAlarm {
  id: string
  deviceId: string
  deviceName: string
  channelId: string
  channelName: string
  type: 'high_limit' | 'low_limit' | 'communication_error' | 'data_quality'
  severity: 'low' | 'medium' | 'high'
  message: string
  value?: number
  threshold?: number
  timestamp: Date
  acknowledgedAt?: Date
  acknowledgedBy?: string
  resolved: boolean
  resolvedAt?: Date
}

export interface DeviceStatistics {
  period: string
  totalReadings: number
  avgCounterValue: number
  maxCounterValue: number
  avgRate: number
  maxRate: number
  dataQualityStats: {
    good: number
    uncertain: number
    bad: number
  }
  uptimePercent: number
  trends: Array<{
    timestamp: Date
    count: number
    rate: number
    quality: DataQuality
  }>
}

export interface SystemHealth {
  overall: 'healthy' | 'warning' | 'error'
  devices: {
    total: number
    online: number
    offline: number
    error: number
  }
  dataFlow: {
    pointsPerSecond: number
    avgLatency: number
    errorRate: number
  }
  storage: {
    used: number
    available: number
    retentionCompliance: number
  }
  services: Array<{
    name: string
    status: 'running' | 'stopped' | 'error'
    uptime: number
    lastCheck: Date
  }>
}

export interface DiscoveredDevice {
  ipAddress: string
  port: number
  deviceType: string
  serialNumber?: string
  firmware?: string
  model?: string
  configured: boolean
}

// CFR Part 11 Compliance Types
export interface AuditTrail {
  accessTime: Date
  userId: string
  userRole: string
  action: string
  dataPoint: {
    deviceId: string
    channel: number
    value: number
    timestamp: Date
    quality: DataQuality
  }
  reason: string
  purpose: AuditPurpose
  ipAddress: string
}

export type AuditPurpose = 'compliance-review' | 'quality-audit' | 'process-investigation' | 'troubleshooting' | 'performance-analysis' | 'other'

export interface DataQualityInfo {
  quality: DataQuality
  source: {
    deviceId: string
    channel: number
  }
  timestamp: Date
  auditTrail?: AuditTrail[]
  complianceFlags: string[]
}