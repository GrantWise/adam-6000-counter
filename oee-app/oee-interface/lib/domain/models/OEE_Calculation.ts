import { Availability } from './Availability'
import { Performance } from './Performance'
import { Quality } from './Quality'
import { oeeConfig } from '../../../config/oee.config'

/**
 * OEE_Calculation Value Object
 * 
 * Represents a complete OEE (Overall Equipment Effectiveness) calculation
 * following the canonical model structure.
 * 
 * OEE = Availability × Performance × Quality
 * 
 * This is the key metric for measuring manufacturing productivity, 
 * identifying losses, and improving equipment efficiency.
 */
export class OEE_Calculation {
  readonly oee_id: string
  readonly resource_reference: string
  readonly calculation_period_start: Date
  readonly calculation_period_end: Date
  
  private readonly availability: Availability
  private readonly performance: Performance
  private readonly quality: Quality

  constructor(params: {
    oee_id?: string
    resource_reference: string
    calculation_period_start: Date
    calculation_period_end: Date
    availability: Availability
    performance: Performance
    quality: Quality
  }) {
    if (params.calculation_period_end <= params.calculation_period_start) {
      throw new Error('Calculation period end must be after start')
    }

    this.oee_id = params.oee_id ?? `OEE-${params.resource_reference}-${Date.now()}`
    this.resource_reference = params.resource_reference
    this.calculation_period_start = params.calculation_period_start
    this.calculation_period_end = params.calculation_period_end
    this.availability = params.availability
    this.performance = params.performance
    this.quality = params.quality
  }

  /**
   * Get availability percentage (0-100)
   */
  get availability_percentage(): number {
    return this.availability.percentage
  }

  /**
   * Get performance percentage (0-100)
   */
  get performance_percentage(): number {
    return this.performance.percentage
  }

  /**
   * Get quality percentage (0-100)
   */
  get quality_percentage(): number {
    return this.quality.percentage
  }

  /**
   * Calculate overall OEE percentage (0-100)
   * OEE = Availability × Performance × Quality
   */
  get oee_percentage(): number {
    return (
      this.availability.decimal * 
      this.performance.decimal * 
      this.quality.decimal * 
      100
    )
  }

  /**
   * Get OEE as a decimal (0-1)
   */
  get oee_decimal(): number {
    return this.oee_percentage / 100
  }

  /**
   * Determine if OEE requires attention based on thresholds
   */
  requiresAttention(thresholds?: {
    oee?: number
    availability?: number
    performance?: number
    quality?: number
  }): boolean {
    const defaults = oeeConfig.alerts.requiresAttention
    const limits = { ...defaults, ...thresholds }

    return (
      this.oee_percentage < limits.oee ||
      this.availability_percentage < limits.availability ||
      this.performance_percentage < limits.performance ||
      this.quality_percentage < limits.quality
    )
  }

  /**
   * Identify which factor is the worst (constraining factor)
   */
  getWorstFactor(): 'availability' | 'performance' | 'quality' {
    const factors = {
      availability: this.availability_percentage,
      performance: this.performance_percentage,
      quality: this.quality_percentage
    }

    return Object.entries(factors).reduce((worst, [factor, value]) => 
      value < factors[worst as keyof typeof factors] ? factor : worst
    , 'availability') as 'availability' | 'performance' | 'quality'
  }

  /**
   * Get improvement potential for each factor
   */
  getImprovementPotential(worldClassTargets?: {
    availability?: number
    performance?: number
    quality?: number
  }): {
    availability: number
    performance: number
    quality: number
    overall: number
  } {
    const targets = {
      availability: worldClassTargets?.availability ?? oeeConfig.thresholds.availability.excellent,
      performance: worldClassTargets?.performance ?? oeeConfig.thresholds.performance.excellent,
      quality: worldClassTargets?.quality ?? oeeConfig.thresholds.quality.excellent
    }

    return {
      availability: Math.max(0, targets.availability - this.availability_percentage),
      performance: Math.max(0, targets.performance - this.performance_percentage),
      quality: Math.max(0, targets.quality - this.quality_percentage),
      overall: Math.max(0, (targets.availability * targets.performance * targets.quality / 10000) - this.oee_percentage)
    }
  }

  /**
   * Calculate what OEE would be if one factor improved
   */
  simulateImprovement(
    factor: 'availability' | 'performance' | 'quality',
    newPercentage: number
  ): number {
    const factors = {
      availability: factor === 'availability' ? newPercentage : this.availability_percentage,
      performance: factor === 'performance' ? newPercentage : this.performance_percentage,
      quality: factor === 'quality' ? newPercentage : this.quality_percentage
    }

    return (factors.availability * factors.performance * factors.quality) / 10000
  }

  /**
   * Get OEE classification based on industry standards
   */
  getClassification(): string {
    const oee = this.oee_percentage

    if (oee >= oeeConfig.thresholds.oee.excellent) {
      return 'World Class - Excellent performance'
    } else if (oee >= oeeConfig.thresholds.oee.good) {
      return 'Good - Acceptable performance'
    } else if (oee >= 40) {
      return 'Fair - Improvement needed'
    } else {
      return 'Poor - Significant improvement required'
    }
  }

  /**
   * Get detailed breakdown of all components
   */
  getBreakdown(): {
    oee: number
    availability: ReturnType<Availability['getBreakdown']>
    performance: ReturnType<Performance['getBreakdown']>
    quality: ReturnType<Quality['getBreakdown']>
    worstFactor: string
    classification: string
    periodHours: number
  } {
    const periodMs = this.calculation_period_end.getTime() - this.calculation_period_start.getTime()
    const periodHours = periodMs / (1000 * 60 * 60)

    return {
      oee: this.oee_percentage,
      availability: this.availability.getBreakdown(),
      performance: this.performance.getBreakdown(),
      quality: this.quality.getBreakdown(),
      worstFactor: this.getWorstFactor(),
      classification: this.getClassification(),
      periodHours
    }
  }

  /**
   * Create a summary object for reporting
   */
  toSummary(): {
    oee_id: string
    resource_reference: string
    period_start: string
    period_end: string
    oee_percentage: number
    availability_percentage: number
    performance_percentage: number
    quality_percentage: number
    worst_factor: string
    requires_attention: boolean
  } {
    return {
      oee_id: this.oee_id,
      resource_reference: this.resource_reference,
      period_start: this.calculation_period_start.toISOString(),
      period_end: this.calculation_period_end.toISOString(),
      oee_percentage: this.oee_percentage,
      availability_percentage: this.availability_percentage,
      performance_percentage: this.performance_percentage,
      quality_percentage: this.quality_percentage,
      worst_factor: this.getWorstFactor(),
      requires_attention: this.requiresAttention()
    }
  }

  /**
   * Value object equality
   */
  equals(other: OEE_Calculation): boolean {
    return (
      this.oee_id === other.oee_id &&
      this.resource_reference === other.resource_reference &&
      this.calculation_period_start.getTime() === other.calculation_period_start.getTime() &&
      this.calculation_period_end.getTime() === other.calculation_period_end.getTime() &&
      this.availability.equals(other.availability) &&
      this.performance.equals(other.performance) &&
      this.quality.equals(other.quality)
    )
  }

  toString(): string {
    return `OEE: ${this.oee_percentage.toFixed(1)}% (A:${this.availability_percentage.toFixed(1)}% × P:${this.performance_percentage.toFixed(1)}% × Q:${this.quality_percentage.toFixed(1)}%)`
  }
}