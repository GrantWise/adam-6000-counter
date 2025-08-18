# OEE Interface Configuration

This document provides comprehensive documentation for all configuration options in the OEE Interface application.

## Overview

The configuration system is designed to centralize all application settings, making it easy to adjust behavior without code changes. Configuration values can be set via environment variables for deployment flexibility.

## Configuration Files

### `oee.config.ts`
Contains all OEE-specific business rules, thresholds, and production parameters.

### `app.config.ts`
Contains application-level settings for devices, timing, database connections, etc.

### `validation.ts`
Provides validation functions to ensure configuration values are valid and follow business logic.

### `index.ts`
Main export point that aggregates all configurations and provides helper functions.

## OEE Configuration (`oee.config.ts`)

### Thresholds

Define what constitutes "excellent", "good", and "needs improvement" performance levels.

#### Availability Thresholds
```typescript
thresholds.availability: {
  excellent: 90,     // World-class availability target (%)
  good: 85,          // Acceptable availability level (%)
  needsImprovement: 85 // Below this requires attention (%)
}
```

**Environment Variables:**
- `AVAILABILITY_EXCELLENT`: Excellent threshold (default: 90)
- `AVAILABILITY_GOOD`: Good threshold (default: 85)

#### Performance Thresholds
```typescript
thresholds.performance: {
  excellent: 95,     // World-class performance target (%)
  good: 85,          // Acceptable performance level (%)
  needsImprovement: 85 // Below this requires attention (%)
}
```

**Environment Variables:**
- `PERFORMANCE_EXCELLENT`: Excellent threshold (default: 95)
- `PERFORMANCE_GOOD`: Good threshold (default: 85)

#### Quality Thresholds
```typescript
thresholds.quality: {
  excellent: 99,     // World-class quality target (%)
  good: 95,          // Acceptable quality level (%)
  needsImprovement: 95 // Below this requires attention (%)
}
```

**Environment Variables:**
- `QUALITY_EXCELLENT`: Excellent threshold (default: 99)
- `QUALITY_GOOD`: Good threshold (default: 95)

#### Overall OEE Thresholds
```typescript
thresholds.oee: {
  excellent: 85,     // World-class OEE target (%)
  good: 65,          // Acceptable OEE level (%)
  needsImprovement: 65 // Below this requires attention (%)
}
```

**Environment Variables:**
- `OEE_EXCELLENT`: Excellent threshold (default: 85)
- `OEE_GOOD`: Good threshold (default: 65)

### Production Configuration

#### Target Rate Limits
```typescript
production.targetRate: {
  min: 10,           // Minimum allowed target rate (pieces/min)
  max: 1000,         // Maximum operational target rate (pieces/min)
  validationMax: 10000 // Maximum for validation (pieces/min)
}
```

**Environment Variables:**
- `MIN_TARGET_RATE`: Minimum target rate (default: 10)
- `MAX_TARGET_RATE`: Maximum target rate (default: 1000)
- `MAX_RATE_VALIDATION`: Validation maximum (default: 10000)

#### Default Values
```typescript
production.defaults: {
  quantity: 1000,    // Default job quantity (pieces)
  targetRate: 100    // Default target rate (pieces/min)
}
```

**Environment Variables:**
- `DEFAULT_JOB_QUANTITY`: Default quantity (default: 1000)
- `DEFAULT_TARGET_RATE`: Default target rate (default: 100)

#### Validation Rules
```typescript
production.validation: {
  jobNumberPattern: '^(JOB|PART)-\\d{4,}', // Regex pattern for job numbers
  minJobNumberDigits: 4,                    // Minimum digits in job number
  requireUppercasePartNumber: true          // Require uppercase part numbers
}
```

**Environment Variables:**
- `JOB_NUMBER_PATTERN`: Job number regex pattern

### Alert Configuration

#### Attention Thresholds
```typescript
alerts.requiresAttention: {
  oee: 65,           // OEE below this triggers attention (%)
  availability: 85,  // Availability below this triggers attention (%)
  performance: 85,   // Performance below this triggers attention (%)
  quality: 95        // Quality below this triggers attention (%)
}
```

**Environment Variables:**
- `OEE_ATTENTION`: OEE attention threshold (default: 65)
- `AVAILABILITY_ATTENTION`: Availability attention threshold (default: 85)
- `PERFORMANCE_ATTENTION`: Performance attention threshold (default: 85)
- `QUALITY_ATTENTION`: Quality attention threshold (default: 95)

#### Data Freshness
```typescript
alerts.dataAge: {
  warning: 5,        // Warn if data is older than 5 minutes
  error: 15          // Error if data is older than 15 minutes
}
```

**Environment Variables:**
- `DATA_AGE_WARNING_MINUTES`: Warning threshold in minutes (default: 5)
- `DATA_AGE_ERROR_MINUTES`: Error threshold in minutes (default: 15)

### Time Range Configuration

