/**
 * Test Fixtures for OEE Calculations
 * 
 * Provides consistent test data for domain model testing,
 * following realistic manufacturing scenarios.
 */

import { Availability } from '@/lib/domain/models/Availability'
import { Performance } from '@/lib/domain/models/Performance'
import { Quality } from '@/lib/domain/models/Quality'
import { OEE_Calculation } from '@/lib/domain/models/OEE_Calculation'

// Base scenario values
export const SHIFT_DURATION_MINUTES = 480 // 8 hours
export const TARGET_RATE_PER_MINUTE = 100 // pieces per minute
export const PLANNED_PRODUCTION = TARGET_RATE_PER_MINUTE * SHIFT_DURATION_MINUTES // 48,000 pieces

// Excellent Performance Scenario (World Class OEE ~85%)
export const excellentScenario = {
  availability: {
    plannedMinutes: SHIFT_DURATION_MINUTES,
    actualRunMinutes: 432, // 90% availability
    downtimeMinutes: 48
  },
  performance: {
    totalPiecesProduced: 41040, // 95% performance (432 min * 100 rate * 0.95)
    runTimeMinutes: 432,
    targetRatePerMinute: TARGET_RATE_PER_MINUTE
  },
  quality: {
    goodPieces: 40630, // 99% quality
    defectivePieces: 410,
    totalPiecesProduced: 41040
  }
}

// Good Performance Scenario (Acceptable OEE ~65%)
export const goodScenario = {
  availability: {
    plannedMinutes: SHIFT_DURATION_MINUTES,
    actualRunMinutes: 408, // 85% availability
    downtimeMinutes: 72
  },
  performance: {
    totalPiecesProduced: 34680, // 85% performance (408 min * 100 rate * 0.85)
    runTimeMinutes: 408,
    targetRatePerMinute: TARGET_RATE_PER_MINUTE
  },
  quality: {
    goodPieces: 32946, // 95% quality
    defectivePieces: 1734,
    totalPiecesProduced: 34680
  }
}

// Poor Performance Scenario (Needs Improvement OEE ~40%)
export const poorScenario = {
  availability: {
    plannedMinutes: SHIFT_DURATION_MINUTES,
    actualRunMinutes: 360, // 75% availability
    downtimeMinutes: 120
  },
  performance: {
    totalPiecesProduced: 25200, // 70% performance (360 min * 100 rate * 0.70)
    runTimeMinutes: 360,
    targetRatePerMinute: TARGET_RATE_PER_MINUTE
  },
  quality: {
    goodPieces: 22680, // 90% quality
    defectivePieces: 2520,
    totalPiecesProduced: 25200
  }
}

// Edge Cases
export const edgeCases = {
  // Zero production (equipment down all shift)
  zeroProduction: {
    availability: {
      plannedMinutes: SHIFT_DURATION_MINUTES,
      actualRunMinutes: 0,
      downtimeMinutes: SHIFT_DURATION_MINUTES
    },
    performance: {
      totalPiecesProduced: 0,
      runTimeMinutes: 0,
      targetRatePerMinute: TARGET_RATE_PER_MINUTE
    },
    quality: {
      goodPieces: 0,
      defectivePieces: 0,
      totalPiecesProduced: 0
    }
  },
  
  // Perfect production (theoretical maximum)
  perfectProduction: {
    availability: {
      plannedMinutes: SHIFT_DURATION_MINUTES,
      actualRunMinutes: SHIFT_DURATION_MINUTES,
      downtimeMinutes: 0
    },
    performance: {
      totalPiecesProduced: PLANNED_PRODUCTION,
      runTimeMinutes: SHIFT_DURATION_MINUTES,
      targetRatePerMinute: TARGET_RATE_PER_MINUTE
    },
    quality: {
      goodPieces: PLANNED_PRODUCTION,
      defectivePieces: 0,
      totalPiecesProduced: PLANNED_PRODUCTION
    }
  },

  // High performance but low quality
  speedOverQuality: {
    availability: {
      plannedMinutes: SHIFT_DURATION_MINUTES,
      actualRunMinutes: 456, // 95% availability
      downtimeMinutes: 24
    },
    performance: {
      totalPiecesProduced: 45600, // 100% performance (456 min * 100 rate)
      runTimeMinutes: 456,
      targetRatePerMinute: TARGET_RATE_PER_MINUTE
    },
    quality: {
      goodPieces: 36480, // 80% quality (high defect rate)
      defectivePieces: 9120,
      totalPiecesProduced: 45600
    }
  }
}

