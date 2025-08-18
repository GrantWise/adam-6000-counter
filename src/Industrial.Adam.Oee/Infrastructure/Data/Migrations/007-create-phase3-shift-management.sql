-- Phase 3 Enhancement: Shift Management Tables
-- Migration: 007-create-phase3-shift-management.sql
-- Description: Create tables for shift management, handover workflows, and performance tracking

-- Create shifts table
CREATE TABLE IF NOT EXISTS shifts (
    shift_id VARCHAR(255) PRIMARY KEY,
    shift_name VARCHAR(100) NOT NULL,
    shift_pattern_id VARCHAR(100) NOT NULL,
    scheduled_start_time TIMESTAMPTZ NOT NULL,
    scheduled_end_time TIMESTAMPTZ NOT NULL,
    actual_start_time TIMESTAMPTZ,
    actual_end_time TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'Planned',
    supervisor_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT chk_shift_status 
        CHECK (status IN ('Planned', 'Active', 'Completed', 'Cancelled')),
    CONSTRAINT chk_shift_times 
        CHECK (scheduled_end_time > scheduled_start_time)
);

-- Create shift operators table (many-to-many relationship)
CREATE TABLE IF NOT EXISTS shift_operators (
    shift_operator_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id VARCHAR(255) NOT NULL,
    operator_id VARCHAR(100) NOT NULL,
    role VARCHAR(100) NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    equipment_line_id VARCHAR(100),
    
    CONSTRAINT fk_shift_operators_shift 
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id) ON DELETE CASCADE,
    CONSTRAINT uk_shift_operator 
        UNIQUE (shift_id, operator_id)
);

-- Create shift equipment lines table (many-to-many relationship)
CREATE TABLE IF NOT EXISTS shift_equipment_lines (
    shift_equipment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id VARCHAR(255) NOT NULL,
    equipment_line_id VARCHAR(100) NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_shift_equipment_shift 
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id) ON DELETE CASCADE,
    CONSTRAINT uk_shift_equipment 
        UNIQUE (shift_id, equipment_line_id)
);

-- Create shift planned work orders table (many-to-many relationship)
CREATE TABLE IF NOT EXISTS shift_planned_work_orders (
    shift_work_order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id VARCHAR(255) NOT NULL,
    work_order_id VARCHAR(255) NOT NULL,
    planned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_shift_work_orders_shift 
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id) ON DELETE CASCADE,
    CONSTRAINT fk_shift_work_orders_work_order 
        FOREIGN KEY (work_order_id) REFERENCES work_orders(work_order_id),
    CONSTRAINT uk_shift_work_order 
        UNIQUE (shift_id, work_order_id)
);

-- Create shift handover notes table
CREATE TABLE IF NOT EXISTS shift_handover_notes (
    handover_note_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id VARCHAR(255) NOT NULL,
    note_text TEXT NOT NULL,
    author_id VARCHAR(100) NOT NULL,
    note_type VARCHAR(20) NOT NULL DEFAULT 'Information',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    target_shift_id VARCHAR(255),
    
    CONSTRAINT fk_handover_notes_shift 
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id) ON DELETE CASCADE,
    CONSTRAINT fk_handover_notes_target_shift 
        FOREIGN KEY (target_shift_id) REFERENCES shifts(shift_id),
    CONSTRAINT chk_handover_note_type 
        CHECK (note_type IN ('Information', 'Alert', 'Issue', 'Maintenance', 'Quality', 'Production'))
);

