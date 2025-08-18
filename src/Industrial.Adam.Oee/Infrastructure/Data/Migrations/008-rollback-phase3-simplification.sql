-- OEE Simplification: Phase 3 Rollback Migration
-- Migration: 008-rollback-phase3-simplification.sql
-- Description: Safely remove Phase 3 over-implementations to return focus to core OEE monitoring
-- Author: Claude AI Assistant
-- Date: 2025-08-18

-- 1. First, backup any data that should be preserved (work order core data)
-- Note: This script assumes proper backup has been taken before execution

-- 2. Remove Phase 3 foreign key constraints
ALTER TABLE IF EXISTS batches DROP CONSTRAINT IF EXISTS fk_batches_work_order;
ALTER TABLE IF EXISTS shift_planned_work_orders DROP CONSTRAINT IF EXISTS fk_shift_work_orders_work_order;
ALTER TABLE IF EXISTS batch_notes DROP CONSTRAINT IF EXISTS fk_batch_notes_batch;
ALTER TABLE IF EXISTS batch_quality_checks DROP CONSTRAINT IF EXISTS fk_batch_quality_checks_batch;
ALTER TABLE IF EXISTS batch_material_consumptions DROP CONSTRAINT IF EXISTS fk_batch_material_consumptions_batch;
ALTER TABLE IF EXISTS shift_handover_notes DROP CONSTRAINT IF EXISTS fk_shift_handover_notes_shift;
ALTER TABLE IF EXISTS shift_performance_metrics DROP CONSTRAINT IF EXISTS fk_shift_performance_metrics_shift;
ALTER TABLE IF EXISTS shift_equipment_lines DROP CONSTRAINT IF EXISTS fk_shift_equipment_lines_shift;
ALTER TABLE IF EXISTS shift_operators DROP CONSTRAINT IF EXISTS fk_shift_operators_shift;

-- 3. Drop Phase 3 tables in correct order (dependencies first)
DROP TABLE IF EXISTS batch_notes CASCADE;
DROP TABLE IF EXISTS batch_quality_checks CASCADE;  
DROP TABLE IF EXISTS batch_material_consumptions CASCADE;
DROP TABLE IF EXISTS batches CASCADE;

DROP TABLE IF EXISTS shift_handover_notes CASCADE;
DROP TABLE IF EXISTS shift_performance_metrics CASCADE;
DROP TABLE IF EXISTS shift_planned_work_orders CASCADE;
DROP TABLE IF EXISTS shift_equipment_lines CASCADE; 
DROP TABLE IF EXISTS shift_operators CASCADE;
DROP TABLE IF EXISTS shifts CASCADE;

-- 4. Remove Phase 3 columns from work_orders table (if they exist)
-- Note: Since WorkOrder.cs analysis showed these columns don't exist, 
-- these statements will be no-ops if columns don't exist
ALTER TABLE work_orders 
DROP COLUMN IF EXISTS priority,
DROP COLUMN IF EXISTS shift_id,
DROP COLUMN IF EXISTS job_schedule_id,
DROP COLUMN IF EXISTS batch_tracking_enabled,
DROP COLUMN IF EXISTS planned_batch_size,
DROP COLUMN IF EXISTS setup_time_minutes,
DROP COLUMN IF EXISTS teardown_time_minutes,
DROP COLUMN IF EXISTS complexity_score,
DROP COLUMN IF EXISTS customer_priority,
DROP COLUMN IF EXISTS due_date;

-- 5. Create new simple_job_queues table to replace complex scheduling
CREATE TABLE IF NOT EXISTS simple_job_queues (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(100) NOT NULL,
    work_order_id VARCHAR(255) NOT NULL,
    product_description VARCHAR(500) NOT NULL,
    priority INTEGER DEFAULT 5,
    queued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operator_id VARCHAR(100),
    started_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_simple_queue_work_order 
        FOREIGN KEY (work_order_id) REFERENCES work_orders(work_order_id) ON DELETE CASCADE,
    CONSTRAINT uk_line_work_order 
        UNIQUE (line_id, work_order_id),
    CONSTRAINT chk_priority 
        CHECK (priority >= 1 AND priority <= 10)
);

-- 6. Create indexes for simple_job_queues performance
CREATE INDEX IF NOT EXISTS idx_simple_job_queues_line_id ON simple_job_queues(line_id);
CREATE INDEX IF NOT EXISTS idx_simple_job_queues_priority ON simple_job_queues(priority);
CREATE INDEX IF NOT EXISTS idx_simple_job_queues_queued_at ON simple_job_queues(queued_at);
CREATE INDEX IF NOT EXISTS idx_simple_job_queues_operator_id ON simple_job_queues(operator_id);

-- 7. Create quality_records table for simplified quality tracking
CREATE TABLE IF NOT EXISTS quality_records (
    id SERIAL PRIMARY KEY,
    work_order_id VARCHAR(255) NOT NULL,
    good_count INTEGER NOT NULL DEFAULT 0,
    scrap_count INTEGER NOT NULL DEFAULT 0,
    scrap_reason_code VARCHAR(100),
    notes TEXT,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_quality_records_work_order 
        FOREIGN KEY (work_order_id) REFERENCES work_orders(work_order_id) ON DELETE CASCADE,
    CONSTRAINT chk_good_count_non_negative 
        CHECK (good_count >= 0),
    CONSTRAINT chk_scrap_count_non_negative 
        CHECK (scrap_count >= 0),
    CONSTRAINT chk_at_least_one_count 
        CHECK (good_count > 0 OR scrap_count > 0)
);

-- 8. Create indexes for quality_records performance
CREATE INDEX IF NOT EXISTS idx_quality_records_work_order_id ON quality_records(work_order_id);
CREATE INDEX IF NOT EXISTS idx_quality_records_recorded_at ON quality_records(recorded_at);
CREATE INDEX IF NOT EXISTS idx_quality_records_scrap_reason ON quality_records(scrap_reason_code) 
    WHERE scrap_reason_code IS NOT NULL;

-- 9. Add helpful comments to new tables
COMMENT ON TABLE simple_job_queues IS 'Simple job queue for basic work order sequencing - replaces complex Phase 3 scheduling';
COMMENT ON COLUMN simple_job_queues.priority IS 'Job priority: 1 = highest priority, 10 = lowest priority, 5 = default';
COMMENT ON COLUMN simple_job_queues.operator_id IS 'Operator assigned to job (NULL = not started)';

COMMENT ON TABLE quality_records IS 'Simple quality tracking records - replaces complex Phase 3 quality inspection system';
COMMENT ON COLUMN quality_records.good_count IS 'Number of good pieces produced';
COMMENT ON COLUMN quality_records.scrap_count IS 'Number of scrap/defective pieces';
COMMENT ON COLUMN quality_records.scrap_reason_code IS 'Optional reason code for scrap production';

-- 10. Grant necessary permissions (adjust as needed for your environment)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON simple_job_queues TO oee_application_user;
-- GRANT USAGE, SELECT ON SEQUENCE simple_job_queues_id_seq TO oee_application_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON quality_records TO oee_application_user;
-- GRANT USAGE, SELECT ON SEQUENCE quality_records_id_seq TO oee_application_user;

-- Migration complete: Phase 3 over-implementations removed, core OEE functionality preserved