-- Performance indexes for OEE application queries
-- Based on existing counter_data table structure from the Industrial Counter Platform

-- Index for counter_data queries by device and time (most common OEE query pattern)
CREATE INDEX IF NOT EXISTS idx_counter_data_device_time 
  ON counter_data (device_id, time DESC);

-- Index for counter_data queries by channel and time for specific device
CREATE INDEX IF NOT EXISTS idx_counter_data_device_channel_time 
  ON counter_data (device_id, channel, time DESC);

-- Index for counter_data rate queries (for performance calculations)
CREATE INDEX IF NOT EXISTS idx_counter_data_rate_nonzero 
  ON counter_data (device_id, time DESC) 
  WHERE rate > 0;

-- Index for active production jobs (most frequently queried)
CREATE INDEX IF NOT EXISTS idx_production_jobs_active 
  ON production_jobs (device_id, status, start_time DESC) 
  WHERE status = 'active';

-- Index for production jobs by status and time
CREATE INDEX IF NOT EXISTS idx_production_jobs_status_time
  ON production_jobs (status, start_time DESC, end_time DESC);

-- Index for unclassified stoppages (frequently queried for operator dashboard)
CREATE INDEX IF NOT EXISTS idx_stoppage_events_unclassified
  ON stoppage_events (device_id, status, start_time DESC)
  WHERE status = 'unclassified';

-- Index for stoppage events by job_id (for job-specific analysis)
CREATE INDEX IF NOT EXISTS idx_stoppage_events_job_time
  ON stoppage_events (job_id, start_time DESC, end_time DESC);

-- Composite index for OEE calculations (device, time range queries)
CREATE INDEX IF NOT EXISTS idx_counter_data_oee_calc
  ON counter_data (device_id, time DESC, channel, rate, processed_value, quality)
  WHERE channel IN (0, 1); -- Production (0) and reject (1) channels

-- Index for recent data queries (last hour, commonly used in OEE)
CREATE INDEX IF NOT EXISTS idx_counter_data_recent
  ON counter_data (device_id, channel, time DESC)
  WHERE time >= NOW() - INTERVAL '1 hour';

-- Partial index for stoppage detection (zero rate periods)
CREATE INDEX IF NOT EXISTS idx_counter_data_zero_rates
  ON counter_data (device_id, time DESC)
  WHERE channel = 0 AND rate = 0;

-- Index for quality calculations (processed_value tracking)
CREATE INDEX IF NOT EXISTS idx_counter_data_production_values
  ON counter_data (device_id, time ASC, processed_value)
  WHERE channel = 0;

-- Performance statistics (optional - for query optimization analysis)
-- Run these after indexes are created to update table statistics
-- ANALYZE counter_data;
-- ANALYZE production_jobs;
-- ANALYZE stoppage_events;

-- Comments for index usage
COMMENT ON INDEX idx_counter_data_device_time IS 'Primary index for OEE time-series queries';
COMMENT ON INDEX idx_counter_data_device_channel_time IS 'Channel-specific queries for production vs reject data';
COMMENT ON INDEX idx_production_jobs_active IS 'Fast lookup of currently active jobs';
COMMENT ON INDEX idx_stoppage_events_unclassified IS 'Quick access to stoppages needing operator classification';