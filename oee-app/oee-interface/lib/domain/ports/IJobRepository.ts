import { ProductionJob, NewJobRequest, OperationResult } from '@/lib/types/oee'
import { Work_Order } from '../models'

/**
 * Job Repository Interface
 * Defines the contract for job data access operations
 * Separates business logic from database concerns
 */
export interface IJobRepository {
  /**
   * Find active job for device
   */
  findActive(deviceId: string): Promise<ProductionJob | null>

  /**
   * Find active job as Work_Order domain model
   */
  findActiveWorkOrder(deviceId: string): Promise<Work_Order | null>

  /**
   * Create a new job
   */
  create(jobRequest: NewJobRequest): Promise<ProductionJob>

  /**
   * Create a new job as Work_Order domain model
   */
  createWorkOrder(jobRequest: NewJobRequest): Promise<Work_Order>

  /**
   * Complete/end a job
   */
  complete(jobId: number): Promise<void>

  /**
   * Find job by ID
   */
  findById(jobId: number): Promise<ProductionJob | null>

  /**
   * Get job history for device
   */
  findByDevice(deviceId: string, limit?: number): Promise<ProductionJob[]>

  /**
   * Get job analytics data including production and quality metrics
   */
  getAnalytics(jobId: number): Promise<{
    efficiency: {
      current: number
      average: number
      trend: 'improving' | 'declining' | 'stable'
    }
    quality: {
      current: number
      defectRate: number
      trendDirection: 'improving' | 'declining' | 'stable'
    }
    timing: {
      elapsedMinutes: number
      estimatedTotalMinutes: number
      onSchedule: boolean
    }
    production: {
      actualQuantity: number
      targetQuantity: number
      currentRate: number
      targetRate: number
    }
  }>

  /**
   * Calculate job progress with counter data
   */
  calculateProgress(jobId: number): Promise<{
    targetQuantity: number
    actualQuantity: number
    progressPercent: number
    rateActual: number
    rateTarget: number
    estimatedCompletionTime: Date | null
  }>

  /**
   * Check if device has an active job
   */
  hasActive(deviceId: string): Promise<boolean>
}