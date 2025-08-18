import { ProductionJob, NewJobRequest, OperationResult } from '../types/oee'
import { JobValidationService } from './jobValidationService'
import { Work_Order } from '../domain/models'
import { JobRepository } from '../infrastructure/repositories'
import { ErrorHandler } from '../utils/errorHandler'

/**
 * Job Service - Production job management using repository pattern
 * Handles job lifecycle: start, end, status tracking, and validation
 * Now uses JobRepository for data access and applies ErrorHandler consistently
 */
export class JobService {
  private deviceId: string
  private jobRepository: JobRepository

  constructor(deviceId: string) {
    this.deviceId = deviceId
    this.jobRepository = new JobRepository()
  }

  /**
   * Start a new production job as Work_Order
   * Returns domain model for business context
   */
  async startWorkOrder(jobRequest: NewJobRequest): Promise<OperationResult<Work_Order>> {
    try {
      // Enhanced validation using JobValidationService
      const validationResult = await JobValidationService.validateJobRequest(jobRequest)
      if (!validationResult.isSuccess) {
        return {
          isSuccess: false,
          value: {} as Work_Order,
          errorMessage: validationResult.errorMessage || 'Validation failed'
        }
      }

      const workOrder = await this.jobRepository.createWorkOrder(jobRequest)
      
      return {
        isSuccess: true,
        value: workOrder,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleJobError(error, 'work order creation')
      return {
        isSuccess: false,
        value: {} as Work_Order,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Start a new production job with enhanced validation (legacy)
   * Automatically ends any active jobs before starting the new one
   */
  async startJob(jobRequest: NewJobRequest): Promise<OperationResult<ProductionJob>> {
    try {
      // Enhanced validation using JobValidationService
      const validationResult = await JobValidationService.validateJobRequest(jobRequest)
      if (!validationResult.isSuccess) {
        return {
          isSuccess: false,
          value: {} as ProductionJob,
          errorMessage: validationResult.errorMessage || 'Validation failed'
        }
      }

      const newJob = await this.jobRepository.create(jobRequest)
      
      return {
        isSuccess: true,
        value: newJob,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleJobError(error, 'creation')
      return {
        isSuccess: false,
        value: {} as ProductionJob,
        errorMessage: appError.message
      }
    }
  }

  /**
   * End the currently active job
   */
  async endJob(jobId: number): Promise<OperationResult<void>> {
    try {
      // Verify job exists and is active
      const job = await this.jobRepository.findById(jobId)
      if (!job) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Job not found'
        }
      }

      if (job.status !== 'active') {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Job is not active'
        }
      }

      if (job.device_id !== this.deviceId) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Job does not belong to this device'
        }
      }

      await this.jobRepository.complete(jobId)

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleJobError(error, 'completion')
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get the currently active job as Work_Order domain model
   */
  async getCurrentWorkOrder(): Promise<OperationResult<Work_Order | null>> {
    try {
      const workOrder = await this.jobRepository.findActiveWorkOrder(this.deviceId)
      
      return {
        isSuccess: true,
        value: workOrder,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: null,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get the currently active job for this device (legacy format)
   */
  async getCurrentJob(): Promise<OperationResult<ProductionJob | null>> {
    try {
      const job = await this.jobRepository.findActive(this.deviceId)
      
      return {
        isSuccess: true,
        value: job,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: null,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get job by ID
   */
  async getJobById(jobId: number): Promise<OperationResult<ProductionJob | null>> {
    try {
      const job = await this.jobRepository.findById(jobId)
      
      return {
        isSuccess: true,
        value: job,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: null,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get job history for the device
   */
  async getJobHistory(limit: number = 50): Promise<ProductionJob[]> {
    try {
      return await this.jobRepository.findByDevice(this.deviceId, limit)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return []
    }
  }

  /**
   * Get detailed job analytics including efficiency trends
   */
  async getJobAnalytics(jobId: number): Promise<OperationResult<{
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
  }>> {
    try {
      const analytics = await this.jobRepository.getAnalytics(jobId)
      
      return {
        isSuccess: true,
        value: analytics,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleCalculationError(error, `job-${jobId}`)
      return {
        isSuccess: false,
        value: {} as any,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Calculate job progress based on counter data
   */
  async calculateJobProgress(jobId: number): Promise<OperationResult<{
    targetQuantity: number
    actualQuantity: number
    progressPercent: number
    rateActual: number
    rateTarget: number
    estimatedCompletionTime: Date | null
  }>> {
    try {
      const progress = await this.jobRepository.calculateProgress(jobId)
      
      return {
        isSuccess: true,
        value: progress,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleCalculationError(error, `job-${jobId}`)
      return {
        isSuccess: false,
        value: {} as any,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get validation summary for UI feedback
   */
  async getValidationSummary(jobRequest: NewJobRequest) {
    return await JobValidationService.getValidationSummary(jobRequest)
  }

  /**
   * Check if device has an active job
   */
  async hasActiveJob(): Promise<boolean> {
    try {
      return await this.jobRepository.hasActive(this.deviceId)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return false
    }
  }
}

/**
 * Factory function to create a JobService instance
 */
export function createJobService(deviceId: string): JobService {
  return new JobService(deviceId)
}