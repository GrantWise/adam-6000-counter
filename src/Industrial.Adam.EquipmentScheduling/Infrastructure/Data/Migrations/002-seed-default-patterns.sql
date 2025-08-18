-- ================================================
-- Equipment Scheduling System - Seed Data
-- Migration 002: Insert default operating patterns
-- ================================================

SET search_path TO equipment_scheduling, public;

-- ================================================
-- Default Operating Patterns
-- ================================================

-- Insert standard operating patterns if they don't exist
INSERT INTO sched_operating_patterns (
    name, pattern_type, cycle_days, weekly_hours, configuration, description
) VALUES
-- Continuous Operation (24/7)
(
    'Continuous 24/7',
    'Continuous',
    7,
    168.00,
    '{
        "shifts": [
            {
                "code": "24HR",
                "name": "24 Hour Operation",
                "startTime": "00:00",
                "endTime": "23:59",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
            }
        ],
        "description": "Continuous 24 hours per day, 7 days per week operation"
    }'::jsonb,
    'Continuous operation 24 hours per day, 7 days per week'
),

-- Two Shift Operation (Monday-Friday)
(
    'Two Shift - Monday to Friday',
    'TwoShift',
    7,
    80.00,
    '{
        "shifts": [
            {
                "code": "DAY",
                "name": "Day Shift",
                "startTime": "06:00",
                "endTime": "14:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            },
            {
                "code": "EVE",
                "name": "Evening Shift",
                "startTime": "14:00",
                "endTime": "22:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            }
        ],
        "description": "Two 8-hour shifts Monday through Friday"
    }'::jsonb,
    'Two 8-hour shifts per day, Monday through Friday (16 hours/day, 5 days/week)'
),

-- Day Only Operation (Monday-Friday)
(
    'Day Only - Monday to Friday',
    'DayOnly',
    7,
    40.00,
    '{
        "shifts": [
            {
                "code": "DAY",
                "name": "Day Shift",
                "startTime": "08:00",
                "endTime": "16:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            }
        ],
        "description": "Single day shift Monday through Friday"
    }'::jsonb,
    'Single 8-hour day shift, Monday through Friday'
),

-- Extended Day Operation (Monday-Friday)
(
    'Extended Day - Monday to Friday',
    'Extended',
    7,
    60.00,
    '{
        "shifts": [
            {
                "code": "EXT",
                "name": "Extended Day Shift",
                "startTime": "06:00",
                "endTime": "18:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            }
        ],
        "description": "Extended 12-hour day shift Monday through Friday"
    }'::jsonb,
    'Single 12-hour extended day shift, Monday through Friday'
),

-- Weekend Operation (Saturday-Sunday)
(
    'Weekend Only',
    'Custom',
    7,
    32.00,
    '{
        "shifts": [
            {
                "code": "WKD",
                "name": "Weekend Shift",
                "startTime": "08:00",
                "endTime": "16:00",
                "operatingDays": ["Saturday", "Sunday"]
            }
        ],
        "description": "Weekend operation only - 8 hours Saturday and Sunday"
    }'::jsonb,
    'Weekend operation only - 8 hours per day on Saturday and Sunday'
),

-- Three Shift Continuous (Monday-Friday)
(
    'Three Shift - Monday to Friday',
    'Custom',
    7,
    120.00,
    '{
        "shifts": [
            {
                "code": "DAY",
                "name": "Day Shift",
                "startTime": "06:00",
                "endTime": "14:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            },
            {
                "code": "EVE",
                "name": "Evening Shift", 
                "startTime": "14:00",
                "endTime": "22:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            },
            {
                "code": "NIGHT",
                "name": "Night Shift",
                "startTime": "22:00",
                "endTime": "06:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            }
        ],
        "description": "Three 8-hour shifts Monday through Friday"
    }'::jsonb,
    'Three 8-hour shifts per day, Monday through Friday (24 hours/day, 5 days/week)'
),

-- Four Day Work Week (10-hour days)
(
    'Four Day Work Week',
    'Custom',
    7,
    40.00,
    '{
        "shifts": [
            {
                "code": "4DAY",
                "name": "Four Day Shift",
                "startTime": "07:00",
                "endTime": "17:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday"]
            }
        ],
        "description": "Four 10-hour days per week"
    }'::jsonb,
    'Four 10-hour days per week (Monday through Thursday)'
),

