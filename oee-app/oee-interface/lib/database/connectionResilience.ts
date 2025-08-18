/**
 * Database connection resilience utilities
 * Provides enhanced retry logic and circuit breaker pattern for database operations
 * Uses centralized utilities from errorHandler.ts
 */

import { withRetry as coreWithRetry, RetryOptions as CoreRetryOptions, CircuitBreaker } from '@/lib/utils/errorHandler';

// Re-export the retry options interface with database-specific defaults
export interface DatabaseRetryOptions extends CoreRetryOptions {
  maxAttempts: number;
  baseDelay: number;
  maxDelay: number;
  exponential?: boolean;
}

const DEFAULT_DATABASE_RETRY_OPTIONS: DatabaseRetryOptions = {
  maxAttempts: 3,
  baseDelay: 1000,
  maxDelay: 10000,
  exponential: true,
};

/**
 * Enhanced retry function for database operations using centralized withRetry
 * Includes database-specific error classification
 */
export async function withRetry<T>(
  operation: () => Promise<T>,
  options: Partial<DatabaseRetryOptions> = {}
): Promise<T> {
  const config = { ...DEFAULT_DATABASE_RETRY_OPTIONS, ...options };
  
  return coreWithRetry(async () => {
    try {
      return await operation();
    } catch (error) {
      const dbError = error instanceof Error ? error : new Error('Unknown database error');
      
      // Don't retry on certain types of errors (e.g., syntax errors, constraint violations)
      if (isNonRetryableError(dbError)) {
        throw dbError;
      }
      
      throw dbError;
    }
  }, config);
}

/**
 * Check if an error is non-retryable (e.g., syntax error, constraint violation)
 */
function isNonRetryableError(error: Error): boolean {
  const message = error.message.toLowerCase();
  
  // PostgreSQL error codes that shouldn't be retried
  const nonRetryablePatterns = [
    'syntax error',
    'column does not exist',
    'table does not exist',
    'duplicate key value',
    'foreign key constraint',
    'check constraint',
    'not null violation',
    'unique constraint',
  ];
  
  return nonRetryablePatterns.some(pattern => message.includes(pattern));
}

// Global circuit breaker instance for database operations
// Configured for industrial environment with appropriate thresholds
export const dbCircuitBreaker = new CircuitBreaker(5, 30000); // 5 failures, 30 second timeout

/**
 * Execute database operation with circuit breaker and retry
 * Combines circuit breaker pattern with retry logic for maximum resilience
 */
export async function executeWithResilience<T>(
  operation: () => Promise<T>,
  retryOptions?: Partial<DatabaseRetryOptions>
): Promise<T> {
  return dbCircuitBreaker.execute(() => withRetry(operation, retryOptions));
}

/**
 * Connection pool health checker
 * Tests database connectivity with circuit breaker protection
 */
export async function checkConnectionHealth(): Promise<{
  isHealthy: boolean;
  latencyMs?: number;
  circuitBreakerState: string;
  error?: string;
}> {
  const startTime = Date.now();
  
  try {
    await dbCircuitBreaker.execute(async () => {
      // Import here to avoid circular dependency
      const { queryDatabase } = await import('./connection');
      await queryDatabase('SELECT 1 as health_check');
    });
    
    const latencyMs = Date.now() - startTime;
    return {
      isHealthy: true,
      latencyMs,
      circuitBreakerState: dbCircuitBreaker.getState()
    };
  } catch (error) {
    return {
      isHealthy: false,
      circuitBreakerState: dbCircuitBreaker.getState(),
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

/**
 * Advanced retry configuration for critical operations
 */
export const CRITICAL_OPERATION_RETRY_CONFIG: DatabaseRetryOptions = {
  maxAttempts: 5,
  baseDelay: 2000,
  maxDelay: 30000,
  exponential: true,
};

/**
 * Retry configuration for read operations (more aggressive)
 */
export const READ_OPERATION_RETRY_CONFIG: DatabaseRetryOptions = {
  maxAttempts: 4,
  baseDelay: 500,
  maxDelay: 5000,
  exponential: true,
};

/**
 * Retry configuration for write operations (more conservative)
 */
export const WRITE_OPERATION_RETRY_CONFIG: DatabaseRetryOptions = {
  maxAttempts: 2,
  baseDelay: 1000,
  maxDelay: 10000,
  exponential: true,
};

/**
 * Circuit breaker for external API calls
 * Separate from database circuit breaker with different thresholds
 */
export const apiCircuitBreaker = new CircuitBreaker(3, 60000); // 3 failures, 60 second timeout

/**
 * Execute external API operation with circuit breaker and retry
 * Use this for any HTTP calls to external services
 */
export async function executeApiWithResilience<T>(
  operation: () => Promise<T>,
  retryOptions?: Partial<DatabaseRetryOptions>
): Promise<T> {
  return apiCircuitBreaker.execute(() => withRetry(operation, {
    ...READ_OPERATION_RETRY_CONFIG,
    ...retryOptions
  }));
}

/**
 * Get API circuit breaker status for monitoring
 */
export function getApiCircuitBreakerStatus(): {
  state: string;
  isHealthy: boolean;
} {
  const state = apiCircuitBreaker.getState();
  return {
    state,
    isHealthy: state === 'closed'
  };
}