// TypeScript interfaces and types for OEE calculations and database models
// Based on the Industrial Counter Platform architecture and TimescaleDB schema

// === Database Model Types ===

/**
 * Counter data reading from TimescaleDB counter_data table
 * Maps to the existing counter_data schema from ADAM devices
 */
export interface CounterReading {
  time: string | Date; // Timestamp (TimescaleDB uses timestamptz)
  device_id: string;
  channel: number; // 0 = production, 1 = rejects, etc.
  raw_value: number;
  processed_value: number;
  rate: number; // Units per second
  quality: string | number; // May need conversion from TEXT to numeric
}

/**
 * Production job record from production_jobs table
 */
export interface ProductionJob {
  job_id: number;
  job_number: string;
  part_number: string;
  device_id: string;
  target_rate: number; // Units per minute
  start_time: string | Date;
  end_time?: string | Date;
  operator_id?: string;
  status: 'active' | 'completed' | 'cancelled';
}

/**
 * Stoppage event record from stoppage_events table
 */
export interface StoppageEvent {
  event_id: number;
  device_id: string;
  job_id?: number;
  start_time: string | Date;
  end_time?: string | Date;
  duration_minutes?: number;
  duration_seconds?: number;
  category?: string; // Mechanical, Electrical, etc.
  sub_category?: string; // Jam, Wear, etc.
  comments?: string;
  classified_at?: string | Date;
  classified_by?: string;
  operator_id?: string;
  status: 'unclassified' | 'classified';
}

// === OEE Calculation Types ===

/**
 * Core OEE metrics following Pattern 2 from counter-application-patterns.md
 */
export interface OeeMetrics {
  availability: number; // 0-1 (percentage as decimal)
  performance: number; // 0-1 (percentage as decimal)
  quality: number; // 0-1 (percentage as decimal)
  oee: number; // Overall OEE (availability * performance * quality)
  calculatedAt: Date;
  availabilityRating?: 'excellent' | 'good' | 'needs-improvement'
  performanceRating?: 'excellent' | 'good' | 'needs-improvement'
  qualityRating?: 'excellent' | 'good' | 'needs-improvement'
  oeeRating?: 'excellent' | 'good' | 'needs-improvement'
  requiresAttention?: boolean
  trend?: 'improving' | 'stable' | 'declining'
}

/**
 * Detailed OEE calculation breakdown for debugging and analysis
 */
export interface OeeCalculationDetails {
  // Availability calculation details
  plannedRunTime: number; // Minutes
  actualRunTime: number; // Minutes
  downtime: number; // Minutes
  
  // Performance calculation details
  actualOutput: number; // Units produced
  targetOutput: number; // Expected units for time period
  currentRate: number; // Current units per minute
  targetRate: number; // Target units per minute
  
  // Quality calculation details
  totalProduced: number; // Total units produced
  goodParts: number; // Good parts produced
  rejectedParts: number; // Rejected parts
  
  // Meta information
  calculationPeriod: {
    start: Date;
    end: Date;
    durationMinutes: number;
  };
  dataPoints: number; // Number of counter readings used
}

/**
 * OEE configuration parameters
 */
export interface OeeConfiguration {
  deviceId: string;
  productionChannel: number; // Usually channel 0
  rejectsChannel?: number; // Usually channel 1
  targetRate: number; // Units per minute
  shiftDuration: number; // Hours
  minimumDataPoints: number; // Minimum readings required for calculation
  stoppageThresholdMinutes: number; // Minutes of zero rate to consider stoppage
}

// === API Response Types ===

/**
 * Current metrics API response
 */
export interface CurrentMetricsResponse {
  currentRate: number; // Units per minute
  targetRate: number;
  performancePercent: number;
  qualityPercent: number;
  availabilityPercent: number;
  oeePercent: number;
  status: 'running' | 'stopped' | 'error';
  lastUpdate: string;
}

/**
 * Historical metrics for charts
 */
export interface HistoricalMetricsPoint {
  time: string;
  rate: number;
  oee: number;
  availability: number;
  performance: number;
  quality: number;
}

/**
 * Stoppage information for UI
 */
export interface StoppageInfo {
  startTime: Date;
  durationMinutes: number;
  isActive: boolean;
  category?: string;
  subCategory?: string;
  description?: string;
}

// === Utility Types ===

/**
 * Operation result pattern for error handling
 */
export interface OperationResult<T> {
  isSuccess: boolean;
  value: T;
  errorMessage: string | null;
}

/**
 * Time range for queries
 */
export interface TimeRange {
  start: Date;
  end: Date;
}

/**
 * Device status information
 */
export interface DeviceStatus {
  deviceId: string;
  isOnline: boolean;
  lastDataReceived: Date;
  connectionLatency?: number; // Milliseconds
}

// === Job Management Types ===

/**
 * Job interface for UI components
 */
export interface Job {
  jobId?: number;
  jobNumber: string;
  partNumber: string;
  targetRate: number;
  quantity: number;
  startTime: Date;
  status?: string;
}

/**
 * Request to start a new job
 */
export interface NewJobRequest {
  jobNumber: string;
  partNumber: string;
  deviceId: string;
  targetRate: number;
  operatorId?: string;
}

/**
 * Request to classify a stoppage
 */
export interface StoppageClassificationRequest {
  category: string;
  subCategory: string;
  comments?: string;
  operatorId: string;
}

// === Aggregation Types ===

/**
 * Shift summary data
 */
export interface ShiftSummary {
  shiftStart: Date;
  shiftEnd: Date;
  totalProduction: number;
  targetProduction: number;
  oeeAverage: number;
  downtimeMinutes: number;
  qualityPercent: number;
  jobs: ProductionJob[];
  stoppages: StoppageEvent[];
}

/**
 * Rate calculation result
 */
export interface RateCalculation {
  unitsPerMinute: number;
  averageOverPeriod: number;
  trend: 'increasing' | 'decreasing' | 'stable';
  confidence: number; // 0-1, based on data quality
}

// === Chart Data Types ===

/**
 * Chart data point for production rate visualization
 */
export interface ChartDataPoint {
  time: string;
  value: number;
  label?: string;
}

/**
 * Multi-series chart data
 */
export interface MultiSeriesChartData {
  labels: string[];
  datasets: Array<{
    name: string;
    data: number[];
    color?: string;
  }>;
}

// === Error Types ===

/**
 * Database connection status
 */
export interface DatabaseStatus {
  isConnected: boolean;
  latencyMs?: number;
  error?: string;
  lastCheck: Date;
}

/**
 * Performance metrics for monitoring
 */
export interface PerformanceMetrics {
  averageOeeCalculationTime: number; // Milliseconds
  averageQueryDuration: number; // Milliseconds
  queryCount: number;
  cacheHitRate: number; // 0-1
  slowQueries: number; // Queries over threshold
  uptimeSeconds: number;
  peakMemoryUsage?: number; // Bytes
  activeConnections?: number;
}

/**
 * Application health status with performance metrics
 */
export interface HealthStatus {
  status: 'healthy' | 'warning' | 'error';
  database: DatabaseStatus;
  dataAge: number; // Milliseconds since last data
  version: string;
  uptime: number; // Seconds
  performance?: PerformanceMetrics;
}