/**
 * Performance Domain Model Tests
 * 
 * Tests for performance/efficiency calculation logic and business rules.
 */

import { describe, it, expect } from 'vitest'
import { Performance } from '@/lib/domain/models/Performance'

describe('Performance', () => {
  describe('Construction', () => {
    it('should create performance with valid inputs', () => {
      const performance = new Performance({
        totalPiecesProduced: 38000,
        runTimeMinutes: 400,
        targetRatePerMinute: 100
      })

      expect(performance.percentage).toBeCloseTo(95, 1) // 38000 / (400 * 100) * 100
      expect(performance.decimal).toBeCloseTo(0.95, 3)
    })

    it('should calculate actual rate when not provided', () => {
      const performance = new Performance({
        totalPiecesProduced: 20000,
        runTimeMinutes: 200,
        targetRatePerMinute: 100
      })

      const breakdown = performance.getBreakdown()
      expect(breakdown.actualRate).toBe(100) // 20000 / 200
    })

    it('should use provided actual rate', () => {
      const performance = new Performance({
        totalPiecesProduced: 18000,
        runTimeMinutes: 200,
        targetRatePerMinute: 100,
        actualRatePerMinute: 90
      })

      const breakdown = performance.getBreakdown()
      expect(breakdown.actualRate).toBe(90)
    })

    it('should throw error for negative pieces produced', () => {
      expect(() => new Performance({
        totalPiecesProduced: -100,
        runTimeMinutes: 200,
        targetRatePerMinute: 100
      })).toThrow('Total pieces produced cannot be negative')
    })

    it('should throw error for negative run time', () => {
      expect(() => new Performance({
        totalPiecesProduced: 100,
        runTimeMinutes: -200,
        targetRatePerMinute: 100
      })).toThrow('Run time cannot be negative')
    })

    it('should throw error for zero or negative target rate', () => {
      expect(() => new Performance({
        totalPiecesProduced: 100,
        runTimeMinutes: 200,
        targetRatePerMinute: 0
      })).toThrow('Target rate must be positive')

      expect(() => new Performance({
        totalPiecesProduced: 100,
        runTimeMinutes: 200,
        targetRatePerMinute: -50
      })).toThrow('Target rate must be positive')
    })
  })

  describe('Percentage Calculations', () => {
    it('should calculate 100% performance for target rate achievement', () => {
      const performance = new Performance({
        totalPiecesProduced: 48000,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance.percentage).toBe(100)
      expect(performance.rawPercentage).toBe(100)
      expect(performance.decimal).toBe(1)
    })

    it('should calculate 0% performance for zero production', () => {
      const performance = new Performance({
        totalPiecesProduced: 0,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance.percentage).toBe(0)
      expect(performance.decimal).toBe(0)
    })

    it('should cap percentage at 100% for OEE calculation', () => {
      const performance = new Performance({
        totalPiecesProduced: 55000, // Over target
        runTimeMinutes: 480,
        targetRatePerMinute: 100 // Target is 48000
      })

      expect(performance.rawPercentage).toBeCloseTo(114.58, 1) // Raw can exceed 100%
      expect(performance.percentage).toBe(100) // Capped for OEE
    })

    it('should handle zero run time gracefully', () => {
      const performance = new Performance({
        totalPiecesProduced: 0,
        runTimeMinutes: 0,
        targetRatePerMinute: 100
      })

      expect(performance.percentage).toBe(0)
      expect(performance.decimal).toBe(0)
    })

    it('should calculate typical performance scenarios', () => {
      // 95% performance scenario
      const performance95 = new Performance({
        totalPiecesProduced: 45600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance95.percentage).toBeCloseTo(95, 1)

      // 85% performance scenario
      const performance85 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance85.percentage).toBeCloseTo(85, 1)
    })
  })

  describe('Speed Loss Calculations', () => {
    it('should calculate speed loss in pieces', () => {
      const performance = new Performance({
        totalPiecesProduced: 40000,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const speedLoss = performance.getSpeedLoss()
      expect(speedLoss).toBe(8000) // 48000 - 40000
    })

    it('should calculate speed loss percentage', () => {
      const performance = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const speedLossPercentage = performance.getSpeedLossPercentage()
      expect(speedLossPercentage).toBeCloseTo(15, 1) // 100 - 85
    })

    it('should return zero speed loss for target achievement', () => {
      const performance = new Performance({
        totalPiecesProduced: 48000,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance.getSpeedLoss()).toBe(0)
      expect(performance.getSpeedLossPercentage()).toBe(0)
    })

    it('should handle over-performance gracefully', () => {
      const performance = new Performance({
        totalPiecesProduced: 50000,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance.getSpeedLoss()).toBe(0) // No loss when over-performing
      expect(performance.getSpeedLossPercentage()).toBe(0)
    })
  })

  describe('Target Comparison', () => {
    it('should correctly identify when target is met', () => {
      const performance = new Performance({
        totalPiecesProduced: 45600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      }) // 95%

      expect(performance.meetsTarget(90)).toBe(true)
      expect(performance.meetsTarget(95)).toBe(true)
      expect(performance.meetsTarget(98)).toBe(false)
    })

    it('should handle edge case of exact target match', () => {
      const performance = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      }) // 85%

      expect(performance.meetsTarget(85)).toBe(true)
      expect(performance.meetsTarget(85.1)).toBe(false)
    })
  })

  describe('Bottleneck Analysis', () => {
    it('should identify no bottleneck for high performance', () => {
      const performance = new Performance({
        totalPiecesProduced: 45600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100,
        actualRatePerMinute: 95
      })

      const bottleneck = performance.identifyBottleneck()
      expect(bottleneck).toContain('No bottleneck')
    })

    it('should identify minor speed losses', () => {
      const performance = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100,
        actualRatePerMinute: 85
      })

      const bottleneck = performance.identifyBottleneck()
      expect(bottleneck).toContain('Minor speed losses')
    })

    it('should identify moderate speed losses', () => {
      const performance = new Performance({
        totalPiecesProduced: 33600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100,
        actualRatePerMinute: 70
      })

      const bottleneck = performance.identifyBottleneck()
      expect(bottleneck).toContain('Moderate speed losses')
    })

    it('should identify significant speed losses', () => {
      const performance = new Performance({
        totalPiecesProduced: 24000,
        runTimeMinutes: 480,
        targetRatePerMinute: 100,
        actualRatePerMinute: 50
      })

      const bottleneck = performance.identifyBottleneck()
      expect(bottleneck).toContain('Significant speed losses')
    })

    it('should identify severe speed losses', () => {
      const performance = new Performance({
        totalPiecesProduced: 14400,
        runTimeMinutes: 480,
        targetRatePerMinute: 100,
        actualRatePerMinute: 30
      })

      const bottleneck = performance.identifyBottleneck()
      expect(bottleneck).toContain('Severe speed losses')
    })
  })

  describe('Breakdown Analysis', () => {
    it('should provide detailed breakdown', () => {
      const performance = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const breakdown = performance.getBreakdown()

      expect(breakdown.actualProduction).toBe(40800)
      expect(breakdown.theoreticalMax).toBe(48000)
      expect(breakdown.speedLoss).toBe(7200)
      expect(breakdown.actualRate).toBe(85) // 40800 / 480
      expect(breakdown.targetRate).toBe(100)
      expect(breakdown.efficiency).toBeCloseTo(85, 1)
    })
  })

  describe('Constraining Factor Analysis', () => {
    it('should identify as constraining factor when lowest', () => {
      const performance = new Performance({
        totalPiecesProduced: 33600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      }) // 70%

      // Performance is constraining when it's lower than availability and quality
      expect(performance.isConstrainingFactor(85, 90)).toBe(true)
      expect(performance.isConstrainingFactor(65, 90)).toBe(false) // Availability is lower
      expect(performance.isConstrainingFactor(85, 65)).toBe(false) // Quality is lower
    })

    it('should not be constraining when equal to other factors', () => {
      const performance = new Performance({
        totalPiecesProduced: 33600,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      }) // 70%

      expect(performance.isConstrainingFactor(70, 90)).toBe(false)
      expect(performance.isConstrainingFactor(85, 70)).toBe(false)
    })
  })

  describe('Factory Methods', () => {
    it('should create from production data correctly', () => {
      const performance = Performance.fromProductionData(40800, 480, 100)

      expect(performance.percentage).toBeCloseTo(85, 1)
      expect(performance.getBreakdown().actualProduction).toBe(40800)
    })

    it('should handle zero production in factory method', () => {
      const performance = Performance.fromProductionData(0, 480, 100)

      expect(performance.percentage).toBe(0)
      expect(performance.getSpeedLoss()).toBe(48000)
    })
  })

  describe('Equality', () => {
    it('should return true for equal performance objects', () => {
      const performance1 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const performance2 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance1.equals(performance2)).toBe(true)
    })

    it('should return false for different performance objects', () => {
      const performance1 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const performance2 = new Performance({
        totalPiecesProduced: 38400,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance1.equals(performance2)).toBe(false)
    })

    it('should return false when target rate differs', () => {
      const performance1 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const performance2 = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 120
      })

      expect(performance1.equals(performance2)).toBe(false)
    })
  })

  describe('String Representation', () => {
    it('should format toString correctly', () => {
      const performance = new Performance({
        totalPiecesProduced: 40800,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const str = performance.toString()

      expect(str).toContain('Performance: 85.0%')
      expect(str).toContain('(40800/48000 pieces')
      expect(str).toContain('85.0/100 pcs/min)')
    })

    it('should handle zero values in toString', () => {
      const performance = new Performance({
        totalPiecesProduced: 0,
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      const str = performance.toString()

      expect(str).toContain('Performance: 0.0%')
      expect(str).toContain('(0/48000 pieces')
    })
  })

  describe('Edge Cases', () => {
    it('should handle very small production runs', () => {
      const performance = new Performance({
        totalPiecesProduced: 5,
        runTimeMinutes: 10,
        targetRatePerMinute: 1
      })

      expect(performance.percentage).toBeCloseTo(50, 1)
      expect(performance.getSpeedLoss()).toBe(5)
    })

    it('should handle very large production runs', () => {
      const performance = new Performance({
        totalPiecesProduced: 1440000, // 30 days worth
        runTimeMinutes: 43200, // 30 days
        targetRatePerMinute: 50
      })

      expect(performance.percentage).toBeCloseTo(66.67, 1)
    })

    it('should maintain precision for high-precision calculations', () => {
      const performance = new Performance({
        totalPiecesProduced: 99999,
        runTimeMinutes: 1000,
        targetRatePerMinute: 100
      })

      expect(performance.percentage).toBeCloseTo(99.999, 2)
      expect(performance.getSpeedLoss()).toBe(1)
    })

    it('should handle extreme over-performance', () => {
      const performance = new Performance({
        totalPiecesProduced: 96000, // 200% of target
        runTimeMinutes: 480,
        targetRatePerMinute: 100
      })

      expect(performance.rawPercentage).toBe(200)
      expect(performance.percentage).toBe(100) // Capped for OEE
      expect(performance.getSpeedLoss()).toBe(0)
    })
  })
})