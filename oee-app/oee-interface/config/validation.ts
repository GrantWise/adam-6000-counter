/**
 * Configuration Validation
 * 
 * Validates OEE configuration values to ensure they are within acceptable ranges
 * and follow business logic constraints.
 */

import { oeeConfig, type OeeConfig } from './oee.config'

export interface ConfigValidationError {
  field: string
  value: number
  message: string
  severity: 'error' | 'warning'
}

export interface ConfigValidationResult {
  isValid: boolean
  errors: ConfigValidationError[]
  warnings: ConfigValidationError[]
}

/**
 * Validate threshold values are within acceptable ranges and follow logical order
 */
function validateThresholds(config: OeeConfig): ConfigValidationError[] {
  const errors: ConfigValidationError[] = []

  // Helper function to validate threshold group
  const validateThresholdGroup = (
    group: string,
    excellent: number,
    good: number,
    needsImprovement: number
  ) => {
    // Check ranges (0-100 for percentages)
    if (excellent < 0 || excellent > 100) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `Excellent threshold must be between 0 and 100, got ${excellent}`,
        severity: 'error'
      })
    }

    if (good < 0 || good > 100) {
      errors.push({
        field: `thresholds.${group}.good`,
        value: good,
        message: `Good threshold must be between 0 and 100, got ${good}`,
        severity: 'error'
      })
    }

    // Check logical order: excellent >= good >= needsImprovement
    if (excellent < good) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `Excellent threshold (${excellent}) should be >= good threshold (${good})`,
        severity: 'error'
      })
    }

    if (good < needsImprovement) {
      errors.push({
        field: `thresholds.${group}.good`,
        value: good,
        message: `Good threshold (${good}) should be >= needs improvement threshold (${needsImprovement})`,
        severity: 'error'
      })
    }

    // Industry standard warnings
    if (group === 'oee' && excellent < 85) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `OEE excellent threshold (${excellent}) is below industry world-class standard (85%)`,
        severity: 'warning'
      })
    }

    if (group === 'availability' && excellent < 90) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `Availability excellent threshold (${excellent}) is below industry standard (90%)`,
        severity: 'warning'
      })
    }

    if (group === 'performance' && excellent < 95) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `Performance excellent threshold (${excellent}) is below industry standard (95%)`,
        severity: 'warning'
      })
    }

    if (group === 'quality' && excellent < 99) {
      errors.push({
        field: `thresholds.${group}.excellent`,
        value: excellent,
        message: `Quality excellent threshold (${excellent}) is below industry standard (99%)`,
        severity: 'warning'
      })
    }
  }

  // Validate each threshold group
  validateThresholdGroup(
    'availability',
    config.thresholds.availability.excellent,
    config.thresholds.availability.good,
    config.thresholds.availability.needsImprovement
  )

  validateThresholdGroup(
    'performance',
    config.thresholds.performance.excellent,
    config.thresholds.performance.good,
    config.thresholds.performance.needsImprovement
  )

  validateThresholdGroup(
    'quality',
    config.thresholds.quality.excellent,
    config.thresholds.quality.good,
    config.thresholds.quality.needsImprovement
  )

  validateThresholdGroup(
    'oee',
    config.thresholds.oee.excellent,
    config.thresholds.oee.good,
    config.thresholds.oee.needsImprovement
  )

  return errors
}

/**
 * Validate production configuration values
 */
function validateProduction(config: OeeConfig): ConfigValidationError[] {
  const errors: ConfigValidationError[] = []

  // Target rate validation
  if (config.production.targetRate.min <= 0) {
    errors.push({
      field: 'production.targetRate.min',
      value: config.production.targetRate.min,
      message: `Minimum target rate must be positive, got ${config.production.targetRate.min}`,
      severity: 'error'
    })
  }

  if (config.production.targetRate.max <= config.production.targetRate.min) {
    errors.push({
      field: 'production.targetRate.max',
      value: config.production.targetRate.max,
      message: `Maximum target rate (${config.production.targetRate.max}) must be greater than minimum (${config.production.targetRate.min})`,
      severity: 'error'
    })
  }

  if (config.production.targetRate.validationMax <= config.production.targetRate.max) {
    errors.push({
      field: 'production.targetRate.validationMax',
      value: config.production.targetRate.validationMax,
      message: `Validation maximum (${config.production.targetRate.validationMax}) should be greater than operational maximum (${config.production.targetRate.max})`,
      severity: 'warning'
    })
  }

  // Default values validation
  if (config.production.defaults.quantity <= 0) {
    errors.push({
      field: 'production.defaults.quantity',
      value: config.production.defaults.quantity,
      message: `Default quantity must be positive, got ${config.production.defaults.quantity}`,
      severity: 'error'
    })
  }

  if (config.production.defaults.targetRate < config.production.targetRate.min ||
      config.production.defaults.targetRate > config.production.targetRate.max) {
    errors.push({
      field: 'production.defaults.targetRate',
      value: config.production.defaults.targetRate,
      message: `Default target rate (${config.production.defaults.targetRate}) must be between min (${config.production.targetRate.min}) and max (${config.production.targetRate.max})`,
      severity: 'error'
    })
  }

  return errors
}

