import { StoppageEvent, StoppageInfo, OperationResult, StoppageClassificationRequest } from '../types/oee'
import { Downtime_Record } from '../domain/models'
import { StoppageRepository, CounterDataRepository } from '../infrastructure/repositories'
import { ErrorHandler } from '../utils/errorHandler'

/**
 * Stoppage Service - Real-time stoppage detection and classification
 * Now uses repository pattern for data access and applies ErrorHandler consistently
 */
export class StoppageService {
  private deviceId: string
  private stoppageThresholdMinutes: number
  private stoppageRepository: StoppageRepository
  private counterDataRepository: CounterDataRepository

  constructor(deviceId: string, stoppageThresholdMinutes: number = 1) {
    this.deviceId = deviceId
    this.stoppageThresholdMinutes = stoppageThresholdMinutes
    this.stoppageRepository = new StoppageRepository()
    this.counterDataRepository = new CounterDataRepository()
  }

  /**
   * Detect if a stoppage is currently active based on zero rate readings
   * Returns stoppage info if detected, null if machine is running
   */
  async detectCurrentStoppage(): Promise<StoppageInfo | null> {
    try {
      const stoppageData = await this.counterDataRepository.detectCurrentStoppage(this.deviceId)
      
      if (!stoppageData || stoppageData.duration_minutes < this.stoppageThresholdMinutes) {
        return null
      }

      return {
        startTime: stoppageData.stoppage_start,
        durationMinutes: Math.round(stoppageData.duration_minutes),
        isActive: true,
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      console.error('Error detecting current stoppage:', appError.message)
      return null
    }
  }

  /**
   * Create a new stoppage event record
   * Called automatically when stoppage threshold is exceeded
   */
  async createStoppageEvent(startTime: Date, jobId?: number): Promise<OperationResult<number>> {
    try {
      const eventId = await this.stoppageRepository.create(this.deviceId, startTime, jobId)
      
      return {
        isSuccess: true,
        value: eventId,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: 0,
        errorMessage: appError.message
      }
    }
  }

  /**
   * End an active stoppage event when machine starts running again
   */
  async endStoppageEvent(eventId: number, endTime: Date): Promise<OperationResult<void>> {
    try {
      await this.stoppageRepository.end(eventId, endTime)

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get all unclassified stoppages for a device
   * Returns domain models for business context
   */
  async getUnclassifiedStoppages(): Promise<Downtime_Record[]> {
    try {
      return await this.stoppageRepository.findUnclassified(this.deviceId)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return []
    }
  }

  /**
   * Get all unclassified stoppages (legacy format)
   */
  async getUnclassifiedStoppagesLegacy(): Promise<StoppageEvent[]> {
    try {
      return await this.stoppageRepository.findUnclassifiedLegacy(this.deviceId)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return []
    }
  }

  /**
   * Classify a stoppage event with category and reason
   */
  async classifyStoppage(
    eventId: number, 
    classification: StoppageClassificationRequest
  ): Promise<OperationResult<void>> {
    try {
      await this.stoppageRepository.classify(eventId, classification)

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      const appError = ErrorHandler.handleDatabaseError(error)
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: appError.message
      }
    }
  }

  /**
   * Get stoppage statistics for a time period
   */
  async getStoppageStatistics(startTime: Date, endTime: Date): Promise<{
    totalStoppages: number
    totalDowntimeMinutes: number
    averageStoppageMinutes: number
    stoppagesByCategory: Array<{ category: string; count: number; totalMinutes: number }>
  }> {
    try {
      return await this.stoppageRepository.getStatistics(this.deviceId, startTime, endTime)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return {
        totalStoppages: 0,
        totalDowntimeMinutes: 0,
        averageStoppageMinutes: 0,
        stoppagesByCategory: []
      }
    }
  }

  /**
   * Get current active downtime record if any
   */
  async getCurrentDowntime(): Promise<Downtime_Record | null> {
    try {
      return await this.stoppageRepository.findCurrentDowntime(this.deviceId)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return null
    }
  }

  /**
   * Auto-detect and create stoppage events based on counter data
   * Should be called periodically to monitor for new stoppages
   */
  async monitorForNewStoppages(): Promise<OperationResult<number | null>> {
    try {
      // Check if there's already an active (unclosed) stoppage event
      const activeStoppage = await this.stoppageRepository.findActiveEvent(this.deviceId)

      // Get current machine state
      const currentStoppage = await this.detectCurrentStoppage()

      if (currentStoppage && !activeStoppage) {
        // Machine is stopped but no active stoppage event exists - create one
        const currentJob = await this.stoppageRepository.getCurrentJobId(this.deviceId)
        const createResult = await this.createStoppageEvent(currentStoppage.startTime, currentJob || undefined)
        
        if (createResult.isSuccess) {
          return {
            isSuccess: true,
            value: createResult.value,
            errorMessage: null
          }
        } else {
          return createResult
        }
      } else if (!currentStoppage && activeStoppage) {
        // Machine is running but there's an active stoppage event - close it
        const endResult = await this.endStoppageEvent(activeStoppage.event_id, new Date())
        
        if (endResult.isSuccess) {
          return {
            isSuccess: true,
            value: null, // No new event created
            errorMessage: null
          }
        } else {
          return {
            isSuccess: false,
            value: null,
            errorMessage: endResult.errorMessage
          }
        }
      }

      // No action needed
      return {
        isSuccess: true,
        value: null,
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
   * Helper method to get current active job ID
   */
  private async getCurrentJobId(): Promise<number | null> {
    try {
      return await this.stoppageRepository.getCurrentJobId(this.deviceId)
    } catch (error) {
      ErrorHandler.handleDatabaseError(error)
      return null
    }
  }
}

/**
 * Factory function to create a StoppageService instance
 */
export function createStoppageService(deviceId: string, stoppageThresholdMinutes?: number): StoppageService {
  return new StoppageService(deviceId, stoppageThresholdMinutes)
}