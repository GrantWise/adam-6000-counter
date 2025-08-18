/**
 * Quality Domain Model Tests
 * 
 * Tests for quality/first pass yield calculation logic and business rules.
 */

import { describe, it, expect } from 'vitest'
import { Quality } from '@/lib/domain/models/Quality'

describe('Quality', () => {
  describe('Construction', () => {
    it('should create quality with valid inputs', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      expect(quality.percentage).toBeCloseTo(95, 1)
      expect(quality.decimal).toBeCloseTo(0.95, 3)
    })

    it('should calculate total pieces when not provided', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      const breakdown = quality.getBreakdown()
      expect(breakdown.total).toBe(10000) // 9500 + 500
    })

    it('should use provided total pieces', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500,
        totalPiecesProduced: 10500 // Some pieces unaccounted for
      })

      const breakdown = quality.getBreakdown()
      expect(breakdown.total).toBe(10500)
    })

    it('should throw error for negative good pieces', () => {
      expect(() => new Quality({
        goodPieces: -100,
        defectivePieces: 50
      })).toThrow('Good pieces cannot be negative')
    })

    it('should throw error for negative defective pieces', () => {
      expect(() => new Quality({
        goodPieces: 100,
        defectivePieces: -50
      })).toThrow('Defective pieces cannot be negative')
    })

    it('should throw error when total is less than good + defective', () => {
      expect(() => new Quality({
        goodPieces: 100,
        defectivePieces: 50,
        totalPiecesProduced: 120 // Less than 150
      })).toThrow('Total pieces cannot be less than good + defective pieces')
    })
  })

  describe('Percentage Calculations', () => {
    it('should calculate 100% quality for perfect production', () => {
      const quality = new Quality({
        goodPieces: 10000,
        defectivePieces: 0
      })

      expect(quality.percentage).toBe(100)
      expect(quality.decimal).toBe(1)
      expect(quality.getDefectRate()).toBe(0)
    })

    it('should calculate 0% quality for all defects', () => {
      const quality = new Quality({
        goodPieces: 0,
        defectivePieces: 1000
      })

      expect(quality.percentage).toBe(0)
      expect(quality.decimal).toBe(0)
      expect(quality.getDefectRate()).toBe(100)
    })

    it('should return 100% for no production (no defects possible)', () => {
      const quality = new Quality({
        goodPieces: 0,
        defectivePieces: 0
      })

      expect(quality.percentage).toBe(100)
      expect(quality.getDefectRate()).toBe(0)
    })

    it('should calculate typical quality scenarios correctly', () => {
      // 99% quality scenario
      const quality99 = new Quality({
        goodPieces: 9900,
        defectivePieces: 100
      })

      expect(quality99.percentage).toBeCloseTo(99, 1)
      expect(quality99.getDefectRate()).toBeCloseTo(1, 1)

      // 95% quality scenario
      const quality95 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      expect(quality95.percentage).toBeCloseTo(95, 1)
      expect(quality95.getDefectRate()).toBeCloseTo(5, 1)

      // 90% quality scenario
      const quality90 = new Quality({
        goodPieces: 9000,
        defectivePieces: 1000
      })

      expect(quality90.percentage).toBeCloseTo(90, 1)
      expect(quality90.getDefectRate()).toBeCloseTo(10, 1)
    })
  })

  describe('Defect Rate Calculations', () => {
    it('should calculate defect rate correctly', () => {
      const quality = new Quality({
        goodPieces: 9700,
        defectivePieces: 300
      })

      expect(quality.getDefectRate()).toBeCloseTo(3, 1)
    })

    it('should calculate DPMO (Defects Per Million Opportunities)', () => {
      const quality = new Quality({
        goodPieces: 9990,
        defectivePieces: 10
      })

      expect(quality.getDPMO()).toBeCloseTo(1000, 1) // 10/10000 * 1,000,000
    })

    it('should return zero DPMO for perfect quality', () => {
      const quality = new Quality({
        goodPieces: 10000,
        defectivePieces: 0
      })

      expect(quality.getDPMO()).toBe(0)
    })

    it('should handle zero production in DPMO calculation', () => {
      const quality = new Quality({
        goodPieces: 0,
        defectivePieces: 0
      })

      expect(quality.getDPMO()).toBe(0)
    })
  })

  describe('Quality Loss Analysis', () => {
    it('should correctly report quality loss', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      expect(quality.getQualityLoss()).toBe(500)
    })

    it('should calculate cost impact of quality issues', () => {
      const quality = new Quality({
        goodPieces: 9800,
        defectivePieces: 200
      })

      const costPerDefect = 25 // $25 per defective piece
      const costImpact = quality.calculateCostImpact(costPerDefect)

      expect(costImpact).toBe(5000) // 200 * 25
    })

    it('should return zero cost impact for perfect quality', () => {
      const quality = new Quality({
        goodPieces: 10000,
        defectivePieces: 0
      })

      expect(quality.calculateCostImpact(100)).toBe(0)
    })
  })

  describe('Target Comparison', () => {
    it('should correctly identify when target is met', () => {
      const quality = new Quality({
        goodPieces: 9700,
        defectivePieces: 300
      }) // 97%

      expect(quality.meetsTarget(95)).toBe(true)
      expect(quality.meetsTarget(97)).toBe(true)
      expect(quality.meetsTarget(98)).toBe(false)
    })

    it('should handle edge case of exact target match', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      }) // 95%

      expect(quality.meetsTarget(95)).toBe(true)
      expect(quality.meetsTarget(95.1)).toBe(false)
    })
  })

  describe('Quality Alert System', () => {
    it('should trigger quality alert for high defect rates', () => {
      const quality = new Quality({
        goodPieces: 9000,
        defectivePieces: 1000
      }) // 10% defect rate

      expect(quality.requiresQualityAlert()).toBe(true) // Default threshold 5%
      expect(quality.requiresQualityAlert(15)).toBe(false) // Custom threshold 15%
    })

    it('should not trigger alert for acceptable defect rates', () => {
      const quality = new Quality({
        goodPieces: 9900,
        defectivePieces: 100
      }) // 1% defect rate

      expect(quality.requiresQualityAlert()).toBe(false) // Below 5% threshold
    })

    it('should handle edge case at threshold boundary', () => {
      const quality = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      }) // 5% defect rate

      expect(quality.requiresQualityAlert(5)).toBe(false) // Equal to threshold
      expect(quality.requiresQualityAlert(4.9)).toBe(true) // Just above threshold
    })
  })

  describe('Quality Level Classification', () => {
    it('should classify perfect quality correctly', () => {
      const quality = new Quality({
        goodPieces: 10000,
        defectivePieces: 0
      })

      expect(quality.getQualityLevel()).toContain('Perfect')
    })

    it('should classify excellent quality correctly', () => {
      const quality = new Quality({
        goodPieces: 9999,
        defectivePieces: 1
      }) // 0.01% defect rate

      expect(quality.getQualityLevel()).toContain('Excellent')
    })

    it('should classify good quality correctly', () => {
      const quality = new Quality({
        goodPieces: 9950,
        defectivePieces: 50
      }) // 0.5% defect rate

      expect(quality.getQualityLevel()).toContain('Good')
    })

    it('should classify acceptable quality correctly', () => {
      const quality = new Quality({
        goodPieces: 9800,
        defectivePieces: 200
      }) // 2% defect rate

      expect(quality.getQualityLevel()).toContain('Acceptable')
    })

    it('should classify marginal quality correctly', () => {
      const quality = new Quality({
        goodPieces: 9600,
        defectivePieces: 400
      }) // 4% defect rate

      expect(quality.getQualityLevel()).toContain('Marginal')
    })

    it('should classify poor quality correctly', () => {
      const quality = new Quality({
        goodPieces: 9000,
        defectivePieces: 1000
      }) // 10% defect rate

      expect(quality.getQualityLevel()).toContain('Poor')
    })
  })

  describe('Breakdown Analysis', () => {
    it('should provide detailed breakdown', () => {
      const quality = new Quality({
        goodPieces: 9700,
        defectivePieces: 300
      })

      const breakdown = quality.getBreakdown()

      expect(breakdown.good).toBe(9700)
      expect(breakdown.defective).toBe(300)
      expect(breakdown.total).toBe(10000)
      expect(breakdown.yieldRate).toBeCloseTo(97, 1)
      expect(breakdown.defectRate).toBeCloseTo(3, 1)
      expect(breakdown.dpmo).toBeCloseTo(30000, 1)
    })
  })

  describe('Constraining Factor Analysis', () => {
    it('should identify as constraining factor when lowest', () => {
      const quality = new Quality({
        goodPieces: 8000,
        defectivePieces: 2000
      }) // 80%

      // Quality is constraining when it's lower than availability and performance
      expect(quality.isConstrainingFactor(90, 85)).toBe(true)
      expect(quality.isConstrainingFactor(75, 85)).toBe(false) // Availability is lower
      expect(quality.isConstrainingFactor(90, 75)).toBe(false) // Performance is lower
    })

    it('should not be constraining when equal to other factors', () => {
      const quality = new Quality({
        goodPieces: 8000,
        defectivePieces: 2000
      }) // 80%

      expect(quality.isConstrainingFactor(80, 90)).toBe(false)
      expect(quality.isConstrainingFactor(90, 80)).toBe(false)
    })
  })

  describe('Factory Methods', () => {
    it('should create from counter channels correctly', () => {
      const quality = Quality.fromCounterChannels(9800, 200)

      expect(quality.percentage).toBeCloseTo(98, 1)
      expect(quality.getBreakdown().good).toBe(9800)
      expect(quality.getBreakdown().defective).toBe(200)
      expect(quality.getBreakdown().total).toBe(10000)
    })

    it('should handle zero rejects in counter channels', () => {
      const quality = Quality.fromCounterChannels(10000, 0)

      expect(quality.percentage).toBe(100)
      expect(quality.getDefectRate()).toBe(0)
    })

    it('should handle zero good pieces in counter channels', () => {
      const quality = Quality.fromCounterChannels(0, 1000)

      expect(quality.percentage).toBe(0)
      expect(quality.getDefectRate()).toBe(100)
    })
  })

  describe('Equality', () => {
    it('should return true for equal quality objects', () => {
      const quality1 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      const quality2 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      expect(quality1.equals(quality2)).toBe(true)
    })

    it('should return false for different quality objects', () => {
      const quality1 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500
      })

      const quality2 = new Quality({
        goodPieces: 9600,
        defectivePieces: 400
      })

      expect(quality1.equals(quality2)).toBe(false)
    })

    it('should return false when total production differs', () => {
      const quality1 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500,
        totalPiecesProduced: 10000
      })

      const quality2 = new Quality({
        goodPieces: 9500,
        defectivePieces: 500,
        totalPiecesProduced: 10500
      })

      expect(quality1.equals(quality2)).toBe(false)
    })
  })

  describe('String Representation', () => {
    it('should format toString correctly', () => {
      const quality = new Quality({
        goodPieces: 9700,
        defectivePieces: 300
      })

      const str = quality.toString()

      expect(str).toContain('Quality: 97.0%')
      expect(str).toContain('(9700/10000 good, 300 defects)')
    })

    it('should handle perfect quality in toString', () => {
      const quality = new Quality({
        goodPieces: 10000,
        defectivePieces: 0
      })

      const str = quality.toString()

      expect(str).toContain('Quality: 100.0%')
      expect(str).toContain('(10000/10000 good, 0 defects)')
    })
  })

  describe('Edge Cases', () => {
    it('should handle very small production runs', () => {
      const quality = new Quality({
        goodPieces: 8,
        defectivePieces: 2
      })

      expect(quality.percentage).toBe(80)
      expect(quality.getDefectRate()).toBe(20)
      expect(quality.getDPMO()).toBe(200000) // 2/10 * 1,000,000
    })

    it('should handle very large production runs', () => {
      const quality = new Quality({
        goodPieces: 999999,
        defectivePieces: 1
      })

      expect(quality.percentage).toBeCloseTo(99.9999, 4)
      expect(quality.getDPMO()).toBeCloseTo(1, 1)
    })

    it('should maintain precision for high-precision calculations', () => {
      const quality = new Quality({
        goodPieces: 99999,
        defectivePieces: 1
      })

      expect(quality.percentage).toBeCloseTo(99.999, 3)
      expect(quality.getDefectRate()).toBeCloseTo(0.001, 3)
    })

    it('should handle unaccounted production correctly', () => {
      // Some pieces might be in-process or lost
      const quality = new Quality({
        goodPieces: 9000,
        defectivePieces: 500,
        totalPiecesProduced: 10000 // 500 pieces unaccounted
      })

      expect(quality.percentage).toBe(90) // 9000/10000
      expect(quality.getDefectRate()).toBe(5) // 500/10000
    })
  })
})