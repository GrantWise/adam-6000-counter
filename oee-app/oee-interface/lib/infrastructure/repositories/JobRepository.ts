import { queryDatabase, queryDatabaseSingle, queryDatabaseTransaction } from '@/lib/database/connection'
import { ProductionJob, NewJobRequest } from '@/lib/types/oee'
import { Work_Order } from '@/lib/domain/models'
import { IJobRepository } from '@/lib/domain/ports'
import { channels } from '@/config'
import { ErrorHandler } from '@/lib/utils/errorHandler'
import { withDatabaseFallback, DatabaseFallbackStrategies } from '@/lib/database/fallbackStrategies'
import { executeWithResilience, READ_OPERATION_RETRY_CONFIG, WRITE_OPERATION_RETRY_CONFIG } from '@/lib/database/connectionResilience'
import { withActiveJobCache, invalidateDeviceCache } from '@/lib/services/queryCacheService'

/**
 * Job Repository Implementation
 * Handles all job-related database operations
 * Migrated from JobService to separate concerns
 */
export class JobRepository implements IJobRepository {
  
  async findActive(deviceId: string): Promise<ProductionJob | null> {
    return withActiveJobCache(deviceId, async () => {
      return withDatabaseFallback(
        async () => {
          return executeWithResilience(async () => {
            const query = `
              SELECT 
                job_id,
                job_number,
                part_number,
                device_id,
                target_rate,
                start_time,
                end_time,
                operator_id,
                status
              FROM production_jobs
              WHERE device_id = $1 AND status = 'active'
              ORDER BY start_time DESC
              LIMIT 1`

            const job = await queryDatabaseSingle<ProductionJob>(query, [deviceId])
            return job || null
          }, READ_OPERATION_RETRY_CONFIG)
        },
        () => DatabaseFallbackStrategies.findActiveJobFallback(deviceId),
        (result) => {
          // Cache successful result for fallback use
          if (result) {
            DatabaseFallbackStrategies.cacheForFallback({ deviceId, activeJob: result })
          }
        }
      )
    })
  }

  async findActiveWorkOrder(deviceId: string): Promise<Work_Order | null> {
    return withDatabaseFallback(
      async () => {
        return executeWithResilience(async () => {
          const query = `
            SELECT 
              j.job_id,
              j.job_number,
              j.part_number,
              j.device_id,
              j.target_rate,
              j.start_time,
              j.end_time,
              j.operator_id,
              j.status,
              COALESCE(SUM(CASE WHEN c.channel = $2 THEN c.processed_value ELSE 0 END), 0) as good_count,
              COALESCE(SUM(CASE WHEN c.channel = $3 THEN c.processed_value ELSE 0 END), 0) as scrap_count
            FROM production_jobs j
            LEFT JOIN counter_data c ON c.device_id = j.device_id 
              AND c.timestamp >= j.start_time 
              AND c.timestamp <= COALESCE(j.end_time, NOW())
            WHERE j.device_id = $1 AND j.status = 'active'
            GROUP BY j.job_id, j.job_number, j.part_number, j.device_id, 
                     j.target_rate, j.start_time, j.end_time, j.operator_id, j.status
            ORDER BY j.start_time DESC
            LIMIT 1`

          const job = await queryDatabaseSingle<any>(query, [
            deviceId,
            channels.production(),
            channels.rejects()
          ])

          if (!job) return null

          // Convert to Work_Order domain model
          const workOrder = new Work_Order({
            work_order_id: job.job_number,
            work_order_description: `Production of ${job.part_number}`,
            product_id: job.part_number,
            product_description: job.part_number,
            planned_quantity: Math.round(job.target_rate * 8), // Assuming 8 hour shift
            scheduled_start_time: new Date(job.start_time),
            scheduled_end_time: new Date(new Date(job.start_time).getTime() + 8 * 60 * 60 * 1000),
            resource_reference: job.device_id,
            actual_quantity_good: parseInt(job.good_count) || 0,
            actual_quantity_scrap: parseInt(job.scrap_count) || 0,
            actual_start_time: new Date(job.start_time),
            actual_end_time: job.end_time ? new Date(job.end_time) : undefined,
            status: job.status === 'active' ? 'active' : 'completed'
          })

          return workOrder
        }, READ_OPERATION_RETRY_CONFIG)
      },
      () => DatabaseFallbackStrategies.findActiveWorkOrderFallback(deviceId)
    )
  }

