import { queryDatabase, queryDatabaseSingle } from '@/lib/database/connection'
import { CounterReading } from '@/lib/types/oee'
import { ICounterDataRepository } from '@/lib/domain/ports'
import { ErrorHandler } from '@/lib/utils/errorHandler'
import { withDatabaseFallback, DatabaseFallbackStrategies } from '@/lib/database/fallbackStrategies'
import { executeWithResilience, READ_OPERATION_RETRY_CONFIG } from '@/lib/database/connectionResilience'
import { withCurrentRateCache, getQueryCache } from '@/lib/services/queryCacheService'

/**
 * Counter Data Repository Implementation
 * Handles all counter_data table operations
 * Migrated from database-queries.ts to centralize data access
 */
export class CounterDataRepository implements ICounterDataRepository {

  async getCurrentRate(deviceId: string): Promise<{
    units_per_minute: number
    timestamp: Date
  } | null> {
    return withCurrentRateCache(deviceId, async () => {
      return withDatabaseFallback(
        async () => {
          return executeWithResilience(async () => {
            const query = `
              SELECT 
                rate * 60 as units_per_minute,
                timestamp
              FROM counter_data 
              WHERE 
                device_id = $1 
                AND channel = 0
                AND timestamp > NOW() - INTERVAL '2 minutes'
              ORDER BY timestamp DESC 
              LIMIT 1`

            const result = await queryDatabaseSingle<{
              units_per_minute: number
              timestamp: string
            }>(query, [deviceId])

            if (!result) return null

            return {
              units_per_minute: result.units_per_minute,
              timestamp: new Date(result.timestamp)
            }
          }, READ_OPERATION_RETRY_CONFIG)
        },
        () => DatabaseFallbackStrategies.getCurrentRateFallback(deviceId)
      )
    })
  }

