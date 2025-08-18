-- Create production_jobs table for job management
CREATE TABLE IF NOT EXISTS production_jobs (
    job_id SERIAL PRIMARY KEY,
    job_number TEXT NOT NULL,
    part_number TEXT NOT NULL,
    device_id TEXT NOT NULL,
    target_rate DOUBLE PRECISION,  -- units/minute
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    operator_id TEXT,
    status TEXT DEFAULT 'active'   -- active, completed, cancelled
);

-- Create index for faster queries
CREATE INDEX IF NOT EXISTS idx_production_jobs_status ON production_jobs(status);
CREATE INDEX IF NOT EXISTS idx_production_jobs_device_id ON production_jobs(device_id);
