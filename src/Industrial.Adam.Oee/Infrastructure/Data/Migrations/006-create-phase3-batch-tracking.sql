-- Phase 3 Enhancement: Batch Tracking Tables
-- Migration: 006-create-phase3-batch-tracking.sql
-- Description: Create tables for batch tracking capabilities including genealogy and quality metrics

-- Create batches table
CREATE TABLE IF NOT EXISTS batches (
    batch_id VARCHAR(255) PRIMARY KEY,
    batch_number VARCHAR(100) NOT NULL UNIQUE,
    work_order_id VARCHAR(255) NOT NULL,
    product_id VARCHAR(255) NOT NULL,
    planned_quantity DECIMAL(18,4) NOT NULL,
    actual_quantity DECIMAL(18,4) DEFAULT 0,
    good_quantity DECIMAL(18,4) DEFAULT 0,
    defective_quantity DECIMAL(18,4) DEFAULT 0,
    unit_of_measure VARCHAR(50) NOT NULL DEFAULT 'pieces',
    status VARCHAR(20) NOT NULL DEFAULT 'Planned',
    quality_score DECIMAL(5,2) DEFAULT 100.00,
    start_time TIMESTAMPTZ,
    completion_time TIMESTAMPTZ,
    operator_id VARCHAR(100) NOT NULL,
    shift_id VARCHAR(255),
    parent_batch_id VARCHAR(255),
    equipment_line_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_batches_work_order 
        FOREIGN KEY (work_order_id) REFERENCES work_orders(work_order_id),
    CONSTRAINT fk_batches_parent 
        FOREIGN KEY (parent_batch_id) REFERENCES batches(batch_id),
    CONSTRAINT chk_batch_status 
        CHECK (status IN ('Planned', 'InProgress', 'OnHold', 'Completed', 'Cancelled')),
    CONSTRAINT chk_batch_quantities 
        CHECK (planned_quantity > 0 AND actual_quantity >= 0 AND good_quantity >= 0 AND defective_quantity >= 0),
    CONSTRAINT chk_batch_quality_score 
        CHECK (quality_score >= 0 AND quality_score <= 100)
);

-- Create batch material consumptions table
CREATE TABLE IF NOT EXISTS batch_material_consumptions (
    consumption_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id VARCHAR(255) NOT NULL,
    material_id VARCHAR(100) NOT NULL,
    quantity_used DECIMAL(18,4) NOT NULL,
    unit_of_measure VARCHAR(50) NOT NULL,
    consumed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    batch_lot_number VARCHAR(100),
    
    CONSTRAINT fk_material_consumption_batch 
        FOREIGN KEY (batch_id) REFERENCES batches(batch_id) ON DELETE CASCADE,
    CONSTRAINT chk_material_quantity_used 
        CHECK (quantity_used > 0)
);

-- Create batch quality checks table
CREATE TABLE IF NOT EXISTS batch_quality_checks (
    quality_check_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id VARCHAR(255) NOT NULL,
    check_type VARCHAR(100) NOT NULL,
    result VARCHAR(20) NOT NULL,
    checked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operator_id VARCHAR(100) NOT NULL,
    value DECIMAL(18,4),
    specification VARCHAR(500),
    
    CONSTRAINT fk_quality_check_batch 
        FOREIGN KEY (batch_id) REFERENCES batches(batch_id) ON DELETE CASCADE,
    CONSTRAINT chk_quality_check_result 
        CHECK (result IN ('Passed', 'Failed', 'Pending'))
);

-- Create batch notes table
CREATE TABLE IF NOT EXISTS batch_notes (
    note_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id VARCHAR(255) NOT NULL,
    note_text TEXT NOT NULL,
    author_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_batch_note_batch 
        FOREIGN KEY (batch_id) REFERENCES batches(batch_id) ON DELETE CASCADE
);

-- Create indexes for batch tables
CREATE INDEX IF NOT EXISTS idx_batches_work_order_id ON batches(work_order_id);
CREATE INDEX IF NOT EXISTS idx_batches_batch_number ON batches(batch_number);
CREATE INDEX IF NOT EXISTS idx_batches_status ON batches(status);
CREATE INDEX IF NOT EXISTS idx_batches_equipment_line_id ON batches(equipment_line_id);
CREATE INDEX IF NOT EXISTS idx_batches_operator_id ON batches(operator_id);
CREATE INDEX IF NOT EXISTS idx_batches_shift_id ON batches(shift_id);
CREATE INDEX IF NOT EXISTS idx_batches_parent_batch_id ON batches(parent_batch_id);
CREATE INDEX IF NOT EXISTS idx_batches_start_time ON batches(start_time);
CREATE INDEX IF NOT EXISTS idx_batches_completion_time ON batches(completion_time);
CREATE INDEX IF NOT EXISTS idx_batches_quality_score ON batches(quality_score);
CREATE INDEX IF NOT EXISTS idx_batches_created_at ON batches(created_at);

