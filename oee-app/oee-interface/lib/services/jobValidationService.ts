import { queryDatabase, queryDatabaseSingle } from '../database/connection'
import { NewJobRequest, ProductionJob, OperationResult } from '../types/oee'

/**
 * Advanced job validation service with business logic rules
 * Implements comprehensive validation for production job management
 */
export class JobValidationService {
  /**
   * Comprehensive job validation with business logic
   */
  static async validateJobRequest(request: NewJobRequest): Promise<OperationResult<void>> {
    try {
      // Basic field validation
      const basicValidation = this.validateBasicFields(request)
      if (!basicValidation.isSuccess) {
        return basicValidation
      }

      // Business rule validations
      const duplicateCheck = await this.checkDuplicateJobNumber(request.jobNumber)
      if (!duplicateCheck.isSuccess) {
        return duplicateCheck
      }

      const shiftConstraints = await this.validateShiftConstraints(request.deviceId)
      if (!shiftConstraints.isSuccess) {
        return shiftConstraints
      }

      const operatorPermissions = await this.validateOperatorPermissions(request.operatorId)
      if (!operatorPermissions.isSuccess) {
        return operatorPermissions
      }

      const deviceAvailability = await this.validateDeviceAvailability(request.deviceId)
      if (!deviceAvailability.isSuccess) {
        return deviceAvailability
      }

      const rateValidation = await this.validateTargetRate(request.partNumber, request.targetRate)
      if (!rateValidation.isSuccess) {
        return rateValidation
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: `Validation error: ${error instanceof Error ? error.message : 'Unknown error'}`
      }
    }
  }

  /**
   * Basic field validation
   */
  private static validateBasicFields(request: NewJobRequest): OperationResult<void> {
    const errors: string[] = []

    // Job number validation
    if (!request.jobNumber || request.jobNumber.trim().length === 0) {
      errors.push('Job number is required')
    } else if (request.jobNumber.length > 50) {
      errors.push('Job number must be 50 characters or less')
    } else if (!/^[A-Za-z0-9\-_]+$/.test(request.jobNumber)) {
      errors.push('Job number can only contain letters, numbers, hyphens, and underscores')
    }

    // Part number validation
    if (!request.partNumber || request.partNumber.trim().length === 0) {
      errors.push('Part number is required')
    } else if (request.partNumber.length > 100) {
      errors.push('Part number must be 100 characters or less')
    }

    // Device ID validation
    if (!request.deviceId || request.deviceId.trim().length === 0) {
      errors.push('Device ID is required')
    }

    // Target rate validation
    if (!request.targetRate || request.targetRate <= 0) {
      errors.push('Target rate must be greater than 0')
    } else if (request.targetRate > 10000) {
      errors.push('Target rate must be less than 10,000 units/minute')
    }

    // Operator ID validation (optional but format check if provided)
    if (request.operatorId && request.operatorId.length > 50) {
      errors.push('Operator ID must be 50 characters or less')
    }

    if (errors.length > 0) {
      return {
        isSuccess: false,
        value: undefined,
        errorMessage: errors.join('. ')
      }
    }

    return {
      isSuccess: true,
      value: undefined,
      errorMessage: null
    }
  }

  /**
   * Check for duplicate job numbers within recent time window
   */
  private static async checkDuplicateJobNumber(jobNumber: string): Promise<OperationResult<void>> {
    try {
      // Check for active jobs with same number
      const activeJobQuery = `
        SELECT job_id, job_number, device_id
        FROM production_jobs
        WHERE UPPER(job_number) = UPPER($1) AND status = 'active'
        LIMIT 1`

      const activeJob = await queryDatabaseSingle<ProductionJob>(activeJobQuery, [jobNumber])
      if (activeJob) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: `Job number "${jobNumber}" is already active on device ${activeJob.device_id}`
        }
      }

      // Check for recent completed jobs (within last 24 hours) to prevent accidental duplicates
      const recentJobQuery = `
        SELECT job_id, job_number, device_id, end_time
        FROM production_jobs
        WHERE UPPER(job_number) = UPPER($1) 
          AND status = 'completed'
          AND end_time >= NOW() - INTERVAL '24 hours'
        ORDER BY end_time DESC
        LIMIT 1`