  async create(jobRequest: NewJobRequest): Promise<ProductionJob> {
    return executeWithResilience(async () => {
      // Use transaction to ensure data consistency
      const queries = [
        // End any active jobs for this device
        {
          query: `
            UPDATE production_jobs 
            SET 
              end_time = NOW(),
              status = 'completed'
            WHERE device_id = $1 AND status = 'active'`,
          params: [jobRequest.deviceId]
        },
        // Insert new job
        {
          query: `
            INSERT INTO production_jobs (
              job_number,
              part_number,
              device_id,
              target_rate,
              start_time,
              operator_id,
              status
            ) VALUES ($1, $2, $3, $4, NOW(), $5, 'active')
            RETURNING 
              job_id,
              job_number,
              part_number,
              device_id,
              target_rate,
              start_time,
              operator_id,
              status`,
          params: [
            jobRequest.jobNumber,
            jobRequest.partNumber,
            jobRequest.deviceId,
            jobRequest.targetRate,
            jobRequest.operatorId || 'system'
          ]
        }
      ]

      const results = await queryDatabaseTransaction(queries)
      const newJob = results[1][0] as ProductionJob

      if (!newJob) {
        throw new Error('Failed to create new job')
      }

      // Cache the new job for fallback use
      DatabaseFallbackStrategies.cacheForFallback({ 
        deviceId: newJob.device_id, 
        activeJob: newJob 
      })

      // Invalidate cache for this device since job changed
      invalidateDeviceCache(newJob.device_id)

      return newJob
    }, WRITE_OPERATION_RETRY_CONFIG)
  }

  async createWorkOrder(jobRequest: NewJobRequest): Promise<Work_Order> {
    try {
      const job = await this.create(jobRequest)
      
      const workOrder = new Work_Order({
        work_order_id: job.job_number,
        work_order_description: `Production of ${job.part_number}`,
        product_id: job.part_number,
        product_description: job.part_number,
        planned_quantity: Math.round(job.target_rate * 8),
        scheduled_start_time: new Date(job.start_time),
        scheduled_end_time: new Date(new Date(job.start_time).getTime() + 8 * 60 * 60 * 1000),
        resource_reference: job.device_id,
        status: 'active'
      })

      workOrder.start()
      return workOrder
    } catch (error) {
      throw ErrorHandler.handleJobError(error, 'work order creation')
    }
  }

  async complete(jobId: number): Promise<void> {
    try {
      const updateQuery = `
        UPDATE production_jobs 
        SET 
          end_time = NOW(),
          status = 'completed'
        WHERE job_id = $1 AND status = 'active'`

      await queryDatabase(updateQuery, [jobId])
    } catch (error) {
      throw ErrorHandler.handleJobError(error, 'completion')
    }
  }

