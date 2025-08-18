import { oeeConfig } from '../../../config/oee.config'

/**
 * Performance Value Object
 * 
 * Represents the Performance (Efficiency) factor in OEE calculation.
 * Performance = (Total Pieces Produced / Theoretical Max Production) × 100
 * or
 * Performance = (Actual Production Rate / Target Production Rate) × 100
 * 
 * This measures speed losses - when equipment runs slower than its theoretical maximum speed.
 */
export class Performance {
  private readonly totalPiecesProduced: number
  private readonly theoreticalMaxProduction: number
  private readonly actualRatePerMinute: number
  private readonly targetRatePerMinute: number

  constructor(params: {
    totalPiecesProduced: number
    runTimeMinutes: number
    targetRatePerMinute: number
    actualRatePerMinute?: number
  }) {
    if (params.totalPiecesProduced < 0) {
      throw new Error('Total pieces produced cannot be negative')
    }
    if (params.runTimeMinutes < 0) {
      throw new Error('Run time cannot be negative')
    }
    if (params.targetRatePerMinute <= 0) {
      throw new Error('Target rate must be positive')
    }

    this.totalPiecesProduced = params.totalPiecesProduced
    this.targetRatePerMinute = params.targetRatePerMinute
    this.theoreticalMaxProduction = params.targetRatePerMinute * params.runTimeMinutes
    this.actualRatePerMinute = params.actualRatePerMinute ?? 
      (params.runTimeMinutes > 0 ? params.totalPiecesProduced / params.runTimeMinutes : 0)
  }

  /**
   * Calculate performance percentage (0-100)
   * Can exceed 100% if running faster than target
   */
  get percentage(): number {
    if (this.theoreticalMaxProduction === 0) {
      return 0
    }
    const rawPercentage = (this.totalPiecesProduced / this.theoreticalMaxProduction) * 100
    // Cap at 100% for OEE calculation (can't have OEE > 100%)
    return Math.min(rawPercentage, 100)
  }

  /**
   * Get raw performance percentage (can exceed 100%)
   */
  get rawPercentage(): number {
    if (this.theoreticalMaxProduction === 0) {
      return 0
    }
    return (this.totalPiecesProduced / this.theoreticalMaxProduction) * 100
  }

  /**
   * Get performance as a decimal (0-1) for OEE calculation
   */
  get decimal(): number {
    return this.percentage / 100
  }

  /**
   * Calculate speed loss in pieces
   */
  getSpeedLoss(): number {
    return Math.max(0, this.theoreticalMaxProduction - this.totalPiecesProduced)
  }

  /**
   * Calculate speed loss in percentage points
   */
  getSpeedLossPercentage(): number {
    return Math.max(0, 100 - this.percentage)
  }

  /**
   * Check if performance meets the target percentage
   */
  meetsTarget(targetPercentage: number): boolean {
    return this.percentage >= targetPercentage
  }

  /**
   * Identify potential bottleneck based on performance
   */
  identifyBottleneck(): string {
    const ratio = this.actualRatePerMinute / this.targetRatePerMinute
    
    if (ratio >= (oeeConfig.thresholds.performance.excellent / 100)) {
      return 'No bottleneck - running at or near target speed'
    } else if (ratio >= (oeeConfig.thresholds.performance.good / 100)) {
      return 'Minor speed losses - possible minor adjustments needed'
    } else if (ratio >= 0.70) {
      return 'Moderate speed losses - equipment may need adjustment or maintenance'
    } else if (ratio >= 0.50) {
      return 'Significant speed losses - investigate mechanical issues or operator training'
    } else {
      return 'Severe speed losses - critical bottleneck requiring immediate attention'
    }
  }

  /**
   * Get breakdown of performance components
   */
  getBreakdown(): {
    actualProduction: number
    theoreticalMax: number
    speedLoss: number
    actualRate: number
    targetRate: number
    efficiency: number
  } {
    return {
      actualProduction: this.totalPiecesProduced,
      theoreticalMax: this.theoreticalMaxProduction,
      speedLoss: this.getSpeedLoss(),
      actualRate: this.actualRatePerMinute,
      targetRate: this.targetRatePerMinute,
      efficiency: this.percentage
    }
  }

  /**
   * Identify if this is the constraining factor for OEE
   */
  isConstrainingFactor(availabilityPercentage: number, qualityPercentage: number): boolean {
    return this.percentage < availabilityPercentage && this.percentage < qualityPercentage
  }

  /**
   * Create from production counts and time
   */
  static fromProductionData(
    goodPieces: number,
    runTimeMinutes: number,
    targetRatePerMinute: number
  ): Performance {
    return new Performance({
      totalPiecesProduced: goodPieces,
      runTimeMinutes,
      targetRatePerMinute
    })
  }

  /**
   * Value object equality
   */
  equals(other: Performance): boolean {
    return (
      this.totalPiecesProduced === other.totalPiecesProduced &&
      this.theoreticalMaxProduction === other.theoreticalMaxProduction &&
      this.targetRatePerMinute === other.targetRatePerMinute
    )
  }

  toString(): string {
    return `Performance: ${this.percentage.toFixed(1)}% (${this.totalPiecesProduced}/${this.theoreticalMaxProduction} pieces, ${this.actualRatePerMinute.toFixed(1)}/${this.targetRatePerMinute} pcs/min)`
  }
}