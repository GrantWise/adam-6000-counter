-- 005-seed-oee-phase1-data.sql
-- Phase 1 OEE Implementation: Initial data seeding
-- Seeds standard reason codes (3x3 matrix) and sample equipment configuration

-- Insert default stoppage reason categories (Level 1: 3x3 matrix)
-- Row 1: Equipment-related issues
INSERT INTO stoppage_reason_categories (category_code, category_name, category_description, matrix_row, matrix_col, is_active, created_at, updated_at) VALUES
('A1', 'Mechanical Failure', 'Equipment mechanical problems and breakdowns', 1, 1, true, NOW(), NOW()),
('A2', 'Electrical Issues', 'Electrical faults, power problems, controls', 1, 2, true, NOW(), NOW()),
('A3', 'Tooling Problems', 'Tool wear, breakage, adjustment issues', 1, 3, true, NOW(), NOW());

-- Row 2: Process-related issues  
INSERT INTO stoppage_reason_categories (category_code, category_name, category_description, matrix_row, matrix_col, is_active, created_at, updated_at) VALUES
('B1', 'Material Issues', 'Raw material problems, shortages, quality', 2, 1, true, NOW(), NOW()),
('B2', 'Process Setup', 'Job changeover, setup, adjustment time', 2, 2, true, NOW(), NOW()),
('B3', 'Quality Problems', 'Quality issues requiring process adjustment', 2, 3, true, NOW(), NOW());

-- Row 3: External factors
INSERT INTO stoppage_reason_categories (category_code, category_name, category_description, matrix_row, matrix_col, is_active, created_at, updated_at) VALUES
('C1', 'Operator Issues', 'Training, availability, procedure issues', 3, 1, true, NOW(), NOW()),
('C2', 'Planned Downtime', 'Scheduled maintenance, breaks, meetings', 3, 2, true, NOW(), NOW()),
('C3', 'External Factors', 'Utilities, upstream/downstream issues', 3, 3, true, NOW(), NOW())
ON CONFLICT (category_code) DO NOTHING;

-- Insert subcodes for A1 - Mechanical Failure
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'A1', '1', 'Motor Failure', 'Drive motor or servo motor issues', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '2', 'Bearing Problems', 'Bearing wear, noise, or seizure', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '3', 'Belt/Chain Issues', 'Drive belt or chain problems', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '4', 'Hydraulic Issues', 'Hydraulic system problems', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '5', 'Pneumatic Issues', 'Air system or cylinder problems', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '6', 'Gearbox Problems', 'Gearbox or transmission issues', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '7', 'Structural Issues', 'Frame, mounting, alignment problems', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '8', 'Safety System', 'Safety guard or interlock activation', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1'
UNION ALL
SELECT id, 'A1', '9', 'Other Mechanical', 'Other mechanical issues not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A1';

-- Insert subcodes for A2 - Electrical Issues
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'A2', '1', 'Power Supply', 'Main power supply issues', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '2', 'Control System', 'PLC or control system faults', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '3', 'Sensor Problems', 'Sensor malfunction or misalignment', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '4', 'Wiring Issues', 'Loose connections, damaged wires', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '5', 'Motor Drive', 'Variable frequency drive problems', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '6', 'HMI Problems', 'Human machine interface issues', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '7', 'Communication', 'Network or communication faults', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '8', 'Instrumentation', 'Measurement device problems', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2'
UNION ALL
SELECT id, 'A2', '9', 'Other Electrical', 'Other electrical issues not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A2';

-- Insert subcodes for A3 - Tooling Problems
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'A3', '1', 'Tool Wear', 'Excessive tool wear requiring replacement', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '2', 'Tool Breakage', 'Tool breakage or damage', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '3', 'Tool Adjustment', 'Tool position or setting adjustment', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '4', 'Die/Mold Issues', 'Die, mold, or fixture problems', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '5', 'Cutting Tools', 'Cutting tool specific issues', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '6', 'Tool Change', 'Time for scheduled tool change', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '7', 'Jigs/Fixtures', 'Work holding fixture problems', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '8', 'Tool Setup', 'Initial tool setup or installation', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3'
UNION ALL
SELECT id, 'A3', '9', 'Other Tooling', 'Other tooling issues not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'A3';

-- Insert subcodes for B1 - Material Issues  
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'B1', '1', 'Material Shortage', 'Raw material not available', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '2', 'Material Quality', 'Material quality issues or defects', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '3', 'Wrong Material', 'Incorrect material delivered/loaded', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '4', 'Material Handling', 'Loading, unloading, transport issues', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '5', 'Material Prep', 'Material preparation or conditioning', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '6', 'Consumables', 'Consumable supplies shortage', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '7', 'Material Jam', 'Material feed jam or blockage', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '8', 'Storage Issues', 'Material storage or inventory problems', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1'
UNION ALL
SELECT id, 'B1', '9', 'Other Material', 'Other material issues not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B1';

-- Insert subcodes for B2 - Process Setup
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'B2', '1', 'Job Changeover', 'Product changeover time', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '2', 'Machine Setup', 'Machine parameter setup/adjustment', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '3', 'Program Change', 'Control program changes', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '4', 'First Article', 'First piece inspection and approval', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '5', 'Warm Up', 'Machine or process warm-up time', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '6', 'Calibration', 'Equipment calibration or verification', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '7', 'Documentation', 'Setup documentation or work instructions', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '8', 'Trial Run', 'Process trial or test run', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2'
UNION ALL
SELECT id, 'B2', '9', 'Other Setup', 'Other setup activities not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B2';

