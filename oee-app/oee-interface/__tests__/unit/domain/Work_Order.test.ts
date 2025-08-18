/**
 * Work_Order Domain Model Tests
 * 
 * Tests for work order entity and production tracking logic.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { Work_Order } from '@/lib/domain/models/Work_Order'

describe('Work_Order', () => {
  let baseWorkOrderParams: any
  let mockCurrentTime: Date

  beforeEach(() => {
    mockCurrentTime = new Date('2024-01-15T08:00:00Z')
    vi.setSystemTime(mockCurrentTime)

    baseWorkOrderParams = {
      work_order_id: 'WO-12345',
      work_order_description: 'Production Run - Widget A',
      product_id: 'WIDGET-A-001',
      product_description: 'Standard Widget A',
      planned_quantity: 1000,
      scheduled_start_time: new Date('2024-01-15T06:00:00Z'),
      scheduled_end_time: new Date('2024-01-15T14:00:00Z'),
      resource_reference: 'PRESS-001'
    }
  })

  describe('Construction', () => {
    it('should create work order with valid inputs', () => {
      const workOrder = new Work_Order(baseWorkOrderParams)

      expect(workOrder.work_order_id).toBe('WO-12345')
      expect(workOrder.work_order_description).toBe('Production Run - Widget A')
      expect(workOrder.product_id).toBe('WIDGET-A-001')
      expect(workOrder.product_description).toBe('Standard Widget A')
      expect(workOrder.planned_quantity).toBe(1000)
      expect(workOrder.unit_of_measure).toBe('pieces') // Default value
      expect(workOrder.resource_reference).toBe('PRESS-001')
      expect(workOrder.getStatus()).toBe('pending')
    })

    it('should accept custom unit of measure', () => {
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        unit_of_measure: 'kilograms'
      })

      expect(workOrder.unit_of_measure).toBe('kilograms')
    })

    it('should initialize with provided actual quantities', () => {
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        actual_quantity_good: 500,
        actual_quantity_scrap: 50,
        status: 'active'
      })

      const quantities = workOrder.getActualQuantities()
      expect(quantities.good).toBe(500)
      expect(quantities.scrap).toBe(50)
      expect(workOrder.getStatus()).toBe('active')
    })

    it('should throw error for missing work order ID', () => {
      expect(() => new Work_Order({
        ...baseWorkOrderParams,
        work_order_id: ''
      })).toThrow('Work order ID is required')
    })

    it('should throw error for non-positive planned quantity', () => {
      expect(() => new Work_Order({
        ...baseWorkOrderParams,
        planned_quantity: 0
      })).toThrow('Planned quantity must be positive')

      expect(() => new Work_Order({
        ...baseWorkOrderParams,
        planned_quantity: -100
      })).toThrow('Planned quantity must be positive')
    })

    it('should throw error when end time is before start time', () => {
      expect(() => new Work_Order({
        ...baseWorkOrderParams,
        scheduled_end_time: new Date('2024-01-15T05:00:00Z') // Before start time
      })).toThrow('Scheduled end time must be after start time')
    })
  })

  describe('Status Management', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order(baseWorkOrderParams)
    })

    it('should start work order successfully', () => {
      workOrder.start()

      expect(workOrder.getStatus()).toBe('active')
      expect(workOrder.toSummary().actual_start).toBeDefined()
    })

    it('should not start non-pending work order', () => {
      workOrder.start()
      
      expect(() => workOrder.start()).toThrow('Cannot start work order with status: active')
    })

    it('should pause active work order', () => {
      workOrder.start()
      workOrder.pause()

      expect(workOrder.getStatus()).toBe('paused')
    })

    it('should not pause non-active work order', () => {
      expect(() => workOrder.pause()).toThrow('Cannot pause work order with status: pending')
    })

    it('should resume paused work order', () => {
      workOrder.start()
      workOrder.pause()
      workOrder.resume()

      expect(workOrder.getStatus()).toBe('active')
    })

    it('should not resume non-paused work order', () => {
      expect(() => workOrder.resume()).toThrow('Cannot resume work order with status: pending')
    })

    it('should complete active work order', () => {
      workOrder.start()
      workOrder.complete()

      expect(workOrder.getStatus()).toBe('completed')
      expect(workOrder.toSummary().actual_end).toBeDefined()
    })

    it('should complete paused work order', () => {
      workOrder.start()
      workOrder.pause()
      workOrder.complete()

      expect(workOrder.getStatus()).toBe('completed')
    })

    it('should not complete pending work order', () => {
      expect(() => workOrder.complete()).toThrow('Cannot complete work order with status: pending')
    })

    it('should cancel work order in various states', () => {
      // Cancel pending
      const pendingOrder = new Work_Order(baseWorkOrderParams)
      pendingOrder.cancel('Customer request')
      expect(pendingOrder.getStatus()).toBe('cancelled')

      // Cancel active
      const activeOrder = new Work_Order(baseWorkOrderParams)
      activeOrder.start()
      activeOrder.cancel('Equipment failure')
      expect(activeOrder.getStatus()).toBe('cancelled')

      // Cancel paused
      const pausedOrder = new Work_Order(baseWorkOrderParams)
      pausedOrder.start()
      pausedOrder.pause()
      pausedOrder.cancel('Material shortage')
      expect(pausedOrder.getStatus()).toBe('cancelled')
    })

    it('should not cancel completed or cancelled work order', () => {
      workOrder.start()
      workOrder.complete()
      
      expect(() => workOrder.cancel()).toThrow('Cannot cancel work order with status: completed')

      const cancelledOrder = new Work_Order(baseWorkOrderParams)
      cancelledOrder.cancel()
      
      expect(() => cancelledOrder.cancel()).toThrow('Cannot cancel work order with status: cancelled')
    })
  })

  describe('Counter Data Updates', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order(baseWorkOrderParams)
    })

    it('should update quantities from counter data', () => {
      workOrder.updateFromCounterData(850, 150)

      const quantities = workOrder.getActualQuantities()
      expect(quantities.good).toBe(850)
      expect(quantities.scrap).toBe(150)
      expect(workOrder.getTotalQuantityProduced()).toBe(1000)
    })

    it('should throw error for negative counter values', () => {
      expect(() => workOrder.updateFromCounterData(-50, 100)).toThrow('Counter values cannot be negative')
      expect(() => workOrder.updateFromCounterData(100, -50)).toThrow('Counter values cannot be negative')
    })

    it('should handle zero values correctly', () => {
      workOrder.updateFromCounterData(0, 0)

      const quantities = workOrder.getActualQuantities()
      expect(quantities.good).toBe(0)
      expect(quantities.scrap).toBe(0)
      expect(workOrder.getTotalQuantityProduced()).toBe(0)
    })
  })

  describe('Production Calculations', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order(baseWorkOrderParams)
      workOrder.updateFromCounterData(850, 150) // 1000 total pieces
    })

    it('should calculate completion percentage correctly', () => {
      const completion = workOrder.getCompletionPercentage()
      expect(completion).toBe(100) // 1000/1000 * 100
    })

    it('should calculate yield percentage correctly', () => {
      const yield_ = workOrder.getYieldPercentage()
      expect(yield_).toBe(85) // 850/1000 * 100
    })

    it('should handle zero production for yield calculation', () => {
      workOrder.updateFromCounterData(0, 0)
      expect(workOrder.getYieldPercentage()).toBe(100) // No production = no defects
    })

    it('should calculate production rate when active', () => {
      workOrder.start()
      
      // Mock time passing (2 hours = 120 minutes)
      vi.setSystemTime(new Date('2024-01-15T10:00:00Z'))
      
      const rate = workOrder.getProductionRate()
      expect(rate).toBeCloseTo(8.33, 1) // 1000 pieces / 120 minutes
    })

    it('should return zero production rate for pending work order', () => {
      const rate = workOrder.getProductionRate()
      expect(rate).toBe(0)
    })

    it('should calculate estimated completion time', () => {
      // Set up partial completion
      workOrder.updateFromCounterData(500, 50) // 550 total, 450 remaining
      workOrder.start()
      
      // Mock time passing (1 hour = 60 minutes)
      vi.setSystemTime(new Date('2024-01-15T09:00:00Z'))
      
      const estimatedTime = workOrder.getEstimatedCompletionTime()
      expect(estimatedTime).toBeInstanceOf(Date)
      
      // Should be a future time since there's remaining production
      expect(estimatedTime?.getTime()).toBeGreaterThan(mockCurrentTime.getTime())
    })

    it('should return null for estimated completion when no production rate', () => {
      workOrder.updateFromCounterData(0, 0)
      const estimatedTime = workOrder.getEstimatedCompletionTime()
      expect(estimatedTime).toBeNull()
    })

    it('should return current time for estimated completion when already complete', () => {
      // Over-production scenario - need to start first and wait a bit
      workOrder.start()
      
      // Mock some time passing to establish a production rate
      vi.setSystemTime(new Date('2024-01-15T08:01:00Z')) // 1 minute later
      workOrder.updateFromCounterData(1100, 100) // More than planned
      
      const estimatedTime = workOrder.getEstimatedCompletionTime()
      expect(estimatedTime).toBeInstanceOf(Date)
      // Should return approximately current time when complete
      expect(estimatedTime?.getTime()).toBeLessThanOrEqual(Date.now() + 1000)
    })
  })

  describe('Schedule Analysis', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order(baseWorkOrderParams)
    })

    it('should detect when behind schedule', () => {
      // At 8:00 AM, we're 2 hours into an 8-hour shift (25% elapsed)
      // With only 20% completion, we're behind schedule
      workOrder.updateFromCounterData(200, 0) // 20% completion
      
      expect(workOrder.isBehindSchedule()).toBe(true)
    })

    it('should detect when on or ahead of schedule', () => {
      // At 8:00 AM, we're 2 hours into an 8-hour shift (25% elapsed)
      // With 30% completion, we're ahead of schedule
      workOrder.updateFromCounterData(300, 0) // 30% completion
      
      expect(workOrder.isBehindSchedule()).toBe(false)
    })

    it('should handle time before scheduled start', () => {
      // Mock time before scheduled start
      vi.setSystemTime(new Date('2024-01-15T05:00:00Z'))
      
      workOrder.updateFromCounterData(0, 0)
      expect(workOrder.isBehindSchedule()).toBe(false)
    })
  })

  describe('Attention Detection', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order(baseWorkOrderParams)
    })

    it('should require attention when behind schedule', () => {
      workOrder.updateFromCounterData(200, 50) // Behind schedule
      expect(workOrder.requiresAttention()).toBe(true)
    })

    it('should require attention for low yield', () => {
      workOrder.updateFromCounterData(500, 500) // 50% yield (< 95%)
      expect(workOrder.requiresAttention()).toBe(true)
    })

    it('should require attention when active but no production', () => {
      workOrder.start()
      workOrder.updateFromCounterData(0, 0) // No production while active
      expect(workOrder.requiresAttention()).toBe(true)
    })

    it('should not require attention for good performance', () => {
      workOrder.updateFromCounterData(250, 10) // Good yield and on schedule
      expect(workOrder.requiresAttention()).toBe(false)
    })
  })

  describe('Summary Generation', () => {
    let workOrder: Work_Order

    beforeEach(() => {
      workOrder = new Work_Order({
        ...baseWorkOrderParams,
        actual_quantity_good: 850,
        actual_quantity_scrap: 150,
        status: 'active',
        actual_start_time: new Date('2024-01-15T06:30:00Z')
      })
    })

    it('should generate comprehensive summary', () => {
      const summary = workOrder.toSummary()

      expect(summary.work_order_id).toBe('WO-12345')
      expect(summary.product).toBe('Standard Widget A')
      expect(summary.status).toBe('active')
      expect(summary.progress).toBe(100)
      expect(summary.yield).toBe(85)
      expect(summary.scheduled_start).toBe('2024-01-15T06:00:00.000Z')
      expect(summary.scheduled_end).toBe('2024-01-15T14:00:00.000Z')
      expect(summary.actual_start).toBe('2024-01-15T06:30:00.000Z')
      expect(summary.actual_end).toBeUndefined()

      expect(summary.quantities.planned).toBe(1000)
      expect(summary.quantities.good).toBe(850)
      expect(summary.quantities.scrap).toBe(150)
      expect(summary.quantities.total).toBe(1000)

      expect(summary.performance.completion_percentage).toBe(100)
      expect(summary.performance.yield_percentage).toBe(85)
      expect(summary.performance.production_rate).toBeGreaterThan(0)
      expect(summary.performance.is_behind_schedule).toBeDefined()
      expect(summary.performance.requires_attention).toBeDefined()
    })
  })

  describe('Factory Methods', () => {
    it('should create from counter snapshot correctly', () => {
      const workOrderData = {
        work_order_id: 'WO-SNAPSHOT-001',
        work_order_description: 'Snapshot Production Run',
        product_id: 'PROD-001',
        product_description: 'Product from Snapshot',
        planned_quantity: 2000,
        scheduled_start_time: new Date('2024-01-15T06:00:00Z'),
        scheduled_end_time: new Date('2024-01-15T14:00:00Z'),
        resource_reference: 'MACHINE-002'
      }

      const counterSnapshot = {
        channel_0_count: 1800,
        channel_1_count: 200
      }

      const workOrder = Work_Order.fromCounterSnapshot(workOrderData, counterSnapshot)

      expect(workOrder.work_order_id).toBe('WO-SNAPSHOT-001')
      expect(workOrder.getStatus()).toBe('active')
      expect(workOrder.getActualQuantities().good).toBe(1800)
      expect(workOrder.getActualQuantities().scrap).toBe(200)
      expect(workOrder.toSummary().actual_start).toBeDefined()
    })
  })

  describe('Equality', () => {
    it('should return true for work orders with same ID', () => {
      const workOrder1 = new Work_Order(baseWorkOrderParams)
      const workOrder2 = new Work_Order({
        ...baseWorkOrderParams,
        work_order_description: 'Different Description' // Different data but same ID
      })

      expect(workOrder1.equals(workOrder2)).toBe(true)
    })

    it('should return false for work orders with different IDs', () => {
      const workOrder1 = new Work_Order(baseWorkOrderParams)
      const workOrder2 = new Work_Order({
        ...baseWorkOrderParams,
        work_order_id: 'WO-DIFFERENT'
      })

      expect(workOrder1.equals(workOrder2)).toBe(false)
    })
  })

  describe('String Representation', () => {
    it('should format toString correctly', () => {
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        actual_quantity_good: 850,
        actual_quantity_scrap: 150
      })

      const str = workOrder.toString()

      expect(str).toContain('Work Order WO-12345')
      expect(str).toContain('Standard Widget A')
      expect(str).toContain('100.0% complete')
      expect(str).toContain('85.0% yield')
    })

    it('should handle zero completion in toString', () => {
      const workOrder = new Work_Order(baseWorkOrderParams)

      const str = workOrder.toString()

      expect(str).toContain('0.0% complete')
      expect(str).toContain('100.0% yield') // No production = no defects
    })
  })

  describe('Edge Cases', () => {
    it('should handle very small quantities', () => {
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        planned_quantity: 1
      })

      workOrder.updateFromCounterData(1, 0)
      expect(workOrder.getCompletionPercentage()).toBe(100)
      expect(workOrder.getYieldPercentage()).toBe(100)
    })

    it('should handle very large quantities', () => {
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        planned_quantity: 1000000
      })

      workOrder.updateFromCounterData(950000, 50000)
      expect(workOrder.getCompletionPercentage()).toBe(100)
      expect(workOrder.getYieldPercentage()).toBe(95)
    })

    it('should handle over-production scenarios', () => {
      const workOrder = new Work_Order(baseWorkOrderParams)
      workOrder.updateFromCounterData(1100, 100) // 1200 total vs 1000 planned

      expect(workOrder.getCompletionPercentage()).toBe(120) // Can exceed 100%
      expect(workOrder.getYieldPercentage()).toBeCloseTo(91.67, 1) // 1100/1200
    })

    it('should handle zero planned quantity gracefully', () => {
      // This shouldn't happen due to validation, but test defensive coding
      const workOrder = new Work_Order({
        ...baseWorkOrderParams,
        planned_quantity: 1 // Minimum allowed
      })

      // Modify internally for test (bypassing validation)
      ;(workOrder as any).planned_quantity = 0

      expect(workOrder.getCompletionPercentage()).toBe(0)
    })

    it('should handle instantaneous production rate calculation', () => {
      const workOrder = new Work_Order(baseWorkOrderParams)
      workOrder.start()
      workOrder.updateFromCounterData(100, 10)

      // Immediately after starting (same millisecond)
      const rate = workOrder.getProductionRate()
      expect(rate).toBeGreaterThanOrEqual(0) // Should not throw or return NaN
    })
  })
})