-- Create shift performance metrics table
CREATE TABLE IF NOT EXISTS shift_performance_metrics (
    metrics_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id VARCHAR(255) NOT NULL,
    planned_production_hours DECIMAL(8,2) NOT NULL,
    actual_production_hours DECIMAL(8,2) NOT NULL,
    total_oee_percent DECIMAL(5,2) DEFAULT 0,
    average_availability_percent DECIMAL(5,2) DEFAULT 0,
    average_performance_percent DECIMAL(5,2) DEFAULT 0,
    average_quality_percent DECIMAL(5,2) DEFAULT 0,
    work_orders_planned INTEGER DEFAULT 0,
    work_orders_completed INTEGER DEFAULT 0,
    total_stoppage_minutes DECIMAL(10,2) DEFAULT 0,
    total_units_produced DECIMAL(18,4) DEFAULT 0,
    total_units_defective DECIMAL(18,4) DEFAULT 0,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_shift_performance_shift 
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id) ON DELETE CASCADE,
    CONSTRAINT uk_shift_performance 
        UNIQUE (shift_id),
    CONSTRAINT chk_shift_performance_hours 
        CHECK (planned_production_hours >= 0 AND actual_production_hours >= 0),
    CONSTRAINT chk_shift_performance_percentages 
        CHECK (total_oee_percent >= 0 AND total_oee_percent <= 100 
               AND average_availability_percent >= 0 AND average_availability_percent <= 100
               AND average_performance_percent >= 0 AND average_performance_percent <= 100
               AND average_quality_percent >= 0 AND average_quality_percent <= 100)
);

-- Create indexes for shift tables
CREATE INDEX IF NOT EXISTS idx_shifts_shift_name ON shifts(shift_name);
CREATE INDEX IF NOT EXISTS idx_shifts_shift_pattern_id ON shifts(shift_pattern_id);
CREATE INDEX IF NOT EXISTS idx_shifts_status ON shifts(status);
CREATE INDEX IF NOT EXISTS idx_shifts_supervisor_id ON shifts(supervisor_id);
CREATE INDEX IF NOT EXISTS idx_shifts_scheduled_start_time ON shifts(scheduled_start_time);
CREATE INDEX IF NOT EXISTS idx_shifts_scheduled_end_time ON shifts(scheduled_end_time);
CREATE INDEX IF NOT EXISTS idx_shifts_actual_start_time ON shifts(actual_start_time);
CREATE INDEX IF NOT EXISTS idx_shifts_actual_end_time ON shifts(actual_end_time);
CREATE INDEX IF NOT EXISTS idx_shifts_created_at ON shifts(created_at);

CREATE INDEX IF NOT EXISTS idx_shift_operators_shift_id ON shift_operators(shift_id);
CREATE INDEX IF NOT EXISTS idx_shift_operators_operator_id ON shift_operators(operator_id);
CREATE INDEX IF NOT EXISTS idx_shift_operators_equipment_line_id ON shift_operators(equipment_line_id);

CREATE INDEX IF NOT EXISTS idx_shift_equipment_shift_id ON shift_equipment_lines(shift_id);
CREATE INDEX IF NOT EXISTS idx_shift_equipment_equipment_line_id ON shift_equipment_lines(equipment_line_id);

CREATE INDEX IF NOT EXISTS idx_shift_work_orders_shift_id ON shift_planned_work_orders(shift_id);
CREATE INDEX IF NOT EXISTS idx_shift_work_orders_work_order_id ON shift_planned_work_orders(work_order_id);

CREATE INDEX IF NOT EXISTS idx_handover_notes_shift_id ON shift_handover_notes(shift_id);
CREATE INDEX IF NOT EXISTS idx_handover_notes_author_id ON shift_handover_notes(author_id);
CREATE INDEX IF NOT EXISTS idx_handover_notes_note_type ON shift_handover_notes(note_type);
CREATE INDEX IF NOT EXISTS idx_handover_notes_created_at ON shift_handover_notes(created_at);
CREATE INDEX IF NOT EXISTS idx_handover_notes_target_shift_id ON shift_handover_notes(target_shift_id);

CREATE INDEX IF NOT EXISTS idx_shift_performance_shift_id ON shift_performance_metrics(shift_id);
CREATE INDEX IF NOT EXISTS idx_shift_performance_calculated_at ON shift_performance_metrics(calculated_at);

-- Add trigger to update updated_at timestamp on shifts
CREATE OR REPLACE FUNCTION update_shift_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_shift_updated_at
    BEFORE UPDATE ON shifts
    FOR EACH ROW
    EXECUTE FUNCTION update_shift_updated_at();

