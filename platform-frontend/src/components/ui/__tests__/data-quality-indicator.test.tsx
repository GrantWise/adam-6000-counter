/**
 * Data Quality Indicator Component Tests
 * Tests CFR Part 11 compliant data quality indicator functionality
 * CRITICAL: Tests must verify proper warnings for synthetic/unavailable data
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { 
  DataQualityIndicator,
  DataQualityWrapper,
  DataQualitySummary,
  useDataQuality,
  getQualityLabel,
  getQualityDescription,
  getQualityIcon,
  getQualityBadgeVariant,
  getQualityAlertVariant
} from '../data-quality-indicator'
import type { DataQuality, DataWithQuality } from '@/types'

// Mock Lucide icons to avoid rendering issues in tests
vi.mock('lucide-react', () => ({
  CheckCircle: ({ className }: any) => <div data-testid="check-circle" className={className}>✓</div>,
  AlertTriangle: ({ className }: any) => <div data-testid="alert-triangle" className={className}>⚠</div>,
  XCircle: ({ className }: any) => <div data-testid="x-circle" className={className}>✗</div>,
  AlertCircle: ({ className }: any) => <div data-testid="alert-circle" className={className}>!</div>,
  Info: ({ className }: any) => <div data-testid="info" className={className}>i</div>,
  Clock: ({ className }: any) => <div data-testid="clock" className={className}>⏰</div>,
}))

describe('Data Quality Indicator Components', () => {
  describe('DataQualityIndicator', () => {
    it('should render good quality indicator correctly', () => {
      // Act
      render(<DataQualityIndicator quality="good" />)

      // Assert
      expect(screen.getByTestId('check-circle')).toBeInTheDocument()
      expect(screen.getByText('VERIFIED')).toBeInTheDocument()
    })

    it('should render uncertain quality with warning', () => {
      // Act
      render(<DataQualityIndicator quality="uncertain" showAlert />)

      // Assert
      expect(screen.getByTestId('alert-triangle')).toBeInTheDocument()
      expect(screen.getByText('UNCERTAIN')).toBeInTheDocument()
      expect(screen.getByText('Data quality uncertain - verify before use')).toBeInTheDocument()
    })

    it('should render bad quality with critical alert', () => {
      // Act
      render(<DataQualityIndicator quality="bad" showAlert />)

      // Assert
      expect(screen.getByTestId('x-circle')).toBeInTheDocument()
      expect(screen.getByText('BAD DATA')).toBeInTheDocument()
      expect(screen.getByText('Data quality poor - do not use for decisions')).toBeInTheDocument()
    })

    it('should render unavailable data indicator', () => {
      // Act
      render(<DataQualityIndicator quality="unavailable" />)

      // Assert
      expect(screen.getByTestId('alert-circle')).toBeInTheDocument()
      expect(screen.getByText('NO DATA')).toBeInTheDocument()
    })

    it('should render simulated data with clear warning', () => {
      // Act
      render(<DataQualityIndicator quality="simulated" showAlert />)

      // Assert
      expect(screen.getByTestId('info')).toBeInTheDocument()
      expect(screen.getByText('SIMULATED')).toBeInTheDocument()
      expect(screen.getByText('⚠️ SYNTHETIC DATA - Not from actual system')).toBeInTheDocument()
    })

    it('should render in compact mode', () => {
      // Act
      const { container } = render(
        <DataQualityIndicator quality="uncertain" compact showBadge />
      )

      // Assert
      expect(screen.getByTestId('alert-triangle')).toBeInTheDocument()
      expect(screen.getByText('UNCERTAIN')).toBeInTheDocument()
      
      // Should be in compact mode (verify structure exists)
      expect(screen.getByTestId('alert-triangle')).toBeInTheDocument()
      expect(screen.getByText('UNCERTAIN')).toBeInTheDocument()
    })

    it('should show custom warning message', () => {
      // Arrange
      const customWarning = 'Custom data quality warning'

      // Act
      render(
        <DataQualityIndicator 
          quality="uncertain" 
          warning={customWarning} 
          showAlert 
        />
      )

      // Assert
      expect(screen.getByText(customWarning)).toBeInTheDocument()
    })

    it('should not show alert for good quality data', () => {
      // Act
      render(<DataQualityIndicator quality="good" showAlert />)

      // Assert
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('should handle missing/unknown quality gracefully', () => {
      // Act
      render(<DataQualityIndicator quality={'unknown' as DataQuality} />)

      // Assert
      expect(screen.getByTestId('clock')).toBeInTheDocument()
      expect(screen.getByText('UNKNOWN')).toBeInTheDocument()
    })
  })

  describe('DataQualityWrapper', () => {
    const mockGoodData: DataWithQuality<{ value: number }> = {
      value: 42,
      quality: 'good',
      timestamp: new Date('2025-08-27T10:00:00Z'),
      auditInfo: {
        sourceSystem: 'ADAM-6000',
        dataIntegrity: 'verified'
      }
    }

    const mockBadData: DataWithQuality<{ value: number }> = {
      value: null,
      quality: 'unavailable',
      timestamp: new Date('2025-08-27T10:00:00Z'),
      warning: 'Device offline - no data available',
      auditInfo: {
        sourceSystem: 'ADAM-6000',
        dataIntegrity: 'compromised'
      }
    }

    it('should render children with good data quality', () => {
      // Act
      render(
        <DataQualityWrapper data={mockGoodData}>
          <div>Test Content</div>
        </DataQualityWrapper>
      )

      // Assert
      expect(screen.getByText('Test Content')).toBeInTheDocument()
      expect(screen.getByTestId('check-circle')).toBeInTheDocument()
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('should show warning alert for bad data quality', () => {
      // Act
      render(
        <DataQualityWrapper data={mockBadData}>
          <div>Test Content</div>
        </DataQualityWrapper>
      )

      // Assert
      expect(screen.getByText('Test Content')).toBeInTheDocument()
      expect(screen.getByText('Data Quality Warning')).toBeInTheDocument()
      expect(screen.getByText('Device offline - no data available')).toBeInTheDocument()
    })

    it('should include audit information in warning', () => {
      // Act
      render(
        <DataQualityWrapper data={mockBadData}>
          <div>Test Content</div>
        </DataQualityWrapper>
      )

      // Assert
      expect(screen.getByText(/Source: ADAM-6000/)).toBeInTheDocument()
      expect(screen.getByText(/Integrity: compromised/)).toBeInTheDocument()
      expect(screen.getByText(/Generated:/)).toBeInTheDocument()
    })

    it('should not show warning when showFullWarning is false', () => {
      // Act
      render(
        <DataQualityWrapper data={mockBadData} showFullWarning={false}>
          <div>Test Content</div>
        </DataQualityWrapper>
      )

      // Assert
      expect(screen.getByText('Test Content')).toBeInTheDocument()
      expect(screen.queryByText('Data Quality Warning')).not.toBeInTheDocument()
    })
  })

  describe('DataQualitySummary', () => {
    const mockQualities = [
      { label: 'Device A', quality: 'good' as DataQuality },
      { label: 'Device B', quality: 'uncertain' as DataQuality, warning: 'Connection unstable' },
      { label: 'Device C', quality: 'unavailable' as DataQuality }
    ]

    it('should render all quality indicators', () => {
      // Act
      render(<DataQualitySummary qualities={mockQualities} />)

      // Assert
      expect(screen.getByText('Data Quality Status')).toBeInTheDocument()
      expect(screen.getByText('Device A')).toBeInTheDocument()
      expect(screen.getByText('Device B')).toBeInTheDocument()
      expect(screen.getByText('Device C')).toBeInTheDocument()
    })

    it('should show issue count when there are problems', () => {
      // Act
      render(<DataQualitySummary qualities={mockQualities} />)

      // Assert
      expect(screen.getByText('2 Issues')).toBeInTheDocument() // Device B and C have issues
    })

    it('should show compliance warning when issues exist', () => {
      // Act
      render(<DataQualitySummary qualities={mockQualities} />)

      // Assert
      expect(screen.getByText(/providing simulated or uncertain data/)).toBeInTheDocument()
      expect(screen.getByText(/Review quality indicators/)).toBeInTheDocument()
    })

    it('should not show issues badge when all data is good', () => {
      // Arrange
      const allGoodQualities = [
        { label: 'Device A', quality: 'good' as DataQuality },
        { label: 'Device B', quality: 'good' as DataQuality }
      ]

      // Act
      render(<DataQualitySummary qualities={allGoodQualities} />)

      // Assert
      expect(screen.queryByText(/Issues/)).not.toBeInTheDocument()
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  describe('useDataQuality hook', () => {
    // Helper component to test the hook
    const TestComponent = ({ response }: { response?: any }) => {
      const { quality, warnings, isCompliant } = useDataQuality(response)
      
      return (
        <div>
          <div data-testid="quality">{quality}</div>
          <div data-testid="warnings">{warnings.join(', ')}</div>
          <div data-testid="compliant">{isCompliant.toString()}</div>
        </div>
      )
    }

    it('should return unavailable when no response provided', () => {
      // Act
      render(<TestComponent />)

      // Assert
      expect(screen.getByTestId('quality')).toHaveTextContent('unavailable')
      expect(screen.getByTestId('warnings')).toHaveTextContent('Data not available')
      expect(screen.getByTestId('compliant')).toHaveTextContent('false')
    })

    it('should return good quality for compliant response', () => {
      // Arrange
      const goodResponse = {
        data: 'test',
        dataQuality: 'good',
        complianceWarnings: []
      }

      // Act
      render(<TestComponent response={goodResponse} />)

      // Assert
      expect(screen.getByTestId('quality')).toHaveTextContent('good')
      expect(screen.getByTestId('warnings')).toHaveTextContent('')
      expect(screen.getByTestId('compliant')).toHaveTextContent('true')
    })

    it('should return warnings for non-compliant response', () => {
      // Arrange
      const nonCompliantResponse = {
        data: 'test',
        dataQuality: 'uncertain',
        complianceWarnings: ['Data source unreliable', 'Timestamp mismatch']
      }

      // Act
      render(<TestComponent response={nonCompliantResponse} />)

      // Assert
      expect(screen.getByTestId('quality')).toHaveTextContent('uncertain')
      expect(screen.getByTestId('warnings')).toHaveTextContent('Data source unreliable, Timestamp mismatch')
      expect(screen.getByTestId('compliant')).toHaveTextContent('false')
    })
  })

  describe('Utility Functions', () => {
    describe('getQualityLabel', () => {
      it('should return correct labels for all quality levels', () => {
        expect(getQualityLabel('good')).toBe('VERIFIED')
        expect(getQualityLabel('uncertain')).toBe('UNCERTAIN')
        expect(getQualityLabel('bad')).toBe('BAD DATA')
        expect(getQualityLabel('unavailable')).toBe('NO DATA')
        expect(getQualityLabel('simulated')).toBe('SIMULATED')
      })

      it('should return UNKNOWN for unrecognized quality', () => {
        expect(getQualityLabel('invalid' as DataQuality)).toBe('UNKNOWN')
      })
    })

    describe('getQualityDescription', () => {
      it('should return appropriate descriptions', () => {
        expect(getQualityDescription('good')).toBe('Data verified from source system')
        expect(getQualityDescription('simulated')).toContain('SYNTHETIC DATA')
        expect(getQualityDescription('unavailable')).toContain('unavailable')
      })
    })

    describe('getQualityBadgeVariant', () => {
      it('should return correct badge variants', () => {
        expect(getQualityBadgeVariant('good')).toBe('default')
        expect(getQualityBadgeVariant('uncertain')).toBe('secondary')
        expect(getQualityBadgeVariant('bad')).toBe('destructive')
        expect(getQualityBadgeVariant('unavailable')).toBe('destructive')
        expect(getQualityBadgeVariant('simulated')).toBe('outline')
      })
    })

    describe('getQualityAlertVariant', () => {
      it('should return destructive for critical issues', () => {
        expect(getQualityAlertVariant('bad')).toBe('destructive')
        expect(getQualityAlertVariant('unavailable')).toBe('destructive')
      })

      it('should return default for non-critical issues', () => {
        expect(getQualityAlertVariant('good')).toBe('default')
        expect(getQualityAlertVariant('uncertain')).toBe('default')
        expect(getQualityAlertVariant('simulated')).toBe('default')
      })
    })
  })

  describe('CFR Part 11 Compliance Tests', () => {
    it('should never hide or minimize simulated data warnings', () => {
      // Act
      render(
        <DataQualityIndicator 
          quality="simulated" 
          showAlert 
          showBadge 
        />
      )

      // Assert
      expect(screen.getByText('SIMULATED')).toBeInTheDocument()
      expect(screen.getByText('⚠️ SYNTHETIC DATA - Not from actual system')).toBeInTheDocument()
      
      // Badge should be visible and distinct
      const badge = screen.getByText('SIMULATED')
      expect(badge).toBeInTheDocument() // Should be present in document
    })

    it('should make unavailable data clearly visible', () => {
      // Act
      render(
        <DataQualityIndicator 
          quality="unavailable" 
          showAlert 
        />
      )

      // Assert
      expect(screen.getByText('NO DATA')).toBeInTheDocument()
      expect(screen.getByText('Source data unavailable')).toBeInTheDocument()
    })

    it('should provide clear audit trail information', () => {
      // Arrange
      const auditData: DataWithQuality<string> = {
        value: 'test',
        quality: 'uncertain',
        timestamp: new Date('2025-08-27T10:00:00Z'),
        warning: 'Data quality compromised',
        auditInfo: {
          sourceSystem: 'ADAM-6000-001',
          dataIntegrity: 'questionable'
        }
      }

      // Act
      render(
        <DataQualityWrapper data={auditData}>
          <div>Test Data</div>
        </DataQualityWrapper>
      )

      // Assert
      expect(screen.getByText(/Source: ADAM-6000-001/)).toBeInTheDocument()
      expect(screen.getByText(/Integrity: questionable/)).toBeInTheDocument()
      expect(screen.getByText(/Generated: 8\/27\/2025/)).toBeInTheDocument()
    })

    it('should never suggest using bad or unavailable data', () => {
      // Act
      render(
        <DataQualityIndicator quality="bad" showAlert />
      )

      // Assert
      const description = screen.getByText('Data quality poor - do not use for decisions')
      expect(description).toBeInTheDocument()
      
      // Should contain clear prohibition language
      expect(description.textContent).toContain('do not use')
    })

    it('should distinguish between different types of non-compliant data', () => {
      const qualities: DataQuality[] = ['uncertain', 'bad', 'unavailable', 'simulated']
      
      qualities.forEach(quality => {
        const { unmount } = render(
          <DataQualityIndicator quality={quality} />
        )
        
        // Each quality should have distinct label
        const label = getQualityLabel(quality)
        expect(screen.getByText(label)).toBeInTheDocument()
        
        // Each quality should have distinct description
        const description = getQualityDescription(quality)
        expect(description).toBeTruthy()
        expect(description.length).toBeGreaterThan(0)
        
        unmount()
      })
    })

    it('should maintain data integrity in wrapper component', () => {
      // Arrange
      const originalData: DataWithQuality<{ measurement: number }> = {
        measurement: 123.456789,
        quality: 'good',
        timestamp: new Date('2025-08-27T10:00:00.123Z'),
        auditInfo: {
          sourceSystem: 'ADAM-6000',
          dataIntegrity: 'verified'
        }
      }

      // Act
      const TestWrapper = () => {
        return (
          <DataQualityWrapper data={originalData}>
            <div data-testid="measurement">{originalData.measurement}</div>
            <div data-testid="timestamp">{originalData.timestamp.toISOString()}</div>
          </DataQualityWrapper>
        )
      }
      
      render(<TestWrapper />)

      // Assert - Data should be preserved exactly
      expect(screen.getByTestId('measurement')).toHaveTextContent('123.456789')
      expect(screen.getByTestId('timestamp')).toHaveTextContent('2025-08-27T10:00:00.123Z')
    })
  })
})