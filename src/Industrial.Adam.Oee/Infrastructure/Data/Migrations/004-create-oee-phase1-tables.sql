-- 004-create-oee-phase1-tables.sql
-- Phase 1 OEE Implementation: Core business rules and data model
-- Creates tables for equipment configuration, reason codes, stoppages, and job completion issues

-- Equipment Lines Configuration
-- Maps ADAM devices to physical production lines (1:1 relationship)
CREATE TABLE IF NOT EXISTS equipment_lines (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(50) UNIQUE NOT NULL,
    line_name VARCHAR(100) NOT NULL,
    adam_device_id VARCHAR(50) NOT NULL,
    adam_channel INTEGER NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT equipment_lines_line_id_not_empty CHECK (line_id != ''),
    CONSTRAINT equipment_lines_line_name_not_empty CHECK (line_name != ''),
    CONSTRAINT equipment_lines_adam_device_not_empty CHECK (adam_device_id != ''),
    CONSTRAINT equipment_lines_adam_channel_valid CHECK (adam_channel >= 0 AND adam_channel <= 15),
    UNIQUE(adam_device_id, adam_channel)
);

-- Stoppage Reason Categories (Level 1: 3x3 Matrix)
-- Nine high-level categories arranged in a 3x3 matrix layout
CREATE TABLE IF NOT EXISTS stoppage_reason_categories (
    id SERIAL PRIMARY KEY,
    category_code VARCHAR(10) UNIQUE NOT NULL, -- A1, A2, A3, B1, B2, B3, C1, C2, C3
    category_name VARCHAR(100) NOT NULL,
    category_description TEXT,
    matrix_row INTEGER NOT NULL CHECK (matrix_row BETWEEN 1 AND 3),
    matrix_col INTEGER NOT NULL CHECK (matrix_col BETWEEN 1 AND 3),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT stoppage_categories_code_not_empty CHECK (category_code != ''),
    CONSTRAINT stoppage_categories_name_not_empty CHECK (category_name != ''),
    UNIQUE(matrix_row, matrix_col)
);

-- Stoppage Reason Subcodes (Level 2: 9 per Category)
-- Specific reasons within each category, also arranged in 3x3 matrix
CREATE TABLE IF NOT EXISTS stoppage_reason_subcodes (
    id SERIAL PRIMARY KEY,
    category_id INTEGER NOT NULL REFERENCES stoppage_reason_categories(id) ON DELETE CASCADE,
    subcode VARCHAR(10) NOT NULL, -- 1, 2, 3, 4, 5, 6, 7, 8, 9
    subcode_name VARCHAR(100) NOT NULL,
    subcode_description TEXT,
    matrix_row INTEGER NOT NULL CHECK (matrix_row BETWEEN 1 AND 3),
    matrix_col INTEGER NOT NULL CHECK (matrix_col BETWEEN 1 AND 3),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT stoppage_subcodes_code_not_empty CHECK (subcode != ''),
    CONSTRAINT stoppage_subcodes_name_not_empty CHECK (subcode_name != ''),
    UNIQUE(category_id, subcode),
    UNIQUE(category_id, matrix_row, matrix_col)
);

-- Enhanced Stoppages Table
-- Real-time stoppage tracking with classification and audit trail
CREATE TABLE IF NOT EXISTS equipment_stoppages (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(50) NOT NULL REFERENCES equipment_lines(line_id) ON DELETE CASCADE,
    work_order_id VARCHAR(100) REFERENCES work_orders(work_order_id) ON DELETE SET NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    duration_minutes DECIMAL(10,2),
    is_classified BOOLEAN DEFAULT false,
    category_code VARCHAR(10) REFERENCES stoppage_reason_categories(category_code) ON DELETE SET NULL,
    subcode VARCHAR(10),
    operator_comments TEXT,
    classified_by VARCHAR(100),
    classified_at TIMESTAMPTZ,
    auto_detected BOOLEAN DEFAULT true,
    minimum_threshold_minutes INTEGER DEFAULT 5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT equipment_stoppages_times_valid CHECK (
        end_time IS NULL OR end_time > start_time
    ),
    CONSTRAINT equipment_stoppages_duration_positive CHECK (
        duration_minutes IS NULL OR duration_minutes >= 0
    ),
    CONSTRAINT equipment_stoppages_threshold_positive CHECK (
        minimum_threshold_minutes > 0
    ),
    CONSTRAINT equipment_stoppages_classification_complete CHECK (
        (is_classified = false) OR 
        (is_classified = true AND category_code IS NOT NULL AND classified_by IS NOT NULL AND classified_at IS NOT NULL)
    )
);