  async getQualityMetrics(deviceId: string): Promise<{
    total_production: number
    total_rejects: number
    quality_percent: number
  } | null> {
    try {
      const query = `
        SELECT 
          production.total_production,
          COALESCE(rejects.total_rejects, 0) as total_rejects,
          (production.total_production - COALESCE(rejects.total_rejects, 0)) / 
          production.total_production * 100 as quality_percent
        FROM (
          SELECT MAX(processed_value) - MIN(processed_value) as total_production
          FROM counter_data cd
          JOIN production_jobs pj ON cd.device_id = pj.device_id
          WHERE pj.status = 'active' 
            AND cd.channel = 0
            AND cd.timestamp >= pj.start_time
        ) production
        LEFT JOIN (
          SELECT MAX(processed_value) - MIN(processed_value) as total_rejects
          FROM counter_data cd
          JOIN production_jobs pj ON cd.device_id = pj.device_id
          WHERE pj.status = 'active' 
            AND cd.channel = 1
            AND cd.timestamp >= pj.start_time
        ) rejects ON true`

      const result = await queryDatabaseSingle<{
        total_production: number
        total_rejects: number
        quality_percent: number
      }>(query, [])

      return result || null
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async detectCurrentStoppage(deviceId: string): Promise<{
    stoppage_start: Date
    duration_minutes: number
  } | null> {
    try {
      const query = `
        SELECT 
          MIN(timestamp) as stoppage_start,
          EXTRACT(EPOCH FROM (NOW() - MIN(timestamp))) / 60 as duration_minutes
        FROM counter_data 
        WHERE 
          device_id = $1 
          AND channel = 0
          AND rate = 0 
          AND timestamp > NOW() - INTERVAL '1 hour'
          AND timestamp >= ALL(
            SELECT timestamp 
            FROM counter_data 
            WHERE device_id = $1 AND channel = 0 AND rate > 0 
            ORDER BY timestamp DESC 
            LIMIT 1
          )
        HAVING COUNT(*) > 12`

      const result = await queryDatabaseSingle<{
        stoppage_start: string
        duration_minutes: number
      }>(query, [deviceId])

      if (!result) return null

      return {
        stoppage_start: new Date(result.stoppage_start),
        duration_minutes: result.duration_minutes
      }
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getRateHistory(deviceId: string, hours: number): Promise<Array<{
    timestamp: Date
    units_per_minute: number
  }>> {
    try {
      const query = `
        SELECT 
          timestamp,
          rate * 60 as units_per_minute
        FROM counter_data
        WHERE 
          device_id = $1
          AND channel = 0
          AND timestamp > NOW() - INTERVAL '$2 hours'
        ORDER BY timestamp ASC`

      const results = await queryDatabase<{
        timestamp: string
        units_per_minute: number
      }>(query, [deviceId, hours])

      return results.map(result => ({
        timestamp: new Date(result.timestamp),
        units_per_minute: result.units_per_minute
      }))
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getProductionData(deviceId: string, startTime: Date, endTime?: Date): Promise<{
    actual_quantity: number
    avg_rate_per_minute: number
    data_points: number
    current_rate: number
  } | null> {
    try {
      const query = `
        SELECT 
          MAX(processed_value) - MIN(processed_value) as actual_quantity,
          AVG(rate) * 60 as avg_rate_per_minute,
          COUNT(*) as data_points,
          (
            SELECT AVG(rate) * 60 
            FROM counter_data 
            WHERE device_id = $1 AND channel = 0 
              AND timestamp >= NOW() - INTERVAL '10 minutes'
          ) as current_rate
        FROM counter_data
        WHERE device_id = $1 
          AND channel = 0
          AND timestamp >= $2
          ${endTime ? 'AND timestamp <= $3' : ''}
        HAVING COUNT(*) > 0`

      const params = endTime ? [deviceId, startTime, endTime] : [deviceId, startTime]
      
      const result = await queryDatabaseSingle<{
        actual_quantity: number
        avg_rate_per_minute: number
        data_points: number
        current_rate: number
      }>(query, params)

      return result || null
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getReadings(
    deviceId: string, 
    channel: number, 
    startTime: Date, 
    endTime?: Date
  ): Promise<CounterReading[]> {
    try {
      const query = `
        SELECT 
          timestamp as time,
          device_id,
          channel,
          raw_value,
          processed_value,
          rate,
          quality
        FROM counter_data
        WHERE 
          device_id = $1
          AND channel = $2
          AND timestamp >= $3
          ${endTime ? 'AND timestamp <= $4' : ''}
        ORDER BY timestamp ASC`

      const params = endTime ? [deviceId, channel, startTime, endTime] : [deviceId, channel, startTime]
      
      const results = await queryDatabase<{
        time: string
        device_id: string
        channel: number
        raw_value: number
        processed_value: number
        rate: number
        quality: string | number
      }>(query, params)

      return results.map(result => ({
        time: new Date(result.time),
        device_id: result.device_id,
        channel: result.channel,
        raw_value: result.raw_value,
        processed_value: result.processed_value,
        rate: result.rate,
        quality: result.quality
      }))
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getLatestReading(deviceId: string, channel: number): Promise<CounterReading | null> {
    return withDatabaseFallback(
      async () => {
        return executeWithResilience(async () => {
          const query = `
            SELECT 
              timestamp as time,
              device_id,
              channel,
              raw_value,
              processed_value,
              rate,
              quality
            FROM counter_data
            WHERE 
              device_id = $1
              AND channel = $2
            ORDER BY timestamp DESC
            LIMIT 1`

          const result = await queryDatabaseSingle<{
            time: string
            device_id: string
            channel: number
            raw_value: number
            processed_value: number
            rate: number
            quality: string | number
          }>(query, [deviceId, channel])

          if (!result) return null

          const reading: CounterReading = {
            time: new Date(result.time),
            device_id: result.device_id,
            channel: result.channel,
            raw_value: result.raw_value,
            processed_value: result.processed_value,
            rate: result.rate,
            quality: result.quality
          }

          return reading
        }, READ_OPERATION_RETRY_CONFIG)
      },
      () => DatabaseFallbackStrategies.getLatestReadingFallback(deviceId, channel),
      (result) => {
        // Cache successful result for fallback use
        if (result) {
          DatabaseFallbackStrategies.cacheForFallback({ reading: result })
        }
      }
    )
  }

  async hasRecentData(deviceId: string, withinMinutes: number): Promise<boolean> {
    try {
      const query = `
        SELECT COUNT(*) as count
        FROM counter_data
        WHERE 
          device_id = $1
          AND timestamp > NOW() - INTERVAL '$2 minutes'`

      const result = await queryDatabaseSingle<{ count: number }>(query, [deviceId, withinMinutes])
      return (result?.count || 0) > 0
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  /**
   * Phase 4: Optimized aggregate calculations for performance
   * Uses TimescaleDB time_bucket for efficient aggregation
   */

  /**
   * Get hourly production aggregates with caching
   */
  async getHourlyProductionAggregates(
    deviceId: string, 
    hours: number = 24
  ): Promise<Array<{
    hour: Date
    total_production: number
    avg_rate: number
    max_rate: number
    uptime_minutes: number
  }>> {
    const cache = getQueryCache()
    const cacheKey = `hourly_production:${deviceId}:${hours}`
    
    return cache.getOrSet(cacheKey, async () => {
      const query = `
        SELECT 
          time_bucket('1 hour', timestamp) AS hour,
          MAX(processed_value) - MIN(processed_value) as total_production,
          AVG(rate) * 60 as avg_rate,
          MAX(rate) * 60 as max_rate,
          COUNT(CASE WHEN rate > 0 THEN 1 END) as uptime_readings,
          COUNT(*) as total_readings
        FROM counter_data
        WHERE 
          device_id = $1
          AND channel = 0
          AND timestamp >= NOW() - INTERVAL '$2 hours'
        GROUP BY hour
        ORDER BY hour ASC`

      const results = await queryDatabase<{
        hour: string
        total_production: number
        avg_rate: number
        max_rate: number
        uptime_readings: number
        total_readings: number
      }>(query, [deviceId, hours])

      return results.map(result => ({
        hour: new Date(result.hour),
        total_production: Math.max(0, result.total_production || 0),
        avg_rate: Math.round((result.avg_rate || 0) * 10) / 10,
        max_rate: Math.round((result.max_rate || 0) * 10) / 10,
        uptime_minutes: Math.round((result.uptime_readings / Math.max(1, result.total_readings)) * 60)
      }))
    }, 300000) // Cache for 5 minutes
  }

  /**
   * Get shift summary data (optimized for daily reporting)
   */
  async getShiftSummary(
    deviceId: string,
    shiftStart: Date,
    shiftEnd: Date
  ): Promise<{
    total_production: number
    total_rejects: number
    quality_rate: number
    availability_rate: number
    avg_performance_rate: number
    oee_estimate: number
  } | null> {
    const cache = getQueryCache()
    const cacheKey = `shift_summary:${deviceId}:${shiftStart.getTime()}:${shiftEnd.getTime()}`
    
    return cache.getOrSet(cacheKey, async () => {
      const query = `
        WITH production_data AS (
          SELECT 
            MAX(processed_value) - MIN(processed_value) as total_production,
            AVG(rate) * 60 as avg_rate,
            COUNT(CASE WHEN rate > 0 THEN 1 END) as uptime_readings,
            COUNT(*) as total_readings
          FROM counter_data
          WHERE 
            device_id = $1
            AND channel = 0
            AND timestamp >= $2
            AND timestamp <= $3
        ),
        reject_data AS (
          SELECT 
            COALESCE(MAX(processed_value) - MIN(processed_value), 0) as total_rejects
          FROM counter_data
          WHERE 
            device_id = $1
            AND channel = 1
            AND timestamp >= $2
            AND timestamp <= $3
        ),
        job_data AS (
          SELECT AVG(target_rate) as avg_target_rate
          FROM production_jobs
          WHERE 
            device_id = $1
            AND start_time <= $3
            AND (end_time >= $2 OR end_time IS NULL)
        )
        SELECT 
          p.total_production,
          r.total_rejects,
          CASE 
            WHEN p.total_production > 0 
            THEN ((p.total_production - r.total_rejects) / p.total_production * 100)
            ELSE 100
          END as quality_rate,
          (p.uptime_readings::float / GREATEST(p.total_readings, 1) * 100) as availability_rate,
          CASE 
            WHEN j.avg_target_rate > 0
            THEN (p.avg_rate / j.avg_target_rate * 100)
            ELSE 0
          END as avg_performance_rate
        FROM production_data p, reject_data r, job_data j`

      const result = await queryDatabaseSingle<{
        total_production: number
        total_rejects: number
        quality_rate: number
        availability_rate: number
        avg_performance_rate: number
      }>(query, [deviceId, shiftStart, shiftEnd])

      if (!result) return null

      const oee_estimate = (result.availability_rate / 100) * 
                          (result.avg_performance_rate / 100) * 
                          (result.quality_rate / 100) * 100

      return {
        total_production: Math.max(0, result.total_production || 0),
        total_rejects: Math.max(0, result.total_rejects || 0),
        quality_rate: Math.round((result.quality_rate || 100) * 10) / 10,
        availability_rate: Math.round((result.availability_rate || 0) * 10) / 10,
        avg_performance_rate: Math.round((result.avg_performance_rate || 0) * 10) / 10,
        oee_estimate: Math.round(oee_estimate * 10) / 10
      }
    }, 600000) // Cache for 10 minutes
  }

  /**
   * Get daily production trends (optimized for weekly/monthly views)
   */
  async getDailyProductionTrends(
    deviceId: string,
    days: number = 7
  ): Promise<Array<{
    date: Date
    total_production: number
    avg_oee: number
    uptime_hours: number
    total_jobs: number
  }>> {
    const cache = getQueryCache()
    const cacheKey = `daily_trends:${deviceId}:${days}`
    
    return cache.getOrSet(cacheKey, async () => {
      const query = `
        WITH daily_production AS (
          SELECT 
            date_trunc('day', timestamp) as date,
            MAX(processed_value) - MIN(processed_value) as daily_production,
            COUNT(CASE WHEN rate > 0 THEN 1 END) as uptime_readings,
            COUNT(*) as total_readings
          FROM counter_data
          WHERE 
            device_id = $1
            AND channel = 0
            AND timestamp >= NOW() - INTERVAL '$2 days'
          GROUP BY date_trunc('day', timestamp)
        ),
        daily_jobs AS (
          SELECT 
            date_trunc('day', start_time) as date,
            COUNT(*) as job_count
          FROM production_jobs
          WHERE 
            device_id = $1
            AND start_time >= NOW() - INTERVAL '$2 days'
          GROUP BY date_trunc('day', start_time)
        )
        SELECT 
          p.date,
          p.daily_production as total_production,
          (p.uptime_readings::float / GREATEST(p.total_readings, 1) * 100) as availability_estimate,
          (p.uptime_readings * 5.0 / 60) as uptime_hours, -- 5-second intervals
          COALESCE(j.job_count, 0) as total_jobs
        FROM daily_production p
        LEFT JOIN daily_jobs j ON p.date = j.date
        ORDER BY p.date ASC`

      const results = await queryDatabase<{
        date: string
        total_production: number
        availability_estimate: number
        uptime_hours: number
        total_jobs: number
      }>(query, [deviceId, days])

      return results.map(result => ({
        date: new Date(result.date),
        total_production: Math.max(0, result.total_production || 0),
        avg_oee: Math.round((result.availability_estimate || 0) * 0.8 * 10) / 10, // Rough OEE estimate
        uptime_hours: Math.round((result.uptime_hours || 0) * 10) / 10,
        total_jobs: result.total_jobs || 0
      }))
    }, 3600000) // Cache for 1 hour
  }
}