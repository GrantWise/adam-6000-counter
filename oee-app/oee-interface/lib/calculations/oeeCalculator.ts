import { queryDatabase, queryDatabaseSingle } from '../database/connection';
import { 
  OeeMetrics, 
  OeeCalculationDetails, 
  OeeConfiguration,
  CounterReading, 
  ProductionJob, 
  StoppageInfo,
  OperationResult 
} from '../types/oee';
import { config, channels, timing, fromMinutes } from '@/config';
import { Availability, Performance, Quality, OEE_Calculation } from '../domain/models';
import { timeOeeCalculation } from '../services/performanceMonitoringService';
import { withOeeCache } from '../services/queryCacheService';

/**
 * OEE Calculator following Pattern 2 from counter-application-patterns.md
 * Implements Overall Equipment Effectiveness calculations using TimescaleDB counter data
 */
export class OeeCalculator {
  private config: OeeConfiguration;

  constructor(configOverride?: Partial<OeeConfiguration>) {
    this.config = {
      deviceId: configOverride?.deviceId || config.app.device.defaultId,
      productionChannel: configOverride?.productionChannel || channels.production(),
      rejectsChannel: configOverride?.rejectsChannel || channels.rejects(),
      targetRate: configOverride?.targetRate || config.oee.production.defaults.targetRate,
      shiftDuration: configOverride?.shiftDuration || config.oee.timeRanges.shiftDuration,
      minimumDataPoints: configOverride?.minimumDataPoints || config.oee.calculations.minimumDataPoints,
      stoppageThresholdMinutes: configOverride?.stoppageThresholdMinutes || timing.stoppageThreshold(),
    };
  }

  /**
   * Calculate current OEE metrics for the specified device
   * Returns domain models for clear business meaning
   * Phase 4: Wrapped with performance monitoring and caching
   */
  async calculateCurrentOEE(deviceId?: string): Promise<OEE_Calculation> {
    const targetDeviceId = deviceId || this.config.deviceId;
    
    return withOeeCache(targetDeviceId, async () => {
      return timeOeeCalculation(async () => {
        const endTime = new Date();
        
        try {
          // Get current active job
          const currentJob = await this.getCurrentJob(targetDeviceId);
          if (!currentJob) {
            return this.getDefaultOeeCalculation(targetDeviceId, endTime);
          }

          // Get counter data from job start timestamp (or last hour if job is very long)
          const dataStartTime = this.getDataStartTime(currentJob);
          const counterData = await this.getCounterData(targetDeviceId, dataStartTime);

          if (counterData.length < this.config.minimumDataPoints) {
            console.warn(`Insufficient data points for OEE calculation: ${counterData.length} < ${this.config.minimumDataPoints}`);
            return this.getDefaultOeeCalculation(targetDeviceId, endTime);
          }

          // Create domain value objects
          const availability = this.createAvailability(counterData, currentJob);
          const performance = this.createPerformance(counterData, currentJob);
          const quality = this.createQuality(counterData);

          return new OEE_Calculation({
            resource_reference: targetDeviceId,
            calculation_period_start: dataStartTime,
            calculation_period_end: endTime,
            availability,
            performance,
            quality
          });
        } catch (error) {
          console.error('Error calculating OEE:', error);
          return this.getDefaultOeeCalculation(targetDeviceId, endTime);
        }
      });
    });
  }

  /**
   * Calculate current OEE metrics (legacy format for backward compatibility)
   */
  async calculateCurrentOEELegacy(deviceId?: string): Promise<OeeMetrics> {
    const oeeCalc = await this.calculateCurrentOEE(deviceId);
    return {
      availability: oeeCalc.availability_percentage / 100,
      performance: oeeCalc.performance_percentage / 100,
      quality: oeeCalc.quality_percentage / 100,
      oee: oeeCalc.oee_percentage / 100,
      calculatedAt: new Date(),
    };
  }

  /**
   * Get detailed OEE calculation breakdown for analysis
   */
  async calculateDetailedOEE(
    deviceId?: string, 
    timestampRange?: { start: Date; end: Date }
  ): Promise<OeeCalculationDetails> {
    const targetDeviceId = deviceId || this.config.deviceId;
    const endTime = timestampRange?.end || new Date();
    const startTime = timestampRange?.start || new Date(endTime.getTime() - fromMinutes(config.oee.timeRanges.defaultLookback));

    const currentJob = await this.getCurrentJob(targetDeviceId);
    const counterData = await this.getCounterData(targetDeviceId, startTime, endTime);

    const durationMinutes = (endTime.getTime() - startTime.getTime()) / (1000 * 60);

    // Availability calculations
    const actualRunTime = this.getActualRunTime(counterData);
    const plannedRunTime = this.getPlannedRunTime(currentJob, durationMinutes);
    const downtimestamp = plannedRunTime - actualRunTime;

    // Performance calculations
    const actualOutput = this.getActualOutput(counterData);
    const targetOutput = (currentJob?.target_rate || this.config.targetRate) * (durationMinutes / 60);
    const currentRate = this.getCurrentRate(counterData);

    // Quality calculations
    const totalProduced = this.getTotalCount(counterData, this.config.productionChannel);
    const rejectedParts = this.getTotalCount(counterData, this.config.rejectsChannel);
    const goodParts = totalProduced - rejectedParts;

    return {
      // Availability
      plannedRunTime,
      actualRunTime,
      downtimestamp,

      // Performance
      actualOutput,
      targetOutput,
      currentRate,
      targetRate: currentJob?.target_rate || this.config.targetRate,

      // Quality
      totalProduced,
      goodParts,
      rejectedParts,

      // Meta
      calculationPeriod: {
        start: startTime,
        end: endTime,
        durationMinutes,
      },
      dataPoints: counterData.length,
    };
  }

