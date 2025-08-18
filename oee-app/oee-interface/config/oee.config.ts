/**
 * OEE Business Configuration
 * Central configuration for all OEE-specific business rules, thresholds,
 * and production parameters. These values define how OEE is calculated
 * and what constitutes good/excellent performance.
 */

export const oeeConfig = {
  // OEE Rating Thresholds (all values are percentages 0-100)
  thresholds: {
    // Availability thresholds
    availability: {
      excellent: Number(process.env.AVAILABILITY_EXCELLENT) || 90,
      good: Number(process.env.AVAILABILITY_GOOD) || 85,
      needsImprovement: 85, // Below this threshold
    },
    
    // Performance/Efficiency thresholds
    performance: {
      excellent: Number(process.env.PERFORMANCE_EXCELLENT) || 95,
      good: Number(process.env.PERFORMANCE_GOOD) || 85,
      needsImprovement: 85, // Below this threshold
    },
    
    // Quality thresholds
    quality: {
      excellent: Number(process.env.QUALITY_EXCELLENT) || 99,
      good: Number(process.env.QUALITY_GOOD) || 95,
      needsImprovement: 95, // Below this threshold
    },
    
    // Overall OEE thresholds
    oee: {
      excellent: Number(process.env.OEE_EXCELLENT) || 85,
      good: Number(process.env.OEE_GOOD) || 65,
      needsImprovement: 65, // Below this threshold
    }
  },
  
  // Production Configuration
  production: {
    // Target rate limits (units per minute)
    targetRate: {
      min: Number(process.env.MIN_TARGET_RATE) || 10,
      max: Number(process.env.MAX_TARGET_RATE) || 1000,
      validationMax: Number(process.env.MAX_RATE_VALIDATION) || 10000,
    },
    
    // Default values for production jobs
    defaults: {
      quantity: Number(process.env.DEFAULT_JOB_QUANTITY) || 1000,
      targetRate: Number(process.env.DEFAULT_TARGET_RATE) || 100,
    },
    
    // Job validation rules
    validation: {
      jobNumberPattern: process.env.JOB_NUMBER_PATTERN || '^(JOB|PART)-\\d{4,}',
      minJobNumberDigits: 4,
      requireUppercasePartNumber: true,
    }
  },
  
  // Alert and Attention Thresholds
  alerts: {
    // When to flag metrics as requiring attention
    requiresAttention: {
      oee: Number(process.env.OEE_ATTENTION) || 65,
      availability: Number(process.env.AVAILABILITY_ATTENTION) || 85,
      performance: Number(process.env.PERFORMANCE_ATTENTION) || 85,
      quality: Number(process.env.QUALITY_ATTENTION) || 95,
    },
    
    // Data freshness thresholds
    dataAge: {
      warning: Number(process.env.DATA_AGE_WARNING_MINUTES) || 5, // minutes
      error: Number(process.env.DATA_AGE_ERROR_MINUTES) || 15, // minutes
    }
  },
  
  // Time Range Configuration
  timeRanges: {
    // Default time ranges for queries (in minutes)
    defaultLookback: Number(process.env.DEFAULT_LOOKBACK_MINUTES) || 60, // 1 hour
    shiftDuration: Number(process.env.SHIFT_DURATION_HOURS) || 8, // hours
    
    // Maximum allowed query ranges
    maxQueryRange: {
      days: Number(process.env.MAX_QUERY_DAYS) || 30,
      minutes: Number(process.env.MAX_QUERY_DAYS) * 24 * 60 || 43200, // 30 days in minutes
    },
    
    // History display settings
    chartHistory: {
      defaultHours: Number(process.env.CHART_HISTORY_HOURS) || 8,
      maxHours: Number(process.env.CHART_MAX_HISTORY_HOURS) || 24,
    }
  },
  
  // Stoppage Classification
  stoppages: {
    // Valid stoppage categories
    categories: [
      'Equipment Failure',
      'Setup/Changeover',
      'Material Issue',
      'Quality Issue',
      'Operator Issue',
      'Planned Maintenance',
      'Other'
    ],
    
    // Stoppage detection parameters
    detection: {
      minDurationSeconds: Number(process.env.MIN_STOPPAGE_SECONDS) || 30,
      maxCommentLength: 500,
    }
  },
  
  // Calculation Parameters
  calculations: {
    // Minimum data requirements for valid calculations
    minimumDataPoints: Number(process.env.MIN_DATA_POINTS) || 12, // At least 1 minute of 5-second data
    
    // Rounding precision
    precision: {
      rate: 1, // decimal places for rate display
      percentage: 1, // decimal places for percentages
      oee: 1, // decimal places for OEE score
    }
  }
} as const

// Type for the configuration
export type OeeConfig = typeof oeeConfig

// Helper functions for common calculations
export const isExcellentRating = (value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): boolean => {
  return value >= oeeConfig.thresholds[metric].excellent
}

export const isGoodRating = (value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): boolean => {
  return value >= oeeConfig.thresholds[metric].good
}

export const getRating = (value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): 'excellent' | 'good' | 'needs-improvement' => {
  if (value >= oeeConfig.thresholds[metric].excellent) return 'excellent'
  if (value >= oeeConfig.thresholds[metric].good) return 'good'
  return 'needs-improvement'
}

export const requiresAttention = (metrics: {
  oee?: number
  availability?: number
  performance?: number
  quality?: number
}): boolean => {
  const alerts = oeeConfig.alerts.requiresAttention
  return (
    (metrics.oee !== undefined && metrics.oee < alerts.oee) ||
    (metrics.availability !== undefined && metrics.availability < alerts.availability) ||
    (metrics.performance !== undefined && metrics.performance < alerts.performance) ||
    (metrics.quality !== undefined && metrics.quality < alerts.quality)
  )
}