// Downtime categories for availability testing
export const downtimeRecords = {
  typical: [
    { duration_minutes: 15, category: 'planned' as const }, // Break
    { duration_minutes: 30, category: 'planned' as const }, // Lunch
    { duration_minutes: 10, category: 'unplanned' as const }, // Material shortage
    { duration_minutes: 5, category: 'unplanned' as const }, // Minor adjustment
  ],
  
  heavyDowntime: [
    { duration_minutes: 15, category: 'planned' as const }, // Break
    { duration_minutes: 30, category: 'planned' as const }, // Lunch
    { duration_minutes: 60, category: 'unplanned' as const }, // Equipment failure
    { duration_minutes: 20, category: 'unplanned' as const }, // Quality issue
    { duration_minutes: 15, category: 'unplanned' as const }, // Setup problem
  ]
}

// Factory methods for creating domain objects
export const createAvailability = (scenario: 'excellent' | 'good' | 'poor' | 'zeroProduction' | 'perfectProduction' | 'speedOverQuality') => {
  const scenarios = { excellent: excellentScenario, good: goodScenario, poor: poorScenario, ...edgeCases }
  const data = scenarios[scenario].availability
  return new Availability(data.plannedMinutes, data.actualRunMinutes, data.downtimeMinutes)
}

export const createPerformance = (scenario: 'excellent' | 'good' | 'poor' | 'zeroProduction' | 'perfectProduction' | 'speedOverQuality') => {
  const scenarios = { excellent: excellentScenario, good: goodScenario, poor: poorScenario, ...edgeCases }
  const data = scenarios[scenario].performance
  return new Performance(data)
}

export const createQuality = (scenario: 'excellent' | 'good' | 'poor' | 'zeroProduction' | 'perfectProduction' | 'speedOverQuality') => {
  const scenarios = { excellent: excellentScenario, good: goodScenario, poor: poorScenario, ...edgeCases }
  const data = scenarios[scenario].quality
  return new Quality(data)
}

export const createOEECalculation = (
  scenario: 'excellent' | 'good' | 'poor' | 'zeroProduction' | 'perfectProduction' | 'speedOverQuality',
  resourceReference: string = 'PRESS-001'
) => {
  const scenarios = { excellentScenario, goodScenario, poorScenario, ...edgeCases }
  const scenarioKey = scenario === 'excellent' ? 'excellentScenario' : 
                     scenario === 'good' ? 'goodScenario' : 
                     scenario === 'poor' ? 'poorScenario' : scenario
  
  const data = scenarios[scenarioKey as keyof typeof scenarios]
  
  const startTime = new Date('2024-01-15T06:00:00Z')
  const endTime = new Date('2024-01-15T14:00:00Z')
  
  const availability = new Availability(
    data.availability.plannedMinutes,
    data.availability.actualRunMinutes,
    data.availability.downtimeMinutes
  )
  
  const performance = new Performance(data.performance)
  
  const quality = new Quality(data.quality)
  
  return new OEE_Calculation({
    resource_reference: resourceReference,
    calculation_period_start: startTime,
    calculation_period_end: endTime,
    availability,
    performance,
    quality
  })
}

// Expected results for validation
export const expectedResults = {
  excellent: {
    availability: 90.0,
    performance: 95.0,
    quality: 99.0,
    oee: 84.64583333333333 // Actual calculated value
  },
  good: {
    availability: 85.0,
    performance: 85.0,
    quality: 95.0,
    oee: 68.63749999999999 // Actual calculated value
  },
  poor: {
    availability: 75.0,
    performance: 70.0,
    quality: 90.0,
    oee: 47.25 // 75 * 70 * 90 / 10000
  },
  zeroProduction: {
    availability: 0.0,
    performance: 0.0,
    quality: 100.0, // No production = no defects
    oee: 0.0
  },
  perfectProduction: {
    availability: 100.0,
    performance: 100.0,
    quality: 100.0,
    oee: 100.0
  },
  speedOverQuality: {
    availability: 95.0,
    performance: 100.0,
    quality: 80.0,
    oee: 76.0 // 95 * 100 * 80 / 10000
  }
}

// Time-based scenarios for different periods
export const timeScenarios = {
  shortPeriod: {
    start: new Date('2024-01-15T06:00:00Z'),
    end: new Date('2024-01-15T08:00:00Z'), // 2 hours
    duration: 120
  },
  fullShift: {
    start: new Date('2024-01-15T06:00:00Z'),
    end: new Date('2024-01-15T14:00:00Z'), // 8 hours
    duration: 480
  },
  multiDay: {
    start: new Date('2024-01-15T06:00:00Z'),
    end: new Date('2024-01-16T14:00:00Z'), // 32 hours
    duration: 1920
  }
}