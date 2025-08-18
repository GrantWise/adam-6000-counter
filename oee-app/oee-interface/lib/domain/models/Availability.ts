/**
 * Availability Value Object
 * 
 * Represents the Availability factor in OEE calculation.
 * Availability = (Actual Run Time / Planned Production Time) × 100
 * 
 * This measures the percentage of scheduled time that the equipment is available 
 * to operate, accounting for both planned and unplanned downtime.
 */
export class Availability {
  private readonly plannedProductionTimeMinutes: number
  private readonly actualRunTimeMinutes: number
  private readonly downtimeMinutes: number

  constructor(
    plannedProductionTimeMinutes: number,
    actualRunTimeMinutes: number,
    downtimeMinutes?: number
  ) {
    if (plannedProductionTimeMinutes < 0) {
      throw new Error('Planned production time cannot be negative')
    }
    if (actualRunTimeMinutes < 0) {
      throw new Error('Actual run time cannot be negative')
    }
    if (actualRunTimeMinutes > plannedProductionTimeMinutes) {
      throw new Error('Actual run time cannot exceed planned production time')
    }

    this.plannedProductionTimeMinutes = plannedProductionTimeMinutes
    this.actualRunTimeMinutes = actualRunTimeMinutes
    this.downtimeMinutes = downtimeMinutes ?? (plannedProductionTimeMinutes - actualRunTimeMinutes)
  }

  /**
   * Calculate availability percentage (0-100)
   */
  get percentage(): number {
    if (this.plannedProductionTimeMinutes === 0) {
      return 0
    }
    return (this.actualRunTimeMinutes / this.plannedProductionTimeMinutes) * 100
  }

  /**
   * Get availability as a decimal (0-1) for OEE calculation
   */
  get decimal(): number {
    return this.percentage / 100
  }

  /**
   * Check if availability meets the target percentage
   */
  meetsTarget(targetPercentage: number): boolean {
    return this.percentage >= targetPercentage
  }

  /**
   * Calculate the production impact of downtime in minutes
   */
  getDowntimeImpact(): number {
    return this.downtimeMinutes
  }

  /**
   * Get breakdown of time components
   */
  getBreakdown(): {
    planned: number
    actual: number
    downtime: number
    utilizationRate: number
  } {
    return {
      planned: this.plannedProductionTimeMinutes,
      actual: this.actualRunTimeMinutes,
      downtime: this.downtimeMinutes,
      utilizationRate: this.percentage
    }
  }

  /**
   * Identify if this is the constraining factor for OEE
   */
  isConstrainingFactor(performancePercentage: number, qualityPercentage: number): boolean {
    return this.percentage < performancePercentage && this.percentage < qualityPercentage
  }

  /**
   * Create from downtime records
   */
  static fromDowntimeRecords(
    plannedMinutes: number,
    downtimeRecords: Array<{ duration_minutes: number; category: 'planned' | 'unplanned' }>
  ): Availability {
    const totalDowntime = downtimeRecords.reduce((sum, record) => sum + record.duration_minutes, 0)
    const actualRunTime = plannedMinutes - totalDowntime
    return new Availability(plannedMinutes, actualRunTime, totalDowntime)
  }

  /**
   * Value object equality
   */
  equals(other: Availability): boolean {
    return (
      this.plannedProductionTimeMinutes === other.plannedProductionTimeMinutes &&
      this.actualRunTimeMinutes === other.actualRunTimeMinutes &&
      this.downtimeMinutes === other.downtimeMinutes
    )
  }

  toString(): string {
    return `Availability: ${this.percentage.toFixed(1)}% (${this.actualRunTimeMinutes}/${this.plannedProductionTimeMinutes} min)`
  }
}