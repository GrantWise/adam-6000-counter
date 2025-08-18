import { z } from 'zod'

/**
 * API Validation Schemas using Zod
 * Provides type-safe validation for all API endpoints
 */

// Base schemas for common types
export const deviceIdSchema = z.string()
  .min(1, 'Device ID is required')
  .max(50, 'Device ID must be 50 characters or less')
  .regex(/^[A-Za-z0-9\-_]+$/, 'Device ID can only contain letters, numbers, hyphens, and underscores');

export const jobNumberSchema = z.string()
  .min(1, 'Job number is required')
  .max(50, 'Job number must be 50 characters or less')
  .regex(/^[A-Za-z0-9\-_]+$/, 'Job number can only contain letters, numbers, hyphens, and underscores')
  .transform(s => s.trim());

export const partNumberSchema = z.string()
  .min(1, 'Part number is required')
  .max(100, 'Part number must be 100 characters or less')
  .transform(s => s.trim());

export const operatorIdSchema = z.string()
  .max(50, 'Operator ID must be 50 characters or less')
  .optional()
  .transform(s => s?.trim());

export const targetRateSchema = z.number()
  .positive('Target rate must be greater than 0')
  .max(10000, 'Target rate must be less than 10,000 units/minute')
  .finite('Target rate must be a valid number');

// Job API schemas
export const startJobSchema = z.object({
  jobNumber: jobNumberSchema,
  partNumber: partNumberSchema,
  deviceId: deviceIdSchema,
  targetRate: targetRateSchema,
  operatorId: operatorIdSchema,
});

export const endJobSchema = z.object({
  deviceId: deviceIdSchema,
});

export const getJobSchema = z.object({
  deviceId: deviceIdSchema,
});

// Metrics API schemas
export const getCurrentMetricsSchema = z.object({
  deviceId: deviceIdSchema.optional(),
});

export const getHistoricalMetricsSchema = z.object({
  deviceId: deviceIdSchema.optional(),
  hours: z.number()
    .int('Hours must be a whole number')
    .min(1, 'Hours must be at least 1')
    .max(168, 'Hours cannot exceed 1 week (168 hours)')
    .default(8),
  startTime: z.string().datetime().optional(),
  endTime: z.string().datetime().optional(),
});

// Stoppage API schemas
export const getStoppagesSchema = z.object({
  deviceId: deviceIdSchema.optional(),
});

export const classifyStoppageSchema = z.object({
  category: z.string()
    .min(1, 'Category is required')
    .max(50, 'Category must be 50 characters or less'),
  subCategory: z.string()
    .min(1, 'Sub-category is required')
    .max(50, 'Sub-category must be 50 characters or less'),
  comments: z.string()
    .max(500, 'Comments must be 500 characters or less')
    .optional()
    .transform(s => s?.trim() || null),
  deviceId: deviceIdSchema,
  operatorId: z.string()
    .min(1, 'Operator ID is required for classification')
    .max(50, 'Operator ID must be 50 characters or less'),
});

// Health check schema
export const healthCheckSchema = z.object({
  // No parameters needed for health check
});

// Authentication schemas
export const loginSchema = z.object({
  username: z.string()
    .min(1, 'Username is required')
    .max(50, 'Username must be 50 characters or less'),
  password: z.string()
    .min(1, 'Password is required')
    .max(100, 'Password is too long'),
});

// Query parameter schemas
export const paginationSchema = z.object({
  page: z.number()
    .int('Page must be a whole number')
    .min(1, 'Page must be at least 1')
    .default(1),
  limit: z.number()
    .int('Limit must be a whole number')
    .min(1, 'Limit must be at least 1')
    .max(100, 'Limit cannot exceed 100')
    .default(50),
});

export const timeRangeSchema = z.object({
  startTime: z.string()
    .datetime('Start time must be a valid ISO 8601 datetime'),
  endTime: z.string()
    .datetime('End time must be a valid ISO 8601 datetime'),
}).refine(
  data => new Date(data.startTime) < new Date(data.endTime),
  {
    message: 'Start time must be before end time',
    path: ['endTime'],
  }
);

// Response validation schemas (for type safety)
export const operationResultSchema = <T extends z.ZodType>(valueSchema: T) =>
  z.object({
    isSuccess: z.boolean(),
    value: valueSchema,
    errorMessage: z.string().nullable(),
  });

export const productionJobSchema = z.object({
  job_id: z.number(),
  job_number: z.string(),
  part_number: z.string(),
  device_id: z.string(),
  target_rate: z.number(),
  start_time: z.string().or(z.date()),
  end_time: z.string().or(z.date()).nullable().optional(),
  operator_id: z.string().nullable().optional(),
  status: z.enum(['active', 'completed', 'cancelled']),
});

export const currentMetricsResponseSchema = z.object({
  currentRate: z.number(),
  targetRate: z.number(),
  performancePercent: z.number(),
  qualityPercent: z.number(),
  availabilityPercent: z.number(),
  oeePercent: z.number(),
  status: z.enum(['running', 'stopped', 'error']),
  lastUpdate: z.string(),
});

// Type exports for use in API handlers
export type StartJobRequest = z.infer<typeof startJobSchema>;
export type EndJobRequest = z.infer<typeof endJobSchema>;
export type GetJobRequest = z.infer<typeof getJobSchema>;
export type GetCurrentMetricsRequest = z.infer<typeof getCurrentMetricsSchema>;
export type GetHistoricalMetricsRequest = z.infer<typeof getHistoricalMetricsSchema>;
export type ClassifyStoppageRequest = z.infer<typeof classifyStoppageSchema>;
export type LoginRequest = z.infer<typeof loginSchema>;
export type PaginationRequest = z.infer<typeof paginationSchema>;
export type TimeRangeRequest = z.infer<typeof timeRangeSchema>;

// Validation helper functions
export function validateBody<T>(schema: z.ZodSchema<T>) {
  return (data: unknown): T => {
    try {
      return schema.parse(data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        const messages = error.errors.map(err => 
          `${err.path.join('.')}: ${err.message}`
        ).join(', ');
        throw new Error(`Validation error: ${messages}`);
      }
      throw error;
    }
  };
}

export function validateQuery<T>(schema: z.ZodSchema<T>) {
  return (data: Record<string, string | string[] | undefined>): T => {
    // Convert query parameters to appropriate types
    const converted: any = {};
    
    for (const [key, value] of Object.entries(data)) {
      if (value === undefined) continue;
      
      if (Array.isArray(value)) {
        converted[key] = value;
      } else {
        // Try to convert to number if it looks like a number
        if (/^\d+(\.\d+)?$/.test(value)) {
          converted[key] = parseFloat(value);
        } else if (value === 'true') {
          converted[key] = true;
        } else if (value === 'false') {
          converted[key] = false;
        } else {
          converted[key] = value;
        }
      }
    }
    
    try {
      return schema.parse(converted);
    } catch (error) {
      if (error instanceof z.ZodError) {
        const messages = error.errors.map(err => 
          `${err.path.join('.')}: ${err.message}`
        ).join(', ');
        throw new Error(`Query validation error: ${messages}`);
      }
      throw error;
    }
  };
}