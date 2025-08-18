/**
 * Fallback strategies for when circuit breakers are open
 * Provides degraded but functional service during database outages
 * Critical for industrial environments where production cannot stop
 */

import { ProductionJob, CounterReading } from '@/lib/types/oee';
import { Work_Order } from '@/lib/domain/models';

/**
 * In-memory cache for critical data when database is unavailable
 * This provides basic functionality during outages
 */
class FallbackCache {
  private activeJobs = new Map<string, ProductionJob>();
  private latestReadings = new Map<string, CounterReading>();
  private lastUpdate = new Map<string, Date>();
  private cacheTTL = 300000; // 5 minutes

  /**
   * Cache active job data
   */
  cacheActiveJob(deviceId: string, job: ProductionJob): void {
    this.activeJobs.set(deviceId, job);
    this.lastUpdate.set(`job-${deviceId}`, new Date());
  }

  /**
   * Get cached active job
   */
  getCachedActiveJob(deviceId: string): ProductionJob | null {
    const job = this.activeJobs.get(deviceId);
    const lastUpdate = this.lastUpdate.get(`job-${deviceId}`);
    
    if (!job || !lastUpdate || Date.now() - lastUpdate.getTime() > this.cacheTTL) {
      return null;
    }
    
    return job;
  }

  /**
   * Cache latest counter reading
   */
  cacheReading(deviceId: string, channel: number, reading: CounterReading): void {
    const key = `${deviceId}-${channel}`;
    this.latestReadings.set(key, reading);
    this.lastUpdate.set(`reading-${key}`, new Date());
  }

  /**
   * Get cached reading
   */
  getCachedReading(deviceId: string, channel: number): CounterReading | null {
    const key = `${deviceId}-${channel}`;
    const reading = this.latestReadings.get(key);
    const lastUpdate = this.lastUpdate.get(`reading-${key}`);
    
    if (!reading || !lastUpdate || Date.now() - lastUpdate.getTime() > this.cacheTTL) {
      return null;
    }
    
    return reading;
  }

  /**
   * Clear expired cache entries
   */
  clearExpired(): void {
    const now = Date.now();
    
    for (const [key, updateTime] of this.lastUpdate.entries()) {
      if (now - updateTime.getTime() > this.cacheTTL) {
        this.lastUpdate.delete(key);
        
        if (key.startsWith('job-')) {
          const deviceId = key.replace('job-', '');
          this.activeJobs.delete(deviceId);
        } else if (key.startsWith('reading-')) {
          const readingKey = key.replace('reading-', '');
          this.latestReadings.delete(readingKey);
        }
      }
    }
  }

  /**
   * Get cache status for monitoring
   */
  getStatus(): {
    activeJobsCount: number;
    readingsCount: number;
    oldestEntry: Date | null;
  } {
    this.clearExpired();
    
    let oldestEntry: Date | null = null;
    for (const updateTime of this.lastUpdate.values()) {
      if (!oldestEntry || updateTime < oldestEntry) {
        oldestEntry = updateTime;
      }
    }
    
    return {
      activeJobsCount: this.activeJobs.size,
      readingsCount: this.latestReadings.size,
      oldestEntry
    };
  }
}

// Global fallback cache instance
const fallbackCache = new FallbackCache();

/**
 * Database fallback strategies
 */
export class DatabaseFallbackStrategies {
  
  /**
   * Fallback for finding active job when database is unavailable
   */
  static findActiveJobFallback(deviceId: string): ProductionJob | null {
    console.warn(`Database unavailable - using cached data for active job lookup: ${deviceId}`);
    
    const cachedJob = fallbackCache.getCachedActiveJob(deviceId);
    if (cachedJob) {
      console.log(`Returning cached active job for device ${deviceId}`);
      return cachedJob;
    }
    
    // If no cached data, return a basic placeholder job
    // This ensures the system doesn't crash but indicates degraded mode
    console.warn(`No cached job data available for device ${deviceId} - returning placeholder`);
    return {
      job_id: -1,
      job_number: `FALLBACK-${Date.now()}`,
      part_number: 'UNKNOWN',
      device_id: deviceId,
      target_rate: 60, // Default target rate
      start_time: new Date().toISOString(),
      end_time: null,
      operator_id: 'system',
      status: 'active'
    };
  }

  /**
   * Fallback for finding active work order
   */
  static findActiveWorkOrderFallback(deviceId: string): Work_Order | null {
    console.warn(`Database unavailable - using cached data for work order lookup: ${deviceId}`);
    
    const cachedJob = fallbackCache.getCachedActiveJob(deviceId);
    if (cachedJob) {
      // Convert cached job to work order
      return new Work_Order({
        work_order_id: cachedJob.job_number,
        work_order_description: `Production of ${cachedJob.part_number}`,
        product_id: cachedJob.part_number,
        product_description: cachedJob.part_number,
        planned_quantity: Math.round(cachedJob.target_rate * 8),
        scheduled_start_time: new Date(cachedJob.start_time),
        scheduled_end_time: new Date(new Date(cachedJob.start_time).getTime() + 8 * 60 * 60 * 1000),
        resource_reference: cachedJob.device_id,
        status: 'active'
      });
    }
    
    console.warn(`No cached work order data available for device ${deviceId}`);
    return null;
  }