CREATE INDEX IF NOT EXISTS idx_material_consumptions_batch_id ON batch_material_consumptions(batch_id);
CREATE INDEX IF NOT EXISTS idx_material_consumptions_material_id ON batch_material_consumptions(material_id);
CREATE INDEX IF NOT EXISTS idx_material_consumptions_consumed_at ON batch_material_consumptions(consumed_at);

CREATE INDEX IF NOT EXISTS idx_quality_checks_batch_id ON batch_quality_checks(batch_id);
CREATE INDEX IF NOT EXISTS idx_quality_checks_check_type ON batch_quality_checks(check_type);
CREATE INDEX IF NOT EXISTS idx_quality_checks_result ON batch_quality_checks(result);
CREATE INDEX IF NOT EXISTS idx_quality_checks_checked_at ON batch_quality_checks(checked_at);

CREATE INDEX IF NOT EXISTS idx_batch_notes_batch_id ON batch_notes(batch_id);
CREATE INDEX IF NOT EXISTS idx_batch_notes_author_id ON batch_notes(author_id);
CREATE INDEX IF NOT EXISTS idx_batch_notes_created_at ON batch_notes(created_at);

-- Add trigger to update updated_at timestamp on batches
CREATE OR REPLACE FUNCTION update_batch_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_batch_updated_at
    BEFORE UPDATE ON batches
    FOR EACH ROW
    EXECUTE FUNCTION update_batch_updated_at();

