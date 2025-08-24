-- Admin Dashboard Module Database Schema
-- TimescaleDB Migration for System Health Monitoring, Alerts, and Logs

-- =============================================
-- System Health Metrics Table (21 CFR Part 11 Compliant)
-- =============================================
CREATE TABLE IF NOT EXISTS system_health_metrics (
    id SERIAL PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0, -- 0=Healthy, 1=Warning, 2=Critical, 3=Unknown
    response_time_ms INTEGER NOT NULL DEFAULT 0,
    error_count INTEGER NOT NULL DEFAULT 0,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    additional_data JSONB,
    
    -- 21 CFR Part 11 Audit Trail Fields
    created_by VARCHAR(100) NOT NULL DEFAULT 'system',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_by VARCHAR(100) NOT NULL DEFAULT 'system',
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    digital_signature VARCHAR(512), -- Cryptographic signature for data integrity
    version INTEGER NOT NULL DEFAULT 1,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_by VARCHAR(100),
    deleted_at TIMESTAMPTZ,
    modification_reason TEXT
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('system_health_metrics', 'recorded_at', 
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_service_name 
    ON system_health_metrics (service_name, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_status 
    ON system_health_metrics (status, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_recorded_at 
    ON system_health_metrics (recorded_at DESC);
-- 21 CFR Part 11 Audit Indexes
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_created_by 
    ON system_health_metrics (created_by);
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_modified_by 
    ON system_health_metrics (modified_by);
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_version 
    ON system_health_metrics (version);
CREATE INDEX IF NOT EXISTS idx_system_health_metrics_audit_trail 
    ON system_health_metrics (created_at, modified_at, is_deleted);

-- =============================================
-- System Alerts Table (21 CFR Part 11 Compliant)
-- =============================================
CREATE TABLE IF NOT EXISTS system_alerts (
    id SERIAL PRIMARY KEY,
    alert_type VARCHAR(50) NOT NULL,
    severity SMALLINT NOT NULL DEFAULT 0, -- 0=Info, 1=Warning, 2=Error, 3=Critical
    message TEXT NOT NULL,
    acknowledged BOOLEAN NOT NULL DEFAULT FALSE,
    acknowledged_by VARCHAR(100),
    acknowledged_at TIMESTAMPTZ,
    service_name VARCHAR(100),
    additional_data JSONB,
    
    -- 21 CFR Part 11 Audit Trail Fields
    created_by VARCHAR(100) NOT NULL DEFAULT 'system',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_by VARCHAR(100) NOT NULL DEFAULT 'system',
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    digital_signature VARCHAR(512), -- Cryptographic signature for data integrity
    version INTEGER NOT NULL DEFAULT 1,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_by VARCHAR(100),
    deleted_at TIMESTAMPTZ,
    modification_reason TEXT
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('system_alerts', 'created_at',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_system_alerts_acknowledged 
    ON system_alerts (acknowledged, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_alerts_severity 
    ON system_alerts (severity, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_alerts_service_name 
    ON system_alerts (service_name, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_alerts_created_at 
    ON system_alerts (created_at DESC);
-- 21 CFR Part 11 Audit Indexes
CREATE INDEX IF NOT EXISTS idx_system_alerts_created_by 
    ON system_alerts (created_by);
CREATE INDEX IF NOT EXISTS idx_system_alerts_modified_by 
    ON system_alerts (modified_by);
CREATE INDEX IF NOT EXISTS idx_system_alerts_version 
    ON system_alerts (version);
CREATE INDEX IF NOT EXISTS idx_system_alerts_audit_trail 
    ON system_alerts (created_at, modified_at, is_deleted);

-- =============================================
-- System Logs Table (21 CFR Part 11 Compliant)
-- =============================================
CREATE TABLE IF NOT EXISTS system_logs (
    id SERIAL PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL,
    log_level SMALLINT NOT NULL DEFAULT 2, -- 0=Trace, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical, 6=None
    message TEXT NOT NULL,
    exception TEXT,
    properties JSONB,
    correlation_id VARCHAR(100),
    user_context VARCHAR(100),
    
    -- 21 CFR Part 11 Audit Trail Fields
    created_by VARCHAR(100) NOT NULL DEFAULT 'system',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_by VARCHAR(100) NOT NULL DEFAULT 'system',
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    digital_signature VARCHAR(512), -- Cryptographic signature for data integrity
    version INTEGER NOT NULL DEFAULT 1,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_by VARCHAR(100),
    deleted_at TIMESTAMPTZ,
    modification_reason TEXT
);

-- Create hypertable for time-series optimization with partitioning
SELECT create_hypertable('system_logs', 'created_at',
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_system_logs_service_name 
    ON system_logs (service_name, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_logs_log_level 
    ON system_logs (log_level, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_logs_created_at 
    ON system_logs (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_logs_correlation_id 
    ON system_logs (correlation_id) WHERE correlation_id IS NOT NULL;

-- Full-text search index for log messages
CREATE INDEX IF NOT EXISTS idx_system_logs_message_fulltext 
    ON system_logs USING gin(to_tsvector('english', message));

-- 21 CFR Part 11 Audit Indexes
CREATE INDEX IF NOT EXISTS idx_system_logs_created_by 
    ON system_logs (created_by);
CREATE INDEX IF NOT EXISTS idx_system_logs_modified_by 
    ON system_logs (modified_by);
CREATE INDEX IF NOT EXISTS idx_system_logs_version 
    ON system_logs (version);
CREATE INDEX IF NOT EXISTS idx_system_logs_audit_trail 
    ON system_logs (created_at, modified_at, is_deleted);

-- =============================================
-- Retention Policies
-- =============================================

-- Keep system health metrics for 90 days
SELECT add_retention_policy('system_health_metrics', INTERVAL '90 days', if_not_exists => TRUE);

-- Keep system alerts for 1 year (compliance requirement)
SELECT add_retention_policy('system_alerts', INTERVAL '1 year', if_not_exists => TRUE);

-- Keep system logs for 30 days (adjust based on storage requirements)
SELECT add_retention_policy('system_logs', INTERVAL '30 days', if_not_exists => TRUE);

-- =============================================
-- Compression Policies (for older data)
-- =============================================

-- Compress system health metrics older than 7 days
SELECT add_compression_policy('system_health_metrics', INTERVAL '7 days', if_not_exists => TRUE);

-- Compress system alerts older than 30 days
SELECT add_compression_policy('system_alerts', INTERVAL '30 days', if_not_exists => TRUE);

-- Compress system logs older than 3 days
SELECT add_compression_policy('system_logs', INTERVAL '3 days', if_not_exists => TRUE);

-- =============================================
-- Sample Data (for development/testing)
-- =============================================

-- Insert sample health metrics
INSERT INTO system_health_metrics (service_name, status, response_time_ms, error_count, recorded_at) VALUES
    ('OEE-API', 0, 45, 0, NOW() - INTERVAL '1 minute'),
    ('Logger-API', 0, 32, 0, NOW() - INTERVAL '2 minutes'),
    ('Security-API', 1, 156, 1, NOW() - INTERVAL '3 minutes'),
    ('TimescaleDB', 0, 12, 0, NOW() - INTERVAL '30 seconds')
ON CONFLICT DO NOTHING;

-- Insert sample alerts
INSERT INTO system_alerts (alert_type, severity, message, service_name, created_at) VALUES
    ('HighResponseTime', 1, 'Security-API response time exceeded 150ms threshold', 'Security-API', NOW() - INTERVAL '3 minutes'),
    ('ServiceRestart', 0, 'Logger-API service restarted successfully', 'Logger-API', NOW() - INTERVAL '10 minutes')
ON CONFLICT DO NOTHING;

-- Insert sample logs
INSERT INTO system_logs (service_name, log_level, message, created_at) VALUES
    ('OEE-API', 2, 'Application started successfully', NOW() - INTERVAL '1 hour'),
    ('Logger-API', 2, 'Connected to TimescaleDB', NOW() - INTERVAL '50 minutes'),
    ('Security-API', 3, 'High response time detected: 156ms', NOW() - INTERVAL '3 minutes'),
    ('AdminDashboard-API', 2, 'Admin dashboard initialized', NOW())
ON CONFLICT DO NOTHING;

-- =============================================
-- Views for Common Queries
-- =============================================

-- Latest health status for each service
CREATE OR REPLACE VIEW latest_service_health AS
SELECT DISTINCT ON (service_name) 
    service_name,
    status,
    response_time_ms,
    error_count,
    recorded_at,
    additional_data
FROM system_health_metrics 
ORDER BY service_name, recorded_at DESC;

-- Active (unacknowledged) alerts
CREATE OR REPLACE VIEW active_alerts AS
SELECT id, alert_type, severity, message, service_name, created_at, additional_data
FROM system_alerts 
WHERE acknowledged = FALSE
ORDER BY severity DESC, created_at DESC;

-- Recent logs (last 24 hours)
CREATE OR REPLACE VIEW recent_logs AS
SELECT service_name, log_level, message, exception, created_at, correlation_id
FROM system_logs 
WHERE created_at >= NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC;

-- =============================================
-- Permissions and Security
-- =============================================

-- Grant permissions to the industrial_system user
GRANT SELECT, INSERT, UPDATE ON system_health_metrics TO industrial_system;
GRANT SELECT, INSERT, UPDATE ON system_alerts TO industrial_system;
GRANT SELECT, INSERT, DELETE ON system_logs TO industrial_system;

GRANT SELECT ON latest_service_health TO industrial_system;
GRANT SELECT ON active_alerts TO industrial_system;
GRANT SELECT ON recent_logs TO industrial_system;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO industrial_system;

-- Create a comment for documentation
COMMENT ON TABLE system_health_metrics IS 'Time-series data for system health monitoring';
COMMENT ON TABLE system_alerts IS 'System alerts with acknowledgment tracking for compliance';
COMMENT ON TABLE system_logs IS 'Centralized logging for all Industrial ADAM services';