-- Maintenance Pattern (Reduced Hours)
(
    'Maintenance Mode',
    'Custom',
    7,
    20.00,
    '{
        "shifts": [
            {
                "code": "MAINT",
                "name": "Maintenance Window",
                "startTime": "09:00",
                "endTime": "13:00",
                "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
            }
        ],
        "description": "Reduced operation for maintenance activities"
    }'::jsonb,
    'Reduced 4-hour operation per day for maintenance and service activities'
)

ON CONFLICT (name) DO NOTHING;

-- ================================================
-- Sample Resource Hierarchy
-- ================================================

-- Insert sample enterprise-level resource
INSERT INTO sched_resources (
    name, code, resource_type, hierarchy_path, requires_scheduling, description
) VALUES (
    'Industrial Manufacturing Company',
    'IMC-001',
    'Enterprise',
    '/1/',
    false,
    'Top-level enterprise resource for Industrial Manufacturing Company'
) ON CONFLICT (code) DO NOTHING;

-- Get the enterprise ID for hierarchy building
DO $$
DECLARE
    enterprise_id BIGINT;
    site_id BIGINT;
    area_id BIGINT;
    workcenter_id BIGINT;
BEGIN
    -- Get enterprise ID
    SELECT id INTO enterprise_id FROM sched_resources WHERE code = 'IMC-001';
    
    IF enterprise_id IS NOT NULL THEN
        -- Insert sample site
        INSERT INTO sched_resources (
            name, code, resource_type, parent_id, hierarchy_path, requires_scheduling, description
        ) VALUES (
            'Main Manufacturing Site',
            'SITE-001',
            'Site',
            enterprise_id,
            '/1/' || enterprise_id + 1 || '/',
            false,
            'Primary manufacturing facility'
        ) ON CONFLICT (code) DO NOTHING;
        
        -- Get site ID
        SELECT id INTO site_id FROM sched_resources WHERE code = 'SITE-001';
        
        IF site_id IS NOT NULL THEN
            -- Insert sample area
            INSERT INTO sched_resources (
                name, code, resource_type, parent_id, hierarchy_path, requires_scheduling, description
            ) VALUES (
                'Production Area A',
                'AREA-001',
                'Area',
                site_id,
                '/1/' || enterprise_id || '/' || site_id || '/',
                false,
                'Main production area for assembly operations'
            ) ON CONFLICT (code) DO NOTHING;
            
            -- Get area ID
            SELECT id INTO area_id FROM sched_resources WHERE code = 'AREA-001';
            
            IF area_id IS NOT NULL THEN
                -- Insert sample work center
                INSERT INTO sched_resources (
                    name, code, resource_type, parent_id, hierarchy_path, requires_scheduling, description
                ) VALUES (
                    'Assembly Line 1',
                    'WC-001',
                    'WorkCenter',
                    area_id,
                    '/1/' || enterprise_id || '/' || site_id || '/' || area_id || '/',
                    true,
                    'Primary assembly line for product manufacturing'
                ) ON CONFLICT (code) DO NOTHING;
                
                -- Get work center ID
                SELECT id INTO workcenter_id FROM sched_resources WHERE code = 'WC-001';
                
                IF workcenter_id IS NOT NULL THEN
                    -- Insert sample work units
                    INSERT INTO sched_resources (
                        name, code, resource_type, parent_id, hierarchy_path, requires_scheduling, description
                    ) VALUES 
                    (
                        'Assembly Station 1A',
                        'WU-001A',
                        'WorkUnit',
                        workcenter_id,
                        '/1/' || enterprise_id || '/' || site_id || '/' || area_id || '/' || workcenter_id || '/',
                        true,
                        'Individual assembly station for component installation'
                    ),
                    (
                        'Assembly Station 1B',
                        'WU-001B',
                        'WorkUnit',
                        workcenter_id,
                        '/1/' || enterprise_id || '/' || site_id || '/' || area_id || '/' || workcenter_id || '/',
                        true,
                        'Individual assembly station for component installation'
                    ),
                    (
                        'Quality Check Station',
                        'WU-001Q',
                        'WorkUnit',
                        workcenter_id,
                        '/1/' || enterprise_id || '/' || site_id || '/' || area_id || '/' || workcenter_id || '/',
                        true,
                        'Quality inspection and testing station'
                    )
                    ON CONFLICT (code) DO NOTHING;
                END IF;
            END IF;
        END IF;
    END IF;
