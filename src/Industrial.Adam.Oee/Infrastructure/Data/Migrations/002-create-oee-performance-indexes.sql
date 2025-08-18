-- 002-create-oee-performance-indexes.sql
-- Performance indexes for OEE queries on existing counter_data table
-- CRITICAL: These are READ-ONLY indexes on existing data, no schema modifications

-- Index for device + timestamp queries (most common OEE query pattern)
-- This optimizes queries like "get all data for device X in time range Y-Z"
CREATE INDEX IF NOT EXISTS idx_counter_data_device_timestamp_desc 
ON counter_data(device_id, timestamp DESC)
WHERE channel IN (0, 1);  -- Only production and reject channels for OEE

-- Index for device + channel + timestamp queries
-- Optimizes queries for specific channel data (production vs rejects)
CREATE INDEX IF NOT EXISTS idx_counter_data_device_channel_timestamp 
ON counter_data(device_id, channel, timestamp DESC)
WHERE channel IN (0, 1);

-- Index for timestamp range queries across all devices
-- Useful for system-wide OEE reporting
CREATE INDEX IF NOT EXISTS idx_counter_data_timestamp_device 
ON counter_data(timestamp DESC, device_id)
WHERE channel IN (0, 1);

-- Index for latest reading queries per device/channel
-- Optimizes "get current rate" type queries
CREATE INDEX IF NOT EXISTS idx_counter_data_latest_by_device_channel 
ON counter_data(device_id, channel, timestamp DESC, rate)
WHERE channel IN (0, 1) AND rate IS NOT NULL;

-- Index for quality/availability calculations
-- Optimizes queries that need to identify gaps in data
CREATE INDEX IF NOT EXISTS idx_counter_data_quality_analysis 
ON counter_data(device_id, timestamp, quality)
WHERE channel IN (0, 1) AND quality IS NOT NULL;

-- Partial index for active production periods
-- Optimizes queries looking for non-zero production rates
CREATE INDEX IF NOT EXISTS idx_counter_data_active_production 
ON counter_data(device_id, timestamp DESC, rate)
WHERE channel = 0 AND rate > 0;

-- Add comments explaining the purpose of these indexes
COMMENT ON INDEX idx_counter_data_device_timestamp_desc IS 
'Primary OEE query optimization: device + time range queries for OEE calculations';

COMMENT ON INDEX idx_counter_data_device_channel_timestamp IS 
'Channel-specific queries: separate production (0) vs reject (1) data access';

COMMENT ON INDEX idx_counter_data_latest_by_device_channel IS 
'Current rate calculations: quickly find latest readings per device/channel';

COMMENT ON INDEX idx_counter_data_active_production IS 
'Availability calculations: identify periods of active production (rate > 0)';

-- Note: These indexes complement but do not replace TimescaleDB's built-in time partitioning
-- They specifically optimize OEE calculation patterns while maintaining read-only access