      const recentJob = await queryDatabaseSingle<ProductionJob>(recentJobQuery, [jobNumber])
      if (recentJob) {
        const endTime = new Date(recentJob.end_time!)
        const hoursAgo = Math.round((Date.now() - endTime.getTime()) / (1000 * 60 * 60))
        
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: `Job number "${jobNumber}" was recently completed ${hoursAgo} hours ago on device ${recentJob.device_id}. Use a different job number or confirm this is intentional.`
        }
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      throw new Error(`Duplicate check failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Validate shift constraints (production schedule rules)
   */
  private static async validateShiftConstraints(deviceId: string): Promise<OperationResult<void>> {
    try {
      const currentHour = new Date().getHours()
      const currentDay = new Date().getDay() // 0 = Sunday, 1 = Monday, etc.

      // Check if current time is within allowed production hours (6 AM to 10 PM)
      if (currentHour < 6 || currentHour >= 22) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Job cannot be started outside production hours (6:00 AM - 10:00 PM)'
        }
      }

      // Check if current day is a production day (Monday to Friday)
      if (currentDay === 0 || currentDay === 6) { // Weekend
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: 'Job cannot be started on weekends. Production is scheduled Monday through Friday.'
        }
      }

      // Check for device-specific maintenance schedules
      const maintenanceQuery = `
        SELECT COUNT(*) as maintenance_count
        FROM maintenance_schedules ms
        WHERE ms.device_id = $1
          AND ms.scheduled_start <= NOW()
          AND ms.scheduled_end >= NOW()
          AND ms.status = 'active'`

      try {
        const maintenanceCheck = await queryDatabaseSingle<{ maintenance_count: number }>(
          maintenanceQuery, 
          [deviceId]
        )
        
        if (maintenanceCheck && maintenanceCheck.maintenance_count > 0) {
          return {
            isSuccess: false,
            value: undefined,
            errorMessage: 'Device is currently scheduled for maintenance. Job cannot be started.'
          }
        }
      } catch (dbError) {
        // If maintenance_schedules table doesn't exist, skip this check
        console.warn('Maintenance schedule check skipped - table may not exist:', dbError)
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      throw new Error(`Shift validation failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Validate operator permissions and availability
   */
  private static async validateOperatorPermissions(operatorId?: string): Promise<OperationResult<void>> {
    if (!operatorId || operatorId === 'system') {
      // System-initiated jobs are allowed
      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    }

    try {
      // Check operator authentication and permissions
      const operatorQuery = `
        SELECT operator_id, name, role, is_active, last_login
        FROM operators
        WHERE operator_id = $1 AND is_active = true
        LIMIT 1`

      try {
        const operator = await queryDatabaseSingle<{
          operator_id: string
          name: string
          role: string
          is_active: boolean
          last_login?: string
        }>(operatorQuery, [operatorId])

        if (!operator) {
          return {
            isSuccess: false,
            value: undefined,
            errorMessage: `Operator "${operatorId}" not found or inactive`
          }
        }

        // Check if operator has job creation permissions
        if (operator.role !== 'supervisor' && operator.role !== 'operator' && operator.role !== 'manager') {
          return {
            isSuccess: false,
            value: undefined,
            errorMessage: `Operator "${operatorId}" does not have permission to start jobs`
          }
        }

        // Check if operator has been active recently (within last 8 hours)
        if (operator.last_login) {
          const lastLogin = new Date(operator.last_login)
          const hoursAgo = (Date.now() - lastLogin.getTime()) / (1000 * 60 * 60)
          
          if (hoursAgo > 8) {
            return {
              isSuccess: false,
              value: undefined,
              errorMessage: `Operator "${operatorId}" session may have expired. Please re-authenticate.`
            }
          }
        }
      } catch (dbError) {
        // If operators table doesn't exist, allow system to proceed
        console.warn('Operator validation skipped - table may not exist:', dbError)
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      throw new Error(`Operator validation failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Validate device availability and status
   */
  private static async validateDeviceAvailability(deviceId: string): Promise<OperationResult<void>> {
    try {
      // Check if device has recent data (within last 5 minutes)
      const deviceDataQuery = `
        SELECT 
          device_id,
          MAX(timestamp) as last_data_time,
          COUNT(*) as data_points
        FROM counter_data
        WHERE device_id = $1
          AND timestamp >= NOW() - INTERVAL '5 minutes'
        GROUP BY device_id`

      const deviceData = await queryDatabaseSingle<{
        device_id: string
        last_data_time: string
        data_points: number
      }>(deviceDataQuery, [deviceId])

      if (!deviceData || deviceData.data_points === 0) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: `Device "${deviceId}" appears to be offline. No recent data received. Check device connection.`
        }
      }

      // Check device health status
      const lastDataTime = new Date(deviceData.last_data_time)
      const minutesAgo = (Date.now() - lastDataTime.getTime()) / (1000 * 60)

      if (minutesAgo > 2) {
        return {
          isSuccess: false,
          value: undefined,
          errorMessage: `Device "${deviceId}" last data was ${Math.round(minutesAgo)} minutes ago. Device may have connectivity issues.`
        }
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      throw new Error(`Device availability check failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Validate target rate against historical data and part specifications
   */
  private static async validateTargetRate(partNumber: string, targetRate: number): Promise<OperationResult<void>> {
    try {
      // Check historical rates for this part number
      const historicalRateQuery = `
        SELECT 
          AVG(pj.target_rate) as avg_target_rate,
          MIN(pj.target_rate) as min_target_rate,
          MAX(pj.target_rate) as max_target_rate,
          COUNT(*) as job_count
        FROM production_jobs pj
        WHERE pj.part_number = $1
          AND pj.status = 'completed'
          AND pj.end_time >= NOW() - INTERVAL '30 days'
        HAVING COUNT(*) >= 3`

      const historicalData = await queryDatabaseSingle<{
        avg_target_rate: number
        min_target_rate: number
        max_target_rate: number
        job_count: number
      }>(historicalRateQuery, [partNumber])

      if (historicalData && historicalData.job_count >= 3) {
        const avgRate = historicalData.avg_target_rate
        const maxRate = historicalData.max_target_rate
        const minRate = historicalData.min_target_rate

        // Check if target rate is significantly different from historical average
        const deviationPercent = Math.abs(targetRate - avgRate) / avgRate * 100

        if (deviationPercent > 50) {
          return {
            isSuccess: false,
            value: undefined,
            errorMessage: `Target rate ${targetRate} is ${Math.round(deviationPercent)}% different from historical average (${Math.round(avgRate)}) for part "${partNumber}". Historical range: ${Math.round(minRate)}-${Math.round(maxRate)}. Please verify target rate.`
          }
        }

        // Warn if target rate is outside historical range
        if (targetRate > maxRate * 1.2 || targetRate < minRate * 0.8) {
          console.warn(`Target rate ${targetRate} is outside typical range for part ${partNumber}`)
        }
      }

      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    } catch (error) {
      // Don't fail validation for rate check errors, just log them
      console.warn('Target rate validation warning:', error)
      return {
        isSuccess: true,
        value: undefined,
        errorMessage: null
      }
    }
  }

  /**
   * Get validation summary for UI display
   */
  static async getValidationSummary(request: NewJobRequest): Promise<{
    isValid: boolean
    errors: string[]
    warnings: string[]
    deviceStatus: 'online' | 'offline' | 'warning'
    operatorValid: boolean
    duplicateRisk: boolean
  }> {
    const result = await this.validateJobRequest(request)
    
    const errors: string[] = []
    const warnings: string[] = []
    let deviceStatus: 'online' | 'offline' | 'warning' = 'online'
    let operatorValid = true
    let duplicateRisk = false

    if (!result.isSuccess && result.errorMessage) {
      const message = result.errorMessage
      
      // Categorize errors and warnings
      if (message.includes('offline') || message.includes('connectivity')) {
        deviceStatus = 'offline'
        errors.push(message)
      } else if (message.includes('recently completed')) {
        duplicateRisk = true
        warnings.push(message)
      } else if (message.includes('operator') || message.includes('permission')) {
        operatorValid = false
        errors.push(message)
      } else if (message.includes('different from historical')) {
        warnings.push(message)
      } else {
        errors.push(message)
      }
    }

    return {
      isValid: result.isSuccess,
      errors,
      warnings,
      deviceStatus,
      operatorValid,
      duplicateRisk
    }
  }
}