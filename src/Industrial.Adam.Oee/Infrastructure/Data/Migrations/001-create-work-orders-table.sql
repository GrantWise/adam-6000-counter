-- 001-create-work-orders-table.sql
-- OEE Work Orders Table - Provides business context overlay on immutable counter data
-- CRITICAL: This does NOT modify the existing counter_data table from Industrial.Adam.Logger

-- Create work_orders table for OEE business context
CREATE TABLE IF NOT EXISTS work_orders (
    work_order_id VARCHAR(50) PRIMARY KEY,           -- Work order identifier (business key)
    work_order_description TEXT NOT NULL,            -- Work order description
    product_id VARCHAR(50) NOT NULL,                 -- Product identifier
    product_description TEXT NOT NULL,               -- Product description
    planned_quantity DECIMAL(18,3) NOT NULL,         -- Planned quantity to produce
    unit_of_measure VARCHAR(20) NOT NULL DEFAULT 'pieces', -- Unit of measure
    scheduled_start_time TIMESTAMPTZ NOT NULL,       -- Scheduled start time
    scheduled_end_time TIMESTAMPTZ NOT NULL,         -- Scheduled end time
    resource_reference VARCHAR(50) NOT NULL,         -- Device/machine identifier (maps to device_id in counter_data)
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',   -- WorkOrderStatus enum value
    actual_quantity_good DECIMAL(18,3) NOT NULL DEFAULT 0,    -- Good pieces produced
    actual_quantity_scrap DECIMAL(18,3) NOT NULL DEFAULT 0,   -- Scrap/reject pieces
    actual_start_time TIMESTAMPTZ,                   -- When work actually started
    actual_end_time TIMESTAMPTZ,                     -- When work actually ended
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- Record creation timestamp
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- Last update timestamp
    
    -- Constraints
    CONSTRAINT work_orders_planned_quantity_positive CHECK (planned_quantity > 0),
    CONSTRAINT work_orders_actual_quantities_non_negative CHECK (
        actual_quantity_good >= 0 AND actual_quantity_scrap >= 0
    ),
    CONSTRAINT work_orders_scheduled_times_valid CHECK (scheduled_end_time > scheduled_start_time),
    CONSTRAINT work_orders_actual_times_valid CHECK (
        actual_end_time IS NULL OR actual_start_time IS NULL OR actual_end_time >= actual_start_time
    ),
    CONSTRAINT work_orders_status_valid CHECK (
        status IN ('Pending', 'Active', 'Paused', 'Completed', 'Cancelled')
    )
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_work_orders_resource_status 
ON work_orders(resource_reference, status) 
WHERE status IN ('Active', 'Paused');

CREATE INDEX IF NOT EXISTS idx_work_orders_scheduled_times 
ON work_orders(scheduled_start_time, scheduled_end_time);

CREATE INDEX IF NOT EXISTS idx_work_orders_created_at 
ON work_orders(created_at);

CREATE INDEX IF NOT EXISTS idx_work_orders_status_updated 
ON work_orders(status, updated_at);

-- Add comment explaining the purpose
COMMENT ON TABLE work_orders IS 
'OEE work orders table that provides business context to overlay on immutable counter_data from Industrial.Adam.Logger. This table does NOT modify existing counter data.';

COMMENT ON COLUMN work_orders.resource_reference IS 
'Maps to device_id in the counter_data table to link business context with time series data';

COMMENT ON COLUMN work_orders.actual_quantity_good IS 
'Derived from channel 0 (production channel) counter readings';

COMMENT ON COLUMN work_orders.actual_quantity_scrap IS 
'Derived from channel 1 (reject channel) counter readings';