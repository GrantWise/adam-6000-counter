import { getPerformanceMonitoring } from './performanceMonitoringService'

/**
 * Query Cache Service for Phase 4
 * Strategic caching for expensive OEE calculations and aggregates
 * 
 * Design principle: Only cache what provides clear value
 * Focus on:
 * - OEE calculations (expensive to compute)
 * - Current production rates (frequently requested)
 * - Active job lookups (high frequency, low change rate)
 */
export class QueryCacheService {
  private static instance: QueryCacheService | null = null
  
  // Cache storage with TTL
  private cache = new Map<string, CacheEntry>()
  private readonly performanceMonitoring = getPerformanceMonitoring()
  
  // Cache configuration
  private readonly defaultTtl = 30000 // 30 seconds
  private readonly maxCacheSize = 1000 // Prevent memory bloat
  private readonly oeeCalculationTtl = 60000 // 1 minute for OEE calculations
  private readonly activeJobTtl = 300000 // 5 minutes for active jobs
  private readonly currentRateTtl = 15000 // 15 seconds for current rates
  
  private constructor() {
    // Clean expired entries every minute
    setInterval(() => this.cleanExpiredEntries(), 60000)
  }
  
  /**
   * Get singleton instance
   */
  static getInstance(): QueryCacheService {
    if (!QueryCacheService.instance) {
      QueryCacheService.instance = new QueryCacheService()
    }
    return QueryCacheService.instance
  }
  
  /**
   * Get cached value or execute operation and cache result
   */
  async getOrSet<T>(
    key: string,
    operation: () => Promise<T>,
    ttl?: number
  ): Promise<T> {
    // Check cache first
    const cached = this.get<T>(key)
    if (cached !== null) {
      this.performanceMonitoring.recordCacheHit()
      return cached
    }
    
    // Cache miss - execute operation and cache result
    this.performanceMonitoring.recordCacheMiss()
    const result = await operation()
    this.set(key, result, ttl)
    return result
  }
  
  /**
   * Get value from cache
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key)
    if (!entry) {
      return null
    }
    
    // Check if expired
    if (Date.now() > entry.expiresAt) {
      this.cache.delete(key)
      return null
    }
    
    return entry.value as T
  }
  
  /**
   * Set value in cache
   */
  set<T>(key: string, value: T, ttl?: number): void {
    // Prevent memory bloat
    if (this.cache.size >= this.maxCacheSize) {
      this.evictOldest()
    }
    
    const actualTtl = ttl || this.defaultTtl
    const entry: CacheEntry = {
      value,
      createdAt: Date.now(),
      expiresAt: Date.now() + actualTtl
    }
    
    this.cache.set(key, entry)
  }
  
  /**
   * Cache OEE calculation result
   */
  async cacheOeeCalculation<T>(
    deviceId: string,
    operation: () => Promise<T>
  ): Promise<T> {
    const key = `oee:${deviceId}:current`
    return this.getOrSet(key, operation, this.oeeCalculationTtl)
  }
  
  /**
   * Cache active job lookup
   */
  async cacheActiveJob<T>(
    deviceId: string,
    operation: () => Promise<T>
  ): Promise<T> {
    const key = `job:active:${deviceId}`
    return this.getOrSet(key, operation, this.activeJobTtl)
  }
  
  /**
   * Cache current production rate
   */
  async cacheCurrentRate<T>(
    deviceId: string,
    operation: () => Promise<T>
  ): Promise<T> {
    const key = `rate:current:${deviceId}`
    return this.getOrSet(key, operation, this.currentRateTtl)
  }
  
  /**
   * Cache aggregate calculation (hourly/daily summaries)
   */
  async cacheAggregate<T>(
    aggregateKey: string,
    operation: () => Promise<T>,
    ttl = 300000 // 5 minutes default for aggregates
  ): Promise<T> {
    const key = `aggregate:${aggregateKey}`
    return this.getOrSet(key, operation, ttl)
  }
  
