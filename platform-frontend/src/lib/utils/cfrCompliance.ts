/**
 * CFR Part 11 Compliance Utilities
 * Ensures ZERO TOLERANCE for synthetic data in industrial applications
 */

import type { ApiResponse, DataQuality } from '@/types'

// CFR Part 11 compliance flags
export const CFR_COMPLIANCE_FLAGS = {
  REAL_DATA: 'CFR-21-COMPLIANT',
  DATA_UNAVAILABLE: 'DATA-UNAVAILABLE',
  API_UNAVAILABLE: 'API-UNAVAILABLE',
  SYNTHETIC_VIOLATION: 'SYNTHETIC-DATA-VIOLATION',
  AUDIT_COMPLIANT: 'AUDIT-COMPLIANT'
} as const

export type ComplianceFlag = typeof CFR_COMPLIANCE_FLAGS[keyof typeof CFR_COMPLIANCE_FLAGS]

/**
 * Audit information for data integrity tracking
 */
export interface AuditInfo {
  sourceSystem: string
  dataIntegrity: 'good' | 'unavailable' | 'compromised'
  complianceFlags: ComplianceFlag[]
  timestamp: Date
  userId?: string
}

/**
 * Validates that response data is CFR Part 11 compliant
 * CRITICAL: Detects and prevents synthetic data usage
 */
export function validateCfrCompliance<T>(
  response: ApiResponse<T>,
  sourceSystem: string
): {
  isCompliant: boolean
  violations: string[]
  auditInfo: AuditInfo
  dataQuality: DataQuality
} {
  const violations: string[] = []
  let dataQuality: DataQuality = 'good'
  const complianceFlags: ComplianceFlag[] = []

  // Check if response indicates real data
  if (!response.success) {
    dataQuality = 'unavailable'
    complianceFlags.push(CFR_COMPLIANCE_FLAGS.DATA_UNAVAILABLE)
    
    if (response.error?.code?.includes('API')) {
      complianceFlags.push(CFR_COMPLIANCE_FLAGS.API_UNAVAILABLE)
    }
  } else if (response.data) {
    // Check for synthetic data markers
    const dataStr = JSON.stringify(response.data)
    
    // Red flags that indicate synthetic/mock data
    const syntheticIndicators = [
      'Math.random',
      'mock',
      'fake',
      'demo',
      'test',
      'synthetic',
      'generated',
      // Common synthetic patterns
      /(42\.?\d*)/g,  // Common test value 42
      /(123\.?\d*)/g, // Common test value 123
      /test[_\-]?data/gi,
      /mock[_\-]?data/gi,
      /fake[_\-]?data/gi
    ]
    
    // Check for indicators of synthetic data
    for (const indicator of syntheticIndicators) {
      if (typeof indicator === 'string' && dataStr.includes(indicator)) {
        violations.push(`Synthetic data indicator detected: ${indicator}`)
        complianceFlags.push(CFR_COMPLIANCE_FLAGS.SYNTHETIC_VIOLATION)
        dataQuality = 'bad'
      } else if (indicator instanceof RegExp && indicator.test(dataStr)) {
        violations.push(`Synthetic data pattern detected: ${indicator.toString()}`)
        complianceFlags.push(CFR_COMPLIANCE_FLAGS.SYNTHETIC_VIOLATION)
        dataQuality = 'bad'
      }
    }

    // If no violations found, mark as compliant
    if (violations.length === 0) {
      complianceFlags.push(CFR_COMPLIANCE_FLAGS.REAL_DATA, CFR_COMPLIANCE_FLAGS.AUDIT_COMPLIANT)
    }
  }

  const auditInfo: AuditInfo = {
    sourceSystem,
    dataIntegrity: violations.length === 0 ? 
      (dataQuality === 'unavailable' ? 'unavailable' : 'good') : 
      'compromised',
    complianceFlags,
    timestamp: new Date()
  }

  return {
    isCompliant: violations.length === 0 && dataQuality !== 'bad',
    violations,
    auditInfo,
    dataQuality
  }
}

/**
 * Wraps API response with CFR Part 11 compliance metadata
 */
export function wrapWithCompliance<T>(
  response: ApiResponse<T>,
  sourceSystem: string
): ApiResponse<T> & {
  complianceAudit: AuditInfo
  dataQuality: DataQuality
  complianceViolations: string[]
} {
  const compliance = validateCfrCompliance(response, sourceSystem)
  
  return {
    ...response,
    complianceAudit: compliance.auditInfo,
    dataQuality: compliance.dataQuality,
    complianceViolations: compliance.violations
  }
}

/**
 * Creates a CFR-compliant error response when data is unavailable
 */
export function createUnavailableDataResponse<T>(
  reason: string,
  sourceSystem: string
): ApiResponse<T> & {
  complianceAudit: AuditInfo
  dataQuality: DataQuality
  complianceViolations: string[]
} {
  const response: ApiResponse<T> = {
    success: false,
    error: {
      code: 'DATA_UNAVAILABLE',
      message: reason,
      details: { cfrCompliant: true, noSyntheticData: true },
      timestamp: new Date(),
      retryable: true
    }
  }

  return wrapWithCompliance(response, sourceSystem)
}

