import { oeeConfig } from '../../../config/oee.config'

/**
 * Work_Order Entity (Canonical Model)
 * 
 * Represents a production work order that provides business context 
 * to overlay on immutable counter data. This entity bridges the gap between
 * raw counter readings and meaningful production metrics.
 * 
 * Uses exact canonical model field names for future compatibility.
 */
export class Work_Order {
  // Canonical model fields
  readonly work_order_id: string
  readonly work_order_description: string
  readonly product_id: string
  readonly product_description: string
  readonly planned_quantity: number
  readonly unit_of_measure: string
  readonly scheduled_start_time: Date
  readonly scheduled_end_time: Date
  
  // Production tracking fields
  private actual_quantity_good: number
  private actual_quantity_scrap: number
  private actual_start_time?: Date
  private actual_end_time?: Date
  private status: 'pending' | 'active' | 'paused' | 'completed' | 'cancelled'
  
  // Resource assignment
  readonly resource_reference: string
  
  // Tracking timestamps
  private readonly created_at: Date
  private updated_at: Date

  constructor(params: {
    work_order_id: string
    work_order_description: string
    product_id: string
    product_description: string
    planned_quantity: number
    unit_of_measure?: string
    scheduled_start_time: Date
    scheduled_end_time: Date
    resource_reference: string
    actual_quantity_good?: number
    actual_quantity_scrap?: number
    actual_start_time?: Date
    actual_end_time?: Date
    status?: 'pending' | 'active' | 'paused' | 'completed' | 'cancelled'
  }) {
    // Validate required fields
    if (!params.work_order_id) {
      throw new Error('Work order ID is required')
    }
    if (params.planned_quantity <= 0) {
      throw new Error('Planned quantity must be positive')
    }
    if (params.scheduled_end_time <= params.scheduled_start_time) {
      throw new Error('Scheduled end time must be after start time')
    }

    this.work_order_id = params.work_order_id
    this.work_order_description = params.work_order_description
    this.product_id = params.product_id
    this.product_description = params.product_description
    this.planned_quantity = params.planned_quantity
    this.unit_of_measure = params.unit_of_measure ?? 'pieces'
    this.scheduled_start_time = params.scheduled_start_time
    this.scheduled_end_time = params.scheduled_end_time
    this.resource_reference = params.resource_reference
    
    this.actual_quantity_good = params.actual_quantity_good ?? 0
    this.actual_quantity_scrap = params.actual_quantity_scrap ?? 0
    this.actual_start_time = params.actual_start_time
    this.actual_end_time = params.actual_end_time
    this.status = params.status ?? 'pending'
    
    this.created_at = new Date()
    this.updated_at = new Date()
  }

  /**
   * Update production quantities from counter channel data
   * This is the bridge between immutable counter data and business context
   * 
   * @param goodCount Count from channel 0 (production/good pieces)
   * @param scrapCount Count from channel 1 (rejects/defects)
   */
  updateFromCounterData(goodCount: number, scrapCount: number): void {
    if (goodCount < 0 || scrapCount < 0) {
      throw new Error('Counter values cannot be negative')
    }
    
    this.actual_quantity_good = goodCount
    this.actual_quantity_scrap = scrapCount
    this.updated_at = new Date()
  }

  /**
   * Start the work order
   */
  start(): void {
    if (this.status !== 'pending') {
      throw new Error(`Cannot start work order with status: ${this.status}`)
    }
    
    this.status = 'active'
    this.actual_start_time = new Date()
    this.updated_at = new Date()
  }

  /**
   * Pause the work order (e.g., for breaks, maintenance)
   */
  pause(): void {
    if (this.status !== 'active') {
      throw new Error(`Cannot pause work order with status: ${this.status}`)
    }
    
    this.status = 'paused'
    this.updated_at = new Date()
  }

  /**
   * Resume a paused work order
   */
  resume(): void {
    if (this.status !== 'paused') {
      throw new Error(`Cannot resume work order with status: ${this.status}`)
    }
    
    this.status = 'active'
    this.updated_at = new Date()
  }

  /**
   * Complete the work order
   */
  complete(): void {
    if (this.status !== 'active' && this.status !== 'paused') {
      throw new Error(`Cannot complete work order with status: ${this.status}`)
    }
    
    this.status = 'completed'
    this.actual_end_time = new Date()
    this.updated_at = new Date()
  }

  /**
   * Cancel the work order
   */
  cancel(reason?: string): void {
    if (this.status === 'completed' || this.status === 'cancelled') {
      throw new Error(`Cannot cancel work order with status: ${this.status}`)
    }
    
    this.status = 'cancelled'
    if (!this.actual_end_time) {
      this.actual_end_time = new Date()
    }
    this.updated_at = new Date()
  }

  /**
   * Get total quantity produced (good + scrap)
   */
  getTotalQuantityProduced(): number {
    return this.actual_quantity_good + this.actual_quantity_scrap
  }