-- Add function to get current shift for a given time
CREATE OR REPLACE FUNCTION get_current_shift(check_time TIMESTAMPTZ DEFAULT NOW())
RETURNS TABLE (
    shift_id VARCHAR(255),
    shift_name VARCHAR(100),
    shift_pattern_id VARCHAR(100),
    supervisor_id VARCHAR(100),
    status VARCHAR(20)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.shift_id,
        s.shift_name,
        s.shift_pattern_id,
        s.supervisor_id,
        s.status
    FROM shifts s
    WHERE s.scheduled_start_time <= check_time 
      AND s.scheduled_end_time > check_time
      AND s.status IN ('Planned', 'Active')
    ORDER BY s.scheduled_start_time DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- Add function to calculate shift duration
CREATE OR REPLACE FUNCTION calculate_shift_duration(shift_id_param VARCHAR(255))
RETURNS TABLE (
    scheduled_duration_hours DECIMAL(8,2),
    actual_duration_hours DECIMAL(8,2),
    is_late_start BOOLEAN,
    is_overtime BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        EXTRACT(EPOCH FROM (s.scheduled_end_time - s.scheduled_start_time)) / 3600.0 as scheduled_duration_hours,
        CASE 
            WHEN s.actual_start_time IS NOT NULL THEN
                EXTRACT(EPOCH FROM (COALESCE(s.actual_end_time, NOW()) - s.actual_start_time)) / 3600.0
            ELSE 0.0
        END as actual_duration_hours,
        CASE 
            WHEN s.actual_start_time IS NOT NULL THEN
                s.actual_start_time > (s.scheduled_start_time + INTERVAL '15 minutes')
            ELSE 
                NOW() > (s.scheduled_start_time + INTERVAL '15 minutes') AND s.status = 'Planned'
        END as is_late_start,
        CASE 
            WHEN s.actual_end_time IS NOT NULL THEN
                s.actual_end_time > (s.scheduled_end_time + INTERVAL '15 minutes')
            ELSE 
                NOW() > (s.scheduled_end_time + INTERVAL '15 minutes') AND s.status = 'Active'
        END as is_overtime
    FROM shifts s
    WHERE s.shift_id = shift_id_param;
END;
$$ LANGUAGE plpgsql;

-- Create view for shift summary information
CREATE OR REPLACE VIEW shift_summary_view AS
SELECT 
    s.shift_id,
    s.shift_name,
    s.shift_pattern_id,
    s.status,
    s.scheduled_start_time,
    s.scheduled_end_time,
    s.actual_start_time,
    s.actual_end_time,
    s.supervisor_id,
    
    -- Calculated duration metrics
    EXTRACT(EPOCH FROM (s.scheduled_end_time - s.scheduled_start_time)) / 3600.0 as scheduled_duration_hours,
    CASE 
        WHEN s.actual_start_time IS NOT NULL THEN
            EXTRACT(EPOCH FROM (COALESCE(s.actual_end_time, NOW()) - s.actual_start_time)) / 3600.0
        ELSE 0.0
    END as actual_duration_hours,
    
    -- Late start and overtime indicators
    CASE 
        WHEN s.actual_start_time IS NOT NULL THEN
            s.actual_start_time > (s.scheduled_start_time + INTERVAL '15 minutes')
        ELSE 
            NOW() > (s.scheduled_start_time + INTERVAL '15 minutes') AND s.status = 'Planned'
    END as is_late_start,
    
    CASE 
        WHEN s.actual_end_time IS NOT NULL THEN
            s.actual_end_time > (s.scheduled_end_time + INTERVAL '15 minutes')
        ELSE 
            NOW() > (s.scheduled_end_time + INTERVAL '15 minutes') AND s.status = 'Active'
    END as is_overtime,
    
    -- Counts
    (SELECT COUNT(*) FROM shift_operators so WHERE so.shift_id = s.shift_id) as operator_count,
    (SELECT COUNT(*) FROM shift_equipment_lines sel WHERE sel.shift_id = s.shift_id) as equipment_line_count,
    (SELECT COUNT(*) FROM shift_planned_work_orders spwo WHERE spwo.shift_id = s.shift_id) as planned_work_order_count,
    (SELECT COUNT(*) FROM shift_handover_notes shn WHERE shn.shift_id = s.shift_id) as handover_note_count,
    
    -- Performance metrics
    spm.total_oee_percent,
    spm.average_availability_percent,
    spm.average_performance_percent,
    spm.average_quality_percent,
    spm.work_orders_completed,
    spm.total_units_produced,
    
    s.created_at,
    s.updated_at
    
FROM shifts s
LEFT JOIN shift_performance_metrics spm ON s.shift_id = spm.shift_id;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON shifts TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON shift_operators TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON shift_equipment_lines TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON shift_planned_work_orders TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON shift_handover_notes TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON shift_performance_metrics TO adam_user;
GRANT SELECT ON shift_summary_view TO adam_user;
GRANT EXECUTE ON FUNCTION get_current_shift(TIMESTAMPTZ) TO adam_user;
GRANT EXECUTE ON FUNCTION calculate_shift_duration(VARCHAR) TO adam_user;

-- Insert sample shift data for testing
INSERT INTO shifts (
    shift_id, shift_name, shift_pattern_id, scheduled_start_time, scheduled_end_time,
    supervisor_id, status
) VALUES
    ('SHIFT-DAY-20240818', 'Day Shift', 'DAY-PATTERN', '2024-08-18 06:00:00+00', '2024-08-18 14:00:00+00', 'SUP001', 'Completed'),
    ('SHIFT-EVENING-20240818', 'Evening Shift', 'EVENING-PATTERN', '2024-08-18 14:00:00+00', '2024-08-18 22:00:00+00', 'SUP002', 'Active'),
    ('SHIFT-NIGHT-20240818', 'Night Shift', 'NIGHT-PATTERN', '2024-08-18 22:00:00+00', '2024-08-19 06:00:00+00', 'SUP003', 'Planned')
ON CONFLICT (shift_id) DO NOTHING;

-- Insert sample shift operators
INSERT INTO shift_operators (shift_id, operator_id, role, equipment_line_id) VALUES
    ('SHIFT-DAY-20240818', 'OP001', 'Lead Operator', 'LINE-001'),
    ('SHIFT-DAY-20240818', 'OP002', 'Operator', 'LINE-002'),
    ('SHIFT-EVENING-20240818', 'OP003', 'Lead Operator', 'LINE-001'),
    ('SHIFT-EVENING-20240818', 'OP004', 'Operator', 'LINE-002')
ON CONFLICT (shift_operator_id) DO NOTHING;

-- Insert sample shift equipment lines
INSERT INTO shift_equipment_lines (shift_id, equipment_line_id) VALUES
    ('SHIFT-DAY-20240818', 'LINE-001'),
    ('SHIFT-DAY-20240818', 'LINE-002'),
    ('SHIFT-EVENING-20240818', 'LINE-001'),
    ('SHIFT-EVENING-20240818', 'LINE-002')
ON CONFLICT (shift_equipment_id) DO NOTHING;

-- Insert sample shift planned work orders
INSERT INTO shift_planned_work_orders (shift_id, work_order_id) VALUES
    ('SHIFT-DAY-20240818', 'WO-001'),
    ('SHIFT-EVENING-20240818', 'WO-002'),
    ('SHIFT-NIGHT-20240818', 'WO-003')
ON CONFLICT (shift_work_order_id) DO NOTHING;

-- Insert sample handover notes
INSERT INTO shift_handover_notes (shift_id, note_text, author_id, note_type, target_shift_id) VALUES
    ('SHIFT-DAY-20240818', 'Line 1 experienced minor vibration issue around 10 AM. Maintenance checked and cleared.', 'SUP001', 'Information', 'SHIFT-EVENING-20240818'),
    ('SHIFT-DAY-20240818', 'Work order WO-001 completed ahead of schedule. Quality checks all passed.', 'SUP001', 'Production', 'SHIFT-EVENING-20240818'),
    ('SHIFT-EVENING-20240818', 'Quality gate failed on batch B002. Batch placed on hold pending review.', 'SUP002', 'Quality', 'SHIFT-NIGHT-20240818')
ON CONFLICT (handover_note_id) DO NOTHING;