  /**
   * Create Availability domain value object
   */
  private createAvailability(data: CounterReading[], job?: ProductionJob | null): Availability {
    const actualRunTime = this.getActualRunTime(data);
    const plannedTime = this.getPlannedRunTime(job);
    
    return new Availability(plannedTime, actualRunTime);
  }

  /**
   * Create Performance domain value object
   */
  private createPerformance(data: CounterReading[], job?: ProductionJob | null): Performance {
    const totalProduced = this.getTotalCount(data, this.config.productionChannel);
    const runTimeMinutes = this.getActualRunTime(data);
    const targetRate = (job?.target_rate || this.config.targetRate) / 60; // Convert to per minute
    
    return new Performance({
      totalPiecesProduced: totalProduced,
      runTimeMinutes,
      targetRatePerMinute: targetRate
    });
  }

  /**
   * Create Quality domain value object
   */
  private createQuality(data: CounterReading[]): Quality {
    const productionData = data.filter(d => d.channel === this.config.productionChannel);
    const rejectData = data.filter(d => d.channel === this.config.rejectsChannel);
    
    const totalProduced = this.getTotalCount(productionData);
    const totalRejects = rejectData.length > 0 ? this.getTotalCount(rejectData) : 0;
    const goodPieces = Math.max(0, totalProduced - totalRejects);
    
    return new Quality({
      goodPieces,
      defectivePieces: totalRejects,
      totalPiecesProduced: totalProduced
    });
  }

  // Legacy methods for backward compatibility
  private calculateAvailability(data: CounterReading[], job?: ProductionJob | null): number {
    const availability = this.createAvailability(data, job);
    return availability.decimal;
  }

  private calculatePerformance(data: CounterReading[], job?: ProductionJob | null): number {
    const performance = this.createPerformance(data, job);
    return performance.decimal;
  }

  private calculateQuality(data: CounterReading[]): number {
    const quality = this.createQuality(data);
    return quality.decimal;
  }

  /**
   * Detect current stoppage if machine is stopped
   */
  async detectCurrentStoppage(deviceId?: string): Promise<StoppageInfo | null> {
    const targetDeviceId = deviceId || this.config.deviceId;
    
    try {
      const stoppageQuery = `
        SELECT 
          MIN(timestamp) as stoppage_start,
          EXTRACT(EPOCH FROM (NOW() - MIN(timestamp))) / 60 as duration_minutes,
          COUNT(*) as zero_rate_count
        FROM counter_data 
        WHERE 
          device_id = $1 
          AND channel = $2
          AND rate = 0 
          AND timestamp > NOW() - INTERVAL '2 hours'
          AND timestamp >= COALESCE((
            SELECT timestamp 
            FROM counter_data 
            WHERE device_id = $1 AND channel = $2 AND rate > 0 
            ORDER BY timestamp DESC 
            LIMIT 1
          ), NOW() - INTERVAL '2 hours')
        HAVING COUNT(*) > $3`;

      const [stoppage] = await queryDatabase<{
        stoppage_start: string;
        duration_minutes: number;
        zero_rate_count: number;
      }>(stoppageQuery, [
        targetDeviceId, 
        this.config.productionChannel, 
        this.config.stoppageThresholdMinutes * (60 / timing.dataInterval()) // Convert minutes to reading count
      ]);

      if (!stoppage) return null;

      return {
        startTime: new Date(stoppage.stoppage_start),
        durationMinutes: Math.round(stoppage.duration_minutes),
        isActive: true,
      };
    } catch (error) {
      console.error('Error detecting stoppage:', error);
      return null;
    }
  }

  // === Private Helper Methods ===

  private async getCurrentJob(deviceId: string): Promise<ProductionJob | null> {
    const query = `
      SELECT 
        job_id,
        job_number,
        part_number,
        device_id,
        target_rate,
        start_time,
        operator_id
      FROM production_jobs
      WHERE device_id = $1 AND status = 'active'
      ORDER BY start_time DESC
      LIMIT 1`;

    return await queryDatabaseSingle<ProductionJob>(query, [deviceId]);
  }

