/**
 * Downtime_Record Entity (Canonical Model)
 * 
 * Represents a period of equipment downtime that affects OEE Availability.
 * This entity captures both planned and unplanned downtime events,
 * enabling proper calculation of the Availability factor in OEE.
 * 
 * Uses exact canonical model field names for future compatibility.
 */
export class Downtime_Record {
  // Canonical model fields
  readonly downtime_id: string
  readonly resource_reference: string
  readonly work_order_reference?: string
  readonly downtime_start: Date
  private downtime_end?: Date
  private duration_minutes: number
  
  // Classification fields
  private downtime_category: 'planned' | 'unplanned'
  private downtime_reason_code: string
  private downtime_reason_description: string
  
  // Additional context
  private operator_comments?: string
  private classified_by?: string
  private classified_at?: Date
  private status: 'active' | 'ended' | 'classified'
  
  // Tracking timestamps
  private readonly created_at: Date
  private updated_at: Date

  constructor(params: {
    downtime_id?: string
    resource_reference: string
    work_order_reference?: string
    downtime_start: Date
    downtime_end?: Date
    downtime_category?: 'planned' | 'unplanned'
    downtime_reason_code?: string
    downtime_reason_description?: string
    operator_comments?: string
    classified_by?: string
    classified_at?: Date
    status?: 'active' | 'ended' | 'classified'
  }) {
    // Validate required fields
    if (!params.resource_reference) {
      throw new Error('Resource reference is required')
    }
    if (params.downtime_end && params.downtime_end <= params.downtime_start) {
      throw new Error('Downtime end must be after downtime start')
    }

    this.downtime_id = params.downtime_id ?? `DT-${params.resource_reference}-${Date.now()}`
    this.resource_reference = params.resource_reference
    this.work_order_reference = params.work_order_reference
    this.downtime_start = params.downtime_start
    this.downtime_end = params.downtime_end
    
    // Calculate duration if end time is provided
    this.duration_minutes = this.calculateDuration()
    
    this.downtime_category = params.downtime_category ?? 'unplanned'
    this.downtime_reason_code = params.downtime_reason_code ?? 'UNCLASSIFIED'
    this.downtime_reason_description = params.downtime_reason_description ?? 'Unclassified downtime'
    
    this.operator_comments = params.operator_comments
    this.classified_by = params.classified_by
    this.classified_at = params.classified_at
    
    // Determine status based on provided data
    if (params.status) {
      this.status = params.status
    } else if (params.downtime_reason_code && params.downtime_reason_code !== 'UNCLASSIFIED') {
      this.status = 'classified'
    } else if (params.downtime_end) {
      this.status = 'ended'
    } else {
      this.status = 'active'
    }
    
    this.created_at = new Date()
    this.updated_at = new Date()
  }

  /**
   * Calculate duration in minutes
   */
  private calculateDuration(): number {
    if (!this.downtime_end) {
      // For active downtime, calculate from start to now
      const now = new Date()
      return Math.floor((now.getTime() - this.downtime_start.getTime()) / (1000 * 60))
    }
    return Math.floor((this.downtime_end.getTime() - this.downtime_start.getTime()) / (1000 * 60))
  }

  /**
   * End the downtime period
   */
  endDowntime(endTime?: Date): void {
    if (this.status === 'ended' || this.status === 'classified') {
      throw new Error('Downtime has already ended')
    }
    
    const end = endTime ?? new Date()
    if (end <= this.downtime_start) {
      throw new Error('End time must be after start time')
    }
    
    this.downtime_end = end
    this.duration_minutes = this.calculateDuration()
    this.status = this.status === 'active' ? 'ended' : this.status
    this.updated_at = new Date()
  }

  /**
   * Classify the downtime with a reason
   */
  classify(params: {
    category: 'planned' | 'unplanned'
    reason_code: string
    reason_description: string
    comments?: string
    classified_by: string
  }): void {
    this.downtime_category = params.category
    this.downtime_reason_code = params.reason_code
    this.downtime_reason_description = params.reason_description
    this.operator_comments = params.comments
    this.classified_by = params.classified_by
    this.classified_at = new Date()
    this.status = 'classified'
    this.updated_at = new Date()
  }

  /**
   * Get current duration in minutes
   */
  getDurationMinutes(): number {
    if (this.status === 'active') {
      // Recalculate for active downtime
      return this.calculateDuration()
    }
    return this.duration_minutes
  }

  /**
   * Get duration in hours
   */
  getDurationHours(): number {
    return this.getDurationMinutes() / 60
  }

  /**
   * Check if this is planned downtime
   */
  isPlanned(): boolean {
    return this.downtime_category === 'planned'
  }

  /**
   * Check if this is unplanned downtime
   */
  isUnplanned(): boolean {
    return this.downtime_category === 'unplanned'
  }

  /**
   * Check if downtime is currently active
   */
  isActive(): boolean {
    return this.status === 'active'
  }