END $$;

-- ================================================
-- Sample Pattern Assignments
-- ================================================

-- Assign default patterns to work units that require scheduling
INSERT INTO sched_pattern_assignments (
    resource_id,
    pattern_id,
    effective_date,
    is_override,
    assigned_by,
    notes
)
SELECT 
    r.id,
    p.id,
    CURRENT_DATE,
    false,
    'System',
    'Initial default pattern assignment for ' || r.name
FROM sched_resources r
CROSS JOIN sched_operating_patterns p
WHERE r.requires_scheduling = true
  AND r.resource_type = 'WorkUnit'
  AND p.name = 'Two Shift - Monday to Friday'
  AND NOT EXISTS (
      SELECT 1 FROM sched_pattern_assignments pa 
      WHERE pa.resource_id = r.id
  );

-- ================================================
-- Update Resource Hierarchy Paths
-- ================================================

-- Function to rebuild hierarchy paths
CREATE OR REPLACE FUNCTION equipment_scheduling.rebuild_hierarchy_paths()
RETURNS void AS $$
DECLARE
    resource_record RECORD;
BEGIN
    -- Update hierarchy paths for all resources
    FOR resource_record IN 
        SELECT id, parent_id 
        FROM sched_resources 
        ORDER BY COALESCE(parent_id, 0), id
    LOOP
        IF resource_record.parent_id IS NULL THEN
            -- Root level resource
            UPDATE sched_resources 
            SET hierarchy_path = '/' || resource_record.id || '/'
            WHERE id = resource_record.id;
        ELSE
            -- Child resource - build path from parent
            UPDATE sched_resources 
            SET hierarchy_path = (
                SELECT hierarchy_path || resource_record.id || '/'
                FROM sched_resources 
                WHERE id = resource_record.parent_id
            )
            WHERE id = resource_record.id;
        END IF;
    END LOOP;
    
    RAISE NOTICE 'Hierarchy paths rebuilt for all resources';
END;
$$ LANGUAGE plpgsql;

-- Execute hierarchy path rebuild
SELECT equipment_scheduling.rebuild_hierarchy_paths();

-- ================================================
-- Data Validation and Summary
-- ================================================

-- Display summary of seeded data
DO $$
DECLARE
    pattern_count INTEGER;
    resource_count INTEGER;
    assignment_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO pattern_count FROM sched_operating_patterns;
    SELECT COUNT(*) INTO resource_count FROM sched_resources;
    SELECT COUNT(*) INTO assignment_count FROM sched_pattern_assignments;
    
    RAISE NOTICE 'Equipment Scheduling seed data completed:';
    RAISE NOTICE '  Operating Patterns: %', pattern_count;
    RAISE NOTICE '  Resources: %', resource_count;
    RAISE NOTICE '  Pattern Assignments: %', assignment_count;
END $$;

-- Reset search path
RESET search_path;

RAISE NOTICE 'Equipment Scheduling seed data migration completed successfully';