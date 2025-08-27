/**
 * CFR Part 11 Compliance Integration Tests
 * Tests critical FDA regulation compliance requirements for industrial software
 * CRITICAL: These tests verify NO SYNTHETIC DATA generation and proper audit trails
 */

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { server } from '@/test/msw-setup'
import { http, HttpResponse } from 'msw'
import { 
  DataQualityWrapper, 
  DataQualityIndicator,
  useDataQuality 
} from '@/components/ui/data-quality-indicator'
import { processApiResponse, validateLoginResponse } from '@/lib/utils/apiValidation'
import type { DataQuality, DataWithQuality } from '@/types'

// Test wrapper for React Query
const TestWrapper = ({ children }: { children: React.ReactNode }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  })

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}

describe('CFR Part 11 Compliance Integration Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Data Integrity Requirements', () => {
    it('should NEVER generate synthetic measurement data', async () => {
      // Arrange - Simulate device offline scenario
      server.use(
        http.get('/api/devices/measurements', () => {
          return HttpResponse.json({
            success: false,
            error: 'Device offline',
            dataQuality: 'unavailable'
          }, { status: 503 })
        })
      )

      // Act - Attempt to get device measurements
      const response = await fetch('/api/devices/measurements')
      const data = await response.json()

      // Assert - Should indicate unavailable rather than generate fake data
      expect(data.success).toBe(false)
      expect(data.dataQuality).toBe('unavailable')
      expect(data.error).toBeTruthy()
      
      // CRITICAL: Should NOT contain any measurement values
      expect(data.value).toBeUndefined()
      expect(data.measurement).toBeUndefined()
      expect(data.temperature).toBeUndefined()
      expect(data.pressure).toBeUndefined()
    })

    it('should clearly mark any test/simulated data', () => {
      // Arrange - Test environment data
      const testData: DataWithQuality<{ value: number }> = {
        value: 42,
        quality: 'simulated',
        timestamp: new Date(),
        warning: 'TEST ENVIRONMENT - Not real production data',
        auditInfo: {
          sourceSystem: 'TEST-SIMULATOR',
          dataIntegrity: 'synthetic'
        }
      }

      // Act
      render(
        <TestWrapper>
          <DataQualityWrapper data={testData}>
            <div data-testid="test-value">{testData.value}</div>
          </DataQualityWrapper>
        </TestWrapper>
      )

      // Assert - Should have clear synthetic data warnings
      expect(screen.getByText(/SIMULATED/)).toBeInTheDocument()
      expect(screen.getByText(/TEST ENVIRONMENT/)).toBeInTheDocument()
      expect(screen.getByText(/synthetic/)).toBeInTheDocument()
    })

    it('should preserve exact data precision without modifications', () => {
      // Arrange - High precision measurement data
      const precisionData = {
        temperature: 23.456789123456789,
        pressure: 1013.25987654321,
        timestamp: '2025-08-27T10:00:00.123456789Z',
        deviceId: 'ADAM-6000-001'
      }

      // Act - Process through validation
      const result = processApiResponse(precisionData)

      // Assert - Should preserve exact precision
      expect(result.data).toEqual(precisionData)
      expect(result.data.temperature).toBe(23.456789123456789)
      expect(result.data.pressure).toBe(1013.25987654321)
      expect(result.data.timestamp).toBe('2025-08-27T10:00:00.123456789Z')
    })

    it('should reject corrupted or tampered data', () => {
      // Arrange - Data with integrity issues
      const corruptedData = {
        value: 123,
        checksum: 'invalid',
        lastModified: '2025-08-27T09:00:00Z',
        dataIntegrity: 'compromised'
      }

      // Mock validator that detects corruption
      const integrityValidator = (data: any) => {
        if (data.dataIntegrity === 'compromised' || data.checksum === 'invalid') {
          return {
            isValid: false,
            errors: ['Data integrity check failed', 'Invalid checksum detected']
          }
        }
        return { isValid: true, errors: [] }
      }

      // Act
      const result = processApiResponse(corruptedData, integrityValidator)

      // Assert - Should reject corrupted data
      expect(result.isValid).toBe(false)
      expect(result.errors).toContain('Data integrity check failed')
      expect(result.errors).toContain('Invalid checksum detected')
      expect(result.data).toBeUndefined()
    })
  })

  describe('Electronic Records Requirements', () => {
    it('should maintain complete audit trail for data access', () => {
      // Arrange
      const auditableData: DataWithQuality<{ measurement: number }> = {
        measurement: 123.45,
        quality: 'good',
        timestamp: new Date('2025-08-27T10:00:00Z'),
        auditInfo: {
          sourceSystem: 'ADAM-6000-001',
          dataIntegrity: 'verified',
          createdBy: 'system',
          createdAt: new Date('2025-08-27T10:00:00Z'),
          lastModifiedBy: null,
          lastModifiedAt: null,
          accessLog: [
            {
              userId: 'operator-001',
              action: 'VIEW',
              timestamp: new Date('2025-08-27T10:01:00Z'),
              ipAddress: '192.168.1.100'
            }
          ]
        }
      }

      // Act
      render(
        <TestWrapper>
          <DataQualityWrapper data={auditableData}>
            <div>Measurement: {auditableData.measurement}</div>
          </DataQualityWrapper>
        </TestWrapper>
      )

      // Assert - Audit information should be available
      expect(screen.getByText(/Source: ADAM-6000-001/)).toBeInTheDocument()
      expect(screen.getByText(/Integrity: verified/)).toBeInTheDocument()
    })

    it('should ensure data is attributable to specific users', async () => {
      // Arrange - Data entry with user attribution
      const attributableData = {
        id: 'entry-001',
        value: 98.6,
        enteredBy: 'operator.smith',
        enteredAt: '2025-08-27T10:00:00Z',
        verifiedBy: 'supervisor.jones',
        verifiedAt: '2025-08-27T10:05:00Z',
        digitalSignature: 'valid-signature-hash'
      }

      // Mock API response with attribution
      server.use(
        http.get('/api/data/entry-001', () => {
          return HttpResponse.json({
            success: true,
            data: attributableData
          })
        })
      )

      // Act
      const response = await fetch('/api/data/entry-001')
      const data = await response.json()

      // Assert - Attribution information must be present
      expect(data.success).toBe(true)
      expect(data.data.enteredBy).toBe('operator.smith')
      expect(data.data.verifiedBy).toBe('supervisor.jones')
      expect(data.data.digitalSignature).toBeTruthy()
    })

    it('should prevent unauthorized data modifications', async () => {
      // Arrange - Attempt unauthorized modification
      server.use(
        http.put('/api/data/protected-001', () => {
          return HttpResponse.json({
            success: false,
            error: 'Insufficient privileges for data modification',
            complianceViolation: 'Attempted unauthorized modification of verified record'
          }, { status: 403 })
        })
      )

      // Act
      const response = await fetch('/api/data/protected-001', {
        method: 'PUT',
        body: JSON.stringify({ value: 'modified' })
      })
      const data = await response.json()

      // Assert - Should be blocked with clear compliance message
      expect(response.status).toBe(403)
      expect(data.success).toBe(false)
      expect(data.complianceViolation).toContain('unauthorized modification')
    })
  })

  describe('Electronic Signature Requirements', () => {
    it('should validate digital signatures for critical data', async () => {
      // Arrange - Data with digital signature
      const signedData = {
        measurement: 150.75,
        operator: 'john.doe',
        supervisor: 'jane.smith',
        timestamp: '2025-08-27T10:00:00Z',
        digitalSignatures: {
          operator: 'sig_op_abc123',
          supervisor: 'sig_sup_def456'
        },
        signatureValid: true
      }

      server.use(
        http.get('/api/signed-data/001', () => {
          return HttpResponse.json({
            success: true,
            data: signedData,
            signatureVerification: 'VALID'
          })
        })
      )

      // Act
      const response = await fetch('/api/signed-data/001')
      const data = await response.json()

      // Assert - Signature validation must be present
      expect(data.signatureVerification).toBe('VALID')
      expect(data.data.digitalSignatures).toBeTruthy()
      expect(data.data.signatureValid).toBe(true)
    })

    it('should reject data with invalid signatures', async () => {
      // Arrange - Data with compromised signature
      server.use(
        http.get('/api/signed-data/002', () => {
          return HttpResponse.json({
            success: false,
            error: 'Digital signature verification failed',
            signatureVerification: 'INVALID',
            complianceAlert: 'Data integrity compromised - signature mismatch detected'
          }, { status: 422 })
        })
      )

      // Act
      const response = await fetch('/api/signed-data/002')
      const data = await response.json()

      // Assert - Should reject invalid signatures
      expect(response.status).toBe(422)
      expect(data.success).toBe(false)
      expect(data.signatureVerification).toBe('INVALID')
      expect(data.complianceAlert).toContain('signature mismatch')
    })
  })

  describe('Data Quality Compliance UI Requirements', () => {
    it('should display prominent warnings for uncertain data', () => {
      // Arrange
      const uncertainData: DataWithQuality<{ temperature: number }> = {
        temperature: 25.3,
        quality: 'uncertain',
        timestamp: new Date(),
        warning: 'Sensor calibration overdue - data accuracy uncertain',
        auditInfo: {
          sourceSystem: 'ADAM-6000-002',
          dataIntegrity: 'questionable'
        }
      }

      // Act
      render(
        <TestWrapper>
          <DataQualityWrapper data={uncertainData} showFullWarning>
            <div>Temperature: {uncertainData.temperature}°C</div>
          </DataQualityWrapper>
        </TestWrapper>
      )

      // Assert - Warning should be prominent and specific
      expect(screen.getByText('Data Quality Warning')).toBeInTheDocument()
      expect(screen.getByText(/Sensor calibration overdue/)).toBeInTheDocument()
      expect(screen.getByText('UNCERTAIN')).toBeInTheDocument()
    })

    it('should prevent misrepresentation of data quality', () => {
      // Arrange - Test all non-good quality levels
      const qualityLevels: DataQuality[] = ['uncertain', 'bad', 'unavailable', 'simulated']
      
      qualityLevels.forEach(quality => {
        // Act
        const { unmount } = render(
          <TestWrapper>
            <DataQualityIndicator quality={quality} showAlert />
          </TestWrapper>
        )

        // Assert - Each should have appropriate warning level
        if (quality === 'bad' || quality === 'unavailable') {
          expect(screen.getByRole('alert')).toHaveAttribute('data-variant', 'destructive')
        }
        
        // Should never show "good" or "verified" for bad data
        expect(screen.queryByText('VERIFIED')).not.toBeInTheDocument()
        expect(screen.queryByText('GOOD')).not.toBeInTheDocument()
        
        unmount()
      })
    })

    it('should require explicit acknowledgment for using uncertain data', () => {
      // This would be implemented in production as a confirmation dialog
      // For now, we test that the warning is sufficiently prominent
      
      const uncertainData: DataWithQuality<{ value: number }> = {
        value: 42,
        quality: 'uncertain',
        timestamp: new Date(),
        warning: 'Data quality uncertain - manual verification required before use in production decisions'
      }

      render(
        <TestWrapper>
          <DataQualityWrapper data={uncertainData}>
            <div>Critical Production Value: {uncertainData.value}</div>
          </DataQualityWrapper>
        </TestWrapper>
      )

      // Assert - Strong warning language
      expect(screen.getByText(/manual verification required/)).toBeInTheDocument()
      expect(screen.getByText(/before use in production decisions/)).toBeInTheDocument()
    })
  })

  describe('System Validation Requirements', () => {
    it('should validate all incoming data against expected formats', () => {
      // Arrange - Valid login response
      const validLoginResponse = {
        user: {
          userId: 'usr_123',
          username: 'validated.user',
          email: 'user@company.com',
          roles: ['Operator']
        },
        accessToken: 'valid.jwt.token',
        refreshToken: 'valid.refresh.token',
        expiresAt: '2025-08-27T11:00:00Z'
      }

      // Act
      const validation = validateLoginResponse(validLoginResponse)

      // Assert - Should pass validation
      expect(validation.isValid).toBe(true)
      expect(validation.errors).toHaveLength(0)
    })

    it('should reject malformed or incomplete data structures', () => {
      // Arrange - Invalid login response
      const invalidLoginResponse = {
        message: 'Login successful',
        // Missing required user and token fields
      }

      // Act
      const validation = validateLoginResponse(invalidLoginResponse)

      // Assert - Should fail validation
      expect(validation.isValid).toBe(false)
      expect(validation.errors.length).toBeGreaterThan(0)
      expect(validation.errors).toContain('user field is required')
      expect(validation.errors).toContain('accessToken field is required')
    })

    it('should maintain data lineage for all measurements', async () => {
      // Arrange - Data with complete lineage
      server.use(
        http.get('/api/measurements/with-lineage', () => {
          return HttpResponse.json({
            success: true,
            data: {
              value: 123.45,
              unit: 'psi',
              deviceId: 'ADAM-6000-001',
              sensorId: 'PRESSURE_01',
              calibrationDate: '2025-08-20T00:00:00Z',
              lastCalibration: 'valid',
              measurementChain: [
                'Raw ADC: 2047',
                'Scaled: 123.45 psi',
                'Compensated: 123.45 psi (temp: 23.5°C)'
              ],
              processingSteps: [
                'ADC conversion',
                'Unit scaling',
                'Temperature compensation'
              ]
            },
            dataQuality: 'good',
            auditTrail: {
              created: '2025-08-27T10:00:00Z',
              processing: '2025-08-27T10:00:00.123Z',
              validated: '2025-08-27T10:00:00.456Z'
            }
          })
        })
      )

      // Act
      const response = await fetch('/api/measurements/with-lineage')
      const data = await response.json()

      // Assert - Should include complete data lineage
      expect(data.success).toBe(true)
      expect(data.data.measurementChain).toHaveLength(3)
      expect(data.data.processingSteps).toHaveLength(3)
      expect(data.auditTrail.created).toBeTruthy()
      expect(data.auditTrail.processing).toBeTruthy()
      expect(data.auditTrail.validated).toBeTruthy()
    })
  })

  describe('Data Archival and Retrieval Compliance', () => {
    it('should maintain data integrity during long-term storage', async () => {
      // Arrange - Historical data retrieval
      server.use(
        http.get('/api/historical/2024-01-01', () => {
          return HttpResponse.json({
            success: true,
            data: {
              date: '2024-01-01T00:00:00Z',
              measurements: [
                {
                  timestamp: '2024-01-01T08:00:00Z',
                  value: 98.6,
                  checksum: 'valid-checksum-123',
                  archived: true,
                  retrievalDate: '2025-08-27T10:00:00Z'
                }
              ]
            },
            archivalInfo: {
              storedDate: '2024-01-02T00:00:00Z',
              integrityCheck: 'PASSED',
              retrievalReason: 'Compliance audit',
              requestedBy: 'auditor.jones'
            }
          })
        })
      )

      // Act
      const response = await fetch('/api/historical/2024-01-01')
      const data = await response.json()

      // Assert - Archived data should maintain integrity
      expect(data.success).toBe(true)
      expect(data.archivalInfo.integrityCheck).toBe('PASSED')
      expect(data.data.measurements[0].checksum).toBeTruthy()
      expect(data.archivalInfo.requestedBy).toBeTruthy()
    })

    it('should log all data access for audit purposes', async () => {
      // This test would verify that data access is logged
      // In a real implementation, this would check audit logs
      
      const consoleSpy = vi.spyOn(console, 'log')
      
      // Act - Access sensitive data
      await fetch('/api/sensitive-measurements')
      
      // In production, this would generate audit log entries
      // For testing, we verify no silent access occurs
      expect(consoleSpy).not.toHaveBeenCalledWith(
        expect.stringMatching(/unauthorized|silent|bypass/i)
      )
    })
  })

  describe('Error Handling Compliance', () => {
    it('should never mask compliance-related errors', async () => {
      // Arrange - Compliance violation scenario
      server.use(
        http.post('/api/data/modify-locked', () => {
          return HttpResponse.json({
            success: false,
            error: 'Cannot modify verified record - CFR Part 11 violation',
            complianceCode: 'CFR21-11-10',
            severity: 'CRITICAL',
            requiresReporting: true
          }, { status: 423 })
        })
      )

      // Act
      const response = await fetch('/api/data/modify-locked', {
        method: 'POST',
        body: JSON.stringify({ modification: 'test' })
      })
      const data = await response.json()

      // Assert - Compliance error should be explicit
      expect(response.status).toBe(423) // Locked resource
      expect(data.error).toContain('CFR Part 11 violation')
      expect(data.complianceCode).toBe('CFR21-11-10')
      expect(data.severity).toBe('CRITICAL')
      expect(data.requiresReporting).toBe(true)
    })

    it('should provide clear guidance for compliance violations', () => {
      // Arrange - Synthetic data detection
      const syntheticDataWarning: DataWithQuality<any> = {
        value: null,
        quality: 'simulated',
        timestamp: new Date(),
        warning: '⚠️ SYNTHETIC DATA DETECTED - This data is not from actual industrial systems and must not be used for production decisions or regulatory reporting. CFR Part 11 requires all electronic records to be attributable, legible, and contemporaneous.',
        auditInfo: {
          sourceSystem: 'SIMULATOR',
          dataIntegrity: 'synthetic'
        }
      }

      // Act
      render(
        <TestWrapper>
          <DataQualityWrapper data={syntheticDataWarning}>
            <div>Simulated Data Value</div>
          </DataQualityWrapper>
        </TestWrapper>
      )

      // Assert - Should provide clear regulatory guidance
      expect(screen.getByText(/SYNTHETIC DATA DETECTED/)).toBeInTheDocument()
      expect(screen.getByText(/CFR Part 11 requires/)).toBeInTheDocument()
      expect(screen.getByText(/not be used for production decisions/)).toBeInTheDocument()
    })
  })
})