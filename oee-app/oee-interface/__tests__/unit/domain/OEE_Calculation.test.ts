/**
 * OEE_Calculation Domain Model Tests
 * 
 * Comprehensive tests for the OEE calculation domain logic,
 * ensuring all business rules and edge cases are covered.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { OEE_Calculation } from '@/lib/domain/models/OEE_Calculation'
import { Availability } from '@/lib/domain/models/Availability'
import { Performance } from '@/lib/domain/models/Performance'
import { Quality } from '@/lib/domain/models/Quality'
import { 
  createOEECalculation, 
  createAvailability, 
  createPerformance, 
  createQuality,
  expectedResults,
  timeScenarios
} from '../../fixtures/oee-fixtures'

describe('OEE_Calculation', () => {
  let startTime: Date
  let endTime: Date

  beforeEach(() => {
    startTime = new Date('2024-01-15T06:00:00Z')
    endTime = new Date('2024-01-15T14:00:00Z')
  })

  describe('Construction', () => {
    it('should create OEE calculation with valid inputs', () => {
      const availability = createAvailability('excellent')
      const performance = createPerformance('excellent')
      const quality = createQuality('excellent')

      const oee = new OEE_Calculation({
        resource_reference: 'PRESS-001',
        calculation_period_start: startTime,
        calculation_period_end: endTime,
        availability,
        performance,
        quality
      })

      expect(oee.resource_reference).toBe('PRESS-001')
      expect(oee.calculation_period_start).toEqual(startTime)
      expect(oee.calculation_period_end).toEqual(endTime)
      expect(oee.oee_id).toMatch(/^OEE-PRESS-001-\d+$/)
    })

    it('should accept custom OEE ID', () => {
      const availability = createAvailability('excellent')
      const performance = createPerformance('excellent')
      const quality = createQuality('excellent')

      const oee = new OEE_Calculation({
        oee_id: 'CUSTOM-OEE-123',
        resource_reference: 'PRESS-001',
        calculation_period_start: startTime,
        calculation_period_end: endTime,
        availability,
        performance,
        quality
      })

      expect(oee.oee_id).toBe('CUSTOM-OEE-123')
    })

    it('should throw error when end time is before start time', () => {
      const availability = createAvailability('excellent')
      const performance = createPerformance('excellent')
      const quality = createQuality('excellent')

      expect(() => new OEE_Calculation({
        resource_reference: 'PRESS-001',
        calculation_period_start: endTime,
        calculation_period_end: startTime, // Invalid: end before start
        availability,
        performance,
        quality
      })).toThrow('Calculation period end must be after start')
    })

    it('should throw error when end time equals start time', () => {
      const availability = createAvailability('excellent')
      const performance = createPerformance('excellent')
      const quality = createQuality('excellent')

      expect(() => new OEE_Calculation({
        resource_reference: 'PRESS-001',
        calculation_period_start: startTime,
        calculation_period_end: startTime, // Invalid: same time
        availability,
        performance,
        quality
      })).toThrow('Calculation period end must be after start')
    })
  })

  describe('OEE Calculation', () => {
    it('should calculate excellent scenario OEE correctly', () => {
      const oee = createOEECalculation('excellent')
      const expected = expectedResults.excellent

      expect(oee.availability_percentage).toBeCloseTo(expected.availability, 1)
      expect(oee.performance_percentage).toBeCloseTo(expected.performance, 1)
      expect(oee.quality_percentage).toBeCloseTo(expected.quality, 1)
      expect(oee.oee_percentage).toBeCloseTo(expected.oee, 1)
    })

    it('should calculate good scenario OEE correctly', () => {
      const oee = createOEECalculation('good')
      const expected = expectedResults.good

      expect(oee.availability_percentage).toBeCloseTo(expected.availability, 1)
      expect(oee.performance_percentage).toBeCloseTo(expected.performance, 1)
      expect(oee.quality_percentage).toBeCloseTo(expected.quality, 1)
      expect(oee.oee_percentage).toBeCloseTo(expected.oee, 1)
    })

    it('should calculate poor scenario OEE correctly', () => {
      const oee = createOEECalculation('poor')
      const expected = expectedResults.poor

      expect(oee.availability_percentage).toBeCloseTo(expected.availability, 1)
      expect(oee.performance_percentage).toBeCloseTo(expected.performance, 1)
      expect(oee.quality_percentage).toBeCloseTo(expected.quality, 1)
      expect(oee.oee_percentage).toBeCloseTo(expected.oee, 1)
    })

    it('should handle zero production scenario', () => {
      const oee = createOEECalculation('zeroProduction')
      const expected = expectedResults.zeroProduction

      expect(oee.availability_percentage).toBe(expected.availability)
      expect(oee.performance_percentage).toBe(expected.performance)
      expect(oee.quality_percentage).toBe(expected.quality)
      expect(oee.oee_percentage).toBe(expected.oee)
    })

    it('should handle perfect production scenario', () => {
      const oee = createOEECalculation('perfectProduction')
      const expected = expectedResults.perfectProduction

      expect(oee.availability_percentage).toBe(expected.availability)
      expect(oee.performance_percentage).toBe(expected.performance)
      expect(oee.quality_percentage).toBe(expected.quality)
      expect(oee.oee_percentage).toBe(expected.oee)
    })

    it('should return OEE as decimal between 0 and 1', () => {
      const scenarios = ['excellent', 'good', 'poor', 'zeroProduction', 'perfectProduction'] as const
      
      scenarios.forEach(scenario => {
        const oee = createOEECalculation(scenario)
        const decimal = oee.oee_decimal
        
        expect(decimal).toBeGreaterThanOrEqual(0)
        expect(decimal).toBeLessThanOrEqual(1)
        expect(decimal).toBeCloseTo(oee.oee_percentage / 100, 5)
      })
    })
  })

  describe('Attention Detection', () => {
    it('should detect when OEE requires attention with default thresholds', () => {
      const poorOEE = createOEECalculation('poor')
      const excellentOEE = createOEECalculation('excellent')

      expect(poorOEE.requiresAttention()).toBe(true)
      expect(excellentOEE.requiresAttention()).toBe(false)
    })

    it('should use custom thresholds for attention detection', () => {
      const goodOEE = createOEECalculation('good')

      // With strict thresholds
      const strictThresholds = {
        oee: 90,
        availability: 95,
        performance: 95,
        quality: 99
      }

      expect(goodOEE.requiresAttention(strictThresholds)).toBe(true)

      // With lenient thresholds
      const lenientThresholds = {
        oee: 50,
        availability: 70,
        performance: 70,
        quality: 80
      }

      expect(goodOEE.requiresAttention(lenientThresholds)).toBe(false)
    })

    it('should trigger attention when any factor is below threshold', () => {
      const speedOverQuality = createOEECalculation('speedOverQuality')
      
      // Should require attention due to low quality (80% < 95% default)
      expect(speedOverQuality.requiresAttention()).toBe(true)
      
      // Should not require attention with lowered quality threshold
      expect(speedOverQuality.requiresAttention({ quality: 75 })).toBe(false)
    })
  })

  describe('Factor Analysis', () => {
    it('should identify worst factor correctly', () => {
      const poorOEE = createOEECalculation('poor')
      const worstFactor = poorOEE.getWorstFactor()
      
      // In poor scenario: Availability 75%, Performance 70%, Quality 90%
      // Performance should be the worst factor
      expect(worstFactor).toBe('performance')
    })

    it('should identify worst factor when availability is lowest', () => {
      // Create custom scenario where availability is worst
      const availability = new Availability(480, 240) // 50%
      const performance = new Performance({
        totalPiecesProduced: 20000,
        runTimeMinutes: 240,
        targetRatePerMinute: 100
      }) // 83.3%
      const quality = new Quality({
        goodPieces: 19000,
        defectivePieces: 1000
      }) // 95%

      const oee = new OEE_Calculation({
        resource_reference: 'TEST',
        calculation_period_start: startTime,
        calculation_period_end: endTime,
        availability,
        performance,
        quality
      })

      expect(oee.getWorstFactor()).toBe('availability')
    })

    it('should calculate improvement potential correctly', () => {
      const poorOEE = createOEECalculation('poor')
      const improvement = poorOEE.getImprovementPotential()

      // With default world-class targets: A=90%, P=95%, Q=99%
      // Current poor: A=75%, P=70%, Q=90%
      expect(improvement.availability).toBeCloseTo(15, 1) // 90 - 75
      expect(improvement.performance).toBeCloseTo(25, 1) // 95 - 70
      expect(improvement.quality).toBeCloseTo(9, 1) // 99 - 90
      expect(improvement.overall).toBeGreaterThan(0)
    })

    it('should simulate improvement scenarios', () => {
      const poorOEE = createOEECalculation('poor')
      const currentOEE = poorOEE.oee_percentage

      // Simulate improving availability to 85%
      const improvedAvailability = poorOEE.simulateImprovement('availability', 85)
      expect(improvedAvailability).toBeGreaterThan(currentOEE)

      // Simulate improving performance to 85%
      const improvedPerformance = poorOEE.simulateImprovement('performance', 85)
      expect(improvedPerformance).toBeGreaterThan(currentOEE)

      // Simulate improving quality to 95%
      const improvedQuality = poorOEE.simulateImprovement('quality', 95)
      expect(improvedQuality).toBeGreaterThan(currentOEE)
    })
  })

  describe('Classification', () => {
    it('should classify OEE levels correctly', () => {
      const excellentOEE = createOEECalculation('excellent')
      const goodOEE = createOEECalculation('good')
      const poorOEE = createOEECalculation('poor')
      const zeroOEE = createOEECalculation('zeroProduction')

      // Excellent scenario actually gets ~84.6% OEE (World Class threshold is 85%)
      expect(excellentOEE.getClassification()).toContain('Good')
      expect(goodOEE.getClassification()).toContain('Good')
      expect(poorOEE.getClassification()).toContain('Fair')
      expect(zeroOEE.getClassification()).toContain('Poor')
    })
  })

  describe('Breakdown and Reporting', () => {
    it('should provide detailed breakdown', () => {
      const oee = createOEECalculation('excellent')
      const breakdown = oee.getBreakdown()

      expect(breakdown).toHaveProperty('oee')
      expect(breakdown).toHaveProperty('availability')
      expect(breakdown).toHaveProperty('performance')
      expect(breakdown).toHaveProperty('quality')
      expect(breakdown).toHaveProperty('worstFactor')
      expect(breakdown).toHaveProperty('classification')
      expect(breakdown).toHaveProperty('periodHours')

      expect(breakdown.periodHours).toBe(8) // 8-hour shift
      expect(breakdown.availability).toHaveProperty('planned')
      expect(breakdown.performance).toHaveProperty('actualProduction')
      expect(breakdown.quality).toHaveProperty('good')
    })

    it('should create summary for reporting', () => {
      const oee = createOEECalculation('good', 'MACHINE-X')
      const summary = oee.toSummary()

      expect(summary.resource_reference).toBe('MACHINE-X')
      expect(summary.period_start).toBe(startTime.toISOString())
      expect(summary.period_end).toBe(endTime.toISOString())
      expect(summary.oee_percentage).toBeCloseTo(expectedResults.good.oee, 3)
      expect(summary.worst_factor).toBe('availability')
      expect(summary.requires_attention).toBe(false) // Good scenario might not require attention
    })

    it('should format toString correctly', () => {
      const oee = createOEECalculation('excellent')
      const str = oee.toString()

      expect(str).toMatch(/^OEE: \d+\.\d%/)
      expect(str).toContain('A:')
      expect(str).toContain('P:')
      expect(str).toContain('Q:')
      expect(str).toContain('×')
    })
  })

  describe('Equality', () => {
    it('should return true for equal OEE calculations', () => {
      const oee1 = createOEECalculation('excellent', 'PRESS-001')
      const oee2 = createOEECalculation('excellent', 'PRESS-001')
      
      // Set same ID for comparison
      const sameIdOEE = new OEE_Calculation({
        oee_id: oee1.oee_id,
        resource_reference: oee1.resource_reference,
        calculation_period_start: oee1.calculation_period_start,
        calculation_period_end: oee1.calculation_period_end,
        availability: createAvailability('excellent'),
        performance: createPerformance('excellent'),
        quality: createQuality('excellent')
      })

      expect(oee1.equals(sameIdOEE)).toBe(true)
    })

    it('should return false for different OEE calculations', () => {
      const oee1 = createOEECalculation('excellent', 'PRESS-001')
      const oee2 = createOEECalculation('good', 'PRESS-002')

      expect(oee1.equals(oee2)).toBe(false)
    })

    it('should return false for different time periods', () => {
      const availability = createAvailability('excellent')
      const performance = createPerformance('excellent')
      const quality = createQuality('excellent')

      const oee1 = new OEE_Calculation({
        oee_id: 'SAME-ID',
        resource_reference: 'PRESS-001',
        calculation_period_start: startTime,
        calculation_period_end: endTime,
        availability,
        performance,
        quality
      })

      const oee2 = new OEE_Calculation({
        oee_id: 'SAME-ID',
        resource_reference: 'PRESS-001',
        calculation_period_start: new Date(startTime.getTime() + 3600000), // +1 hour
        calculation_period_end: new Date(endTime.getTime() + 3600000),
        availability,
        performance,
        quality
      })

      expect(oee1.equals(oee2)).toBe(false)
    })
  })

  describe('Edge Cases and Error Handling', () => {
    it('should handle very short time periods', () => {
      const { start, end } = timeScenarios.shortPeriod
      const availability = new Availability(120, 114) // 95%
      const performance = new Performance({
        totalPiecesProduced: 10800,
        runTimeMinutes: 114,
        targetRatePerMinute: 100
      })
      const quality = new Quality({
        goodPieces: 10692,
        defectivePieces: 108
      })

      const oee = new OEE_Calculation({
        resource_reference: 'PRESS-001',
        calculation_period_start: start,
        calculation_period_end: end,
        availability,
        performance,
        quality
      })

      expect(oee.oee_percentage).toBeGreaterThan(0)
      expect(oee.getBreakdown().periodHours).toBe(2)
    })

    it('should handle multi-day periods', () => {
      const { start, end } = timeScenarios.multiDay
      const availability = new Availability(1920, 1728) // 90%
      const performance = new Performance({
        totalPiecesProduced: 164160,
        runTimeMinutes: 1728,
        targetRatePerMinute: 100
      })
      const quality = new Quality({
        goodPieces: 162518,
        defectivePieces: 1642
      })

      const oee = new OEE_Calculation({
        resource_reference: 'PRESS-001',
        calculation_period_start: start,
        calculation_period_end: end,
        availability,
        performance,
        quality
      })

      expect(oee.oee_percentage).toBeGreaterThan(0)
      expect(oee.getBreakdown().periodHours).toBe(32)
    })
  })
})