  /**
   * Calculate completion percentage based on planned quantity
   */
  getCompletionPercentage(): number {
    if (this.planned_quantity === 0) {
      return 0
    }
    return (this.getTotalQuantityProduced() / this.planned_quantity) * 100
  }

  /**
   * Calculate yield/quality percentage
   */
  getYieldPercentage(): number {
    const total = this.getTotalQuantityProduced()
    if (total === 0) {
      return 100 // No production = no defects
    }
    return (this.actual_quantity_good / total) * 100
  }

  /**
   * Check if work order is behind schedule
   */
  isBehindSchedule(): boolean {
    const now = new Date()
    const scheduledDuration = this.scheduled_end_time.getTime() - this.scheduled_start_time.getTime()
    const elapsedTime = now.getTime() - this.scheduled_start_time.getTime()
    const expectedProgress = (elapsedTime / scheduledDuration) * 100
    
    return this.getCompletionPercentage() < expectedProgress
  }

  /**
   * Get production rate (pieces per minute)
   */
  getProductionRate(): number {
    if (!this.actual_start_time || this.status === 'pending') {
      return 0
    }
    
    const endTime = this.actual_end_time ?? new Date()
    const durationMinutes = (endTime.getTime() - this.actual_start_time.getTime()) / (1000 * 60)
    
    if (durationMinutes === 0) {
      return 0
    }
    
    return this.getTotalQuantityProduced() / durationMinutes
  }

  /**
   * Calculate estimated completion time based on current rate
   */
  getEstimatedCompletionTime(): Date | null {
    const rate = this.getProductionRate()
    if (rate === 0) {
      return null
    }
    
    const remainingQuantity = this.planned_quantity - this.getTotalQuantityProduced()
    if (remainingQuantity <= 0) {
      return new Date()
    }
    
    const remainingMinutes = remainingQuantity / rate
    const estimatedTime = new Date()
    estimatedTime.setMinutes(estimatedTime.getMinutes() + remainingMinutes)
    
    return estimatedTime
  }

  /**
   * Check if work order requires attention
   */
  requiresAttention(): boolean {
    // Check various conditions that might need operator attention
    return (
      this.isBehindSchedule() ||
      this.getYieldPercentage() < oeeConfig.alerts.requiresAttention.quality ||
      (this.status === 'active' && this.getProductionRate() === 0)
    )
  }

  /**
   * Get work order summary for reporting
   */
  toSummary(): {
    work_order_id: string
    product: string
    status: string
    progress: number
    yield: number
    scheduled_start: string
    scheduled_end: string
    actual_start?: string
    actual_end?: string
    quantities: {
      planned: number
      good: number
      scrap: number
      total: number
    }
    performance: {
      completion_percentage: number
      yield_percentage: number
      production_rate: number
      is_behind_schedule: boolean
      requires_attention: boolean
    }
  } {
    return {
      work_order_id: this.work_order_id,
      product: this.product_description,
      status: this.status,
      progress: this.getCompletionPercentage(),
      yield: this.getYieldPercentage(),
      scheduled_start: this.scheduled_start_time.toISOString(),
      scheduled_end: this.scheduled_end_time.toISOString(),
      actual_start: this.actual_start_time?.toISOString(),
      actual_end: this.actual_end_time?.toISOString(),
      quantities: {
        planned: this.planned_quantity,
        good: this.actual_quantity_good,
        scrap: this.actual_quantity_scrap,
        total: this.getTotalQuantityProduced()
      },
      performance: {
        completion_percentage: this.getCompletionPercentage(),
        yield_percentage: this.getYieldPercentage(),
        production_rate: this.getProductionRate(),
        is_behind_schedule: this.isBehindSchedule(),
        requires_attention: this.requiresAttention()
      }
    }
  }

  /**
   * Create a work order from counter snapshot
   */
  static fromCounterSnapshot(
    workOrderData: {
      work_order_id: string
      work_order_description: string
      product_id: string
      product_description: string
      planned_quantity: number
      scheduled_start_time: Date
      scheduled_end_time: Date
      resource_reference: string
    },
    counterSnapshot: {
      channel_0_count: number  // Good pieces
      channel_1_count: number  // Scrap/rejects
    }
  ): Work_Order {
    return new Work_Order({
      ...workOrderData,
      actual_quantity_good: counterSnapshot.channel_0_count,
      actual_quantity_scrap: counterSnapshot.channel_1_count,
      status: 'active',
      actual_start_time: new Date()
    })
  }

  /**
   * Entity equality check
   */
  equals(other: Work_Order): boolean {
    return this.work_order_id === other.work_order_id
  }

  /**
   * Get current status
   */
  getStatus(): string {
    return this.status
  }

  /**
   * Get actual quantities
   */
  getActualQuantities(): { good: number; scrap: number } {
    return {
      good: this.actual_quantity_good,
      scrap: this.actual_quantity_scrap
    }
  }

  toString(): string {
    return `Work Order ${this.work_order_id}: ${this.product_description} (${this.getCompletionPercentage().toFixed(1)}% complete, ${this.getYieldPercentage().toFixed(1)}% yield)`
  }
}