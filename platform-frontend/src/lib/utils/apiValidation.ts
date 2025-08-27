/**
 * API Response Validation Utilities
 * Provides runtime type checking for API responses to prevent integration failures
 */

export interface ValidationResult {
  isValid: boolean
  errors: string[]
  data?: any
}

/**
 * Validate authentication login response
 */
export function validateLoginResponse(data: any): ValidationResult {
  const errors: string[] = []
  
  if (!data || typeof data !== 'object') {
    errors.push('Response data is not an object')
    return { isValid: false, errors }
  }

  // Check for success response format
  if ('success' in data) {
    if (typeof data.success !== 'boolean') {
      errors.push('success field must be boolean')
    }
    
    if (data.success && !data.user) {
      errors.push('user field is required for successful login')
    }
    
    if (data.success && !data.accessToken) {
      errors.push('accessToken field is required for successful login')
    }
  } 
  // Check for direct response format (backend returns auth data directly)
  else {
    if (!data.accessToken) {
      errors.push('accessToken field is required')
    }
    
    if (!data.user) {
      errors.push('user field is required')
    }
    
    if (data.user) {
      if (!data.user.userId && !data.user.id) {
        errors.push('user.userId or user.id is required')
      }
      
      if (!data.user.username) {
        errors.push('user.username is required')
      }
    }
  }
  
  return {
    isValid: errors.length === 0,
    errors,
    data: errors.length === 0 ? data : undefined
  }
}

/**
 * Validate paginated response structure
 */
export function validatePaginatedResponse(data: any): ValidationResult {
  const errors: string[] = []
  
  if (!data || typeof data !== 'object') {
    errors.push('Response data is not an object')
    return { isValid: false, errors }
  }
  
  if (!Array.isArray(data.items) && !Array.isArray(data.data)) {
    errors.push('Response must contain items or data array')
  }
  
  if (typeof data.totalCount !== 'number' && typeof data.total !== 'number') {
    errors.push('Response must contain totalCount or total number')
  }
  
  if (typeof data.pageSize !== 'number' && typeof data.limit !== 'number') {
    errors.push('Response must contain pageSize or limit number')
  }
  
  if (typeof data.currentPage !== 'number' && typeof data.page !== 'number') {
    errors.push('Response must contain currentPage or page number')
  }
  
  return {
    isValid: errors.length === 0,
    errors,
    data: errors.length === 0 ? data : undefined
  }
}

/**
 * Validate security audit log entry
 */
export function validateAuditLogEntry(data: any): ValidationResult {
  const errors: string[] = []
  
  if (!data || typeof data !== 'object') {
    errors.push('Audit log entry must be an object')
    return { isValid: false, errors }
  }
  
  if (!data.id) {
    errors.push('id field is required')
  }
  
  if (!data.userId) {
    errors.push('userId field is required')
  }
  
  if (!data.action) {
    errors.push('action field is required')
  }
  
  if (!data.timestamp) {
    errors.push('timestamp field is required')
  }
  
  if (data.timestamp && !Date.parse(data.timestamp)) {
    errors.push('timestamp must be a valid date string')
  }
  
  return {
    isValid: errors.length === 0,
    errors,
    data: errors.length === 0 ? data : undefined
  }
}

/**
 * Validate security alert
 */
export function validateSecurityAlert(data: any): ValidationResult {
  const errors: string[] = []
  
  if (!data || typeof data !== 'object') {
    errors.push('Security alert must be an object')
    return { isValid: false, errors }
  }
  
  if (!data.id) {
    errors.push('id field is required')
  }
  
  if (!data.type) {
    errors.push('type field is required')
  }
  
  if (!data.severity) {
    errors.push('severity field is required')
  }
  
  const validSeverities = ['low', 'medium', 'high', 'critical']
  if (data.severity && !validSeverities.includes(data.severity)) {
    errors.push(`severity must be one of: ${validSeverities.join(', ')}`)
  }
  
  if (!data.message) {
    errors.push('message field is required')
  }
  
  if (!data.timestamp) {
    errors.push('timestamp field is required')
  }
  
  return {
    isValid: errors.length === 0,
    errors,
    data: errors.length === 0 ? data : undefined
  }
}

/**
 * Safe API response processor with validation
 */
export function processApiResponse<T>(
  data: any,
  validator?: (data: any) => ValidationResult,
  fallbackData?: T
): { data: T | undefined; errors: string[]; isValid: boolean } {
  try {
    if (validator) {
      const validation = validator(data)
      if (!validation.isValid) {
        console.warn('API Response validation failed:', validation.errors)
        return {
          data: fallbackData,
          errors: validation.errors,
          isValid: false
        }
      }
      return {
        data: validation.data as T,
        errors: [],
        isValid: true
      }
    }
    
    return {
      data: data as T,
      errors: [],
      isValid: true
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown validation error'
    console.error('API Response processing error:', errorMessage)
    return {
      data: fallbackData,
      errors: [errorMessage],
      isValid: false
    }
  }
}

/**
 * Create a type-safe API response wrapper
 */
export function createValidatedApiResponse<T>(
  response: any,
  validator?: (data: any) => ValidationResult,
  fallbackData?: T
) {
  if (response.success) {
    return processApiResponse<T>(response.data, validator, fallbackData)
  } else {
    return {
      data: fallbackData,
      errors: [response.error?.message || 'API request failed'],
      isValid: false
    }
  }
}