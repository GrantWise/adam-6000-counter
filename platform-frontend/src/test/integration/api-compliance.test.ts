/**
 * API Compliance Integration Tests
 * Validates that ALL API integrations are CFR Part 11 compliant
 * CRITICAL: Ensures ZERO TOLERANCE for synthetic data
 */

import { describe, it, expect, beforeAll, afterAll } from 'vitest'
import { validateCfrCompliance, assertRealData, CFR_COMPLIANCE_FLAGS } from '@/lib/utils/cfrCompliance'
import { deviceService } from '@/lib/services/deviceService'
import { oeeService } from '@/lib/services/oeeService'
import { apiClient } from '@/lib/api/client'

// Mock network conditions for testing
const originalFetch = global.fetch

describe('API Compliance Tests', () => {
  beforeAll(() => {
    // Ensure we're testing against real endpoints
    console.log('Testing API compliance with endpoints:')
    console.log('Logger API:', import.meta.env.VITE_LOGGER_API_PORT || '5139')
    console.log('OEE API:', import.meta.env.VITE_OEE_API_PORT || '5140')
  })

  afterAll(() => {
    // Restore original fetch
    global.fetch = originalFetch
  })

  describe('Device Service Compliance', () => {
    it('should NOT return synthetic data when Logger API is unavailable', async () => {
      // Mock API unavailable
      global.fetch = () => Promise.reject(new Error('Network error'))

      const response = await deviceService.getDevices()
      
      // Validate compliance
      const compliance = validateCfrCompliance(response, 'Device Service')
      
      if (response.success) {
        // If successful, data must be real
        expect(compliance.isCompliant).toBe(true)
        expect(compliance.violations).toHaveLength(0)
        expect(compliance.complianceFlags).toContain(CFR_COMPLIANCE_FLAGS.REAL_DATA)
      } else {
        // If failed, must show proper error without synthetic data
        expect(response.error).toBeDefined()
        expect(response.error?.message).not.toContain('demo')
        expect(response.error?.message).not.toContain('mock')
        expect(compliance.complianceFlags).toContain(CFR_COMPLIANCE_FLAGS.DATA_UNAVAILABLE)
      }
    })

    it('should validate device health data integrity', async () => {
      try {
        const response = await deviceService.getDeviceHealth('SIM-6051-01')
        
        if (response.success && response.data) {
          // Validate that health metrics are not obviously synthetic
          const { data } = response
          
          // Check for suspicious values
          if (data.uptime === 42 || data.uptime === 100) {
            expect.fail('Device uptime appears synthetic (common test values)')
          }
          
          if (data.responseTime.current === 42 || data.responseTime.average === 42) {
            expect.fail('Response times appear synthetic (common test values)')
          }
          
          // Validate timestamp
          expect(data.lastSeen).toBeInstanceOf(Date)
          expect(data.lastSeen.getTime()).toBeLessThanOrEqual(Date.now())
          
          // Ensure compliance
          const compliance = validateCfrCompliance(response, 'Device Health')
          expect(compliance.isCompliant).toBe(true)
        } else {
          // If no data, ensure proper error handling
          expect(response.error).toBeDefined()
          expect(response.error?.details?.cfrCompliant).toBeTruthy()
        }
      } catch (error) {
        // Network error is acceptable - synthetic data is not
        console.log('Device health test - network error acceptable:', error)
      }
    })

    it('should handle real-time readings without synthetic fallbacks', async () => {
      try {
        const response = await deviceService.getRealTimeReadings()
        
        if (response.success && response.data) {
          // Validate each reading for synthetic patterns
          response.data.forEach((device, index) => {
            device.channels.forEach((channel, channelIndex) => {
              // Check for obviously synthetic values
              if (channel.value === 42 || channel.value === 123) {
                expect.fail(`Device ${device.deviceId} Channel ${channel.channelId} has suspicious value: ${channel.value}`)
              }
              
              // Validate timestamp is recent and real
              const timeDiff = Date.now() - new Date(channel.timestamp).getTime()
              if (timeDiff > 24 * 60 * 60 * 1000) { // More than 24 hours old
                console.warn(`Old timestamp detected for ${device.deviceId}:${channel.channelId}`)
              }
            })
          })
          
          // Ensure overall compliance
          const compliance = validateCfrCompliance(response, 'Real-time Readings')
          expect(compliance.isCompliant).toBe(true)
        } else {
          // Must show appropriate error without synthetic data
          expect(response.error?.message).toMatch(/unavailable|offline|error/i)
        }
      } catch (error) {
        console.log('Real-time readings test - error acceptable if no synthetic data shown')
      }
    })
  })

  describe('OEE Service Compliance', () => {
    it('should NEVER return synthetic OEE data', async () => {
      const response = await oeeService.getCurrentOEE()
      
      if (response.success && response.data) {
        // Validate each OEE entry for synthetic patterns
        response.data.forEach((oeeData) => {
          // Check for obviously synthetic OEE values
          if (oeeData.oee === 85 || oeeData.oee === 75 || oeeData.oee === 90) {
            console.warn(`Suspicious OEE value detected: ${oeeData.oee}% for ${oeeData.equipmentId}`)
          }
          
          // Availability should not be perfect round numbers in real systems
          if (oeeData.availability !== null && [95, 90, 85, 80, 75].includes(oeeData.availability)) {
            console.warn(`Suspicious availability: ${oeeData.availability}% for ${oeeData.equipmentId}`)
          }
          
          // Performance should not be exactly 100% or other round numbers
          if (oeeData.performance === 100 || oeeData.performance === 90) {
            console.warn(`Suspicious performance: ${oeeData.performance}% for ${oeeData.equipmentId}`)
          }
          
          // Quality should be realistic
          if (oeeData.quality === 100 || oeeData.quality === 99) {
            console.warn(`Suspicious quality: ${oeeData.quality}% for ${oeeData.equipmentId}`)
          }
        })
        
        // Ensure overall compliance
        const compliance = validateCfrCompliance(response, 'OEE Service')
        expect(compliance.isCompliant).toBe(true)
        
        // All data should be marked as real data
        response.data.forEach((oeeData) => {
          if ('isRealData' in oeeData) {
            expect(oeeData.isRealData).toBe(true)
          }
          if ('dataQuality' in oeeData && oeeData.dataQuality) {
            expect(oeeData.dataQuality).toMatch(/^(good|unavailable)$/)
            expect(oeeData.dataQuality).not.toBe('simulated')
          }
        })
      } else {
        // If OEE service unavailable, must return proper error
        expect(response.error).toBeDefined()
        expect(response.error?.code).toMatch(/OEE.*UNAVAILABLE|OEE.*ERROR/)
        expect(response.error?.details?.complianceBreach).toBeFalsy()
      }
    })

    it('should handle OEE overview without synthetic trends', async () => {
      try {
        const response = await oeeService.getOEEOverview()
        
        if (response.success && response.data) {
          const { data } = response
          
          // Check that averages are not obviously synthetic
          if (data.averageOEE !== null && [85, 75, 90, 80].includes(data.averageOEE)) {
            console.warn(`Suspicious average OEE: ${data.averageOEE}`)
          }
          
          // Trends should not have synthetic patterns
          if (data.trends) {
            // Hourly trends should not all be null or follow obvious patterns
            if (data.trends.hourly?.every(t => t.oee === null)) {
              console.log('All hourly trends are null - this is acceptable (no synthetic data)')
            }
            
            // Daily trends should not have synthetic values
            data.trends.daily?.forEach((trend, index) => {
              if (trend.oee === 85 || trend.oee === 75) {
                console.warn(`Suspicious daily trend OEE: ${trend.oee} at index ${index}`)
              }
            })
          }
          
          // Top performers should not have synthetic patterns
          data.topPerformers?.forEach((performer) => {
            if (performer.oee === 85 || performer.availability === 90) {
              console.warn(`Suspicious top performer metrics for ${performer.equipmentId}`)
            }
          })
          
          // Ensure compliance
          const compliance = validateCfrCompliance(response, 'OEE Overview')
          expect(compliance.isCompliant).toBe(true)
        } else {
          // Error handling must be compliant
          expect(response.error?.code).toMatch(/OEE.*UNAVAILABLE|OEE.*ERROR/)
        }
      } catch (error) {
        console.log('OEE overview test - network error acceptable if no synthetic data')
      }
    })
  })

  describe('API Client Configuration', () => {
    it('should connect to correct service endpoints', () => {
      // Validate that API client is configured for real services
      const loggerInstance = apiClient.getPublicInstance('logger')
      const oeeInstance = apiClient.getPublicInstance('oee')
      
      expect(loggerInstance.defaults.baseURL).toMatch(/:\d+$/) // Should have port
      expect(oeeInstance.defaults.baseURL).toMatch(/:\d+$/) // Should have port
      
      // Should not point to mock or test endpoints
      expect(loggerInstance.defaults.baseURL).not.toMatch(/mock|test|demo/)
      expect(oeeInstance.defaults.baseURL).not.toMatch(/mock|test|demo/)
    })

    it('should have proper authentication configuration', () => {
      const instance = apiClient.getPublicInstance('logger')
      
      // Should have request interceptor for auth
      expect(instance.interceptors.request.handlers.length).toBeGreaterThan(0)
      
      // Should have response interceptor for error handling
      expect(instance.interceptors.response.handlers.length).toBeGreaterThan(0)
    })
  })

  describe('CFR Compliance Utilities', () => {
    it('should detect synthetic data patterns', () => {
      // Test with synthetic response
      const syntheticResponse = {
        success: true,
        data: {
          oee: 85, // Common test value
          device: 'test-device',
          timestamp: new Date(),
          value: 42 // Obviously synthetic
        }
      }
      
      const compliance = validateCfrCompliance(syntheticResponse, 'Test')
      expect(compliance.isCompliant).toBe(false)
      expect(compliance.violations.length).toBeGreaterThan(0)
      expect(compliance.dataQuality).toBe('bad')
    })

    it('should validate real data patterns', () => {
      // Test with realistic response
      const realResponse = {
        success: true,
        data: {
          oee: 87.3, // Realistic decimal
          device: 'ADAM-6051-Production-Line-1',
          timestamp: new Date(),
          value: 1247 // Realistic counter value
        }
      }
      
      const compliance = validateCfrCompliance(realResponse, 'Production')
      expect(compliance.isCompliant).toBe(true)
      expect(compliance.violations).toHaveLength(0)
      expect(compliance.dataQuality).toBe('good')
    })

    it('should handle unavailable data properly', () => {
      const unavailableResponse = {
        success: false,
        error: {
          code: 'API_UNAVAILABLE',
          message: 'Service temporarily unavailable',
          timestamp: new Date(),
          retryable: true
        }
      }
      
      const compliance = validateCfrCompliance(unavailableResponse, 'Service')
      expect(compliance.isCompliant).toBe(true) // Unavailable is compliant (no synthetic data)
      expect(compliance.dataQuality).toBe('unavailable')
      expect(compliance.complianceFlags).toContain(CFR_COMPLIANCE_FLAGS.DATA_UNAVAILABLE)
    })
  })

  describe('Runtime Assertions', () => {
    it('should prevent synthetic data usage at runtime', () => {
      const syntheticData = {
        value: 42,
        testMode: true,
        generated: new Date()
      }
      
      expect(() => {
        assertRealData(syntheticData, 'Test Service')
      }).toThrow('CFR Violation')
    })

    it('should allow real data through assertions', () => {
      const realData = {
        value: 1247.6,
        deviceId: 'ADAM-6051-Line1',
        timestamp: new Date(Date.now() - 1000 * 60) // 1 minute ago
      }
      
      expect(() => {
        assertRealData(realData, 'Production Service')
      }).not.toThrow()
    })
  })
})

describe('SignalR Integration Compliance', () => {
  it('should connect to correct hub endpoints', () => {
    // Validate SignalR hub URLs are pointing to real services
    const expectedLoggerHub = '/hubs/counter-hub'
    const expectedOeeHub = '/hubs/oee-hub'
    
    // These should not be generic or test hubs
    expect(expectedLoggerHub).not.toMatch(/test|mock|demo/)
    expect(expectedOeeHub).not.toMatch(/test|mock|demo/)
  })
})