-- Insert subcodes for B3 - Quality Problems
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'B3', '1', 'Dimensional Issues', 'Size or dimension out of specification', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '2', 'Surface Defects', 'Surface finish or appearance issues', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '3', 'Function Test Fail', 'Functional testing failures', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '4', 'Contamination', 'Product contamination issues', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '5', 'Assembly Issues', 'Assembly or fit problems', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '6', 'Inspection Hold', 'Quality inspection hold', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '7', 'Process Drift', 'Process parameter drift correction', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '8', 'Rework Required', 'Rework or repair required', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3'
UNION ALL
SELECT id, 'B3', '9', 'Other Quality', 'Other quality issues not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'B3';

-- Insert subcodes for C1 - Operator Issues
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'C1', '1', 'No Operator', 'Operator not available', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '2', 'Training', 'Operator training or learning', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '3', 'Procedure Issue', 'Work instruction or procedure problem', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '4', 'Safety Issue', 'Safety concern or incident', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '5', 'Operator Error', 'Operator mistake or incorrect action', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '6', 'Coordination', 'Coordination with other operations', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '7', 'Personal Time', 'Personal break or time off', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '8', 'Administrative', 'Paperwork or administrative tasks', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1'
UNION ALL
SELECT id, 'C1', '9', 'Other Operator', 'Other operator related issues', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C1';

-- Insert subcodes for C2 - Planned Downtime
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'C2', '1', 'Scheduled Maintenance', 'Planned preventive maintenance', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '2', 'Break Time', 'Scheduled break or meal period', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '3', 'Shift Change', 'Shift changeover time', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '4', 'Meeting/Training', 'Scheduled meetings or training', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '5', 'Cleaning', 'Scheduled equipment cleaning', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '6', 'No Work Order', 'No production scheduled', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '7', 'End of Shift', 'End of production shift', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '8', 'Planned Shutdown', 'Planned equipment shutdown', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2'
UNION ALL
SELECT id, 'C2', '9', 'Other Planned', 'Other planned downtime activities', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C2';

-- Insert subcodes for C3 - External Factors
INSERT INTO stoppage_reason_subcodes (category_id, category_code, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active, created_at, updated_at)
SELECT id, 'C3', '1', 'Power Outage', 'Facility power outage', 1, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '2', 'Utility Issues', 'Compressed air, water, gas issues', 1, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '3', 'HVAC Problems', 'Heating, ventilation, cooling issues', 1, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '4', 'Upstream Delay', 'Waiting for upstream process', 2, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '5', 'Downstream Block', 'Downstream process bottleneck', 2, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '6', 'Facility Issues', 'Building or facility problems', 2, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '7', 'IT/Network Issues', 'Information technology problems', 3, 1, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '8', 'Environmental', 'Weather or environmental factors', 3, 2, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3'
UNION ALL
SELECT id, 'C3', '9', 'Other External', 'Other external factors not listed', 3, 3, true, NOW(), NOW() FROM stoppage_reason_categories WHERE category_code = 'C3';

-- Insert sample equipment lines
INSERT INTO equipment_lines (line_id, line_name, adam_device_id, adam_channel, is_active, created_at, updated_at) VALUES
('LINE001', 'Production Line 1', 'ADAM-001', 0, true, NOW(), NOW()),
('LINE002', 'Production Line 2', 'ADAM-001', 1, true, NOW(), NOW()),
('LINE003', 'Assembly Line A', 'ADAM-002', 0, true, NOW(), NOW()),
('LINE004', 'Assembly Line B', 'ADAM-002', 1, true, NOW(), NOW()),
('LINE005', 'Packaging Line 1', 'ADAM-003', 0, true, NOW(), NOW())
ON CONFLICT (line_id) DO NOTHING;

-- Add comments explaining the reason code structure
COMMENT ON TABLE stoppage_reason_categories IS 'Standard 3x3 matrix reason code categories for intuitive operator selection';
COMMENT ON TABLE stoppage_reason_subcodes IS 'Detailed reason subcodes providing specific classification within each category (9 per category)';

-- Verify the seeded data
DO $$
DECLARE
    category_count INTEGER;
    subcode_count INTEGER;
    equipment_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO category_count FROM stoppage_reason_categories WHERE is_active = true;
    SELECT COUNT(*) INTO subcode_count FROM stoppage_reason_subcodes WHERE is_active = true;
    SELECT COUNT(*) INTO equipment_count FROM equipment_lines WHERE is_active = true;
    
    RAISE NOTICE 'Data seeding completed:';
    RAISE NOTICE '  - Categories: % (expected: 9)', category_count;
    RAISE NOTICE '  - Subcodes: % (expected: 81)', subcode_count;
    RAISE NOTICE '  - Equipment Lines: %', equipment_count;
    
    IF category_count != 9 THEN
        RAISE WARNING 'Expected 9 categories but found %', category_count;
    END IF;
    
    IF subcode_count != 81 THEN
        RAISE WARNING 'Expected 81 subcodes but found %', subcode_count;
    END IF;
END $$;