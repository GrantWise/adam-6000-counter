-- Create stoppage_events table for stoppage classification
CREATE TABLE IF NOT EXISTS stoppage_events (
    event_id SERIAL PRIMARY KEY,
    device_id TEXT NOT NULL,
    job_id INTEGER REFERENCES production_jobs(job_id),
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    duration_minutes INTEGER,
    category TEXT,                 -- Mechanical, Electrical, etc.
    sub_category TEXT,             -- Jam, Wear, etc.
    comments TEXT,
    classified_at TIMESTAMPTZ,
    operator_id TEXT,
    status TEXT DEFAULT 'unclassified'  -- unclassified, classified
);

-- Create indexes for faster queries
CREATE INDEX IF NOT EXISTS idx_stoppage_events_status ON stoppage_events(status);
CREATE INDEX IF NOT EXISTS idx_stoppage_events_device_id ON stoppage_events(device_id);
CREATE INDEX IF NOT EXISTS idx_stoppage_events_job_id ON stoppage_events(job_id);
