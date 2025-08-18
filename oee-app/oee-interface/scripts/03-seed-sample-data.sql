-- Insert sample production job (matches the default job in the UI)
INSERT INTO production_jobs (
    job_number, 
    part_number, 
    device_id, 
    target_rate, 
    start_time, 
    operator_id, 
    status
) VALUES (
    '#12345',
    'Widget A',
    'device_001',
    120,
    NOW() - INTERVAL '3 hours 45 minutes',
    'operator_001',
    'active'
) ON CONFLICT DO NOTHING;

-- Insert sample stoppage event (unclassified)
INSERT INTO stoppage_events (
    device_id,
    job_id,
    start_time,
    end_time,
    duration_minutes,
    status
) VALUES (
    'device_001',
    1,
    NOW() - INTERVAL '8 minutes',
    NOW(),
    8,
    'unclassified'
) ON CONFLICT DO NOTHING;
