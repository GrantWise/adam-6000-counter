import { createJobService } from '@/lib/services/jobService'
import { NewJobRequest, OperationResult, Job } from '@/lib/types/oee'
import { config } from '@/config'

/**
 * Use case for starting a new production job
 * Encapsulates business logic for job creation
 */
export class StartJobUseCase {
  async execute(request: {
    deviceId: string
    jobNumber: string
    partNumber: string
    targetRate: number
    operatorId?: string
  }): Promise<OperationResult<Job>> {
    try {
      // Create job service for the device
      const jobService = createJobService(request.deviceId)
      
      // Prepare job request
      const jobRequest: NewJobRequest = {
        jobNumber: request.jobNumber.trim(),
        partNumber: request.partNumber.trim(),
        deviceId: request.deviceId.trim(),
        targetRate: request.targetRate,
        operatorId: request.operatorId?.trim() || 'system'
      }

      // Additional business logic validations
      const validationResult = await this.validateJobRequest(jobRequest)
      if (!validationResult.isSuccess) {
        return validationResult
      }

      // Start the job
      const result = await jobService.startJob(jobRequest)
      
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
        quantity: config.oee.production.defaults.quantity,
        startTime: new Date(result.value.start_time),
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

  private async validateJobRequest(request: NewJobRequest): Promise<OperationResult<void>> {
    // Business rule: Job numbers must follow configured format
    const jobNumberPattern = new RegExp(config.oee.production.validation.jobNumberPattern)
    if (!jobNumberPattern.test(request.jobNumber)) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: `Job number must match pattern: ${config.oee.production.validation.jobNumberPattern}`
      }
    }

    // Business rule: Target rate must be within configured limits
    const { min, max } = config.oee.production.targetRate
    if (request.targetRate < min || request.targetRate > max) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: `Target rate must be between ${min} and ${max} units per minute`
      }
    }

    // Business rule: Part numbers should be uppercase
    if (request.partNumber !== request.partNumber.toUpperCase()) {
      request.partNumber = request.partNumber.toUpperCase()
    }

    return {
      isSuccess: true,
      value: undefined,
      errorMessage: null
    }
  }
}

// Singleton instance
export const startJobUseCase = new StartJobUseCase()