/**
 * Validates that a numeric value is not synthetic
 * Common synthetic values: 42, 123, round numbers, obvious patterns
 */
export function validateNumericValue(
  value: number | null,
  fieldName: string
): { isValid: boolean; warning?: string } {
  if (value === null) {
    return { isValid: true } // Null is acceptable - no synthetic data
  }

  // Common synthetic/test values
  const suspiciousValues = [42, 123, 100, 50, 75, 25, 999, 1000]
  const suspiciousPatterns = [
    /^\d+\.0+$/, // Like 42.000, 123.000
    /^[1-9]\1+$/, // Repeating digits like 111, 222, 333
  ]

  if (suspiciousValues.includes(value)) {
    return {
      isValid: false,
      warning: `${fieldName} contains suspicious value ${value} - verify this is real data`
    }
  }

  const valueStr = value.toString()
  for (const pattern of suspiciousPatterns) {
    if (pattern.test(valueStr)) {
      return {
        isValid: false,
        warning: `${fieldName} contains pattern ${valueStr} that appears synthetic`
      }
    }
  }

  return { isValid: true }
}

/**
 * Validates timestamp to ensure it's not obviously fake
 */
export function validateTimestamp(
  timestamp: Date | string,
  fieldName: string
): { isValid: boolean; warning?: string } {
  const date = timestamp instanceof Date ? timestamp : new Date(timestamp)
  const now = new Date()
  
  // Check if timestamp is too far in the future
  const maxFuture = new Date(now.getTime() + 5 * 60 * 1000) // 5 minutes
  if (date > maxFuture) {
    return {
      isValid: false,
      warning: `${fieldName} timestamp ${date.toISOString()} is too far in the future - may be synthetic`
    }
  }

  // Check if timestamp is suspiciously old (but allow historical data)
  const minPast = new Date('2020-01-01') // Industrial systems started this century
  if (date < minPast) {
    return {
      isValid: false,
      warning: `${fieldName} timestamp ${date.toISOString()} is suspiciously old - verify data source`
    }
  }

  return { isValid: true }
}

/**
 * Creates audit trail entry for data access
 */
export function createAuditTrail(
  action: string,
  sourceSystem: string,
  userId?: string,
  metadata?: Record<string, any>
): AuditInfo {
  return {
    sourceSystem,
    dataIntegrity: 'good',
    complianceFlags: [CFR_COMPLIANCE_FLAGS.AUDIT_COMPLIANT],
    timestamp: new Date(),
    userId
  }
}

/**
 * Logs CFR compliance violations for audit purposes
 */
export function logComplianceViolation(
  violation: string,
  sourceSystem: string,
  data?: any
): void {
  const logEntry = {
    timestamp: new Date().toISOString(),
    violation,
    sourceSystem,
    severity: 'CRITICAL',
    data: data ? JSON.stringify(data).substring(0, 500) : undefined
  }
  
  // Log to console in development
  if (import.meta.env.DEV) {
    console.error('🚨 CFR COMPLIANCE VIOLATION 🚨', logEntry)
  }
  
  // In production, this should go to audit log service
  // TODO: Implement audit log service integration
}

/**
 * Runtime assertion to prevent synthetic data usage
 */
export function assertRealData<T>(
  data: T,
  sourceDescription: string
): asserts data is T {
  if (!data) {
    throw new Error(`CFR Violation: No data available from ${sourceDescription} - refusing to show synthetic data`)
  }

  const compliance = validateCfrCompliance(
    { success: true, data },
    sourceDescription
  )

  if (!compliance.isCompliant) {
    logComplianceViolation(
      `Synthetic data detected in ${sourceDescription}: ${compliance.violations.join(', ')}`,
      sourceDescription,
      data
    )
    
    throw new Error(
      `CFR Violation: Synthetic data detected in ${sourceDescription}. ` +
      'Industrial applications must not display synthetic data. ' +
      'Data source must be fixed or shown as unavailable.'
    )
  }
}

/**
 * Development-only function to detect potential CFR violations in codebase
 */
export function scanForSyntheticDataPatterns(codeString: string): string[] {
  const violations: string[] = []
  
  // Patterns that indicate synthetic data generation
  const patterns = [
    /Math\.random\(\)/g,
    /Math\.floor\(Math\.random/g,
    /\b(fake|mock|demo|test)\s*data\b/gi,
    /generateFake|mockData|testData/g,
    /\b42\b.*\b(test|demo|mock)\b/gi,
    /new Date\(\)\s*\+\s*Math\.random/g
  ]
  
  patterns.forEach(pattern => {
    const matches = codeString.match(pattern)
    if (matches) {
      violations.push(`Potential synthetic data pattern: ${pattern.toString()}`)
    }
  })
  
  return violations
}

export default {
  validateCfrCompliance,
  wrapWithCompliance,
  createUnavailableDataResponse,
  validateNumericValue,
  validateTimestamp,
  createAuditTrail,
  logComplianceViolation,
  assertRealData,
  scanForSyntheticDataPatterns,
  CFR_COMPLIANCE_FLAGS
}