/**
 * Availability Domain Model Tests
 * 
 * Tests for availability calculation logic and business rules.
 */

import { describe, it, expect } from 'vitest'
import { Availability } from '@/lib/domain/models/Availability'
import { downtimeRecords } from '../../fixtures/oee-fixtures'

describe('Availability', () => {
  describe('Construction', () => {
    it('should create availability with valid inputs', () => {
      const availability = new Availability(480, 432, 48)

      expect(availability.percentage).toBeCloseTo(90, 1)
      expect(availability.decimal).toBeCloseTo(0.9, 3)
    })

    it('should calculate downtime automatically when not provided', () => {
      const availability = new Availability(480, 432)

      expect(availability.getDowntimeImpact()).toBe(48) // 480 - 432
      expect(availability.percentage).toBeCloseTo(90, 1)
    })

    it('should throw error for negative planned time', () => {
      expect(() => new Availability(-100, 90)).toThrow('Planned production time cannot be negative')
    })

    it('should throw error for negative actual time', () => {
      expect(() => new Availability(480, -50)).toThrow('Actual run time cannot be negative')
    })

    it('should throw error when actual exceeds planned time', () => {
      expect(() => new Availability(480, 500)).toThrow('Actual run time cannot exceed planned production time')
    })
  })

  describe('Percentage Calculations', () => {
    it('should calculate 100% availability for no downtime', () => {
      const availability = new Availability(480, 480, 0)

      expect(availability.percentage).toBe(100)
      expect(availability.decimal).toBe(1)
    })

    it('should calculate 0% availability for complete downtime', () => {
      const availability = new Availability(480, 0, 480)

      expect(availability.percentage).toBe(0)
      expect(availability.decimal).toBe(0)
    })

    it('should handle zero planned time gracefully', () => {
      const availability = new Availability(0, 0, 0)

      expect(availability.percentage).toBe(0)
      expect(availability.decimal).toBe(0)
    })

    it('should calculate typical shift availability correctly', () => {
      // 8-hour shift with 1-hour downtime
      const availability = new Availability(480, 420, 60)

      expect(availability.percentage).toBeCloseTo(87.5, 1)
      expect(availability.decimal).toBeCloseTo(0.875, 3)
    })
  })

  describe('Target Comparison', () => {
    it('should correctly identify when target is met', () => {
      const availability = new Availability(480, 432) // 90%

      expect(availability.meetsTarget(85)).toBe(true)
      expect(availability.meetsTarget(90)).toBe(true)
      expect(availability.meetsTarget(95)).toBe(false)
    })

    it('should handle edge case of exact target match', () => {
      const availability = new Availability(100, 85) // 85%

      expect(availability.meetsTarget(85)).toBe(true)
      expect(availability.meetsTarget(85.1)).toBe(false)
    })
  })

  describe('Downtime Analysis', () => {
    it('should correctly report downtime impact', () => {
      const availability = new Availability(480, 420, 60)

      expect(availability.getDowntimeImpact()).toBe(60)
    })

    it('should provide detailed breakdown', () => {
      const availability = new Availability(480, 408, 72)
      const breakdown = availability.getBreakdown()

      expect(breakdown.planned).toBe(480)
      expect(breakdown.actual).toBe(408)
      expect(breakdown.downtime).toBe(72)
      expect(breakdown.utilizationRate).toBeCloseTo(85, 1)
    })
  })

  describe('Constraining Factor Analysis', () => {
    it('should identify as constraining factor when lowest', () => {
      const availability = new Availability(480, 360) // 75%

      // Availability is constraining when it's lower than performance and quality
      expect(availability.isConstrainingFactor(85, 90)).toBe(true)
      expect(availability.isConstrainingFactor(70, 90)).toBe(false) // Performance is lower
      expect(availability.isConstrainingFactor(85, 70)).toBe(false) // Quality is lower
    })

    it('should not be constraining when equal to other factors', () => {
      const availability = new Availability(480, 360) // 75%

      expect(availability.isConstrainingFactor(75, 90)).toBe(false)
      expect(availability.isConstrainingFactor(85, 75)).toBe(false)
    })
  })

  describe('Factory Methods', () => {
    it('should create from downtime records correctly', () => {
      const plannedMinutes = 480
      const records = downtimeRecords.typical

      const availability = Availability.fromDowntimeRecords(plannedMinutes, records)

      const totalDowntime = records.reduce((sum, record) => sum + record.duration_minutes, 0)
      const expectedActual = plannedMinutes - totalDowntime
      const expectedPercentage = (expectedActual / plannedMinutes) * 100

      expect(availability.percentage).toBeCloseTo(expectedPercentage, 1)
      expect(availability.getDowntimeImpact()).toBe(totalDowntime)
    })

    it('should handle heavy downtime scenario', () => {
      const plannedMinutes = 480
      const records = downtimeRecords.heavyDowntime

      const availability = Availability.fromDowntimeRecords(plannedMinutes, records)

      const totalDowntime = records.reduce((sum, record) => sum + record.duration_minutes, 0)
      expect(totalDowntime).toBe(140) // 15 + 30 + 60 + 20 + 15
      expect(availability.percentage).toBeCloseTo(70.83, 1) // (480-140)/480 * 100
    })

    it('should handle empty downtime records', () => {
      const availability = Availability.fromDowntimeRecords(480, [])

      expect(availability.percentage).toBe(100)
      expect(availability.getDowntimeImpact()).toBe(0)
    })
  })

  describe('Equality', () => {
    it('should return true for equal availability objects', () => {
      const availability1 = new Availability(480, 432, 48)
      const availability2 = new Availability(480, 432, 48)

      expect(availability1.equals(availability2)).toBe(true)
    })

    it('should return false for different availability objects', () => {
      const availability1 = new Availability(480, 432, 48)
      const availability2 = new Availability(480, 420, 60)

      expect(availability1.equals(availability2)).toBe(false)
    })

    it('should return false when downtime differs', () => {
      const availability1 = new Availability(480, 432, 48)
      const availability2 = new Availability(480, 432, 50) // Different downtime

      expect(availability1.equals(availability2)).toBe(false)
    })
  })

  describe('String Representation', () => {
    it('should format toString correctly', () => {
      const availability = new Availability(480, 432, 48)
      const str = availability.toString()

      expect(str).toContain('Availability: 90.0%')
      expect(str).toContain('(432/480 min)')
    })

    it('should handle zero values in toString', () => {
      const availability = new Availability(480, 0, 480)
      const str = availability.toString()

      expect(str).toContain('Availability: 0.0%')
      expect(str).toContain('(0/480 min)')
    })
  })

  describe('Edge Cases', () => {
    it('should handle very small time periods', () => {
      const availability = new Availability(1, 0.9, 0.1)

      expect(availability.percentage).toBeCloseTo(90, 1)
      expect(availability.decimal).toBeCloseTo(0.9, 3)
    })

    it('should handle very large time periods', () => {
      const weekMinutes = 7 * 24 * 60 // One week
      const availability = new Availability(weekMinutes, weekMinutes * 0.85)

      expect(availability.percentage).toBeCloseTo(85, 1)
      expect(availability.getDowntimeImpact()).toBeCloseTo(weekMinutes * 0.15, 1)
    })

    it('should maintain precision for high-precision calculations', () => {
      const availability = new Availability(10000, 9999, 1)

      expect(availability.percentage).toBeCloseTo(99.99, 2)
      expect(availability.decimal).toBeCloseTo(0.9999, 4)
    })
  })
})