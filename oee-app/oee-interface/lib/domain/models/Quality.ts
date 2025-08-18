/**
 * Quality Value Object
 * 
 * Represents the Quality factor in OEE calculation.
 * Quality = (Good Pieces / Total Pieces Produced) × 100
 * 
 * This measures the percentage of products that meet quality standards without 
 * requiring rework. Also known as First Pass Yield (FPY).
 */
export class Quality {
  private readonly goodPieces: number
  private readonly defectivePieces: number
  private readonly totalPiecesProduced: number

  constructor(params: {
    goodPieces: number
    defectivePieces: number
    totalPiecesProduced?: number
  }) {
    if (params.goodPieces < 0) {
      throw new Error('Good pieces cannot be negative')
    }
    if (params.defectivePieces < 0) {
      throw new Error('Defective pieces cannot be negative')
    }

    this.goodPieces = params.goodPieces
    this.defectivePieces = params.defectivePieces
    this.totalPiecesProduced = params.totalPiecesProduced ?? 
      (params.goodPieces + params.defectivePieces)

    if (this.totalPiecesProduced < this.goodPieces + this.defectivePieces) {
      throw new Error('Total pieces cannot be less than good + defective pieces')
    }
  }

  /**
   * Calculate quality percentage (0-100)
   * Also known as First Pass Yield (FPY)
   */
  get percentage(): number {
    if (this.totalPiecesProduced === 0) {
      return 100 // No production = no defects
    }
    return (this.goodPieces / this.totalPiecesProduced) * 100
  }

  /**
   * Get quality as a decimal (0-1) for OEE calculation
   */
  get decimal(): number {
    return this.percentage / 100
  }

  /**
   * Calculate defect rate as percentage
   */
  getDefectRate(): number {
    if (this.totalPiecesProduced === 0) {
      return 0
    }
    return (this.defectivePieces / this.totalPiecesProduced) * 100
  }

  /**
   * Calculate defects per million opportunities (DPMO)
   * Standard Six Sigma metric
   */
  getDPMO(): number {
    if (this.totalPiecesProduced === 0) {
      return 0
    }
    return (this.defectivePieces / this.totalPiecesProduced) * 1000000
  }

  /**
   * Calculate quality loss in pieces
   */
  getQualityLoss(): number {
    return this.defectivePieces
  }

  /**
   * Check if quality meets the target percentage
   */
  meetsTarget(targetPercentage: number): boolean {
    return this.percentage >= targetPercentage
  }

  /**
   * Determine if quality alert is required based on defect rate
   */
  requiresQualityAlert(thresholdPercentage: number = 5): boolean {
    return this.getDefectRate() > thresholdPercentage
  }

  /**
   * Get quality classification based on industry standards
   */
  getQualityLevel(): string {
    const defectRate = this.getDefectRate()
    
    if (defectRate === 0) {
      return 'Perfect - Zero defects'
    } else if (defectRate < 0.1) {
      return 'Excellent - World class quality'
    } else if (defectRate < 1) {
      return 'Good - High quality production'
    } else if (defectRate < 3) {
      return 'Acceptable - Standard quality'
    } else if (defectRate < 5) {
      return 'Marginal - Quality improvement needed'
    } else {
      return 'Poor - Immediate quality intervention required'
    }
  }

  /**
   * Calculate the cost impact of quality issues
   * @param costPerDefect Average cost of a defective piece (scrap + rework)
   */
  calculateCostImpact(costPerDefect: number): number {
    return this.defectivePieces * costPerDefect
  }

  /**
   * Get breakdown of quality components
   */
  getBreakdown(): {
    good: number
    defective: number
    total: number
    yieldRate: number
    defectRate: number
    dpmo: number
  } {
    return {
      good: this.goodPieces,
      defective: this.defectivePieces,
      total: this.totalPiecesProduced,
      yieldRate: this.percentage,
      defectRate: this.getDefectRate(),
      dpmo: this.getDPMO()
    }
  }

  /**
   * Identify if this is the constraining factor for OEE
   */
  isConstrainingFactor(availabilityPercentage: number, performancePercentage: number): boolean {
    return this.percentage < availabilityPercentage && this.percentage < performancePercentage
  }

  /**
   * Create from counter channel data
   * @param channelGood Good pieces from channel 0
   * @param channelRejects Reject pieces from channel 1
   */
  static fromCounterChannels(channelGood: number, channelRejects: number): Quality {
    return new Quality({
      goodPieces: channelGood,
      defectivePieces: channelRejects
    })
  }

  /**
   * Value object equality
   */
  equals(other: Quality): boolean {
    return (
      this.goodPieces === other.goodPieces &&
      this.defectivePieces === other.defectivePieces &&
      this.totalPiecesProduced === other.totalPiecesProduced
    )
  }

  toString(): string {
    return `Quality: ${this.percentage.toFixed(1)}% (${this.goodPieces}/${this.totalPiecesProduced} good, ${this.defectivePieces} defects)`
  }
}