  private getDataStartTime(job: ProductionJob | null): Date {
    if (!job) {
      return new Date(Date.now() - fromMinutes(config.oee.timeRanges.defaultLookback));
    }

    const jobStart = new Date(job.start_time);
    const oneHourAgo = new Date(Date.now() - fromMinutes(config.oee.timeRanges.defaultLookback));
    
    // Use job start timestamp, but not more than 1 hour ago for performance
    return jobStart > oneHourAgo ? jobStart : oneHourAgo;
  }

  private async getCounterData(
    deviceId: string, 
    startTime: Date, 
    endTime?: Date
  ): Promise<CounterReading[]> {
    const query = `
      SELECT timestamp, channel, rate, processed_value, quality
      FROM counter_data 
      WHERE device_id = $1 
        AND timestamp >= $2 
        ${endTime ? 'AND timestamp <= $3' : ''}
        AND channel IN ($${endTime ? '4' : '3'}, $${endTime ? '5' : '4'})
      ORDER BY timestamp DESC`;

    const params = endTime 
      ? [deviceId, startTime, endTime, this.config.productionChannel, this.config.rejectsChannel]
      : [deviceId, startTime, this.config.productionChannel, this.config.rejectsChannel];

    return await queryDatabase<CounterReading>(query, params);
  }

  private getActualRunTime(data: CounterReading[]): number {
    // Calculate run timestamp based on non-zero rate periods
    const runningData = data.filter(d => 
      d.channel === this.config.productionChannel && d.rate > 0
    );
    
    // Calculate based on configured data collection interval
    return (runningData.length * timing.dataInterval()) / 60; // Convert to minutes
  }

  private getPlannedRunTime(job?: ProductionJob | null, durationMinutes?: number): number {
    if (durationMinutes) return durationMinutes;
    
    // Default to 1 hour if no job or duration specified
    return 60; // minutes
  }

  private getActualOutput(data: CounterReading[]): number {
    const productionData = data.filter(d => d.channel === this.config.productionChannel);
    if (productionData.length === 0) return 0;

    // Get the change in processed_value over the timestamp period
    const latest = productionData[0]; // Already ordered DESC
    const earliest = productionData[productionData.length - 1];
    
    return latest.processed_value - earliest.processed_value;
  }

  private getCurrentRate(data: CounterReading[]): number {
    const recentData = data
      .filter(d => d.channel === this.config.productionChannel)
      .slice(0, 5); // Last 5 readings

    if (recentData.length === 0) return 0;

    const avgRate = recentData.reduce((sum, reading) => sum + reading.rate, 0) / recentData.length;
    return avgRate * 60; // Convert from units/second to units/minute
  }

  private getTotalCount(data: CounterReading[], channel?: number): number {
    const filteredData = channel !== undefined 
      ? data.filter(d => d.channel === channel)
      : data;

    if (filteredData.length === 0) return 0;

    // Return the difference between max and min processed_value
    const values = filteredData.map(d => d.processed_value);
    return Math.max(...values) - Math.min(...values);
  }

  private getDefaultOeeMetrics(): OeeMetrics {
    return {
      availability: 0,
      performance: 0,
      quality: 1.0,
      oee: 0,
      calculatedAt: new Date(),
    };
  }

  private getDefaultOeeCalculation(deviceId: string, endTime: Date): OEE_Calculation {
    const startTime = new Date(endTime.getTime() - fromMinutes(60));
    return new OEE_Calculation({
      resource_reference: deviceId,
      calculation_period_start: startTime,
      calculation_period_end: endTime,
      availability: new Availability(60, 0),
      performance: new Performance({ 
        totalPiecesProduced: 0, 
        runTimeMinutes: 0, 
        targetRatePerMinute: this.config.targetRate / 60 
      }),
      quality: new Quality({ goodPieces: 0, defectivePieces: 0 })
    });
  }

  // === Public Utility Methods ===

  /**
   * Update calculator configuration
   */
  updateConfiguration(config: Partial<OeeConfiguration>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get current configuration
   */
  getConfiguration(): OeeConfiguration {
    return { ...this.config };
  }
}

/**
 * Factory function to create OEE calculator instance
 */
export function createOeeCalculator(config?: Partial<OeeConfiguration>): OeeCalculator {
  return new OeeCalculator(config);
}

/**
 * Helper function to format OEE metrics as percentages
 */
export function formatOeeMetrics(metrics: OeeMetrics): {
  availabilityPercent: number;
  performancePercent: number;
  qualityPercent: number;
  oeePercent: number;
} {
  return {
    availabilityPercent: Math.round(metrics.availability * 100 * 10) / 10,
    performancePercent: Math.round(metrics.performance * 100 * 10) / 10,
    qualityPercent: Math.round(metrics.quality * 100 * 10) / 10,
    oeePercent: Math.round(metrics.oee * 100 * 10) / 10,
  };
}