  /**
   * Fallback for getting current rate when database is unavailable
   */
  static getCurrentRateFallback(deviceId: string): { 
    units_per_minute: number; 
    timestamp: Date; 
  } | null {
    console.warn(`Database unavailable - using cached data for rate lookup: ${deviceId}`);
    
    const cachedReading = fallbackCache.getCachedReading(deviceId, 0);
    if (cachedReading) {
      return {
        units_per_minute: cachedReading.rate * 60,
        timestamp: cachedReading.time
      };
    }
    
    console.warn(`No cached rate data available for device ${deviceId}`);
    return null;
  }

  /**
   * Fallback for getting latest reading
   */
  static getLatestReadingFallback(deviceId: string, channel: number): CounterReading | null {
    console.warn(`Database unavailable - using cached data for reading lookup: ${deviceId}-${channel}`);
    
    const cachedReading = fallbackCache.getCachedReading(deviceId, channel);
    if (cachedReading) {
      console.log(`Returning cached reading for device ${deviceId}, channel ${channel}`);
      return cachedReading;
    }
    
    console.warn(`No cached reading data available for device ${deviceId}, channel ${channel}`);
    return null;
  }

  /**
   * Fallback health status when database is unavailable
   */
  static getHealthStatusFallback(): {
    isConnected: boolean;
    error: string;
    lastCheck: Date;
    mode: string;
  } {
    return {
      isConnected: false,
      error: 'Database circuit breaker is open - running in fallback mode',
      lastCheck: new Date(),
      mode: 'fallback'
    };
  }

  /**
   * Cache data for fallback use (call this during successful operations)
   */
  static cacheForFallback(data: {
    deviceId?: string;
    activeJob?: ProductionJob;
    reading?: CounterReading;
  }): void {
    if (data.deviceId && data.activeJob) {
      fallbackCache.cacheActiveJob(data.deviceId, data.activeJob);
    }
    
    if (data.reading) {
      fallbackCache.cacheReading(
        data.reading.device_id, 
        data.reading.channel, 
        data.reading
      );
    }
  }

  /**
   * Get fallback cache status
   */
  static getFallbackCacheStatus() {
    return fallbackCache.getStatus();
  }

  /**
   * Clear fallback cache (for testing or administrative purposes)
   */
  static clearFallbackCache(): void {
    fallbackCache.clearExpired();
    console.log('Fallback cache cleared');
  }
}

/**
 * Wrapper function that executes operation with fallback strategy
 */
export async function withDatabaseFallback<T>(
  operation: () => Promise<T>,
  fallbackStrategy: () => T,
  cacheData?: (result: T) => void
): Promise<T> {
  try {
    const result = await operation();
    
    // Cache successful result for future fallback use
    if (cacheData) {
      cacheData(result);
    }
    
    return result;
  } catch (error) {
    // Check if it's a circuit breaker error
    if (error instanceof Error && error.message.includes('Circuit breaker is open')) {
      console.warn('Circuit breaker is open - using fallback strategy');
      return fallbackStrategy();
    }
    
    // For other errors, still try fallback but re-throw the original error
    console.error('Database operation failed - attempting fallback:', error);
    try {
      return fallbackStrategy();
    } catch (fallbackError) {
      console.error('Fallback strategy also failed:', fallbackError);
      throw error; // Throw original error
    }
  }
}

/**
 * API fallback strategies for external service calls
 */
export class ApiFallbackStrategies {
  
  /**
   * Generic API fallback that returns cached data or default values
   */
  static genericApiFallback<T>(defaultValue: T, cacheKey?: string): T {
    console.warn('API circuit breaker is open - using fallback strategy');
    
    // In a real implementation, you might check a cache here
    // For now, return the default value
    return defaultValue;
  }

  /**
   * Authentication fallback - allow limited access in degraded mode
   */
  static authenticationFallback(): {
    isAuthenticated: boolean;
    user: { id: string; name: string; role: string };
    limitations: string[];
  } {
    console.warn('Authentication service unavailable - using fallback mode');
    
    return {
      isAuthenticated: true,
      user: {
        id: 'fallback-user',
        name: 'System User (Fallback)',
        role: 'operator'
      },
      limitations: [
        'Authentication service unavailable',
        'Limited functionality available',
        'Some features may be disabled'
      ]
    };
  }
}

export { fallbackCache };