/**
 * Validate time range configuration
 */
function validateTimeRanges(config: OeeConfig): ConfigValidationError[] {
  const errors: ConfigValidationError[] = []

  // Shift duration validation
  if (config.timeRanges.shiftDuration <= 0) {
    errors.push({
      field: 'timeRanges.shiftDuration',
      value: config.timeRanges.shiftDuration,
      message: `Shift duration must be positive, got ${config.timeRanges.shiftDuration}`,
      severity: 'error'
    })
  }

  if (config.timeRanges.shiftDuration > 24) {
    errors.push({
      field: 'timeRanges.shiftDuration',
      value: config.timeRanges.shiftDuration,
      message: `Shift duration (${config.timeRanges.shiftDuration}) exceeds 24 hours`,
      severity: 'warning'
    })
  }

  // Query range validation
  if (config.timeRanges.maxQueryRange.days <= 0) {
    errors.push({
      field: 'timeRanges.maxQueryRange.days',
      value: config.timeRanges.maxQueryRange.days,
      message: `Maximum query range must be positive, got ${config.timeRanges.maxQueryRange.days}`,
      severity: 'error'
    })
  }

  if (config.timeRanges.maxQueryRange.days > 365) {
    errors.push({
      field: 'timeRanges.maxQueryRange.days',
      value: config.timeRanges.maxQueryRange.days,
      message: `Maximum query range (${config.timeRanges.maxQueryRange.days} days) exceeds 1 year`,
      severity: 'warning'
    })
  }

  return errors
}

/**
 * Validate stoppage configuration
 */
function validateStoppages(config: OeeConfig): ConfigValidationError[] {
  const errors: ConfigValidationError[] = []

  // Minimum duration validation
  if (config.stoppages.detection.minDurationSeconds <= 0) {
    errors.push({
      field: 'stoppages.detection.minDurationSeconds',
      value: config.stoppages.detection.minDurationSeconds,
      message: `Minimum stoppage duration must be positive, got ${config.stoppages.detection.minDurationSeconds}`,
      severity: 'error'
    })
  }

  if (config.stoppages.detection.minDurationSeconds > 300) { // 5 minutes
    errors.push({
      field: 'stoppages.detection.minDurationSeconds',
      value: config.stoppages.detection.minDurationSeconds,
      message: `Minimum stoppage duration (${config.stoppages.detection.minDurationSeconds}s) is very high, may miss important events`,
      severity: 'warning'
    })
  }

  // Categories validation
  if (config.stoppages.categories.length === 0) {
    errors.push({
      field: 'stoppages.categories',
      value: config.stoppages.categories.length,
      message: 'At least one stoppage category must be defined',
      severity: 'error'
    })
  }

  return errors
}

/**
 * Validate calculation parameters
 */
function validateCalculations(config: OeeConfig): ConfigValidationError[] {
  const errors: ConfigValidationError[] = []

  if (config.calculations.minimumDataPoints <= 0) {
    errors.push({
      field: 'calculations.minimumDataPoints',
      value: config.calculations.minimumDataPoints,
      message: `Minimum data points must be positive, got ${config.calculations.minimumDataPoints}`,
      severity: 'error'
    })
  }

  if (config.calculations.minimumDataPoints < 5) {
    errors.push({
      field: 'calculations.minimumDataPoints',
      value: config.calculations.minimumDataPoints,
      message: `Minimum data points (${config.calculations.minimumDataPoints}) is very low, may produce unreliable calculations`,
      severity: 'warning'
    })
  }

  // Precision validation
  if (config.calculations.precision.rate < 0 || config.calculations.precision.rate > 5) {
    errors.push({
      field: 'calculations.precision.rate',
      value: config.calculations.precision.rate,
      message: `Rate precision should be between 0 and 5 decimal places, got ${config.calculations.precision.rate}`,
      severity: 'warning'
    })
  }

  return errors
}

/**
 * Validate the entire OEE configuration
 */
export function validateOeeConfig(config: OeeConfig = oeeConfig): ConfigValidationResult {
  const allErrors: ConfigValidationError[] = [
    ...validateThresholds(config),
    ...validateProduction(config),
    ...validateTimeRanges(config),
    ...validateStoppages(config),
    ...validateCalculations(config)
  ]

  const errors = allErrors.filter(e => e.severity === 'error')
  const warnings = allErrors.filter(e => e.severity === 'warning')

  return {
    isValid: errors.length === 0,
    errors,
    warnings
  }
}

/**
 * Validate configuration and throw if invalid
 */
export function validateOeeConfigOrThrow(config: OeeConfig = oeeConfig): void {
  const result = validateOeeConfig(config)
  
  if (!result.isValid) {
    const errorMessages = result.errors.map(e => `${e.field}: ${e.message}`).join('\n')
    throw new Error(`OEE Configuration validation failed:\n${errorMessages}`)
  }

  // Log warnings if any
  if (result.warnings.length > 0) {
    console.warn('OEE Configuration warnings:')
    result.warnings.forEach(w => {
      console.warn(`  ${w.field}: ${w.message}`)
    })
  }
}

// Validate on module load
validateOeeConfigOrThrow()