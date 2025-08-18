-- 003-create-oee-context-tables.sql
-- Additional OEE context tables that layer business meaning on top of immutable counter data
-- CRITICAL: These tables provide context without modifying existing counter_data

-- Stoppage classifications table
-- Maps time periods to business reasons for production stoppages
CREATE TABLE IF NOT EXISTS stoppage_classifications (
    stoppage_id SERIAL PRIMARY KEY,
    device_id VARCHAR(20) NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    stoppage_reason VARCHAR(100) NOT NULL,
    stoppage_category VARCHAR(50) NOT NULL, -- Planned, Unplanned, Changeover, etc.
    notes TEXT,
    classified_by VARCHAR(50),
    classified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT stoppage_classifications_times_valid CHECK (
        end_time IS NULL OR end_time > start_time
    ),
    CONSTRAINT stoppage_classifications_category_valid CHECK (
        stoppage_category IN ('Planned', 'Unplanned', 'Changeover', 'Maintenance', 'Break', 'Material', 'Quality')
    )
);

-- OEE calculation cache table
-- Stores pre-calculated OEE metrics to improve performance
CREATE TABLE IF NOT EXISTS oee_calculations_cache (
    calculation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id VARCHAR(20) NOT NULL,
    work_order_id VARCHAR(50),
    calculation_period_start TIMESTAMPTZ NOT NULL,
    calculation_period_end TIMESTAMPTZ NOT NULL,
    availability_percent DECIMAL(5,2) NOT NULL,
    performance_percent DECIMAL(5,2) NOT NULL,
    quality_percent DECIMAL(5,2) NOT NULL,
    oee_percent DECIMAL(5,2) NOT NULL,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    
    -- Constraints
    CONSTRAINT oee_cache_periods_valid CHECK (calculation_period_end > calculation_period_start),
    CONSTRAINT oee_cache_percentages_valid CHECK (
        availability_percent >= 0 AND availability_percent <= 100 AND
        performance_percent >= 0 AND performance_percent <= 100 AND
        quality_percent >= 0 AND quality_percent <= 100 AND
        oee_percent >= 0 AND oee_percent <= 100
    ),
    CONSTRAINT oee_cache_expiry_valid CHECK (expires_at > calculated_at)
);

-- Device configuration table
-- Stores device-specific settings for OEE calculations
CREATE TABLE IF NOT EXISTS device_configurations (
    device_id VARCHAR(20) PRIMARY KEY,
    device_name VARCHAR(100) NOT NULL,
    production_channel INTEGER NOT NULL DEFAULT 0,
    reject_channel INTEGER NOT NULL DEFAULT 1,
    target_rate_per_minute DECIMAL(10,2),
    minimum_stoppage_minutes INTEGER NOT NULL DEFAULT 5,
    quality_threshold_percent DECIMAL(5,2) NOT NULL DEFAULT 95.0,
    availability_threshold_percent DECIMAL(5,2) NOT NULL DEFAULT 85.0,
    performance_threshold_percent DECIMAL(5,2) NOT NULL DEFAULT 80.0,
    timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT device_config_channels_different CHECK (production_channel != reject_channel),
    CONSTRAINT device_config_thresholds_valid CHECK (
        quality_threshold_percent >= 0 AND quality_threshold_percent <= 100 AND
        availability_threshold_percent >= 0 AND availability_threshold_percent <= 100 AND
        performance_threshold_percent >= 0 AND performance_threshold_percent <= 100
    ),
    CONSTRAINT device_config_stoppage_minutes_positive CHECK (minimum_stoppage_minutes > 0)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_stoppage_classifications_device_time 
ON stoppage_classifications(device_id, start_time, end_time);

CREATE INDEX IF NOT EXISTS idx_stoppage_classifications_category 
ON stoppage_classifications(stoppage_category, classified_at);

CREATE INDEX IF NOT EXISTS idx_oee_cache_device_period 
ON oee_calculations_cache(device_id, calculation_period_start, calculation_period_end);

CREATE INDEX IF NOT EXISTS idx_oee_cache_expires_at 
ON oee_calculations_cache(expires_at) 
WHERE expires_at > NOW();

CREATE INDEX IF NOT EXISTS idx_device_configurations_active 
ON device_configurations(is_active) 
WHERE is_active = true;

-- Add comments explaining relationships to counter_data
COMMENT ON TABLE stoppage_classifications IS 
'Business context for periods where counter_data shows zero or low production rates';

COMMENT ON TABLE oee_calculations_cache IS 
'Performance optimization: cached OEE calculations derived from counter_data';

COMMENT ON TABLE device_configurations IS 
'Device-specific settings that control how counter_data is interpreted for OEE calculations';

COMMENT ON COLUMN device_configurations.production_channel IS 
'Channel number in counter_data that represents good production (typically 0)';

COMMENT ON COLUMN device_configurations.reject_channel IS 
'Channel number in counter_data that represents rejects/scrap (typically 1)';

-- Insert default device configuration if none exists
INSERT INTO device_configurations (
    device_id, 
    device_name, 
    production_channel, 
    reject_channel,
    target_rate_per_minute,
    minimum_stoppage_minutes,
    quality_threshold_percent,
    availability_threshold_percent,
    performance_threshold_percent
) VALUES (
    'DEFAULT', 
    'Default Device Configuration', 
    0, 
    1,
    60.0,  -- 1 piece per second = 60 per minute
    5,     -- 5 minute minimum stoppage
    95.0,  -- 95% quality threshold
    85.0,  -- 85% availability threshold
    80.0   -- 80% performance threshold
) ON CONFLICT (device_id) DO NOTHING;