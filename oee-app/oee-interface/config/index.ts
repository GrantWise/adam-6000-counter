/**
 * Configuration Module
 * Central configuration loader that aggregates app and OEE configurations,
 * validates settings, and provides helper functions for common config access.
 */

import { appConfig, type AppConfig } from './app.config'
import { oeeConfig, type OeeConfig, getRating, requiresAttention } from './oee.config'
import { validateOeeConfig, validateOeeConfigOrThrow, type ConfigValidationResult } from './validation'

// Configuration validation
function validateConfig(): void {
  const errors: string[] = []

  // Validate channel configuration
  if (appConfig.device.channels.production === appConfig.device.channels.rejects) {
    errors.push('Production and rejects channels cannot be the same')
  }

  // Validate timing intervals
  if (appConfig.timing.polling.metrics < 1000) {
    console.warn('Metrics polling interval is very short (<1s), this may impact performance')
  }

  // Validate OEE thresholds are in correct order
  if (oeeConfig.thresholds.availability.excellent <= oeeConfig.thresholds.availability.good) {
    errors.push('Availability excellent threshold must be greater than good threshold')
  }
  if (oeeConfig.thresholds.performance.excellent <= oeeConfig.thresholds.performance.good) {
    errors.push('Performance excellent threshold must be greater than good threshold')
  }
  if (oeeConfig.thresholds.quality.excellent <= oeeConfig.thresholds.quality.good) {
    errors.push('Quality excellent threshold must be greater than good threshold')
  }
  if (oeeConfig.thresholds.oee.excellent <= oeeConfig.thresholds.oee.good) {
    errors.push('OEE excellent threshold must be greater than good threshold')
  }

  // Validate production rate limits
  if (oeeConfig.production.targetRate.min >= oeeConfig.production.targetRate.max) {
    errors.push('Minimum target rate must be less than maximum target rate')
  }

  // Log configuration in development
  if (process.env.NODE_ENV === 'development') {
    console.log('Configuration loaded:', {
      device: appConfig.device.defaultId,
      channels: appConfig.device.channels,
      pollingInterval: appConfig.timing.polling.metrics,
      oeeThresholds: oeeConfig.thresholds.oee,
    })
  }

  // Throw if critical errors found
  if (errors.length > 0) {
    throw new Error(`Configuration validation failed:\n${errors.join('\n')}`)
  }
}

// Unified configuration export
export const config = {
  app: appConfig,
  oee: oeeConfig,
} as const

// Type exports
export type Config = {
  app: AppConfig
  oee: OeeConfig
}

// Commonly used configuration accessors
export const channels = {
  production: () => config.app.device.channels.production,
  rejects: () => config.app.device.channels.rejects,
} as const

export const timing = {
  metricsPolling: () => config.app.timing.polling.metrics,
  chartUpdate: () => config.app.timing.polling.chartUpdate,
  apiTimeout: () => config.app.timing.timeouts.api,
  retryDelay: () => config.app.timing.retry.baseDelay,
  maxRetries: () => config.app.timing.retry.maxAttempts,
  dataInterval: () => config.app.timing.data.collectionInterval,
  stoppageThreshold: () => config.app.timing.data.stoppageThreshold,
} as const

export const thresholds = {
  oee: () => config.oee.thresholds.oee,
  availability: () => config.oee.thresholds.availability,
  performance: () => config.oee.thresholds.performance,
  quality: () => config.oee.thresholds.quality,
  targetRate: () => config.oee.production.targetRate,
} as const

// Re-export helper functions from oee.config and validation
export { getRating, requiresAttention }
export { validateOeeConfig, validateOeeConfigOrThrow }

// SQL query helpers
export const getChannelParams = () => ({
  productionChannel: channels.production(),
  rejectsChannel: channels.rejects(),
})

// Time calculation helpers
export const toMinutes = (milliseconds: number): number => milliseconds / 60000
export const toSeconds = (milliseconds: number): number => milliseconds / 1000
export const fromMinutes = (minutes: number): number => minutes * 60000
export const fromSeconds = (seconds: number): number => seconds * 1000

// Initialize and validate configuration on module load
try {
  validateConfig()
} catch (error) {
  console.error('Configuration validation failed:', error)
  // In production, you might want to handle this differently
  if (process.env.NODE_ENV === 'production') {
    // Could send to monitoring service
    console.error('Running with invalid configuration - this may cause issues')
  }
}