#### Shift and Query Ranges
```typescript
timeRanges: {
  defaultLookback: 60,     // Default lookback period (minutes)
  shiftDuration: 8,        // Standard shift duration (hours)
  maxQueryRange: {
    days: 30,              // Maximum query range (days)
    minutes: 43200         // Maximum query range (minutes)
  },
  chartHistory: {
    defaultHours: 8,       // Default chart history (hours)
    maxHours: 24           // Maximum chart history (hours)
  }
}
```

**Environment Variables:**
- `DEFAULT_LOOKBACK_MINUTES`: Default lookback (default: 60)
- `SHIFT_DURATION_HOURS`: Shift duration (default: 8)
- `MAX_QUERY_DAYS`: Maximum query days (default: 30)
- `CHART_HISTORY_HOURS`: Chart history hours (default: 8)
- `CHART_MAX_HISTORY_HOURS`: Max chart history (default: 24)

### Stoppage Configuration

#### Categories and Detection
```typescript
stoppages: {
  categories: [            // Valid stoppage categories
    'Equipment Failure',
    'Setup/Changeover',
    'Material Issue',
    'Quality Issue',
    'Operator Issue',
    'Planned Maintenance',
    'Other'
  ],
  detection: {
    minDurationSeconds: 30, // Minimum stoppage duration (seconds)
    maxCommentLength: 500   // Maximum comment length
  }
}
```

**Environment Variables:**
- `MIN_STOPPAGE_SECONDS`: Minimum stoppage duration (default: 30)

### Calculation Parameters

#### Data Requirements and Precision
```typescript
calculations: {
  minimumDataPoints: 12,   // Minimum data points for valid calculations
  precision: {
    rate: 1,               // Decimal places for rate display
    percentage: 1,         // Decimal places for percentages
    oee: 1                 // Decimal places for OEE score
  }
}
```

**Environment Variables:**
- `MIN_DATA_POINTS`: Minimum data points (default: 12)

## Helper Functions

### Rating Functions
```typescript
// Check if value meets excellent threshold
isExcellentRating(value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): boolean

// Check if value meets good threshold
isGoodRating(value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): boolean

// Get rating category
getRating(value: number, metric: 'availability' | 'performance' | 'quality' | 'oee'): 'excellent' | 'good' | 'needs-improvement'

// Check if metrics require attention
requiresAttention(metrics: {
  oee?: number
  availability?: number
  performance?: number
  quality?: number
}): boolean
```

## Configuration Validation

The configuration system includes comprehensive validation to ensure all values are within acceptable ranges and follow business logic constraints.

### Validation Rules

1. **Threshold Ordering**: Excellent ≥ Good ≥ Needs Improvement
2. **Range Validation**: All percentages must be 0-100
3. **Business Logic**: Production rates must be positive and logical
4. **Industry Standards**: Warnings for values below industry benchmarks

### Running Validation

```typescript
import { validateOeeConfig, validateOeeConfigOrThrow } from './config'

// Get validation result
const result = validateOeeConfig()
if (!result.isValid) {
  console.error('Configuration errors:', result.errors)
}

// Throw on validation failure
validateOeeConfigOrThrow() // Throws if invalid
```

### Validation Output

```typescript
interface ConfigValidationResult {
  isValid: boolean
  errors: ConfigValidationError[]    // Critical issues that prevent operation
  warnings: ConfigValidationError[] // Issues that should be addressed
}

interface ConfigValidationError {
  field: string           // Configuration field name
  value: number          // Current value
  message: string        // Description of the issue
  severity: 'error' | 'warning'
}
```

## Best Practices

### Environment Variables
1. **Production**: Always set explicit values for all thresholds
2. **Development**: Use defaults for rapid iteration
3. **Testing**: Use consistent test environment variables

### Threshold Setting
1. **Start Conservative**: Begin with industry-standard thresholds
2. **Gradual Improvement**: Raise thresholds as performance improves
3. **Data-Driven**: Base thresholds on historical performance data

### Validation
1. **Startup Validation**: Validate configuration on application startup
2. **Runtime Checks**: Periodically validate configuration in long-running processes
3. **Deployment**: Include validation in deployment pipelines

## Troubleshooting

### Common Issues

1. **Threshold Order**: Ensure excellent ≥ good ≥ needs improvement
2. **Environment Variables**: Check variable names and types
3. **Range Validation**: Verify percentages are within 0-100 range

### Validation Errors

- **"Excellent threshold must be >= good threshold"**: Check threshold ordering
- **"Value must be between 0 and 100"**: Verify percentage ranges
- **"Target rate must be positive"**: Check production rate values

### Performance Warnings

- **"Below industry standard"**: Consider raising thresholds for world-class performance
- **"Very low minimum data points"**: May produce unreliable calculations
- **"Query range exceeds 1 year"**: May impact performance

## Migration Guide

When updating configuration:

1. **Backup Current Config**: Save current working configuration
2. **Validate New Values**: Use validation functions before deployment
3. **Gradual Rollout**: Test new thresholds in staging environment
4. **Monitor Impact**: Watch for unexpected alert triggers or missing data

## Support

For configuration questions or issues:
1. Check validation error messages for specific guidance
2. Review industry standard values for your manufacturing environment
3. Consult with production engineers for appropriate thresholds