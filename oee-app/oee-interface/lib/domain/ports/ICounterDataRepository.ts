import { CounterReading } from '@/lib/types/oee'

/**
 * Counter Data Repository Interface
 * Defines the contract for counter data access operations
 * Handles all TimescaleDB counter_data table queries
 */
export interface ICounterDataRepository {
  /**
   * Get current production rate
   */
  getCurrentRate(deviceId: string): Promise<{
    units_per_minute: number
    timestamp: Date
  } | null>

  /**
   * Get quality metrics for current job
   */
  getQualityMetrics(deviceId: string): Promise<{
    total_production: number
    total_rejects: number
    quality_percent: number
  } | null>

  /**
   * Detect current stoppage based on zero rate readings
   */
  detectCurrentStoppage(deviceId: string): Promise<{
    stoppage_start: Date
    duration_minutes: number
  } | null>

  /**
   * Get rate history for charts
   */
  getRateHistory(deviceId: string, hours: number): Promise<Array<{
    timestamp: Date
    units_per_minute: number
  }>>

  /**
   * Get production data for a time period
   */
  getProductionData(deviceId: string, startTime: Date, endTime?: Date): Promise<{
    actual_quantity: number
    avg_rate_per_minute: number
    data_points: number
    current_rate: number
  } | null>

  /**
   * Get counter readings for a specific channel and time range
   */
  getReadings(
    deviceId: string, 
    channel: number, 
    startTime: Date, 
    endTime?: Date
  ): Promise<CounterReading[]>

  /**
   * Get latest reading for a device and channel
   */
  getLatestReading(deviceId: string, channel: number): Promise<CounterReading | null>

  /**
   * Check if device has recent data (for availability calculation)
   */
  hasRecentData(deviceId: string, withinMinutes: number): Promise<boolean>
}