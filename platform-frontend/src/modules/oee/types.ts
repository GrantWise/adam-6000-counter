/**
 * OEE Module Types
 * Type definitions for Overall Equipment Effectiveness monitoring and management
 */

import type { DataQuality } from '@/types'

export interface OeeData {
  equipmentId: string
  equipmentName: string
  timestamp: Date
  oee: number | null  // Overall OEE percentage (0-100)
  availability: number | null  // Availability percentage (0-100)
  performance: number | null   // Performance percentage (0-100)
  quality: number | null       // Quality percentage (0-100)
  target?: number             // Target OEE percentage
  
  // Component calculations
  plannedProductionTime: number | null  // minutes
  actualRunTime: number | null          // minutes
  idealCycleTime: number | null         // seconds per unit
  totalCount: number | null
  goodCount: number | null
  rejectCount: number | null
  
  // CFR Part 11 compliance
  dataQuality?: DataQuality
  isRealData?: boolean
  auditInfo?: {
    sourceSystem: string
    dataIntegrity: string
    complianceFlags: string[]
  }
}

export type OeeLevel = 'excellent' | 'good' | 'needs-improvement' | 'poor'

export interface WorkOrderData {
  id: string
  orderNumber: string
  equipmentId: string
  equipmentName: string
  product: string
  productCode: string
  
  // Quantities
  targetQuantity: number
  completedQuantity: number
  rejectQuantity?: number
  
  // OEE targets and actuals
  targetOee: number
  currentOee: number
  
  // Status and timing
  status: 'planned' | 'active' | 'completed' | 'delayed' | 'cancelled'
  priority: 'low' | 'medium' | 'high' | 'urgent'
  
  // Scheduling
  startTime: Date
  endTime?: Date
  dueDate: Date
  estimatedCompletion?: Date
  
  // Resources
  assignedOperators: string[]
  materialStatus: 'available' | 'partial' | 'unavailable'
  equipmentStatus: 'ready' | 'busy' | 'maintenance'
  
  // Performance metrics
  progressPercentage: number
  efficiency: number
  qualityRate: number
  
  createdAt: Date
  updatedAt: Date
}

export interface StoppageData {
  id: string
  equipmentId: string
  equipmentName: string
  workOrderId?: string
  
  // Timing
  startTime: Date
  endTime?: Date
  duration?: number  // minutes
  
  // Classification
  category: 'planned' | 'unplanned'
  reasonCode: string
  reasonCategory: 'maintenance' | 'setup' | 'material' | 'quality' | 'operator' | 'other'
  description?: string
  
  // Impact analysis
  impact: {
    availabilityLoss: number      // percentage
    performanceLoss: number       // percentage
    productionLoss: number        // units lost
    estimatedCost?: number        // monetary impact
  }
  
  // Status tracking
  status: 'active' | 'acknowledged' | 'resolved'
  acknowledgedBy?: string
  acknowledgedAt?: Date
  resolvedBy?: string
  resolvedAt?: Date
  resolution?: string
  
  // Additional metadata
  severity: 'low' | 'medium' | 'high' | 'critical'
  assignedTo?: string
  notes?: string
  
  createdAt: Date
  updatedAt: Date
}

export interface OeeLossAnalysis {
  equipmentId: string
  timeRange: { start: Date; end: Date }
  
  // Total loss breakdown
  totalLoss: number  // minutes
  availabilityLoss: number
  performanceLoss: number
  qualityLoss: number
  
  // Detailed breakdown
  breakdown: {
    plannedDowntime: number
    unplannedDowntime: number
    speedLoss: number
    minorStoppages: number
    defects: number
    reducedYield: number
  }
  
  // Top loss reasons
  topReasons: Array<{
    reason: string
    category: string
    impact: number  // minutes
    frequency: number
    percentage: number
  }>
  
  // Improvement opportunities
  improvementOpportunities: Array<{
    area: 'availability' | 'performance' | 'quality'
    potential: number  // percentage points
    effort: 'low' | 'medium' | 'high'
    priority: 'low' | 'medium' | 'high'
    description: string
  }>
}

export interface ProductionScheduleItem {
  id: string
  equipmentId: string
  equipmentName: string
  workOrderId: string
  productCode: string
  productName: string
  
  plannedStart: Date
  plannedEnd: Date
  actualStart?: Date
  actualEnd?: Date
  
  status: 'scheduled' | 'running' | 'completed' | 'delayed' | 'cancelled'
  progress: number  // percentage
  
  targetQuantity: number
  producedQuantity: number
  targetOee: number
  actualOee?: number
}

export interface OeeBenchmark {
  category: 'industry' | 'world-class' | 'company'
  oee: number
  availability: number
  performance: number
  quality: number
  source?: string
  lastUpdated: Date
}

export interface OeeTarget {
  equipmentId?: string  // undefined for global targets
  oee: number
  availability: number
  performance: number
  quality: number
  effectiveFrom: Date
  effectiveTo?: Date
  setBy: string
  reason?: string
}

export interface OeeTrend {
  timestamp: Date
  oee: number
  availability: number
  performance: number
  quality: number
  target: number
  equipment: string
  shift?: string
  workOrder?: string
}

export interface StoppageCategory {
  category: string
  reasons: string[]
  color: string
  priority: number
  plannedDowntime: boolean
  description?: string
}

export interface OeeReport {
  reportId: string
  title: string
  type: 'summary' | 'detailed' | 'trends' | 'losses' | 'compliance'
  equipmentIds: string[]
  timeRange: { start: Date; end: Date }
  generatedAt: Date
  generatedBy: string
  
  // Report content
  summary?: {
    averageOee: number
    totalLosses: number
    topLossReasons: string[]
    improvementOpportunities: string[]
  }
  
  // CFR Part 11 compliance
  auditTrail: {
    accessedBy: string[]
    exported: boolean
    exportedAt?: Date
    signature?: string
  }
  
  // File information
  fileUrl?: string
  fileName?: string
  fileSize?: number
  format?: 'pdf' | 'excel' | 'csv'
}

// Event types for real-time updates
export interface StoppageEvent extends StoppageData {
  eventType: 'started' | 'ended' | 'updated' | 'acknowledged' | 'resolved'
}

export interface OeeUpdateEvent {
  equipmentId: string
  timestamp: Date
  oee: number
  availability: number
  performance: number
  quality: number
  workOrderId?: string
}

export interface WorkOrderEvent extends WorkOrderData {
  eventType: 'created' | 'started' | 'updated' | 'completed' | 'cancelled'
}

// Audit and compliance types
export interface OeeAuditTrail {
  id: string
  equipmentId: string
  action: 'view' | 'export' | 'modify' | 'acknowledge'
  dataType: 'oee-metrics' | 'work-order' | 'stoppage' | 'report'
  userId: string
  userRole: string
  timestamp: Date
  details: {
    reason: string
    purpose: string
    dataAccessed: string[]
  }
  ipAddress: string
  sessionId: string
}

export interface ComplianceFlag {
  type: 'CFR-21-COMPLIANT' | 'DATA-UNAVAILABLE' | 'SYNTHETIC-DATA-WARNING' | 'AUDIT-REQUIRED'
  message: string
  severity: 'info' | 'warning' | 'error'
  timestamp: Date
}