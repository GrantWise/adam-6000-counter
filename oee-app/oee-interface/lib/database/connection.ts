import { Pool, PoolClient } from 'pg';
import { CircuitBreaker, ErrorHandler } from '@/lib/utils/errorHandler';
import { DatabaseFallbackStrategies } from './fallbackStrategies';
import { timeQueryExecution } from '@/lib/services/performanceMonitoringService';

// Database connection pool configuration
// Use DATABASE_URL if provided, otherwise use individual parameters
const poolConfig = process.env.DATABASE_URL 
  ? {
      connectionString: process.env.DATABASE_URL,
      max: 20,
      idleTimeoutMillis: 30000,
      connectionTimeoutMillis: 2000,
    }
  : {
      host: process.env.TIMESCALEDB_HOST,
      port: parseInt(process.env.TIMESCALEDB_PORT || '5433'),
      database: process.env.TIMESCALEDB_DATABASE,
      user: process.env.TIMESCALEDB_USER,
      password: process.env.TIMESCALEDB_PASSWORD,
      max: 20,
      idleTimeoutMillis: 30000,
      connectionTimeoutMillis: 2000,
    };

// Validate required environment variables
if (!process.env.DATABASE_URL && 
    (!process.env.TIMESCALEDB_HOST || 
     !process.env.TIMESCALEDB_DATABASE || 
     !process.env.TIMESCALEDB_USER || 
     !process.env.TIMESCALEDB_PASSWORD)) {
  throw new Error(
    'Database configuration missing. Please set DATABASE_URL or individual TIMESCALEDB_* environment variables.'
  );
}

const pool = new Pool(poolConfig);

// Circuit breaker for database operations
// Configured for industrial environment: 5 failures over 30 seconds opens breaker
const dbCircuitBreaker = new CircuitBreaker(5, 30000);

/**
 * Generic database query helper function with circuit breaker protection
 * @param query SQL query string with parameter placeholders ($1, $2, etc.)
 * @param params Array of parameters to bind to the query
 * @returns Promise resolving to array of query result rows
 */
export async function queryDatabase<T = any>(
  query: string, 
  params: any[] = []
): Promise<T[]> {
  return timeQueryExecution(async () => {
    return dbCircuitBreaker.execute(async () => {
      const client: PoolClient = await pool.connect();
      try {
        const result = await client.query(query, params);
        return result.rows;
      } catch (error) {
        console.error('Database query error:', {
          query: query.replace(/\s+/g, ' ').trim(),
          params,
          error: error instanceof Error ? error.message : 'Unknown error'
        });
        
        // Let circuit breaker handle the error by re-throwing
        const appError = ErrorHandler.handleDatabaseError(error);
        throw new Error(appError.message);
      } finally {
        client.release();
      }
    });
  });
}

/**
 * Execute a query and return a single row or null
 * @param query SQL query string
 * @param params Array of parameters to bind to the query
 * @returns Promise resolving to single row or null
 */
export async function queryDatabaseSingle<T = any>(
  query: string, 
  params: any[] = []
): Promise<T | null> {
  const results = await queryDatabase<T>(query, params);
  return results.length > 0 ? results[0] : null;
}

/**
 * Execute a query with transaction support and circuit breaker protection
 * @param queries Array of query objects with query and params
 * @returns Promise resolving to array of results
 */
export async function queryDatabaseTransaction<T = any>(
  queries: Array<{ query: string; params?: any[] }>
): Promise<T[][]> {
  if (!queries || queries.length === 0) {
    throw new Error('Transaction requires at least one query');
  }

  return dbCircuitBreaker.execute(async () => {
    const client: PoolClient = await pool.connect();
    let transactionStarted = false;
    
    try {
      await client.query('BEGIN');
      transactionStarted = true;
      
      const results: T[][] = [];
      for (let i = 0; i < queries.length; i++) {
        const { query, params = [] } = queries[i];
        try {
          const result = await client.query(query, params);
          results.push(result.rows);
        } catch (queryError) {
          console.error(`Transaction query ${i + 1} failed:`, {
            query: query.replace(/\s+/g, ' ').trim(),
            params,
            error: queryError instanceof Error ? queryError.message : 'Unknown error'
          });
          throw queryError;
        }
      }
      
      await client.query('COMMIT');
      transactionStarted = false;
      return results;
      
    } catch (error) {
      if (transactionStarted) {
        try {
          await client.query('ROLLBACK');
        } catch (rollbackError) {
          console.error('Failed to rollback transaction:', rollbackError);
          // Still throw the original error
        }
      }
      
      console.error('Database transaction failed:', {
        error: error instanceof Error ? error.message : 'Unknown error',
        queryCount: queries.length
      });
      
      // Let circuit breaker handle the error
      const appError = ErrorHandler.handleDatabaseError(error);
      throw new Error(appError.message);
    } finally {
      client.release();
    }
  });
}

/**
 * Test database connection and return status with fallback information
 * @returns Promise resolving to connection status object
 */
export async function testDatabaseConnection(): Promise<{
  isConnected: boolean;
  latencyMs?: number;
  error?: string;
  fallbackAvailable?: boolean;
  circuitBreakerState?: string;
}> {
  const startTime = Date.now();
  const circuitBreakerState = dbCircuitBreaker.getState();
  
  try {
    await queryDatabase('SELECT 1 as test');
    const latencyMs = Date.now() - startTime;
    return { 
      isConnected: true, 
      latencyMs,
      fallbackAvailable: true,
      circuitBreakerState
    };
  } catch (error) {
    const isCircuitBreakerOpen = error instanceof Error && 
      error.message.includes('Circuit breaker is open');
    
    return { 
      isConnected: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
      fallbackAvailable: isCircuitBreakerOpen,
      circuitBreakerState
    };
  }
}

/**
 * Close the database connection pool
 * Used for graceful shutdown
 */
export async function closeDatabaseConnection(): Promise<void> {
  await pool.end();
}

/**
 * Get circuit breaker status for monitoring
 * @returns Current circuit breaker state and metrics
 */
export function getDatabaseCircuitBreakerStatus(): {
  state: string;
  isHealthy: boolean;
} {
  const state = dbCircuitBreaker.getState();
  return {
    state,
    isHealthy: state === 'closed'
  };
}

/**
 * Reset the database circuit breaker (for administrative purposes)
 */
export function resetDatabaseCircuitBreaker(): void {
  dbCircuitBreaker.reset();
  console.log('Database circuit breaker has been reset');
}

export { pool };