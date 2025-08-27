import { apiClient } from '@/lib/api/client'
import type { ApiResponse, ComplianceMetrics } from '@/types'

/**
 * Compliance Service - CFR Part 11 compliance reporting and utilities
 * Handles regulatory compliance requirements for pharmaceutical and industrial environments
 */
class ComplianceService {
  private readonly baseUrl = '/api/admin/compliance'

  /**
   * Get comprehensive CFR Part 11 compliance report
   */
  async getCFRPart11Report(): Promise<ApiResponse<{
    overallScore: number
    lastAssessment: Date
    requirements: {
      requirement: string
      description: string
      status: 'compliant' | 'partial' | 'non_compliant'
      score: number
      lastChecked: Date
      evidence: string[]
      recommendations?: string[]
    }[]
    auditTrail: {
      totalEntries: number
      oldestEntry: Date
      newestEntry: Date
      retentionDays: number
      integrityVerified: boolean
    }
    electronicSignatures: {
      enabled: boolean
      totalSignatures: number
      verifiedSignatures: number
      integrityViolations: number
    }
    dataIntegrity: {
      checksPerformed: number
      integrityViolations: number
      lastCheck: Date
      status: 'verified' | 'warning' | 'critical'
    }
    userManagement: {
      totalUsers: number
      activeUsers: number
      disabledUsers: number
      passwordCompliance: number
      multiFactorEnabled: number
    }
    accessControl: {
      roleBasedAccess: boolean
      privilegeEscalations: number
      unauthorizedAttempts: number
      sessionManagement: 'compliant' | 'partial' | 'non_compliant'
    }
  }>> {
    return apiClient.get(`${this.baseUrl}/cfr-part-11`)
  }

  /**
   * Get data integrity check results
   */
  async getDataIntegrityReport(): Promise<ApiResponse<{
    overallStatus: 'verified' | 'warning' | 'critical'
    lastCheck: Date
    checksPerformed: number
    violationsFound: number
    checks: {
      checkType: string
      description: string
      status: 'passed' | 'warning' | 'failed'
      lastRun: Date
      details?: string
      affectedRecords?: number
    }[]
    recommendations: string[]
  }>> {
    return apiClient.get(`${this.baseUrl}/data-integrity`)
  }

  /**
   * Get electronic signature compliance status
   */
  async getElectronicSignatureCompliance(): Promise<ApiResponse<{
    enabled: boolean
    totalSignatures: number
    verifiedSignatures: number
    integrityViolations: number
    signingAlgorithm: string
    certificateAuthority: string
    expirationWarnings: number
    recentSignatures: {
      id: string
      userId: string
      username: string
      documentType: string
      timestamp: Date
      verified: boolean
      reason?: string
    }[]
  }>> {
    return apiClient.get(`${this.baseUrl}/electronic-signatures`)
  }

  /**
   * Get audit trail compliance metrics
   */
  async getAuditTrailCompliance(): Promise<ApiResponse<{
    totalEntries: number
    oldestEntry: Date
    newestEntry: Date
    retentionPolicy: {
      retentionDays: number
      automaticPurge: boolean
      nextPurgeDate?: Date
    }
    integrityVerification: {
      enabled: boolean
      lastVerification: Date
      hashAlgorithm: string
      violationsDetected: number
    }
    coverage: {
      userActions: number
      dataChanges: number
      systemEvents: number
      securityEvents: number
    }
    gaps: {
      type: string
      description: string
      startTime: Date
      endTime: Date
      reason: string
    }[]
  }>> {
    return apiClient.get(`${this.baseUrl}/audit-trail`)
  }

  /**
   * Generate compliance report in PDF format
   */
  async generateComplianceReport(reportType: 'cfr_part_11' | 'data_integrity' | 'full' = 'cfr_part_11'): Promise<ApiResponse<Blob>> {
    return apiClient.post(`${this.baseUrl}/reports/generate`, {
      reportType,
      format: 'pdf',
      includeEvidence: true,
      includeRecommendations: true
    }, {
      responseType: 'blob'
    })
  }

  /**
   * Get compliance validation errors and warnings
   */
  async getValidationIssues(severity?: 'low' | 'medium' | 'high' | 'critical'): Promise<ApiResponse<{
    id: string
    severity: 'low' | 'medium' | 'high' | 'critical'
    category: 'audit_trail' | 'data_integrity' | 'access_control' | 'electronic_signatures'
    issue: string
    description: string
    affectedSystems: string[]
    recommendation: string
    detectedAt: Date
    resolved: boolean
    resolvedAt?: Date
    resolvedBy?: string
  }[]>> {
    return apiClient.get(`${this.baseUrl}/validation-issues`, {
      params: severity ? { severity } : undefined
    })
  }

