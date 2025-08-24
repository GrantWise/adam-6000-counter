/**
 * Runtime validation utilities for API responses
 * Provides basic type checking and validation for critical data structures
 */

/**
 * Validate if a value is a valid ServiceStatus object
 */
export function isServiceStatus(value: any): value is ServiceStatus {
  return (
    value &&
    typeof value === 'object' &&
    typeof value.serviceName === 'string' &&
    typeof value.status === 'string' &&
    ['healthy', 'warning', 'error', 'offline'].includes(value.status) &&
    (value.lastCheck === undefined || value.lastCheck instanceof Date || typeof value.lastCheck === 'string') &&
    (value.responseTime === undefined || typeof value.responseTime === 'number')
  )
}

/**
 * Validate if a value is a valid SystemMetrics object
 */
export function isSystemMetrics(value: any): value is SystemMetrics {
  return (
    value &&
    typeof value === 'object' &&
    value.cpu &&
    typeof value.cpu.usage === 'number' &&
    value.memory &&
    typeof value.memory.used === 'number' &&
    typeof value.memory.total === 'number'
  )
}

/**
 * Validate if a value is a valid DatabaseHealth object
 */
export function isDatabaseHealth(value: any): value is DatabaseHealth {
  return (
    value &&
    typeof value === 'object' &&
    typeof value.connected === 'boolean' &&
    (value.responseTime === undefined || typeof value.responseTime === 'number')
  )
}

/**
 * Validate if a value is a valid SystemAlert object
 */
export function isSystemAlert(value: any): value is SystemAlert {
  return (
    value &&
    typeof value === 'object' &&
    typeof value.id === 'string' &&
    typeof value.title === 'string' &&
    typeof value.severity === 'string' &&
    ['low', 'medium', 'high', 'critical'].includes(value.severity)
  )
}

/**
 * Validate API response structure
 */
export function isValidApiResponse<T>(
  value: any,
  validator?: (data: any) => data is T
): value is ApiResponse<T> {
  if (!value || typeof value !== 'object') {
    return false
  }

  if (typeof value.success !== 'boolean') {
    return false
  }

  if (value.success) {
    // For successful responses, validate data if validator provided
    return validator ? validator(value.data) : true
  } else {
    // For failed responses, check error structure
    return (
      value.error &&
      typeof value.error === 'object' &&
      typeof value.error.code === 'string' &&
      typeof value.error.message === 'string'
    )
  }
}

/**
 * Safe API response processor with validation
 */
export function processApiResponse<T>(
  response: any,
  validator?: (data: any) => data is T,
  fallback?: T
): ApiResponse<T> {
  try {
    if (isValidApiResponse(response, validator)) {
      return response
    }
    
    // Invalid response structure, create error response
    return {
      success: false,
      error: {
        code: 'INVALID_RESPONSE',
        message: 'Invalid API response format',
        timestamp: new Date(),
        details: { originalResponse: response }
      }
    }
  } catch (error) {
    return {
      success: false,
      error: {
        code: 'VALIDATION_ERROR',
        message: 'Response validation failed',
        timestamp: new Date(),
        details: { error: error instanceof Error ? error.message : 'Unknown error' }
      }
    }
  }
}

/**
 * Array validator helper
 */
export function isArrayOf<T>(
  value: any,
  itemValidator: (item: any) => item is T
): value is T[] {
  return Array.isArray(value) && value.every(itemValidator)
}

// Re-export types for convenience
import type { ServiceStatus, SystemMetrics, DatabaseHealth, SystemAlert, ApiResponse } from '@/types'
export type { ServiceStatus, SystemMetrics, DatabaseHealth, SystemAlert, ApiResponse }