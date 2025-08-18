import { afterEach } from 'vitest'

// Mock environment variables for consistent testing
process.env.NODE_ENV = 'test'

// Set default test environment variables
process.env.AVAILABILITY_EXCELLENT = '90'
process.env.PERFORMANCE_EXCELLENT = '95'
process.env.QUALITY_EXCELLENT = '99'
process.env.OEE_EXCELLENT = '85'
process.env.SHIFT_DURATION_HOURS = '8'
process.env.MIN_STOPPAGE_SECONDS = '30'