  /**
   * Mark compliance issue as resolved
   */
  async resolveValidationIssue(issueId: string, resolution: string): Promise<ApiResponse<void>> {
    return apiClient.post(`${this.baseUrl}/validation-issues/${issueId}/resolve`, {
      resolution
    })
  }

  /**
   * Run data integrity check
   */
  async runDataIntegrityCheck(checkType?: 'full' | 'incremental'): Promise<ApiResponse<{
    checkId: string
    status: 'running' | 'completed' | 'failed'
    startTime: Date
    estimatedDuration?: number
    progress?: number
  }>> {
    return apiClient.post(`${this.baseUrl}/data-integrity/check`, {
      checkType: checkType || 'incremental'
    })
  }

  /**
   * Get data integrity check status
   */
  async getDataIntegrityCheckStatus(checkId: string): Promise<ApiResponse<{
    checkId: string
    status: 'running' | 'completed' | 'failed'
    startTime: Date
    endTime?: Date
    progress: number
    results?: {
      recordsChecked: number
      violationsFound: number
      errors: string[]
    }
  }>> {
    return apiClient.get(`${this.baseUrl}/data-integrity/check/${checkId}`)
  }

  /**
   * Get user access compliance report
   */
  async getUserAccessCompliance(): Promise<ApiResponse<{
    totalUsers: number
    compliantUsers: number
    nonCompliantUsers: number
    issues: {
      userId: string
      username: string
      issues: {
        type: 'password_expired' | 'no_mfa' | 'excessive_permissions' | 'inactive_account'
        description: string
        severity: 'low' | 'medium' | 'high'
      }[]
    }[]
    recommendations: string[]
  }>> {
    return apiClient.get(`${this.baseUrl}/user-access`)
  }

  /**
   * Get system configuration compliance
   */
  async getSystemConfigCompliance(): Promise<ApiResponse<{
    configurations: {
      category: string
      setting: string
      currentValue: string
      requiredValue: string
      compliant: boolean
      severity: 'low' | 'medium' | 'high'
      description: string
    }[]
    overallCompliance: number
    criticalIssues: number
    warnings: number
  }>> {
    return apiClient.get(`${this.baseUrl}/system-config`)
  }

  /**
   * Get regulatory change notifications
   */
  async getRegulatoryUpdates(): Promise<ApiResponse<{
    id: string
    title: string
    regulation: 'CFR_PART_11' | 'GDP' | 'ISO_27001' | 'SOX'
    effectiveDate: Date
    description: string
    impactLevel: 'low' | 'medium' | 'high' | 'critical'
    actionRequired: boolean
    recommendations: string[]
    acknowledged: boolean
    acknowledgedBy?: string
    acknowledgedAt?: Date
  }[]>> {
    return apiClient.get(`${this.baseUrl}/regulatory-updates`)
  }

  /**
   * Acknowledge regulatory update
   */
  async acknowledgeRegulatoryUpdate(updateId: string): Promise<ApiResponse<void>> {
    return apiClient.post(`${this.baseUrl}/regulatory-updates/${updateId}/acknowledge`)
  }

  /**
   * Schedule compliance assessment
   */
  async scheduleAssessment(params: {
    assessmentType: 'cfr_part_11' | 'data_integrity' | 'full'
    scheduledDate: Date
    notifyUsers: string[]
    includeRemediation: boolean
  }): Promise<ApiResponse<{
    assessmentId: string
    scheduledDate: Date
    estimatedDuration: number
  }>> {
    return apiClient.post(`${this.baseUrl}/assessments/schedule`, {
      assessmentType: params.assessmentType,
      scheduledDate: params.scheduledDate.toISOString(),
      notifyUsers: params.notifyUsers,
      includeRemediation: params.includeRemediation
    })
  }

  /**
   * Get compliance training records
   */
  async getTrainingRecords(): Promise<ApiResponse<{
    userId: string
    username: string
    completedTrainings: {
      trainingId: string
      title: string
      completedDate: Date
      expirationDate?: Date
      score?: number
      certified: boolean
    }[]
    requiredTrainings: {
      trainingId: string
      title: string
      dueDate: Date
      mandatory: boolean
    }[]
    overallCompliance: number
  }[]>> {
    return apiClient.get(`${this.baseUrl}/training-records`)
  }
}

// Export singleton instance
export const complianceService = new ComplianceService()

// Export the class for testing
export { ComplianceService }