-- Job Completion Issues
-- Tracks under-completion and overproduction problems requiring operator resolution
CREATE TABLE IF NOT EXISTS job_completion_issues (
    id SERIAL PRIMARY KEY,
    work_order_id VARCHAR(100) NOT NULL REFERENCES work_orders(work_order_id) ON DELETE CASCADE,
    issue_type VARCHAR(50) NOT NULL,
    completion_percentage DECIMAL(5,2),
    target_quantity DECIMAL(10,2) NOT NULL,
    actual_quantity DECIMAL(10,2) NOT NULL,
    category_code VARCHAR(10) REFERENCES stoppage_reason_categories(category_code) ON DELETE SET NULL,
    subcode VARCHAR(10),
    operator_comments TEXT,
    resolved_by VARCHAR(100),
    resolved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT job_completion_issue_type_valid CHECK (
        issue_type IN ('UNDER_COMPLETION', 'OVERPRODUCTION', 'QUALITY_ISSUE')
    ),
    CONSTRAINT job_completion_percentage_valid CHECK (
        completion_percentage IS NULL OR (completion_percentage >= 0 AND completion_percentage <= 200)
    ),
    CONSTRAINT job_completion_quantities_positive CHECK (
        target_quantity >= 0 AND actual_quantity >= 0
    ),
    CONSTRAINT job_completion_resolution_complete CHECK (
        (resolved_by IS NULL AND resolved_at IS NULL) OR 
        (resolved_by IS NOT NULL AND resolved_at IS NOT NULL)
    )
);

-- Performance Indexes
CREATE INDEX IF NOT EXISTS idx_equipment_lines_adam_device 
ON equipment_lines(adam_device_id, adam_channel) 
WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_equipment_lines_active 
ON equipment_lines(is_active, line_id) 
WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_stoppage_categories_matrix 
ON stoppage_reason_categories(matrix_row, matrix_col) 
WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_stoppage_subcodes_category_matrix 
ON stoppage_reason_subcodes(category_id, matrix_row, matrix_col) 
WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_equipment_stoppages_line_time 
ON equipment_stoppages(line_id, start_time, end_time);

CREATE INDEX IF NOT EXISTS idx_equipment_stoppages_work_order 
ON equipment_stoppages(work_order_id, start_time) 
WHERE work_order_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_equipment_stoppages_unclassified 
ON equipment_stoppages(line_id, created_at) 
WHERE is_classified = false;

CREATE INDEX IF NOT EXISTS idx_equipment_stoppages_classification 
ON equipment_stoppages(category_code, subcode, classified_at) 
WHERE is_classified = true;

CREATE INDEX IF NOT EXISTS idx_job_completion_issues_work_order 
ON job_completion_issues(work_order_id, issue_type);

CREATE INDEX IF NOT EXISTS idx_job_completion_issues_unresolved 
ON job_completion_issues(created_at, issue_type) 
WHERE resolved_by IS NULL;

-- Foreign Key Constraint for subcode validation
-- Ensures subcode exists for the specified category
ALTER TABLE equipment_stoppages 
ADD CONSTRAINT fk_equipment_stoppages_subcode 
FOREIGN KEY (category_code, subcode) 
REFERENCES stoppage_reason_subcodes(category_code, subcode) 
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE job_completion_issues 
ADD CONSTRAINT fk_job_completion_issues_subcode 
FOREIGN KEY (category_code, subcode) 
REFERENCES stoppage_reason_subcodes(category_code, subcode) 
DEFERRABLE INITIALLY DEFERRED;

-- Add missing category_code column to stoppage_reason_subcodes table
-- (This creates a computed column that matches the parent category's code)
ALTER TABLE stoppage_reason_subcodes 
ADD COLUMN category_code VARCHAR(10);

-- Update the category_code from the parent table
UPDATE stoppage_reason_subcodes 
SET category_code = (
    SELECT src.category_code 
    FROM stoppage_reason_categories src 
    WHERE src.id = stoppage_reason_subcodes.category_id
);

-- Make the category_code column not null and add foreign key constraint
ALTER TABLE stoppage_reason_subcodes 
ALTER COLUMN category_code SET NOT NULL,
ADD CONSTRAINT fk_stoppage_subcodes_category_code 
FOREIGN KEY (category_code) REFERENCES stoppage_reason_categories(category_code) ON DELETE CASCADE;

-- Create unique constraint for category_code + subcode combination
ALTER TABLE stoppage_reason_subcodes 
ADD CONSTRAINT uk_stoppage_subcodes_category_subcode 
UNIQUE(category_code, subcode);

-- Comments for documentation
COMMENT ON TABLE equipment_lines IS 
'Maps ADAM devices to physical production lines with 1:1 relationship for counter data attribution';

COMMENT ON TABLE stoppage_reason_categories IS 
'Level 1 reason codes arranged in 3x3 matrix for intuitive operator selection';

COMMENT ON TABLE stoppage_reason_subcodes IS 
'Level 2 reason codes providing specific reasons within each category (9 per category)';

COMMENT ON TABLE equipment_stoppages IS 
'Real-time stoppage tracking with automatic detection and manual classification workflow';

COMMENT ON TABLE job_completion_issues IS 
'Tracks job completion problems requiring operator input and resolution';

COMMENT ON COLUMN equipment_lines.adam_device_id IS 
'ADAM device identifier that provides counter data for this production line';

COMMENT ON COLUMN equipment_lines.adam_channel IS 
'ADAM channel number (0-15) used for production counting on this line';

COMMENT ON COLUMN equipment_stoppages.auto_detected IS 
'True if stoppage was automatically detected by monitoring system, false if manually entered';

COMMENT ON COLUMN equipment_stoppages.minimum_threshold_minutes IS 
'Minimum duration (minutes) before stoppage requires classification';

COMMENT ON COLUMN job_completion_issues.completion_percentage IS 
'Calculated completion percentage: (actual_quantity / target_quantity) * 100';