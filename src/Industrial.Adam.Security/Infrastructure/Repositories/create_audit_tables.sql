-- Industrial Adam Security Module - Audit Tables for Phase 2
-- TimescaleDB schema for security audit functionality
-- 
-- This script creates the necessary tables for:
-- - Login attempt tracking
-- - Audit trail logging
-- - User session management
-- - User activity tracking
-- - System configuration storage
-- - Feature flag management

-- Login attempts table for security monitoring
CREATE TABLE IF NOT EXISTS login_attempts (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    ip_address INET NOT NULL,
    success BOOLEAN NOT NULL DEFAULT false,
    failure_reason VARCHAR(200),
    attempted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_login_attempts_username (username),
    INDEX idx_login_attempts_attempted_at (attempted_at),
    INDEX idx_login_attempts_success (success),
    INDEX idx_login_attempts_ip (ip_address)
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('login_attempts', 'attempted_at', if_not_exists => TRUE);

-- Audit trail table for CFR Part 11 compliance
CREATE TABLE IF NOT EXISTS audit_trail (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    action VARCHAR(200) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    old_value JSONB,
    new_value JSONB,
    ip_address INET NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_audit_trail_user_id (user_id),
    INDEX idx_audit_trail_created_at (created_at),
    INDEX idx_audit_trail_action (action),
    INDEX idx_audit_trail_entity (entity_type, entity_id)
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('audit_trail', 'created_at', if_not_exists => TRUE);

-- User sessions table for session tracking
CREATE TABLE IF NOT EXISTS user_sessions (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    token_hash VARCHAR(256) NOT NULL UNIQUE,
    ip_address INET NOT NULL,
    user_agent TEXT,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    
    -- Indexes for performance
    INDEX idx_user_sessions_user_id (user_id),
    INDEX idx_user_sessions_token_hash (token_hash),
    INDEX idx_user_sessions_started_at (started_at),
    INDEX idx_user_sessions_active (user_id, ended_at) WHERE ended_at IS NULL
);

-- User activity table for detailed activity tracking
CREATE TABLE IF NOT EXISTS user_activity (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    action VARCHAR(200) NOT NULL,
    details TEXT,
    ip_address INET NOT NULL,
    user_agent TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_user_activity_user_id (user_id),
    INDEX idx_user_activity_timestamp (timestamp),
    INDEX idx_user_activity_action (action)
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('user_activity', 'timestamp', if_not_exists => TRUE);

-- System configuration table for settings management
CREATE TABLE IF NOT EXISTS system_configuration (
    key VARCHAR(200) PRIMARY KEY,
    value JSONB NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'General',
    description TEXT,
    updated_by INTEGER NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_system_config_category (category),
    INDEX idx_system_config_updated_at (updated_at)
);

-- Feature flags table for feature toggle management
CREATE TABLE IF NOT EXISTS feature_flags (
    name VARCHAR(100) PRIMARY KEY,
    enabled BOOLEAN NOT NULL DEFAULT false,
    description TEXT,
    conditions JSONB,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_feature_flags_enabled (enabled),
    INDEX idx_feature_flags_updated_at (updated_at)
);

-- Data retention policies for compliance (7 years for regulated industries)
SELECT add_retention_policy('login_attempts', INTERVAL '7 years', if_not_exists => TRUE);
SELECT add_retention_policy('audit_trail', INTERVAL '7 years', if_not_exists => TRUE);
SELECT add_retention_policy('user_activity', INTERVAL '7 years', if_not_exists => TRUE);

-- Compression policies for storage optimization
SELECT add_compression_policy('login_attempts', INTERVAL '30 days', if_not_exists => TRUE);
SELECT add_compression_policy('audit_trail', INTERVAL '30 days', if_not_exists => TRUE);
SELECT add_compression_policy('user_activity', INTERVAL '30 days', if_not_exists => TRUE);

-- Create views for common queries
CREATE OR REPLACE VIEW active_user_sessions AS
SELECT 
    s.id,
    s.user_id,
    s.ip_address,
    s.user_agent,
    s.started_at,
    s.last_activity,
    EXTRACT(EPOCH FROM (NOW() - s.last_activity)) / 60 as minutes_since_activity
FROM user_sessions s
WHERE s.ended_at IS NULL
ORDER BY s.last_activity DESC;

CREATE OR REPLACE VIEW daily_login_summary AS
SELECT 
    DATE_TRUNC('day', attempted_at) as login_date,
    COUNT(*) as total_attempts,
    COUNT(*) FILTER (WHERE success = true) as successful_logins,
    COUNT(*) FILTER (WHERE success = false) as failed_logins,
    COUNT(DISTINCT username) as unique_users,
    COUNT(DISTINCT ip_address) as unique_ips
FROM login_attempts
WHERE attempted_at >= NOW() - INTERVAL '30 days'
GROUP BY DATE_TRUNC('day', attempted_at)
ORDER BY login_date DESC;

CREATE OR REPLACE VIEW security_events_summary AS
SELECT 
    DATE_TRUNC('hour', created_at) as event_hour,
    action,
    entity_type,
    COUNT(*) as event_count,
    COUNT(DISTINCT user_id) as unique_users
FROM audit_trail
WHERE created_at >= NOW() - INTERVAL '24 hours'
GROUP BY DATE_TRUNC('hour', created_at), action, entity_type
ORDER BY event_hour DESC, event_count DESC;

-- Insert default system configurations
INSERT INTO system_configuration (key, value, category, description, updated_by) VALUES
('Security.MaxFailedLoginAttempts', '5', 'Security', 'Maximum failed login attempts before account lockout', 0),
('Security.AccountLockoutDuration', '900', 'Security', 'Account lockout duration in seconds (15 minutes)', 0),
('Security.PasswordMinLength', '12', 'Security', 'Minimum password length requirement', 0),
('Security.SessionTimeoutMinutes', '60', 'Security', 'Session timeout in minutes', 0),
('Audit.RetentionDays', '2555', 'Audit', 'Audit log retention period in days (7 years)', 0),
('Notifications.Email.Enabled', 'false', 'Notifications', 'Enable email notifications', 0),
('Notifications.Webhook.Enabled', 'false', 'Notifications', 'Enable webhook notifications', 0)
ON CONFLICT (key) DO NOTHING;

-- Insert default feature flags
INSERT INTO feature_flags (name, enabled, description) VALUES
('AdvancedAuditLogging', true, 'Enable advanced audit logging features'),
('RealTimeSecurityAlerts', true, 'Enable real-time security alert notifications'),
('BulkUserOperations', true, 'Enable bulk user management operations'),
('ComplianceReporting', true, 'Enable CFR Part 11 compliance reporting'),
('SessionManagement', true, 'Enable advanced session management features'),
('MaintenanceMode', false, 'Enable maintenance mode for system updates')
ON CONFLICT (name) DO NOTHING;

-- Create functions for common operations
CREATE OR REPLACE FUNCTION get_user_login_stats(username_param VARCHAR)
RETURNS TABLE (
    total_attempts BIGINT,
    successful_logins BIGINT,
    failed_logins BIGINT,
    last_successful_login TIMESTAMPTZ,
    last_failed_login TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*) as total_attempts,
        COUNT(*) FILTER (WHERE success = true) as successful_logins,
        COUNT(*) FILTER (WHERE success = false) as failed_logins,
        MAX(attempted_at) FILTER (WHERE success = true) as last_successful_login,
        MAX(attempted_at) FILTER (WHERE success = false) as last_failed_login
    FROM login_attempts 
    WHERE username = username_param;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_audit_summary(start_date TIMESTAMPTZ, end_date TIMESTAMPTZ)
RETURNS TABLE (
    total_events BIGINT,
    unique_users BIGINT,
    actions_count BIGINT,
    entity_types_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*) as total_events,
        COUNT(DISTINCT user_id) as unique_users,
        COUNT(DISTINCT action) as actions_count,
        COUNT(DISTINCT entity_type) as entity_types_count
    FROM audit_trail 
    WHERE created_at BETWEEN start_date AND end_date;
END;
$$ LANGUAGE plpgsql;

-- Grant permissions (adjust as needed for your environment)
-- GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO industrial_system;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO industrial_system;

-- Create notification for successful table creation
DO $$
BEGIN
    RAISE NOTICE 'Industrial Adam Security audit tables created successfully';
    RAISE NOTICE 'Tables: login_attempts, audit_trail, user_sessions, user_activity, system_configuration, feature_flags';
    RAISE NOTICE 'Hypertables configured for time-series optimization';
    RAISE NOTICE 'Retention and compression policies applied';
    RAISE NOTICE 'Views and functions created for common operations';
END $$;