  /**
   * Check if downtime has been classified
   */
  isClassified(): boolean {
    return this.status === 'classified' || 
           (this.downtime_reason_code !== 'UNCLASSIFIED' && this.classified_at !== undefined)
  }

  /**
   * Check if downtime requires classification
   */
  requiresClassification(): boolean {
    return !this.isClassified() && !this.isActive()
  }

  /**
   * Get impact on availability
   * @param plannedProductionMinutes Total planned production time
   */
  getAvailabilityImpact(plannedProductionMinutes: number): number {
    if (plannedProductionMinutes <= 0) {
      return 0
    }
    // Only unplanned downtime affects availability
    if (this.isUnplanned()) {
      return (this.getDurationMinutes() / plannedProductionMinutes) * 100
    }
    return 0
  }

  /**
   * Common downtime reason codes
   */
  static readonly REASON_CODES = {
    // Planned downtime
    PLANNED_MAINTENANCE: 'PM001',
    SCHEDULED_BREAK: 'PM002',
    CHANGEOVER: 'PM003',
    NO_PRODUCTION_SCHEDULED: 'PM004',
    
    // Unplanned downtime - Equipment
    EQUIPMENT_FAILURE: 'EQ001',
    MECHANICAL_BREAKDOWN: 'EQ002',
    ELECTRICAL_FAULT: 'EQ003',
    SENSOR_MALFUNCTION: 'EQ004',
    
    // Unplanned downtime - Material
    MATERIAL_SHORTAGE: 'MT001',
    MATERIAL_QUALITY_ISSUE: 'MT002',
    MATERIAL_CHANGEOVER: 'MT003',
    
    // Unplanned downtime - Operator
    NO_OPERATOR: 'OP001',
    OPERATOR_ERROR: 'OP002',
    TRAINING: 'OP003',
    
    // Unplanned downtime - Process
    QUALITY_ISSUE: 'PR001',
    PROCESS_ADJUSTMENT: 'PR002',
    WAITING_UPSTREAM: 'PR003',
    WAITING_DOWNSTREAM: 'PR004',
    
    // Other
    UNCLASSIFIED: 'UNCLASSIFIED'
  }

  /**
   * Get human-readable description for common reason codes
   */
  static getReasonDescription(code: string): string {
    const descriptions: Record<string, string> = {
      'PM001': 'Planned Maintenance',
      'PM002': 'Scheduled Break',
      'PM003': 'Product Changeover',
      'PM004': 'No Production Scheduled',
      'EQ001': 'Equipment Failure',
      'EQ002': 'Mechanical Breakdown',
      'EQ003': 'Electrical Fault',
      'EQ004': 'Sensor Malfunction',
      'MT001': 'Material Shortage',
      'MT002': 'Material Quality Issue',
      'MT003': 'Material Changeover',
      'OP001': 'No Operator Available',
      'OP002': 'Operator Error',
      'OP003': 'Operator Training',
      'PR001': 'Quality Issue',
      'PR002': 'Process Adjustment',
      'PR003': 'Waiting for Upstream Process',
      'PR004': 'Waiting for Downstream Process',
      'UNCLASSIFIED': 'Unclassified Downtime'
    }
    return descriptions[code] ?? 'Unknown Reason'
  }

  /**
   * Create from stoppage detection
   */
  static fromStoppageDetection(
    resource: string,
    startTime: Date,
    workOrderReference?: string
  ): Downtime_Record {
    return new Downtime_Record({
      resource_reference: resource,
      work_order_reference: workOrderReference,
      downtime_start: startTime,
      downtime_category: 'unplanned',
      downtime_reason_code: 'UNCLASSIFIED',
      downtime_reason_description: 'Automatic stoppage detection - pending classification',
      status: 'active'
    })
  }

  /**
   * Get downtime summary for reporting
   */
  toSummary(): {
    downtime_id: string
    resource: string
    work_order?: string
    start_time: string
    end_time?: string
    duration_minutes: number
    category: string
    reason_code: string
    reason: string
    status: string
    requires_classification: boolean
    availability_impact?: number
  } {
    return {
      downtime_id: this.downtime_id,
      resource: this.resource_reference,
      work_order: this.work_order_reference,
      start_time: this.downtime_start.toISOString(),
      end_time: this.downtime_end?.toISOString(),
      duration_minutes: this.getDurationMinutes(),
      category: this.downtime_category,
      reason_code: this.downtime_reason_code,
      reason: this.downtime_reason_description,
      status: this.status,
      requires_classification: this.requiresClassification(),
      availability_impact: undefined // Set by caller with planned production time
    }
  }

  /**
   * Entity equality check
   */
  equals(other: Downtime_Record): boolean {
    return this.downtime_id === other.downtime_id
  }

  toString(): string {
    const duration = this.getDurationMinutes()
    const status = this.isActive() ? 'Active' : 'Ended'
    return `Downtime ${this.downtime_id}: ${this.downtime_reason_description} (${duration} min, ${status}, ${this.downtime_category})`
  }
}