  /**
   * Invalidate cache for a device (when job changes, etc.)
   */
  invalidateDevice(deviceId: string): void {
    const keysToDelete: string[] = []
    
    for (const key of this.cache.keys()) {
      if (key.includes(deviceId)) {
        keysToDelete.push(key)
      }
    }
    
    keysToDelete.forEach(key => this.cache.delete(key))
    console.log(`Invalidated ${keysToDelete.length} cache entries for device ${deviceId}`)
  }
  
  /**
   * Invalidate specific cache pattern
   */
  invalidatePattern(pattern: string): void {
    const keysToDelete: string[] = []
    
    for (const key of this.cache.keys()) {
      if (key.includes(pattern)) {
        keysToDelete.push(key)
      }
    }
    
    keysToDelete.forEach(key => this.cache.delete(key))
    console.log(`Invalidated ${keysToDelete.length} cache entries matching pattern: ${pattern}`)
  }
  
  /**
   * Get cache statistics
   */
  getStats(): CacheStats {
    const now = Date.now()
    let expiredCount = 0
    let validCount = 0
    
    for (const entry of this.cache.values()) {
      if (now > entry.expiresAt) {
        expiredCount++
      } else {
        validCount++
      }
    }
    
    return {
      totalEntries: this.cache.size,
      validEntries: validCount,
      expiredEntries: expiredCount,
      memoryUsage: this.estimateMemoryUsage()
    }
  }
  
  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear()
    console.log('Cache cleared')
  }
  
  /**
   * Clean expired entries
   */
  private cleanExpiredEntries(): void {
    const now = Date.now()
    const keysToDelete: string[] = []
    
    for (const [key, entry] of this.cache.entries()) {
      if (now > entry.expiresAt) {
        keysToDelete.push(key)
      }
    }
    
    keysToDelete.forEach(key => this.cache.delete(key))
    
    if (keysToDelete.length > 0) {
      console.log(`Cleaned ${keysToDelete.length} expired cache entries`)
    }
  }
  
  /**
   * Evict oldest entries when cache is full
   */
  private evictOldest(): void {
    let oldestKey: string | null = null
    let oldestTime = Date.now()
    
    for (const [key, entry] of this.cache.entries()) {
      if (entry.createdAt < oldestTime) {
        oldestTime = entry.createdAt
        oldestKey = key
      }
    }
    
    if (oldestKey) {
      this.cache.delete(oldestKey)
    }
  }
  
  /**
   * Estimate memory usage (rough calculation)
   */
  private estimateMemoryUsage(): number {
    let totalSize = 0
    
    for (const [key, entry] of this.cache.entries()) {
      // Rough estimation: key length + JSON string length + metadata
      totalSize += key.length * 2 // Unicode characters
      totalSize += JSON.stringify(entry.value).length * 2
      totalSize += 32 // Metadata overhead
    }
    
    return totalSize
  }
}

/**
 * Cache entry interface
 */
interface CacheEntry {
  value: any
  createdAt: number
  expiresAt: number
}

/**
 * Cache statistics interface
 */
interface CacheStats {
  totalEntries: number
  validEntries: number
  expiredEntries: number
  memoryUsage: number // Bytes
}

/**
 * Global helper functions for easy integration
 */

/**
 * Get global cache instance
 */
export function getQueryCache(): QueryCacheService {
  return QueryCacheService.getInstance()
}

/**
 * Helper for caching OEE calculations
 */
export async function withOeeCache<T>(
  deviceId: string,
  operation: () => Promise<T>
): Promise<T> {
  return getQueryCache().cacheOeeCalculation(deviceId, operation)
}

/**
 * Helper for caching active job lookups
 */
export async function withActiveJobCache<T>(
  deviceId: string,
  operation: () => Promise<T>
): Promise<T> {
  return getQueryCache().cacheActiveJob(deviceId, operation)
}

/**
 * Helper for caching current rate queries
 */
export async function withCurrentRateCache<T>(
  deviceId: string,
  operation: () => Promise<T>
): Promise<T> {
  return getQueryCache().cacheCurrentRate(deviceId, operation)
}

/**
 * Helper for cache invalidation on job changes
 */
export function invalidateDeviceCache(deviceId: string): void {
  getQueryCache().invalidateDevice(deviceId)
}