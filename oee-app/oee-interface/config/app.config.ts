/**
 * Application Configuration
 * Central configuration for all application-level settings including
 * device configuration, timing intervals, and UI preferences.
 */

export const appConfig = {
  // Device Settings
  device: {
    defaultId: process.env.NEXT_PUBLIC_DEFAULT_DEVICE_ID || process.env.DEFAULT_DEVICE_ID || 'Device001',
    channels: {
      // Channel mappings - can be overridden per device type
      production: Number(process.env.PRODUCTION_CHANNEL) || 0,
      rejects: Number(process.env.REJECTS_CHANNEL) || 1,
    },
    type: process.env.DEVICE_TYPE || 'generic',
    dataSource: process.env.DATA_SOURCE_TYPE || 'timescaledb',
  },
  
  // Timing Settings (all values in milliseconds unless noted)
  timing: {
    // Polling intervals for real-time data
    polling: {
      metrics: Number(process.env.METRICS_POLLING_INTERVAL) || 5000, // 5 seconds
      chartUpdate: Number(process.env.CHART_UPDATE_INTERVAL) || 30000, // 30 seconds
    },
    
    // Request timeouts
    timeouts: {
      api: Number(process.env.API_TIMEOUT) || 10000, // 10 seconds
      database: Number(process.env.DB_TIMEOUT) || 30000, // 30 seconds
      databaseIdle: Number(process.env.DB_IDLE_TIMEOUT) || 30000, // 30 seconds
    },
    
    // Retry configuration for failed requests
    retry: {
      baseDelay: Number(process.env.RETRY_BASE_DELAY) || 1000, // 1 second
      maxDelay: Number(process.env.RETRY_MAX_DELAY) || 10000, // 10 seconds
      maxAttempts: Number(process.env.MAX_RETRY_ATTEMPTS) || 5,
      circuitBreakerReset: 30000, // 30 seconds to reset circuit breaker
    },
    
    // Data collection and processing intervals
    data: {
      collectionInterval: Number(process.env.DATA_COLLECTION_INTERVAL) || 5, // seconds between readings
      stoppageThreshold: Number(process.env.STOPPAGE_THRESHOLD_MINUTES) || 2, // minutes of zero rate to detect stoppage
      assumedReadingInterval: 5, // seconds - for legacy calculations
    }
  },
  
  // UI Settings
  ui: {
    // Notification display settings
    notifications: {
      defaultDuration: Number(process.env.NOTIFICATION_DURATION) || 5000, // 5 seconds
      activityThrottle: 1000, // 1 second throttle for activity tracking
    },
    
    // Chart display configuration
    chart: {
      maxDataPoints: Number(process.env.CHART_MAX_POINTS) || 96, // 8 hours at 5-minute intervals
      duplicateThreshold: 30000, // prevent duplicate points within 30 seconds
      defaultHistoryHours: 8, // hours of history to display
    }
  },
  
  // Database Connection Settings
  database: {
    connectionPool: {
      max: Number(process.env.DB_POOL_MAX) || 10,
      idleTimeoutMillis: Number(process.env.DB_IDLE_TIMEOUT) || 30000,
      connectionTimeoutMillis: Number(process.env.DB_CONNECTION_TIMEOUT) || 2000,
    }
  }
} as const

// Type for the configuration
export type AppConfig = typeof appConfig