-- Add function to calculate batch efficiency
CREATE OR REPLACE FUNCTION calculate_batch_efficiency(batch_id_param VARCHAR(255))
RETURNS TABLE (
    time_efficiency DECIMAL(5,2),
    yield_percentage DECIMAL(5,2),
    completion_percentage DECIMAL(5,2),
    production_rate DECIMAL(10,4)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        CASE 
            WHEN b.start_time IS NOT NULL AND b.completion_time IS NOT NULL THEN
                LEAST(100.0, 
                    (EXTRACT(EPOCH FROM (b.completion_time - b.start_time)) / 3600.0 / 
                     NULLIF(EXTRACT(EPOCH FROM (wo.scheduled_end_time - wo.scheduled_start_time)) / 3600.0, 0)) * 100.0)::DECIMAL(5,2)
            ELSE 0.0::DECIMAL(5,2)
        END as time_efficiency,
        
        CASE 
            WHEN b.actual_quantity > 0 THEN
                (b.good_quantity / b.actual_quantity * 100.0)::DECIMAL(5,2)
            ELSE 100.0::DECIMAL(5,2)
        END as yield_percentage,
        
        CASE 
            WHEN b.planned_quantity > 0 THEN
                LEAST(100.0, (b.actual_quantity / b.planned_quantity * 100.0))::DECIMAL(5,2)
            ELSE 0.0::DECIMAL(5,2)
        END as completion_percentage,
        
        CASE 
            WHEN b.start_time IS NOT NULL THEN
                CASE 
                    WHEN b.completion_time IS NOT NULL THEN
                        (b.actual_quantity / NULLIF(EXTRACT(EPOCH FROM (b.completion_time - b.start_time)) / 3600.0, 0))::DECIMAL(10,4)
                    ELSE
                        (b.actual_quantity / NULLIF(EXTRACT(EPOCH FROM (NOW() - b.start_time)) / 3600.0, 0))::DECIMAL(10,4)
                END
            ELSE 0.0::DECIMAL(10,4)
        END as production_rate
        
    FROM batches b
    LEFT JOIN work_orders wo ON b.work_order_id = wo.work_order_id
    WHERE b.batch_id = batch_id_param;
END;
$$ LANGUAGE plpgsql;

-- Create view for batch summary information
CREATE OR REPLACE VIEW batch_summary_view AS
SELECT 
    b.batch_id,
    b.batch_number,
    b.work_order_id,
    b.product_id,
    b.status,
    b.planned_quantity,
    b.actual_quantity,
    b.good_quantity,
    b.defective_quantity,
    b.quality_score,
    b.start_time,
    b.completion_time,
    b.operator_id,
    b.shift_id,
    b.parent_batch_id,
    b.equipment_line_id,
    
    -- Calculated metrics
    CASE 
        WHEN b.planned_quantity > 0 THEN
            LEAST(100.0, (b.actual_quantity / b.planned_quantity * 100.0))
        ELSE 0.0
    END as completion_percentage,
    
    CASE 
        WHEN b.actual_quantity > 0 THEN
            (b.good_quantity / b.actual_quantity * 100.0)
        ELSE 100.0
    END as yield_percentage,
    
    CASE 
        WHEN b.start_time IS NOT NULL THEN
            CASE 
                WHEN b.completion_time IS NOT NULL THEN
                    EXTRACT(EPOCH FROM (b.completion_time - b.start_time)) / 3600.0
                ELSE
                    EXTRACT(EPOCH FROM (NOW() - b.start_time)) / 3600.0
            END
        ELSE 0.0
    END as duration_hours,
    
    -- Counts
    (SELECT COUNT(*) FROM batch_material_consumptions bmc WHERE bmc.batch_id = b.batch_id) as material_consumption_count,
    (SELECT COUNT(*) FROM batch_quality_checks bqc WHERE bqc.batch_id = b.batch_id AND bqc.result = 'Passed') as quality_checks_passed_count,
    (SELECT COUNT(*) FROM batch_quality_checks bqc WHERE bqc.batch_id = b.batch_id AND bqc.result = 'Failed') as quality_checks_failed_count,
    (SELECT COUNT(*) FROM batch_notes bn WHERE bn.batch_id = b.batch_id) as notes_count,
    
    b.created_at,
    b.updated_at
    
FROM batches b;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON batches TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON batch_material_consumptions TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON batch_quality_checks TO adam_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON batch_notes TO adam_user;
GRANT SELECT ON batch_summary_view TO adam_user;
GRANT EXECUTE ON FUNCTION calculate_batch_efficiency(VARCHAR) TO adam_user;

-- Insert sample batch tracking data for testing
INSERT INTO batches (
    batch_id, batch_number, work_order_id, product_id, planned_quantity,
    actual_quantity, good_quantity, defective_quantity, operator_id, equipment_line_id, status
) VALUES
    ('BATCH-WO-001-B001-20240818120000', 'B001', 'WO-001', 'WIDGET-A', 100.0, 95.0, 92.0, 3.0, 'OP001', 'LINE-001', 'Completed'),
    ('BATCH-WO-001-B002-20240818140000', 'B002', 'WO-001', 'WIDGET-A', 100.0, 87.0, 85.0, 2.0, 'OP001', 'LINE-001', 'InProgress'),
    ('BATCH-WO-002-B003-20240818160000', 'B003', 'WO-002', 'WIDGET-B', 200.0, 45.0, 43.0, 2.0, 'OP002', 'LINE-002', 'InProgress')
ON CONFLICT (batch_id) DO NOTHING;

-- Insert sample material consumptions
INSERT INTO batch_material_consumptions (
    batch_id, material_id, quantity_used, unit_of_measure
) VALUES
    ('BATCH-WO-001-B001-20240818120000', 'MAT-001', 50.0, 'kg'),
    ('BATCH-WO-001-B001-20240818120000', 'MAT-002', 25.0, 'liters'),
    ('BATCH-WO-001-B002-20240818140000', 'MAT-001', 45.0, 'kg'),
    ('BATCH-WO-002-B003-20240818160000', 'MAT-003', 100.0, 'pieces')
ON CONFLICT (consumption_id) DO NOTHING;

-- Insert sample quality checks
INSERT INTO batch_quality_checks (
    batch_id, check_type, result, operator_id, value, specification
) VALUES
    ('BATCH-WO-001-B001-20240818120000', 'Dimensional Check', 'Passed', 'QC001', 10.2, '10.0 ± 0.5'),
    ('BATCH-WO-001-B001-20240818120000', 'Visual Inspection', 'Passed', 'QC001', null, 'No visible defects'),
    ('BATCH-WO-001-B002-20240818140000', 'Dimensional Check', 'Failed', 'QC001', 11.2, '10.0 ± 0.5'),
    ('BATCH-WO-002-B003-20240818160000', 'Functional Test', 'Passed', 'QC002', null, 'All functions operational')
ON CONFLICT (quality_check_id) DO NOTHING;