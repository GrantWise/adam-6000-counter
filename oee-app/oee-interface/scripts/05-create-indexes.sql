-- Performance indexes for OEE application
-- Run this script after tables are created to improve query performance

-- Indexes for production_jobs table
CREATE INDEX IF NOT EXISTS idx_production_jobs_device_status 
ON production_jobs(device_id, status);

CREATE INDEX IF NOT EXISTS idx_production_jobs_device_start_time 
ON production_jobs(device_id, start_time DESC);

CREATE INDEX IF NOT EXISTS idx_production_jobs_status 
ON production_jobs(status);

CREATE INDEX IF NOT EXISTS idx_production_jobs_job_number 
ON production_jobs(UPPER(job_number));

-- Composite index for most common query (active job by device)
CREATE INDEX IF NOT EXISTS idx_production_jobs_active_lookup 
ON production_jobs(device_id, status, start_time DESC) 
WHERE status = 'active';

-- Indexes for counter_data table (most critical for performance)
CREATE INDEX IF NOT EXISTS idx_counter_data_device_channel_time 
ON counter_data(device_id, channel, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_counter_data_device_time_range 
ON counter_data(device_id, timestamp) 
WHERE timestamp > NOW() - INTERVAL '24 hours';

-- Composite index for OEE calculations (production channel)
CREATE INDEX IF NOT EXISTS idx_counter_data_oee_calculation 
ON counter_data(device_id, channel, timestamp DESC) 
WHERE channel IN (0, 1);

-- Index for rate queries (last N minutes)
CREATE INDEX IF NOT EXISTS idx_counter_data_recent_rates 
ON counter_data(device_id, channel, timestamp DESC, rate) 
WHERE timestamp > NOW() - INTERVAL '10 minutes';

-- Indexes for stoppage_events table
CREATE INDEX IF NOT EXISTS idx_stoppage_events_device_status 
ON stoppage_events(device_id, status);

CREATE INDEX IF NOT EXISTS idx_stoppage_events_device_time 
ON stoppage_events(device_id, start_time DESC);

CREATE INDEX IF NOT EXISTS idx_stoppage_events_job_id 
ON stoppage_events(job_id);

CREATE INDEX IF NOT EXISTS idx_stoppage_events_unclassified 
ON stoppage_events(device_id, start_time DESC) 
WHERE status = 'unclassified';

-- Indexes for time-range queries
CREATE INDEX IF NOT EXISTS idx_stoppage_events_time_range 
ON stoppage_events(device_id, start_time, end_time) 
WHERE end_time IS NOT NULL;

-- Performance monitoring indexes
CREATE INDEX IF NOT EXISTS idx_counter_data_timestamp_btree 
ON counter_data USING btree(timestamp);

-- Partial indexes for common queries
CREATE INDEX IF NOT EXISTS idx_production_jobs_recent 
ON production_jobs(device_id, start_time DESC) 
WHERE start_time > NOW() - INTERVAL '7 days';

CREATE INDEX IF NOT EXISTS idx_stoppage_events_recent 
ON stoppage_events(device_id, start_time DESC) 
WHERE start_time > NOW() - INTERVAL '7 days';

-- Analyze tables for better query planning
ANALYZE production_jobs;
ANALYZE counter_data;
ANALYZE stoppage_events;

-- Create covering indexes for common SELECT patterns
CREATE INDEX IF NOT EXISTS idx_production_jobs_covering 
ON production_jobs(device_id, status) 
INCLUDE (job_id, job_number, part_number, target_rate, start_time, operator_id);

-- Index for job validation queries
CREATE INDEX IF NOT EXISTS idx_production_jobs_duplicate_check 
ON production_jobs(UPPER(job_number), status, end_time);

-- Index for historical rate queries
CREATE INDEX IF NOT EXISTS idx_counter_data_historical 
ON counter_data(device_id, channel, timestamp) 
INCLUDE (rate, processed_value, quality);

COMMIT;