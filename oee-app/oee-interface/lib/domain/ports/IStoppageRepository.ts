import { StoppageEvent, StoppageInfo, StoppageClassificationRequest } from '@/lib/types/oee'
import { Downtime_Record } from '../models'

/**
 * Stoppage Repository Interface
 * Defines the contract for stoppage event data access operations
 * Handles all stoppage_events table queries and downtime tracking
 */
export interface IStoppageRepository {
  /**
   * Create a new stoppage event record
   */
  create(deviceId: string, startTime: Date, jobId?: number): Promise<number>

  /**
   * End an active stoppage event
   */
  end(eventId: number, endTime: Date): Promise<void>

  /**
   * Get unclassified stoppages as domain models
   */
  findUnclassified(deviceId: string): Promise<Downtime_Record[]>

  /**
   * Get unclassified stoppages in legacy format
   */
  findUnclassifiedLegacy(deviceId: string): Promise<StoppageEvent[]>

  /**
   * Classify a stoppage event
   */
  classify(eventId: number, classification: StoppageClassificationRequest): Promise<void>

  /**
   * Get stoppage statistics for a time period
   */
  getStatistics(deviceId: string, startTime: Date, endTime: Date): Promise<{
    totalStoppages: number
    totalDowntimeMinutes: number
    averageStoppageMinutes: number
    stoppagesByCategory: Array<{ 
      category: string
      count: number
      totalMinutes: number 
    }>
  }>

  /**
   * Get current active downtime record
   */
  findCurrentDowntime(deviceId: string): Promise<Downtime_Record | null>

  /**
   * Find active stoppage event
   */
  findActiveEvent(deviceId: string): Promise<{
    event_id: number
    start_time: Date
  } | null>

  /**
   * Get current job ID for stoppage association
   */
  getCurrentJobId(deviceId: string): Promise<number | null>
}