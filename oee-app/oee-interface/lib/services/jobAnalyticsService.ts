import { queryDatabase, queryDatabaseSingle } from '../database/connection'
import { ProductionJob, OperationResult } from '../types/oee'

/**
 * Job Analytics Service - Advanced analytics and progress tracking
 * Provides real-time job metrics, completion estimates, and efficiency tracking
 */
export class JobAnalyticsService {
  private deviceId: string

  constructor(deviceId: string) {
    this.deviceId = deviceId
  }

  /**
   * Get comprehensive real-time job analytics
   */
  async getRealTimeJobAnalytics(jobId: number): Promise<OperationResult<JobAnalytics>> {
    try {
      const job = await this.getJobDetails(jobId)
      if (!job.isSuccess || !job.value) {
        return {
          isSuccess: false,
          value: {} as JobAnalytics,
          errorMessage: 'Job not found'
        }
      }

      const jobData = job.value
      const currentTime = new Date()
      const startTime = new Date(jobData.start_time)
      const elapsedMinutes = Math.floor((currentTime.getTime() - startTime.getTime()) / (1000 * 60))

      // Get production metrics
      const productionMetrics = await this.getProductionMetrics(jobData, elapsedMinutes)
      
      // Get efficiency trends
      const efficiencyTrends = await this.getEfficiencyTrends(jobData)
      
      // Get quality metrics
      const qualityMetrics = await this.getQualityMetrics(jobData)
      
      // Calculate completion estimates
      const completionEstimates = await this.getCompletionEstimates(jobData, productionMetrics)
      
      // Get performance benchmarks
      const benchmarks = await this.getPerformanceBenchmarks(jobData.part_number, jobData.target_rate)

      const analytics: JobAnalytics = {
        jobId: jobData.job_id,
        jobNumber: jobData.job_number,
        partNumber: jobData.part_number,
        startTime: jobData.start_time,
        elapsedMinutes,
        status: jobData.status as 'active' | 'completed' | 'cancelled',
        
        production: productionMetrics,
        efficiency: efficiencyTrends,
        quality: qualityMetrics,
        completion: completionEstimates,
        benchmarks: benchmarks,
        
        lastUpdated: currentTime.toISOString()
      }

      return {
        isSuccess: true,
        value: analytics,
        errorMessage: null
      }
    } catch (error) {
      console.error('Error getting job analytics:', error)
      return {
        isSuccess: false,
        value: {} as JobAnalytics,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }

  /**
   * Get production metrics for the job
   */
  private async getProductionMetrics(job: ProductionJob, elapsedMinutes: number): Promise<ProductionMetrics> {
    try {
      const productionQuery = `
        SELECT 
          MAX(processed_value) - MIN(processed_value) as total_produced,
          COUNT(*) as data_points,
          AVG(rate) * 60 as average_rate,
          (
            SELECT AVG(rate) * 60 
            FROM counter_data 
            WHERE device_id = $1 AND channel = 0 
              AND time >= NOW() - INTERVAL '10 minutes'
          ) as current_rate,
          (
            SELECT AVG(rate) * 60 
            FROM counter_data 
            WHERE device_id = $1 AND channel = 0 
              AND time >= NOW() - INTERVAL '60 minutes'
          ) as hourly_rate
        FROM counter_data
        WHERE device_id = $1 
          AND channel = 0
          AND time >= $2
          ${job.end_time ? 'AND time <= $3' : ''}
        HAVING COUNT(*) > 0`

      const params = job.end_time 
        ? [this.deviceId, job.start_time, job.end_time]
        : [this.deviceId, job.start_time]

      const data = await queryDatabaseSingle<{
        total_produced: number
        data_points: number
        average_rate: number
        current_rate: number
        hourly_rate: number
      }>(productionQuery, params)

      const totalProduced = data?.total_produced || 0
      const averageRate = data?.average_rate || 0
      const currentRate = data?.current_rate || 0
      const hourlyRate = data?.hourly_rate || 0
      
      // Calculate rates and projections
      const targetRate = job.target_rate
      const expectedQuantity = Math.round(targetRate * elapsedMinutes)
      const productionVariance = totalProduced - expectedQuantity
      const rateEfficiency = targetRate > 0 ? (averageRate / targetRate) * 100 : 0

      return {
        totalProduced: Math.round(totalProduced),
        targetQuantity: Math.round(targetRate * 8 * 60), // 8-hour shift target
        expectedQuantityByNow: expectedQuantity,
        productionVariance,
        currentRate: Math.round(currentRate * 10) / 10,
        averageRate: Math.round(averageRate * 10) / 10,
        hourlyRate: Math.round(hourlyRate * 10) / 10,
        targetRate,
        rateEfficiency: Math.round(rateEfficiency * 10) / 10,
        trendDirection: this.calculateTrendDirection(currentRate, hourlyRate, averageRate)
      }
    } catch (error) {
      console.error('Error getting production metrics:', error)
      return this.getDefaultProductionMetrics(job.target_rate, elapsedMinutes)
    }
  }

  /**
   * Get efficiency trends over time
   */
  private async getEfficiencyTrends(job: ProductionJob): Promise<EfficiencyTrends> {
    try {
      const trendsQuery = `
        SELECT 
          DATE_TRUNC('hour', time) as hour_bucket,
          AVG(rate) * 60 as hourly_average_rate,
          MAX(rate) * 60 as hourly_peak_rate,
          MIN(rate) * 60 as hourly_min_rate,
          COUNT(*) as data_points
        FROM counter_data
        WHERE device_id = $1 
          AND channel = 0
          AND time >= $2
          ${job.end_time ? 'AND time <= $3' : ''}
        GROUP BY DATE_TRUNC('hour', time)
        ORDER BY hour_bucket DESC
        LIMIT 8`

      const params = job.end_time 
        ? [this.deviceId, job.start_time, job.end_time]
        : [this.deviceId, job.start_time]

      const hourlyData = await queryDatabase<{
        hour_bucket: string
        hourly_average_rate: number
        hourly_peak_rate: number
        hourly_min_rate: number
        data_points: number
      }>(trendsQuery, params)

      const targetRate = job.target_rate
      const hourlyEfficiency = hourlyData.map(hour => ({
        hour: new Date(hour.hour_bucket).toISOString(),
        efficiency: targetRate > 0 ? (hour.hourly_average_rate / targetRate) * 100 : 0,
        averageRate: Math.round(hour.hourly_average_rate * 10) / 10,
        peakRate: Math.round(hour.hourly_peak_rate * 10) / 10,
        minRate: Math.round(hour.hourly_min_rate * 10) / 10
      }))

      // Calculate overall trends
      const currentEfficiency = hourlyEfficiency.length > 0 ? hourlyEfficiency[0].efficiency : 0
      const previousEfficiency = hourlyEfficiency.length > 1 ? hourlyEfficiency[1].efficiency : currentEfficiency
      
      let overallTrend: 'improving' | 'declining' | 'stable' = 'stable'
      if (currentEfficiency > previousEfficiency * 1.05) {
        overallTrend = 'improving'
      } else if (currentEfficiency < previousEfficiency * 0.95) {
        overallTrend = 'declining'
      }

      const bestEfficiency = Math.max(...hourlyEfficiency.map(h => h.efficiency), 0)
      const worstEfficiency = Math.min(...hourlyEfficiency.map(h => h.efficiency), 0)
      const averageEfficiency = hourlyEfficiency.length > 0 
        ? hourlyEfficiency.reduce((sum, h) => sum + h.efficiency, 0) / hourlyEfficiency.length 
        : 0

      return {
        currentEfficiency: Math.round(currentEfficiency * 10) / 10,
        averageEfficiency: Math.round(averageEfficiency * 10) / 10,
        bestEfficiency: Math.round(bestEfficiency * 10) / 10,
        worstEfficiency: Math.round(worstEfficiency * 10) / 10,
        overallTrend,
        hourlyData: hourlyEfficiency,
        consistencyScore: this.calculateConsistencyScore(hourlyEfficiency)
      }
    } catch (error) {
      console.error('Error getting efficiency trends:', error)
      return this.getDefaultEfficiencyTrends()
    }
  }

  /**
   * Get quality metrics for the job
   */
  private async getQualityMetrics(job: ProductionJob): Promise<QualityMetrics> {
    try {
      // Get reject data from channel 1 (if available)
      const qualityQuery = `
        SELECT 
          COALESCE(SUM(CASE WHEN channel = 0 THEN processed_value END), 0) as total_production,
          COALESCE(SUM(CASE WHEN channel = 1 THEN processed_value END), 0) as total_rejects,
          COUNT(CASE WHEN channel = 0 THEN 1 END) as production_readings,
          COUNT(CASE WHEN channel = 1 THEN 1 END) as reject_readings
        FROM counter_data
        WHERE device_id = $1 
          AND channel IN (0, 1)
          AND time >= $2
          ${job.end_time ? 'AND time <= $3' : ''}
        HAVING COUNT(*) > 0`

      const params = job.end_time 
        ? [this.deviceId, job.start_time, job.end_time]
        : [this.deviceId, job.start_time]

      const data = await queryDatabaseSingle<{
        total_production: number
        total_rejects: number
        production_readings: number
        reject_readings: number
      }>(qualityQuery, params)

      const totalProduction = data?.total_production || 0
      const totalRejects = data?.total_rejects || 0
      const goodParts = Math.max(0, totalProduction - totalRejects)
      
      const qualityPercent = totalProduction > 0 ? (goodParts / totalProduction) * 100 : 100
      const defectRate = totalProduction > 0 ? (totalRejects / totalProduction) * 100 : 0
      
      // Get hourly quality trend
      const hourlyQualityQuery = `
        SELECT 
          DATE_TRUNC('hour', time) as hour_bucket,
          COALESCE(SUM(CASE WHEN channel = 0 THEN processed_value END), 0) as hour_production,
          COALESCE(SUM(CASE WHEN channel = 1 THEN processed_value END), 0) as hour_rejects
        FROM counter_data
        WHERE device_id = $1 
          AND channel IN (0, 1)
          AND time >= $2
          ${job.end_time ? 'AND time <= $3' : ''}
        GROUP BY DATE_TRUNC('hour', time)
        ORDER BY hour_bucket DESC
        LIMIT 6`

      const hourlyQuality = await queryDatabase<{
        hour_bucket: string
        hour_production: number
        hour_rejects: number
      }>(hourlyQualityQuery, params)

      const qualityTrend = hourlyQuality.map(hour => {
        const hourGood = Math.max(0, hour.hour_production - hour.hour_rejects)
        const hourQuality = hour.hour_production > 0 ? (hourGood / hour.hour_production) * 100 : 100
        return {
          hour: new Date(hour.hour_bucket).toISOString(),
          qualityPercent: Math.round(hourQuality * 10) / 10,
          production: hour.hour_production,
          rejects: hour.hour_rejects
        }
      })

      // Determine trend direction
      let trendDirection: 'improving' | 'declining' | 'stable' = 'stable'
      if (qualityTrend.length >= 2) {
        const currentQuality = qualityTrend[0]?.qualityPercent || 100
        const previousQuality = qualityTrend[1]?.qualityPercent || 100
        
        if (currentQuality > previousQuality + 0.5) {
          trendDirection = 'improving'
        } else if (currentQuality < previousQuality - 0.5) {
          trendDirection = 'declining'
        }
      }

      return {
        qualityPercent: Math.round(qualityPercent * 10) / 10,
        defectRate: Math.round(defectRate * 100) / 100,
        totalProduced: Math.round(totalProduction),
        goodParts: Math.round(goodParts),
        rejectedParts: Math.round(totalRejects),
        trendDirection,
        hourlyTrend: qualityTrend,
        isQualityDataAvailable: data?.reject_readings > 0
      }
    } catch (error) {
      console.error('Error getting quality metrics:', error)
      return this.getDefaultQualityMetrics()
    }
  }

  /**
   * Calculate completion estimates
   */
  private async getCompletionEstimates(job: ProductionJob, production: ProductionMetrics): Promise<CompletionEstimates> {
    try {
      const currentTime = new Date()
      const startTime = new Date(job.start_time)
      const elapsedHours = (currentTime.getTime() - startTime.getTime()) / (1000 * 60 * 60)
      
      // Assume 8-hour shift target
      const shiftHours = 8
      const targetQuantity = job.target_rate * shiftHours * 60
      
      const progressPercent = targetQuantity > 0 ? (production.totalProduced / targetQuantity) * 100 : 0
      
      // Estimate completion time based on current rate
      let estimatedCompletionTime: string | null = null
      let onSchedule = false
      
      if (production.currentRate > 0 && job.status === 'active') {
        const remainingQuantity = Math.max(0, targetQuantity - production.totalProduced)
        const hoursToComplete = remainingQuantity / (production.currentRate * 60)
        const completionTime = new Date(currentTime.getTime() + (hoursToComplete * 60 * 60 * 1000))
        estimatedCompletionTime = completionTime.toISOString()
        
        // Check if on schedule (completion within shift time)
        const shiftEndTime = new Date(startTime.getTime() + (shiftHours * 60 * 60 * 1000))
        onSchedule = completionTime <= shiftEndTime
      }
      
      // Calculate schedule adherence
      const expectedProgressPercent = (elapsedHours / shiftHours) * 100
      const scheduleAdherence = expectedProgressPercent > 0 ? (progressPercent / expectedProgressPercent) * 100 : 100
      
      // Calculate efficiency-based estimates
      const currentEfficiency = production.rateEfficiency
      let efficiencyBasedCompletion: string | null = null
      
      if (currentEfficiency > 0 && job.status === 'active') {
        const adjustedRate = job.target_rate * (currentEfficiency / 100)
        const remainingQuantity = Math.max(0, targetQuantity - production.totalProduced)
        const hoursToComplete = remainingQuantity / (adjustedRate * 60)
        const completionTime = new Date(currentTime.getTime() + (hoursToComplete * 60 * 60 * 1000))
        efficiencyBasedCompletion = completionTime.toISOString()
      }

      return {
        progressPercent: Math.min(100, Math.round(progressPercent * 10) / 10),
        targetQuantity: Math.round(targetQuantity),
        remainingQuantity: Math.max(0, Math.round(targetQuantity - production.totalProduced)),
        estimatedCompletionTime,
        efficiencyBasedCompletion,
        onSchedule,
        scheduleAdherence: Math.round(scheduleAdherence * 10) / 10,
        hoursRemaining: elapsedHours < shiftHours ? Math.round((shiftHours - elapsedHours) * 10) / 10 : 0,
        confidenceLevel: this.calculateConfidenceLevel(production, elapsedHours)
      }
    } catch (error) {
      console.error('Error calculating completion estimates:', error)
      return this.getDefaultCompletionEstimates()
    }
  }

  /**
   * Get performance benchmarks for comparison
   */
  private async getPerformanceBenchmarks(partNumber: string, targetRate: number): Promise<PerformanceBenchmarks> {
    try {
      // Get historical performance for this part number
      const benchmarkQuery = `
        SELECT 
          AVG(sub.avg_rate) as historical_avg_rate,
          AVG(sub.efficiency) as historical_avg_efficiency,
          MAX(sub.best_rate) as historical_best_rate,
          COUNT(*) as job_count
        FROM (
          SELECT 
            pj.job_id,
            AVG(cd.rate) * 60 as avg_rate,
            MAX(cd.rate) * 60 as best_rate,
            (AVG(cd.rate) * 60 / pj.target_rate) * 100 as efficiency
          FROM production_jobs pj
          JOIN counter_data cd ON cd.device_id = pj.device_id
            AND cd.time >= pj.start_time 
            AND (pj.end_time IS NULL OR cd.time <= pj.end_time)
            AND cd.channel = 0
          WHERE pj.part_number = $1
            AND pj.status = 'completed'
            AND pj.end_time >= NOW() - INTERVAL '90 days'
          GROUP BY pj.job_id, pj.target_rate
          HAVING COUNT(cd.*) >= 10
        ) sub`

      const benchmarkData = await queryDatabaseSingle<{
        historical_avg_rate: number
        historical_avg_efficiency: number
        historical_best_rate: number
        job_count: number
      }>(benchmarkQuery, [partNumber])

      // Get industry/device averages
      const deviceBenchmarkQuery = `
        SELECT 
          AVG(sub.avg_rate) as device_avg_rate,
          AVG(sub.efficiency) as device_avg_efficiency
        FROM (
          SELECT 
            pj.job_id,
            AVG(cd.rate) * 60 as avg_rate,
            (AVG(cd.rate) * 60 / pj.target_rate) * 100 as efficiency
          FROM production_jobs pj
          JOIN counter_data cd ON cd.device_id = pj.device_id
            AND cd.time >= pj.start_time 
            AND (pj.end_time IS NULL OR cd.time <= pj.end_time)
            AND cd.channel = 0
          WHERE pj.device_id = $1
            AND pj.status = 'completed'
            AND pj.end_time >= NOW() - INTERVAL '30 days'
          GROUP BY pj.job_id, pj.target_rate
          HAVING COUNT(cd.*) >= 10
        ) sub`

      const deviceData = await queryDatabaseSingle<{
        device_avg_rate: number
        device_avg_efficiency: number
      }>(deviceBenchmarkQuery, [this.deviceId])

      return {
        partHistoricalAverage: benchmarkData?.historical_avg_rate ? Math.round(benchmarkData.historical_avg_rate * 10) / 10 : null,
        partHistoricalEfficiency: benchmarkData?.historical_avg_efficiency ? Math.round(benchmarkData.historical_avg_efficiency * 10) / 10 : null,
        partBestRate: benchmarkData?.historical_best_rate ? Math.round(benchmarkData.historical_best_rate * 10) / 10 : null,
        deviceAverage: deviceData?.device_avg_rate ? Math.round(deviceData.device_avg_rate * 10) / 10 : null,
        deviceEfficiencyAverage: deviceData?.device_avg_efficiency ? Math.round(deviceData.device_avg_efficiency * 10) / 10 : null,
        sampleSize: benchmarkData?.job_count || 0,
        hasHistoricalData: (benchmarkData?.job_count || 0) >= 3
      }
    } catch (error) {
      console.error('Error getting benchmarks:', error)
      return this.getDefaultBenchmarks()
    }
  }

  // Helper methods and default data
  private getJobDetails = async (jobId: number): Promise<OperationResult<ProductionJob>> => {
    try {
      const query = `
        SELECT 
          job_id, job_number, part_number, device_id, target_rate,
          start_time, end_time, operator_id, status
        FROM production_jobs
        WHERE job_id = $1`

      const job = await queryDatabaseSingle<ProductionJob>(query, [jobId])
      
      return {
        isSuccess: !!job,
        value: job,
        errorMessage: job ? null : 'Job not found'
      }
    } catch (error) {
      return {
        isSuccess: false,
        value: null,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }

  private calculateTrendDirection(current: number, hourly: number, average: number): 'improving' | 'declining' | 'stable' {
    if (current > Math.max(hourly, average) * 1.05) return 'improving'
    if (current < Math.min(hourly, average) * 0.95) return 'declining'
    return 'stable'
  }

  private calculateConsistencyScore(hourlyData: Array<{ efficiency: number }>): number {
    if (hourlyData.length < 2) return 100
    
    const efficiencies = hourlyData.map(h => h.efficiency)
    const avg = efficiencies.reduce((sum, e) => sum + e, 0) / efficiencies.length
    const variance = efficiencies.reduce((sum, e) => sum + Math.pow(e - avg, 2), 0) / efficiencies.length
    const standardDeviation = Math.sqrt(variance)
    
    // Convert to consistency score (lower deviation = higher consistency)
    return Math.max(0, Math.min(100, 100 - (standardDeviation * 2)))
  }

  private calculateConfidenceLevel(production: ProductionMetrics, elapsedHours: number): 'high' | 'medium' | 'low' {
    if (elapsedHours < 1) return 'low'
    if (elapsedHours >= 2 && production.rateEfficiency > 80 && production.rateEfficiency < 120) return 'high'
    if (elapsedHours >= 1 && production.rateEfficiency > 60 && production.rateEfficiency < 150) return 'medium'
    return 'low'
  }

  // Default data methods
  private getDefaultProductionMetrics(targetRate: number, elapsedMinutes: number): ProductionMetrics {
    return {
      totalProduced: 0,
      targetQuantity: Math.round(targetRate * 8 * 60),
      expectedQuantityByNow: Math.round(targetRate * elapsedMinutes),
      productionVariance: -Math.round(targetRate * elapsedMinutes),
      currentRate: 0,
      averageRate: 0,
      hourlyRate: 0,
      targetRate,
      rateEfficiency: 0,
      trendDirection: 'stable'
    }
  }

  private getDefaultEfficiencyTrends(): EfficiencyTrends {
    return {
      currentEfficiency: 0,
      averageEfficiency: 0,
      bestEfficiency: 0,
      worstEfficiency: 0,
      overallTrend: 'stable',
      hourlyData: [],
      consistencyScore: 0
    }
  }

  private getDefaultQualityMetrics(): QualityMetrics {
    return {
      qualityPercent: 100,
      defectRate: 0,
      totalProduced: 0,
      goodParts: 0,
      rejectedParts: 0,
      trendDirection: 'stable',
      hourlyTrend: [],
      isQualityDataAvailable: false
    }
  }

  private getDefaultCompletionEstimates(): CompletionEstimates {
    return {
      progressPercent: 0,
      targetQuantity: 0,
      remainingQuantity: 0,
      estimatedCompletionTime: null,
      efficiencyBasedCompletion: null,
      onSchedule: true,
      scheduleAdherence: 100,
      hoursRemaining: 8,
      confidenceLevel: 'low'
    }
  }

  private getDefaultBenchmarks(): PerformanceBenchmarks {
    return {
      partHistoricalAverage: null,
      partHistoricalEfficiency: null,
      partBestRate: null,
      deviceAverage: null,
      deviceEfficiencyAverage: null,
      sampleSize: 0,
      hasHistoricalData: false
    }
  }
}

// Type definitions for job analytics
export interface JobAnalytics {
  jobId: number
  jobNumber: string
  partNumber: string
  startTime: string | Date
  elapsedMinutes: number
  status: 'active' | 'completed' | 'cancelled'
  
  production: ProductionMetrics
  efficiency: EfficiencyTrends
  quality: QualityMetrics
  completion: CompletionEstimates
  benchmarks: PerformanceBenchmarks
  
  lastUpdated: string
}

export interface ProductionMetrics {
  totalProduced: number
  targetQuantity: number
  expectedQuantityByNow: number
  productionVariance: number
  currentRate: number
  averageRate: number
  hourlyRate: number
  targetRate: number
  rateEfficiency: number
  trendDirection: 'improving' | 'declining' | 'stable'
}

export interface EfficiencyTrends {
  currentEfficiency: number
  averageEfficiency: number
  bestEfficiency: number
  worstEfficiency: number
  overallTrend: 'improving' | 'declining' | 'stable'
  hourlyData: Array<{
    hour: string
    efficiency: number
    averageRate: number
    peakRate: number
    minRate: number
  }>
  consistencyScore: number
}

export interface QualityMetrics {
  qualityPercent: number
  defectRate: number
  totalProduced: number
  goodParts: number
  rejectedParts: number
  trendDirection: 'improving' | 'declining' | 'stable'
  hourlyTrend: Array<{
    hour: string
    qualityPercent: number
    production: number
    rejects: number
  }>
  isQualityDataAvailable: boolean
}

export interface CompletionEstimates {
  progressPercent: number
  targetQuantity: number
  remainingQuantity: number
  estimatedCompletionTime: string | null
  efficiencyBasedCompletion: string | null
  onSchedule: boolean
  scheduleAdherence: number
  hoursRemaining: number
  confidenceLevel: 'high' | 'medium' | 'low'
}

export interface PerformanceBenchmarks {
  partHistoricalAverage: number | null
  partHistoricalEfficiency: number | null
  partBestRate: number | null
  deviceAverage: number | null
  deviceEfficiencyAverage: number | null
  sampleSize: number
  hasHistoricalData: boolean
}

/**
 * Factory function to create JobAnalyticsService
 */
export function createJobAnalyticsService(deviceId: string): JobAnalyticsService {
  return new JobAnalyticsService(deviceId)
}