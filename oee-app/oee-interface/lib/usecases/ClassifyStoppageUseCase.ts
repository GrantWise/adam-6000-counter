import { createStoppageService } from '@/lib/services/stoppageService'
import { OperationResult, StoppageEvent } from '@/lib/types/oee'

/**
 * Use case for classifying production stoppages
 * Encapsulates business logic for stoppage classification
 */
export class ClassifyStoppageUseCase {
  async execute(request: {
    deviceId: string
    eventId: number
    classification: {
      category: string
      subCategory: string
      comments?: string
    }
    operatorId: string
  }): Promise<OperationResult<StoppageEvent>> {
    try {
      // Create stoppage service for the device
      const stoppageService = createStoppageService(request.deviceId)
      
      // Additional business logic validations
      const validationResult = await this.validateClassificationRequest(request)
      if (!validationResult.isSuccess) {
        return validationResult
      }

      // Classify the stoppage
      const result = await stoppageService.classifyStoppage(
        request.eventId,
        request.classification,
        request.operatorId
      )
      
      if (!result.isSuccess) {
        return {
          isSuccess: false,
          value: {} as StoppageEvent,
          errorMessage: result.errorMessage
        }
      }

      // Transform database response to UI model
      const stoppageEvent: StoppageEvent = {
        event_id: result.value.event_id,
        device_id: result.value.device_id,
        start_time: result.value.start_time,
        end_time: result.value.end_time,
        duration_seconds: result.value.duration_seconds,
        category: result.value.category,
        sub_category: result.value.sub_category,
        comments: result.value.comments,
        classified_by: result.value.classified_by,
        classified_at: result.value.classified_at,
      }

      return {
        isSuccess: true,
        value: stoppageEvent,
        errorMessage: null
      }
    } catch (error) {
      return {
        isSuccess: false,
        value: {} as StoppageEvent,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }

  private async validateClassificationRequest(request: {
    deviceId: string
    eventId: number
    classification: {
      category: string
      subCategory: string
      comments?: string
    }
    operatorId: string
  }): Promise<OperationResult<void>> {
    // Business rule: Event ID must be valid
    if (!request.eventId || request.eventId <= 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Valid event ID is required for classification'
      }
    }

    // Business rule: Category is required
    if (!request.classification.category || request.classification.category.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Stoppage category is required'
      }
    }

    // Business rule: Sub-category is required
    if (!request.classification.subCategory || request.classification.subCategory.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Stoppage sub-category is required'
      }
    }

    // Business rule: Validate category values against predefined list
    const validCategories = [
      'Equipment Failure',
      'Setup/Changeover',
      'Material Issue',
      'Quality Issue',
      'Operator Issue',
      'Planned Maintenance',
      'Other'
    ]
    
    if (!validCategories.includes(request.classification.category)) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: `Invalid category. Must be one of: ${validCategories.join(', ')}`
      }
    }

    // Business rule: Comments should not be excessive
    if (request.classification.comments && request.classification.comments.length > 500) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Comments cannot exceed 500 characters'
      }
    }

    // Business rule: Operator ID is required for audit trail
    if (!request.operatorId || request.operatorId.trim().length === 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: 'Operator ID is required for stoppage classification'
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
export const classifyStoppageUseCase = new ClassifyStoppageUseCase()