/**
 * Centralized error handling utilities for the OEE application
 * Provides consistent error handling patterns and logging
 */

export interface AppError {
  code: string
  message: string
  details?: any
  timestamp: Date
  severity: 'low' | 'medium' | 'high' | 'critical'
}

export interface RetryOptions {
  maxAttempts: number
  baseDelay: number // milliseconds
  maxDelay: number // milliseconds
  exponential?: boolean
}

export class ErrorHandler {
  private static logError(error: AppError): void {
    const logEntry = {
      ...error,
      userAgent: typeof window !== 'undefined' ? navigator.userAgent : 'server',
      url: typeof window !== 'undefined' ? window.location.href : 'server'
    }

    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.error('Application Error:', logEntry)
    }

    // In production, you might send to a logging service
    // Example: sendToLoggingService(logEntry)
  }

  /**
   * Handle database connection errors
   */
  static handleDatabaseError(error: any): AppError {
    const appError: AppError = {
      code: 'DATABASE_ERROR',
      message: 'Database connection failed',
      details: error,
      timestamp: new Date(),
      severity: 'high'
    }

    if (error?.code === 'ECONNREFUSED') {
      appError.message = 'Unable to connect to database server'
      appError.severity = 'critical'
    } else if (error?.code === 'ENOTFOUND') {
      appError.message = 'Database host not found'
      appError.severity = 'critical'
    } else if (error?.message?.includes('timeout')) {
      appError.message = 'Database query timeout'
      appError.severity = 'medium'
    }

    this.logError(appError)
    return appError
  }

  /**
   * Handle API request errors
   */
  static handleApiError(error: any, endpoint: string): AppError {
    const appError: AppError = {
      code: 'API_ERROR',
      message: `API request failed: ${endpoint}`,
      details: error,
      timestamp: new Date(),
      severity: 'medium'
    }

    if (error?.status === 404) {
      appError.message = 'Resource not found'
      appError.severity = 'low'
    } else if (error?.status === 401) {
      appError.message = 'Authentication required'
      appError.severity = 'high'
    } else if (error?.status === 403) {
      appError.message = 'Access denied'
      appError.severity = 'high'
    } else if (error?.status === 500) {
      appError.message = 'Internal server error'
      appError.severity = 'critical'
    } else if (error?.status === 503) {
      appError.message = 'Service temporarily unavailable'
      appError.severity = 'high'
    } else if (!navigator.onLine) {
      appError.message = 'No internet connection'
      appError.code = 'NETWORK_ERROR'
      appError.severity = 'high'
    }

    this.logError(appError)
    return appError
  }

  /**
   * Handle OEE calculation errors
   */
  static handleCalculationError(error: any, deviceId: string): AppError {
    const appError: AppError = {
      code: 'CALCULATION_ERROR',
      message: `OEE calculation failed for device ${deviceId}`,
      details: error,
      timestamp: new Date(),
      severity: 'medium'
    }

    if (error?.message?.includes('insufficient data')) {
      appError.message = 'Insufficient data for OEE calculation'
      appError.severity = 'low'
    } else if (error?.message?.includes('invalid rate')) {
      appError.message = 'Invalid production rate data detected'
      appError.severity = 'medium'
    }

    this.logError(appError)
    return appError
  }

  /**
   * Handle job management errors
   */
  static handleJobError(error: any, operation: string): AppError {
    const appError: AppError = {
      code: 'JOB_ERROR',
      message: `Job ${operation} failed`,
      details: error,
      timestamp: new Date(),
      severity: 'medium'
    }

    if (error?.message?.includes('validation')) {
      appError.message = 'Job data validation failed'
      appError.severity = 'low'
    } else if (error?.message?.includes('concurrent')) {
      appError.message = 'Another job is already active'
      appError.severity = 'medium'
    }

    this.logError(appError)
    return appError
  }
}

/**
 * Retry utility with exponential backoff
 */
export async function withRetry<T>(
  operation: () => Promise<T>,
  options: RetryOptions = {
    maxAttempts: 3,
    baseDelay: 1000,
    maxDelay: 10000,
    exponential: true
  }
): Promise<T> {
  let lastError: any
  
  for (let attempt = 1; attempt <= options.maxAttempts; attempt++) {
    try {
      return await operation()
    } catch (error) {
      lastError = error
      
      if (attempt === options.maxAttempts) {
        throw error
      }

      // Calculate delay
      let delay = options.exponential 
        ? options.baseDelay * Math.pow(2, attempt - 1)
        : options.baseDelay

      delay = Math.min(delay, options.maxDelay)
      
      // Add jitter to prevent thundering herd
      delay = delay + (Math.random() * delay * 0.1)

      console.warn(`Operation failed (attempt ${attempt}/${options.maxAttempts}), retrying in ${delay}ms...`, error)
      
      await new Promise(resolve => setTimeout(resolve, delay))
    }
  }

  throw lastError
}

/**
 * Circuit breaker pattern implementation
 */
export class CircuitBreaker {
  private failures = 0
  private lastFailureTime = 0
  private state: 'closed' | 'open' | 'half-open' = 'closed'

  constructor(
    private readonly failureThreshold: number = 5,
    private readonly recoveryTimeout: number = 30000, // 30 seconds
    private readonly monitoringPeriod: number = 60000 // 1 minute
  ) {}

  async execute<T>(operation: () => Promise<T>): Promise<T> {
    if (this.state === 'open') {
      if (Date.now() - this.lastFailureTime > this.recoveryTimeout) {
        this.state = 'half-open'
      } else {
        throw new Error('Circuit breaker is open - service temporarily unavailable')
      }
    }

    try {
      const result = await operation()
      
      // Success resets the circuit breaker
      if (this.state === 'half-open') {
        this.state = 'closed'
        this.failures = 0
      }
      
      return result
    } catch (error) {
      this.failures++
      this.lastFailureTime = Date.now()

      if (this.failures >= this.failureThreshold) {
        this.state = 'open'
        console.error(`Circuit breaker opened due to ${this.failures} failures`)
      }

      throw error
    }
  }

  getState(): string {
    return this.state
  }

  reset(): void {
    this.state = 'closed'
    this.failures = 0
    this.lastFailureTime = 0
  }
}

/**
 * Timeout wrapper for operations
 */
export async function withTimeout<T>(
  operation: Promise<T>,
  timeoutMs: number,
  errorMessage = 'Operation timed out'
): Promise<T> {
  const timeoutPromise = new Promise<never>((_, reject) => {
    setTimeout(() => reject(new Error(errorMessage)), timeoutMs)
  })

  return Promise.race([operation, timeoutPromise])
}

/**
 * Safe async operation wrapper
 */
export async function safeAsync<T>(
  operation: () => Promise<T>,
  fallback: T,
  errorHandler?: (error: any) => void
): Promise<T> {
  try {
    return await operation()
  } catch (error) {
    if (errorHandler) {
      errorHandler(error)
    } else {
      console.error('Safe async operation failed:', error)
    }
    return fallback
  }
}