  async findById(jobId: number): Promise<ProductionJob | null> {
    try {
      const query = `
        SELECT 
          job_id,
          job_number,
          part_number,
          device_id,
          target_rate,
          start_time,
          end_time,
          operator_id,
          status
        FROM production_jobs
        WHERE job_id = $1`

      const job = await queryDatabaseSingle<ProductionJob>(query, [jobId])
      return job || null
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async findByDevice(deviceId: string, limit: number = 50): Promise<ProductionJob[]> {
    try {
      const query = `
        SELECT 
          job_id,
          job_number,
          part_number,
          device_id,
          target_rate,
          start_time,
          end_time,
          operator_id,
          status
        FROM production_jobs
        WHERE device_id = $1
        ORDER BY start_time DESC
        LIMIT $2`

      return await queryDatabase<ProductionJob>(query, [deviceId, limit])
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }

  async getAnalytics(jobId: number): Promise<{
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
  }> {
    try {
      const job = await this.findById(jobId)
      if (!job) {
        throw new Error('Job not found')
      }

      const elapsedMinutes = Math.floor((Date.now() - new Date(job.start_time).getTime()) / (1000 * 60))
      
      // Get production data with rate calculations
      const productionQuery = `
        SELECT 
          MAX(processed_value) - MIN(processed_value) as actual_quantity,
          AVG(rate) * 60 as avg_rate_per_minute,
          COUNT(*) as data_points,
          (
            SELECT AVG(rate) * 60 
            FROM counter_data 
            WHERE device_id = $1 AND channel = $2 
              AND timestamp >= NOW() - INTERVAL '10 minutes'
          ) as current_rate
        FROM counter_data
        WHERE device_id = $1 
          AND channel = $2
          AND timestamp >= $3
          ${job.end_time ? 'AND timestamp <= $4' : ''}
        HAVING COUNT(*) > 0`

      const params = job.end_time 
        ? [job.device_id, channels.production(), job.start_time, job.end_time]
        : [job.device_id, channels.production(), job.start_time]

      const production = await queryDatabaseSingle<{
        actual_quantity: number
        avg_rate_per_minute: number
        data_points: number
        current_rate: number
      }>(productionQuery, params)

      const actualQuantity = production?.actual_quantity || 0
      const avgRate = production?.avg_rate_per_minute || 0
      const currentRate = production?.current_rate || 0
      const targetRate = job.target_rate
      
      // Calculate estimated total time based on 8-hour shift
      const estimatedTotalMinutes = 8 * 60
      const targetQuantity = Math.round(targetRate * estimatedTotalMinutes)
      
      // Efficiency calculations
      const currentEfficiency = targetRate > 0 ? (currentRate / targetRate) * 100 : 0
      const averageEfficiency = targetRate > 0 ? (avgRate / targetRate) * 100 : 0
      
      // Determine efficiency trend
      let efficiencyTrend: 'improving' | 'declining' | 'stable' = 'stable'
      if (currentEfficiency > averageEfficiency * 1.05) {
        efficiencyTrend = 'improving'
      } else if (currentEfficiency < averageEfficiency * 0.95) {
        efficiencyTrend = 'declining'
      }

      // Quality metrics (placeholder - would integrate with quality data)
      const currentQuality = 98.5 // This would come from quality channel data
      const defectRate = 1.5 // This would be calculated from reject channel
      
      // Schedule adherence
      const expectedQuantityByNow = Math.round(targetRate * elapsedMinutes)
      const onSchedule = actualQuantity >= expectedQuantityByNow * 0.95

      return {
        efficiency: {
          current: Math.round(currentEfficiency * 10) / 10,
          average: Math.round(averageEfficiency * 10) / 10,
          trend: efficiencyTrend
        },
        quality: {
          current: currentQuality,
          defectRate: defectRate,
          trendDirection: 'stable'
        },
        timing: {
          elapsedMinutes,
          estimatedTotalMinutes,
          onSchedule
        },
        production: {
          actualQuantity: Math.round(actualQuantity),
          targetQuantity,
          currentRate: Math.round(currentRate * 10) / 10,
          targetRate
        }
      }
    } catch (error) {
      throw ErrorHandler.handleCalculationError(error, `job-${jobId}`)
    }
  }

  async calculateProgress(jobId: number): Promise<{
    targetQuantity: number
    actualQuantity: number
    progressPercent: number
    rateActual: number
    rateTarget: number
    estimatedCompletionTime: Date | null
  }> {
    try {
      const job = await this.findById(jobId)
      if (!job) {
        throw new Error('Job not found')
      }

      // Get production data since job start
      const productionQuery = `
        SELECT 
          MAX(processed_value) - MIN(processed_value) as actual_quantity,
          AVG(rate) * 60 as avg_rate_per_minute
        FROM counter_data
        WHERE 
          device_id = $1 
          AND channel = 0
          AND timestamp >= $2
          ${job.end_time ? 'AND timestamp <= $3' : ''}
        HAVING COUNT(*) > 0`

      const params = job.end_time 
        ? [job.device_id, job.start_time, job.end_time]
        : [job.device_id, job.start_time]

      const production = await queryDatabaseSingle<{
        actual_quantity: number
        avg_rate_per_minute: number
      }>(productionQuery, params)

      const actualQuantity = production?.actual_quantity || 0
      const rateActual = production?.avg_rate_per_minute || 0
      const rateTarget = job.target_rate
      
      // For now, assume target quantity is based on shift duration (8 hours) and target rate
      const targetQuantity = Math.round(rateTarget * 8 * 60) // 8 hours in minutes
      const progressPercent = Math.min(100, (actualQuantity / targetQuantity) * 100)

      // Estimate completion time based on current rate
      let estimatedCompletionTime: Date | null = null
      if (rateActual > 0 && job.status === 'active') {
        const remainingQuantity = Math.max(0, targetQuantity - actualQuantity)
        const remainingMinutes = remainingQuantity / rateActual
        estimatedCompletionTime = new Date(Date.now() + (remainingMinutes * 60 * 1000))
      }

      return {
        targetQuantity,
        actualQuantity: Math.round(actualQuantity),
        progressPercent: Math.round(progressPercent * 10) / 10,
        rateActual: Math.round(rateActual * 10) / 10,
        rateTarget,
        estimatedCompletionTime
      }
    } catch (error) {
      throw ErrorHandler.handleCalculationError(error, `job-${jobId}`)
    }
  }

  async hasActive(deviceId: string): Promise<boolean> {
    try {
      const job = await this.findActive(deviceId)
      return job !== null
    } catch (error) {
      throw ErrorHandler.handleDatabaseError(error)
    }
  }
}