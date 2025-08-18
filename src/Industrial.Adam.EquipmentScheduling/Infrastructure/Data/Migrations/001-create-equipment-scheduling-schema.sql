-- ================================================
-- Equipment Scheduling System Database Schema
-- Migration 001: Create initial schema and tables
-- Compatible with: TimescaleDB, PostgreSQL 13+
-- ================================================

-- Enable TimescaleDB extension if available
DO $$ 
BEGIN
    CREATE EXTENSION IF NOT EXISTS timescaledb;
    RAISE NOTICE 'TimescaleDB extension enabled successfully';
EXCEPTION 
    WHEN others THEN
        RAISE NOTICE 'TimescaleDB extension not available, using PostgreSQL only';
END $$;

-- Create schema for equipment scheduling
CREATE SCHEMA IF NOT EXISTS equipment_scheduling;
COMMENT ON SCHEMA equipment_scheduling IS 'Equipment scheduling and availability management system';

-- Set search path for this migration
SET search_path TO equipment_scheduling, public;

-- ================================================
-- Phase 1: Equipment Scheduling Core Tables
-- ================================================

-- Resources Table (ISA-95 Equipment Hierarchy)
CREATE TABLE IF NOT EXISTS sched_resources (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    resource_type VARCHAR(20) NOT NULL CHECK (resource_type IN ('Enterprise', 'Site', 'Area', 'WorkCenter', 'WorkUnit')),
    parent_id BIGINT REFERENCES sched_resources(id) ON DELETE RESTRICT,
    hierarchy_path VARCHAR(500),
    requires_scheduling BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE sched_resources IS 'ISA-95 compliant equipment resource hierarchy';
COMMENT ON COLUMN sched_resources.hierarchy_path IS 'Materialized path for efficient hierarchy queries (e.g., /1/5/12/)';
COMMENT ON COLUMN sched_resources.resource_type IS 'ISA-95 resource types: Enterprise, Site, Area, WorkCenter, WorkUnit';
COMMENT ON COLUMN sched_resources.requires_scheduling IS 'Indicates if this resource needs schedule generation';

-- Operating Patterns Table
CREATE TABLE IF NOT EXISTS sched_operating_patterns (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    pattern_type VARCHAR(20) NOT NULL CHECK (pattern_type IN ('Continuous', 'TwoShift', 'DayOnly', 'Extended', 'Custom')),
    cycle_days INTEGER NOT NULL CHECK (cycle_days BETWEEN 1 AND 365),
    weekly_hours DECIMAL(5,2) CHECK (weekly_hours BETWEEN 0 AND 168),
    configuration JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_visible BOOLEAN DEFAULT TRUE,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

COMMENT ON TABLE sched_operating_patterns IS 'Operating patterns define when equipment operates (shifts, hours, cycles)';
COMMENT ON COLUMN sched_operating_patterns.pattern_type IS 'Standard pattern types: Continuous, TwoShift, DayOnly, Extended, Custom';
COMMENT ON COLUMN sched_operating_patterns.cycle_days IS 'Number of days in the pattern cycle (1-365)';
COMMENT ON COLUMN sched_operating_patterns.weekly_hours IS 'Total planned hours per week (0-168)';
COMMENT ON COLUMN sched_operating_patterns.configuration IS 'JSON configuration for shift times, custom patterns, etc.';
COMMENT ON COLUMN sched_operating_patterns.is_visible IS 'Whether this pattern is available for selection';

-- Pattern Assignments Table
CREATE TABLE IF NOT EXISTS sched_pattern_assignments (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    pattern_id INTEGER NOT NULL REFERENCES sched_operating_patterns(id) ON DELETE RESTRICT,
    effective_date DATE NOT NULL,
    end_date DATE,
    is_override BOOLEAN DEFAULT FALSE,
    assigned_by VARCHAR(100),
    assigned_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    
    CONSTRAINT chk_assignment_dates CHECK (end_date IS NULL OR end_date >= effective_date)
);

COMMENT ON TABLE sched_pattern_assignments IS 'Links resources to operating patterns with effective date ranges';
COMMENT ON COLUMN sched_pattern_assignments.effective_date IS 'When this pattern assignment becomes active';
COMMENT ON COLUMN sched_pattern_assignments.end_date IS 'When this assignment expires (NULL for indefinite)';
COMMENT ON COLUMN sched_pattern_assignments.is_override IS 'Whether this is a temporary override assignment';
COMMENT ON COLUMN sched_pattern_assignments.assigned_by IS 'User or system that created this assignment';

-- Equipment Schedules Table (Time-series optimized)
CREATE TABLE IF NOT EXISTS sched_equipment_schedules (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    schedule_date DATE NOT NULL,
    shift_code VARCHAR(10),
    planned_start_time TIMESTAMPTZ,
    planned_end_time TIMESTAMPTZ,
    planned_hours DECIMAL(4,2) NOT NULL CHECK (planned_hours BETWEEN 0 AND 24),
    schedule_status VARCHAR(20) DEFAULT 'Planned' CHECK (schedule_status IN ('Active', 'Planned', 'Cancelled', 'Completed', 'Suspended')),
    pattern_id INTEGER REFERENCES sched_operating_patterns(id) ON DELETE SET NULL,
    is_exception BOOLEAN DEFAULT FALSE,
    generated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    
    CONSTRAINT chk_schedule_times CHECK (
        (planned_start_time IS NULL AND planned_end_time IS NULL) OR
        (planned_start_time IS NOT NULL AND planned_end_time IS NOT NULL AND planned_end_time > planned_start_time)
    )
);

COMMENT ON TABLE sched_equipment_schedules IS 'Generated equipment schedules based on operating patterns';
COMMENT ON COLUMN sched_equipment_schedules.schedule_date IS 'The date this schedule applies to';
COMMENT ON COLUMN sched_equipment_schedules.shift_code IS 'Shift identifier (DAY, EVE, NIGHT, etc.)';
COMMENT ON COLUMN sched_equipment_schedules.planned_hours IS 'Planned operating hours for this schedule entry';
COMMENT ON COLUMN sched_equipment_schedules.schedule_status IS 'Current status of the schedule';
COMMENT ON COLUMN sched_equipment_schedules.is_exception IS 'Whether this is an exception/override schedule';
COMMENT ON COLUMN sched_equipment_schedules.generated_at IS 'When this schedule was automatically generated';

-- ================================================
-- Indexes for Performance Optimization
-- ================================================

-- Resource Indexes
CREATE INDEX IF NOT EXISTS idx_sched_resources_parent ON sched_resources(parent_id) WHERE parent_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sched_resources_hierarchy ON sched_resources USING btree(hierarchy_path) WHERE hierarchy_path IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sched_resources_type ON sched_resources(resource_type);
CREATE INDEX IF NOT EXISTS idx_sched_resources_active ON sched_resources(is_active) WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_sched_resources_scheduling ON sched_resources(requires_scheduling) WHERE requires_scheduling = true;
CREATE INDEX IF NOT EXISTS idx_sched_resources_code_lower ON sched_resources(LOWER(code));

-- Operating Pattern Indexes
CREATE INDEX IF NOT EXISTS idx_sched_patterns_type ON sched_operating_patterns(pattern_type);
CREATE INDEX IF NOT EXISTS idx_sched_patterns_visible ON sched_operating_patterns(is_visible) WHERE is_visible = true;
CREATE INDEX IF NOT EXISTS idx_sched_patterns_weekly_hours ON sched_operating_patterns(weekly_hours);
CREATE INDEX IF NOT EXISTS idx_sched_patterns_name_lower ON sched_operating_patterns(LOWER(name));

-- Pattern Assignment Indexes
CREATE INDEX IF NOT EXISTS idx_sched_assignments_resource ON sched_pattern_assignments(resource_id);
CREATE INDEX IF NOT EXISTS idx_sched_assignments_pattern ON sched_pattern_assignments(pattern_id);
CREATE INDEX IF NOT EXISTS idx_sched_assignments_resource_effective ON sched_pattern_assignments(resource_id, effective_date);
CREATE INDEX IF NOT EXISTS idx_sched_assignments_resource_end ON sched_pattern_assignments(resource_id, end_date) WHERE end_date IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sched_assignments_active ON sched_pattern_assignments(resource_id, effective_date, end_date);
CREATE INDEX IF NOT EXISTS idx_sched_assignments_override ON sched_pattern_assignments(is_override) WHERE is_override = true;

-- Equipment Schedule Indexes (Optimized for time-series queries)
CREATE INDEX IF NOT EXISTS idx_sched_schedules_resource_date ON sched_equipment_schedules(resource_id, schedule_date);
CREATE INDEX IF NOT EXISTS idx_sched_schedules_date ON sched_equipment_schedules(schedule_date);
CREATE INDEX IF NOT EXISTS idx_sched_schedules_resource ON sched_equipment_schedules(resource_id);
CREATE INDEX IF NOT EXISTS idx_sched_schedules_status ON sched_equipment_schedules(schedule_status);
CREATE INDEX IF NOT EXISTS idx_sched_schedules_pattern ON sched_equipment_schedules(pattern_id) WHERE pattern_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sched_schedules_exception ON sched_equipment_schedules(is_exception) WHERE is_exception = true;
CREATE INDEX IF NOT EXISTS idx_sched_schedules_time_range ON sched_equipment_schedules(planned_start_time, planned_end_time) WHERE planned_start_time IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sched_schedules_active_time ON sched_equipment_schedules(resource_id, schedule_date, planned_start_time, planned_end_time) 
    WHERE schedule_status IN ('Active', 'Planned');

-- ================================================
-- TimescaleDB Hypertable Configuration
-- ================================================

-- Convert equipment schedules to hypertable for time-series optimization
DO $$
BEGIN
    -- Check if TimescaleDB is available
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'timescaledb') THEN
        -- Create hypertable on equipment schedules
        PERFORM create_hypertable('sched_equipment_schedules', 'schedule_date', if_not_exists => TRUE);
        
        -- Add compression policy (compress data older than 90 days)
        PERFORM add_compression_policy('sched_equipment_schedules', INTERVAL '90 days', if_not_exists => TRUE);
        
        -- Add retention policy (keep data for 7 years)
        PERFORM add_retention_policy('sched_equipment_schedules', INTERVAL '7 years', if_not_exists => TRUE);
        
        RAISE NOTICE 'TimescaleDB hypertable created for sched_equipment_schedules with compression and retention policies';
    ELSE
        RAISE NOTICE 'TimescaleDB not available, using regular PostgreSQL tables';
    END IF;
EXCEPTION 
    WHEN others THEN
        RAISE NOTICE 'Could not configure TimescaleDB features: %', SQLERRM;
END $$;

-- ================================================
-- Update Triggers for Automatic Timestamp Management
-- ================================================

-- Function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION equipment_scheduling.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply update triggers to all tables
CREATE TRIGGER trigger_sched_resources_updated_at
    BEFORE UPDATE ON sched_resources
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_sched_operating_patterns_updated_at
    BEFORE UPDATE ON sched_operating_patterns
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_sched_pattern_assignments_updated_at
    BEFORE UPDATE ON sched_pattern_assignments
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_sched_equipment_schedules_updated_at
    BEFORE UPDATE ON sched_equipment_schedules
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ================================================
-- Views for Common Queries
-- ================================================

-- Active Pattern Assignments View
CREATE OR REPLACE VIEW v_active_pattern_assignments AS
SELECT 
    pa.id,
    pa.resource_id,
    r.name AS resource_name,
    r.code AS resource_code,
    r.resource_type,
    pa.pattern_id,
    op.name AS pattern_name,
    op.pattern_type,
    op.weekly_hours,
    pa.effective_date,
    pa.end_date,
    pa.is_override,
    pa.assigned_by,
    pa.assigned_at
FROM sched_pattern_assignments pa
JOIN sched_resources r ON pa.resource_id = r.id
JOIN sched_operating_patterns op ON pa.pattern_id = op.id
WHERE (pa.end_date IS NULL OR pa.end_date >= CURRENT_DATE)
  AND pa.effective_date <= CURRENT_DATE
  AND r.is_active = true;

COMMENT ON VIEW v_active_pattern_assignments IS 'Currently active pattern assignments with resource and pattern details';

-- Resource Hierarchy View
CREATE OR REPLACE VIEW v_resource_hierarchy AS
WITH RECURSIVE hierarchy AS (
    -- Base case: root resources (no parent)
    SELECT 
        id,
        name,
        code,
        resource_type,
        parent_id,
        hierarchy_path,
        requires_scheduling,
        is_active,
        0 AS level,
        ARRAY[id] AS path_ids,
        name AS full_path
    FROM sched_resources
    WHERE parent_id IS NULL AND is_active = true
    
    UNION ALL
    
    -- Recursive case: child resources
    SELECT 
        r.id,
        r.name,
        r.code,
        r.resource_type,
        r.parent_id,
        r.hierarchy_path,
        r.requires_scheduling,
        r.is_active,
        h.level + 1,
        h.path_ids || r.id,
        h.full_path || ' > ' || r.name
    FROM sched_resources r
    JOIN hierarchy h ON r.parent_id = h.id
    WHERE r.is_active = true
)
SELECT * FROM hierarchy
ORDER BY level, name;

COMMENT ON VIEW v_resource_hierarchy IS 'Hierarchical view of all active resources with path information';

-- Schedule Summary View
CREATE OR REPLACE VIEW v_schedule_summary AS
SELECT 
    s.resource_id,
    r.name AS resource_name,
    r.code AS resource_code,
    s.schedule_date,
    COUNT(*) AS schedule_count,
    SUM(s.planned_hours) AS total_planned_hours,
    COUNT(*) FILTER (WHERE s.is_exception) AS exception_count,
    COUNT(*) FILTER (WHERE s.schedule_status = 'Active') AS active_count,
    COUNT(*) FILTER (WHERE s.schedule_status = 'Completed') AS completed_count,
    COUNT(*) FILTER (WHERE s.schedule_status = 'Cancelled') AS cancelled_count
FROM sched_equipment_schedules s
JOIN sched_resources r ON s.resource_id = r.id
WHERE s.schedule_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY s.resource_id, r.name, r.code, s.schedule_date;

COMMENT ON VIEW v_schedule_summary IS 'Daily schedule summary with counts and totals per resource';

-- ================================================
-- Grant Permissions
-- ================================================

-- Grant usage on schema
GRANT USAGE ON SCHEMA equipment_scheduling TO PUBLIC;

-- Grant permissions on tables
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA equipment_scheduling TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA equipment_scheduling TO PUBLIC;

-- Grant permissions on views
GRANT SELECT ON ALL TABLES IN SCHEMA equipment_scheduling TO PUBLIC;

-- Reset search path
RESET search_path;

-- ================================================
-- Migration Completion
-- ================================================

RAISE NOTICE 'Equipment Scheduling schema created successfully';
RAISE NOTICE 'Tables created: sched_resources, sched_operating_patterns, sched_pattern_assignments, sched_equipment_schedules';
RAISE NOTICE 'Indexes created for optimal query performance';
RAISE NOTICE 'Views created for common query patterns';
RAISE NOTICE 'TimescaleDB features configured if available';