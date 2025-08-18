import { createJobService } from '@/lib/services/jobService'
import { OperationResult, Job } from '@/lib/types/oee'

/**
 * Use case for ending a production job
 * Encapsulates business logic for job completion
 */
export class EndJobUseCase {
  async execute(request: {
    deviceId: string
    jobId: string
    operatorId?: string
  }): Promise<OperationResult<Job>> {
    try {
      // Create job service for the device
      const jobService = createJobService(request.deviceId)
      
      // Additional business logic validations
      const validationResult = await this.validateEndJobRequest(request)
      if (!validationResult.isSuccess) {
        return validationResult
      }

      // End the job
      const result = await jobService.endJob(request.jobId, request.operatorId || 'system')
      
      if (!result.isSuccess) {
        return {
          isSuccess: false,
          value: {} as Job,
          errorMessage: result.errorMessage
        }
      }

      // Transform database response to UI model
      const job: Job = {
        jobId: result.value.job_id,
        jobNumber: result.value.job_number,
        partNumber: result.value.part_number,
        targetRate: result.value.target_rate,
        quantity: result.value.target_quantity || 1000,
        startTime: new Date(result.value.start_time),
        endTime: result.value.end_time ? new Date(result.value.end_time) : undefined,
        status: result.value.status,
      }

      return {
        isSuccess: true,
        value: job,
        errorMessage: null
      }
    } catch (error) {
      return {
        isSuccess: false,
        value: {} as Job,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }

  private async validateEndJobRequest(request: {
    deviceId: string
    jobId: string
    operatorId?: string
  }): Promise<OperationResult<void>> {
    // Business rule: Job ID must be valid format
    if (!request.jobId || request.jobId.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Job ID is required to end a job'
      }
    }

    // Business rule: Device ID must be valid
    if (!request.deviceId || request.deviceId.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Device ID is required to end a job'
      }
    }

    return {
      isSuccess: true,
      value: undefined,
      errorMessage: null
    }
  }
}

// Singleton instance
export const endJobUseCase = new EndJobUseCase()