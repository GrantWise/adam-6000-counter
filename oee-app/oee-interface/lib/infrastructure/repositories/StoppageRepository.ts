import { queryDatabase, queryDatabaseSingle } from '@/lib/database/connection'
import { StoppageEvent, StoppageClassificationRequest } from '@/lib/types/oee'
import { Downtime_Record } from '@/lib/domain/models'
import { IStoppageRepository } from '@/lib/domain/ports'
import { ErrorHandler } from '@/lib/utils/errorHandler'

/**
 * Stoppage Repository Implementation
 * Handles all stoppage_events table operations
 * Migrated from StoppageService to centralize data access
 */
export class StoppageRepository implements IStoppageRepository {

  async create(deviceId: string, startTime: Date, jobId?: number): Promise<number> {
    try {
      const insertQuery = `
        INSERT INTO stoppage_events (
          device_id,
          job_id,
          start_time,
          status
        ) VALUES ($1, $2, $3, 'unclassified')
        RETURNING event_id`

      const result = await queryDatabaseSingle<{ event_id: number }>(
        insertQuery,
        [deviceId, jobId || null, startTime]
      )

      if (!result) {
        throw new Error('Failed to create stoppage event')
      }

      return result.event_id
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async end(eventId: number, endTime: Date): Promise<void> {
    try {
      const updateQuery = `
        UPDATE stoppage_events 
        SET 
          end_time = $2,
          duration_minutes = EXTRACT(EPOCH FROM ($2 - start_time)) / 60
        WHERE event_id = $1 AND end_time IS NULL`

      await queryDatabase(updateQuery, [eventId, endTime])
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async findUnclassified(deviceId: string): Promise<Downtime_Record[]> {
    try {
      const query = `
        SELECT 
          event_id,
          device_id,
          job_id,
          start_time,
          end_time,
          duration_minutes,
          category,
          sub_category,
          comments,
          classified_at,
          operator_id,
          status
        FROM stoppage_events
        WHERE device_id = $1 AND status = 'unclassified'
        ORDER BY start_time DESC
        LIMIT 50`

      const events = await queryDatabase<StoppageEvent>(query, [deviceId])
      
      // Convert to domain models
      return events.map(event => new Downtime_Record({
        downtime_id: `DT-${event.event_id}`,
        resource_reference: event.device_id,
        work_order_reference: event.job_id?.toString(),
        downtime_start: new Date(event.start_time),
        downtime_end: event.end_time ? new Date(event.end_time) : undefined,
        downtime_category: (event.category as 'planned' | 'unplanned') ?? 'unplanned',
        downtime_reason_code: event.sub_category || 'UNCLASSIFIED',
        downtime_reason_description: event.comments || 'Unclassified downtime',
        operator_comments: event.comments,
        classified_by: event.operator_id?.toString(),
        classified_at: event.classified_at ? new Date(event.classified_at) : undefined,
        status: event.status === 'classified' ? 'classified' : event.end_time ? 'ended' : 'active'
      }))
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async findUnclassifiedLegacy(deviceId: string): Promise<StoppageEvent[]> {
    try {
      const query = `
        SELECT 
          event_id,
          device_id,
          job_id,
          start_time,
          end_time,
          duration_minutes,
          category,
          sub_category,
          comments,
          classified_at,
          operator_id,
          status
        FROM stoppage_events
        WHERE device_id = $1 AND status = 'unclassified'
        ORDER BY start_time DESC
        LIMIT 50`

      return await queryDatabase<StoppageEvent>(query, [deviceId])
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async classify(eventId: number, classification: StoppageClassificationRequest): Promise<void> {
    try {
      const updateQuery = `
        UPDATE stoppage_events 
        SET 
          category = $2,
          sub_category = $3,
          comments = $4,
          operator_id = $5,
          classified_at = NOW(),
          status = 'classified'
        WHERE event_id = $1`

      await queryDatabase(updateQuery, [
        eventId,
        classification.category,
        classification.subCategory,
        classification.comments || null,
        classification.operatorId
      ])
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getStatistics(deviceId: string, startTime: Date, endTime: Date): Promise<{
    totalStoppages: number
    totalDowntimeMinutes: number
    averageStoppageMinutes: number
    stoppagesByCategory: Array<{ 
      category: string
      count: number
      totalMinutes: number 
    }>
  }> {
    try {
      // Get overall statistics
      const statsQuery = `
        SELECT 
          COUNT(*) as total_stoppages,
          COALESCE(SUM(duration_minutes), 0) as total_downtime_minutes,
          COALESCE(AVG(duration_minutes), 0) as average_stoppage_minutes
        FROM stoppage_events
        WHERE device_id = $1 
          AND start_time >= $2 
          AND start_time <= $3
          AND end_time IS NOT NULL`

      const stats = await queryDatabaseSingle<{
        total_stoppages: number
        total_downtime_minutes: number
        average_stoppage_minutes: number
      }>(statsQuery, [deviceId, startTime, endTime])

      // Get breakdown by category
      const categoryQuery = `
        SELECT 
          COALESCE(category, 'Unclassified') as category,
          COUNT(*) as count,
          COALESCE(SUM(duration_minutes), 0) as total_minutes
        FROM stoppage_events
        WHERE device_id = $1 
          AND start_time >= $2 
          AND start_time <= $3
          AND end_time IS NOT NULL
        GROUP BY category
        ORDER BY total_minutes DESC`

      const categoryStats = await queryDatabase<{
        category: string
        count: number
        total_minutes: number
      }>(categoryQuery, [deviceId, startTime, endTime])

      return {
        totalStoppages: stats?.total_stoppages || 0,
        totalDowntimeMinutes: stats?.total_downtime_minutes || 0,
        averageStoppageMinutes: Math.round((stats?.average_stoppage_minutes || 0) * 10) / 10,
        stoppagesByCategory: categoryStats.map(stat => ({
          category: stat.category,
          count: stat.count,
          totalMinutes: stat.total_minutes
        }))
      }
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async findCurrentDowntime(deviceId: string): Promise<Downtime_Record | null> {
    try {
      const query = `
        SELECT 
          event_id,
          device_id,
          job_id,
          start_time,
          category,
          sub_category,
          comments,
          operator_id
        FROM stoppage_events 
        WHERE device_id = $1 AND end_time IS NULL
        ORDER BY start_time DESC 
        LIMIT 1`

      const event = await queryDatabaseSingle<{
        event_id: number
        device_id: string
        job_id?: number
        start_time: string
        category?: string
        sub_category?: string
        comments?: string
        operator_id?: string
      }>(query, [deviceId])

      if (!event) return null

      return new Downtime_Record({
        downtime_id: `DT-${event.event_id}`,
        resource_reference: event.device_id,
        work_order_reference: event.job_id?.toString(),
        downtime_start: new Date(event.start_time),
        downtime_category: (event.category as 'planned' | 'unplanned') ?? 'unplanned',
        downtime_reason_code: event.sub_category || 'UNCLASSIFIED',
        downtime_reason_description: event.comments || 'Automatic stoppage detection',
        status: 'active'
      })
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async findActiveEvent(deviceId: string): Promise<{
    event_id: number
    start_time: Date
  } | null> {
    try {
      const query = `
        SELECT event_id, start_time
        FROM stoppage_events 
        WHERE device_id = $1 AND end_time IS NULL
        ORDER BY start_time DESC 
        LIMIT 1`

      const result = await queryDatabaseSingle<{
        event_id: number
        start_time: string
      }>(query, [deviceId])

      if (!result) return null

      return {
        event_id: result.event_id,
        start_time: new Date(result.start_time)
      }
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getCurrentJobId(deviceId: string): Promise<number | null> {
    try {
      const jobQuery = `
        SELECT job_id
        FROM production_jobs
        WHERE device_id = $1 AND status = 'active'
        ORDER BY start_time DESC
        LIMIT 1`

      const job = await queryDatabaseSingle<{ job_id: number }>(jobQuery, [deviceId])
      return job?.job_id || null
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }
}