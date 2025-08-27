# Canonical Manufacturing Model - Object Catalog (Enhanced)

## Overview

This catalog defines all objects in the canonical manufacturing model, organized by logical dependency and functional area. Each object includes its attributes, transaction effects, and state transitions where applicable.

**New in this version:**
- Environmental Monitoring objects for complete quality compliance
- Specification and Compliance objects for flexible quality management  
- Enhanced Product Family hierarchy
- Progressive implementation patterns showing how objects can start simple and grow sophisticated

---

## Object Organization

### 1. SYSTEM FOUNDATION OBJECTS
Core objects that other objects depend on (UOM, Validation Rules)

### 2. BUSINESS PARTNER OBJECTS  
External entities (Customers, Suppliers)

### 3. PRODUCT HIERARCHY OBJECTS
Product families, products, and their relationships

### 4. SPECIFICATION & COMPLIANCE OBJECTS
Quality specifications and their applications

### 5. ENGINEERING MASTER DATA
BOMs, Routes, and manufacturing definitions

### 6. INVENTORY & MATERIAL OBJECTS
Items, lots, serials, and inventory records

### 7. CONTAINER & LOCATION OBJECTS
Physical and logical locations, handling units

### 8. RESOURCE OBJECTS
Equipment, personnel, and capabilities

### 9. ORDER OBJECTS
Sales, purchase, and transfer orders

### 10. PRODUCTION EXECUTION OBJECTS
Work orders and production tracking

### 11. LABOR TRACKING OBJECTS
Personnel assignments and time tracking

### 12. MAINTENANCE OBJECTS
Maintenance planning and execution

### 13. SHOP FLOOR CONTROL OBJECTS
Real-time equipment and production monitoring

### 14. INVENTORY CONTROL OBJECTS
Cycle counting and inventory management

### 15. QUALITY OBJECTS
Inspections and non-conformances

### 16. ENVIRONMENTAL MONITORING OBJECTS
Environmental conditions and compliance

### 17. TRANSACTION & AUDIT OBJECTS
Event logging and audit trail

---

## 1. SYSTEM FOUNDATION OBJECTS

### UOM Type
**Purpose**: Define categories of units of measure (mass, length, volume, etc.)

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| uom_type_id | **CREATE**: New measurement type |  |
| type_name (mass, length, volume, etc.) | **UPDATE**: Rarely updated |  |
| base_unit_code | **DELETE**: Not allowed |  |
| base_unit_name |  |  |
| base_unit_symbol |  |  |
| status | **UPDATE**: Status changes | Active → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

### UOM (Unit of Measure)
**Purpose**: Define specific units and their conversions

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| uom_code | **CREATE**: New unit definition |  |
| uom_name | **UPDATE**: Description changes only |  |
| uom_symbol | **DELETE**: Not allowed if used |  |
| uom_type_reference: {type: "uom_type", id: "..."} |  |  |
| conversion_factor_to_base |  |  |
| is_base_unit |  |  |
| status | **UPDATE**: Status changes | Active → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

**Real-World Example**: 
- UOM Type: "mass"
- Base Unit: "kilogram" (kg)
- Other Units: "pound" (lb, factor: 0.453592), "ton" (t, factor: 1000)

### Validation Rule
**Purpose**: Configurable business rules that enforce data quality and business logic

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| rule_id | **CREATE**: New validation |  |
| rule_name | **UPDATE**: Logic changes |  |
| applies_to_object | **DELETE**: Deactivate only |  |
| trigger_event (create/update/delete) |  |  |
| validation_expression |  |  |
| error_message |  |  |
| severity (error/warning) |  |  |
| is_active | **UPDATE**: Status changes | Draft → Active → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

**Starting Simple**:
```json
{
  "rule_id": "VAL-001",
  "rule_name": "No negative inventory",
  "applies_to_object": "inventory_record",
  "validation_expression": "quantity_on_hand >= 0",
  "error_message": "Inventory cannot be negative"
}
```

---

## 2. BUSINESS PARTNER OBJECTS

### Customer
**Purpose**: External entities that purchase products

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| customer_id | **CREATE**: New customer setup |  |
| customer_name | **UPDATE**: Address changes, credit updates |  |
| customer_type | **DELETE**: Mark inactive instead |  |
| bill_to_address |  |  |
| ship_to_addresses[] |  |  |
| credit_limit |  |  |
| payment_terms |  |  |
| status | **UPDATE**: Status changes | Active → Hold → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

### Supplier  
**Purpose**: External entities that provide materials and services

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| supplier_id | **CREATE**: New vendor onboarding |  |
| supplier_name | **UPDATE**: Contact info, terms changes |  |
| supplier_type | **DELETE**: Mark inactive instead |  |
| remit_to_address |  |  |
| ship_from_addresses[] |  |  |
| payment_terms |  |  |
| lead_time_days |  |  |
| minimum_order_value |  |  |
| status | **UPDATE**: Status changes | Active → Hold → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

---

## 3. PRODUCT HIERARCHY OBJECTS

### Product Family (NEW)
**Purpose**: Group similar products with shared characteristics and specifications

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| product_family_id | **CREATE**: New family definition |  |
| family_name | **UPDATE**: Description, characteristics |  |
| family_type | **DELETE**: Not allowed if products exist |  |
| common_characteristics |  |  |
| shared_specifications[] |  |  |
| default_quality_plan_reference |  |  |
| status | **UPDATE**: Status changes | Active → Inactive |
| effective_from_date |  |  |
| effective_to_date |  |  |

**Real-World Example**:
```json
{
  "product_family_id": "FAM-PUMPS-CENTRIFUGAL",
  "family_name": "Centrifugal Pumps",
  "common_characteristics": {
    "type": "rotating_equipment",
    "testing_standard": "API-610"
  },
  "shared_specifications": ["SPEC-VIBRATION-STD", "SPEC-HYDRO-TEST"]
}
```

### Product Master (ENHANCED)
**Purpose**: Define finished goods with family inheritance

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| product_id | **CREATE**: New product introduction |
| product_family_reference: {type: "product_family", id: "..."} | **UPDATE**: Engineering changes |
| description | **DELETE**: Obsolete (status change) |
| base_uom_reference: {type: "uom", id: "..."} |  |
| product_type |  |
| revision_level |  |
| engineering_specifications |  |
| make_or_buy |  |
| primary_bom_reference |  |
| primary_route_reference |  |
| quality_plan_reference |  |
| effective_from_date |  |
| effective_to_date |  |

**Growing Sophistication**:
```json
// Year 1: Simple
{
  "product_id": "WIDGET-A",
  "description": "Basic Widget Type A"
}

// Year 2: With family
{
  "product_id": "PUMP-100GPM",
  "product_family_reference": {"type": "product_family", "id": "FAM-PUMPS"},
  "description": "100 GPM Centrifugal Pump",
  "inherits_family_specs": true
}
```

---

## 4. SPECIFICATION & COMPLIANCE OBJECTS

### Specification (NEW)
**Purpose**: Define quality, process, and environmental requirements

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| specification_id | **CREATE**: New specification |  |
| specification_name | **UPDATE**: Version updates |  |
| specification_type | **DELETE**: Not allowed - expire instead |  |
| category (product/process/environmental) |  |  |
| version |  |  |
| standard_reference |  |  |
| description |  |  |
| status | **UPDATE**: Status changes | Draft → Active → Obsolete |
| effective_from_date |  |  |
| effective_to_date |  |  |

### Specification Parameter (NEW)
**Purpose**: Define individual requirements within a specification

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| parameter_id | **CREATE**: Add to specification |
| specification_reference: {type: "specification", id: "..."} | **UPDATE**: Limits, methods |
| parameter_type (product_characteristic/process_variable/environmental_condition) | **DELETE**: Not allowed |
| parameter_name |  |
| characteristic |  |
| nominal_value |  |
| upper_limit |  |
| lower_limit |  |
| units_reference: {type: "uom", id: "..."} |  |
| test_method |  |
| sampling_plan |  |

**Real-World Example - Heat Treatment Specification**:
```json
// Specification Header
{
  "specification_id": "SPEC-HT-4140",
  "specification_name": "4140 Steel Heat Treatment",
  "category": "process"
}

// Parameters
[
  {
    "parameter_type": "product_characteristic",
    "parameter_name": "Final Hardness",
    "nominal_value": 32,
    "upper_limit": 34,
    "lower_limit": 30,
    "units": "HRC"
  },
  {
    "parameter_type": "process_variable",
    "parameter_name": "Austenitizing Temperature",
    "nominal_value": 845,
    "tolerance": 10,
    "units": "celsius"
  }
]
```

### Specification Application (NEW)
**Purpose**: Define where specifications apply with inheritance and overrides

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| application_id | **CREATE**: Link spec to object |
| specification_reference: {type: "specification", id: "..."} | **UPDATE**: Override values |
| applies_to_type (family/product/customer/operation) | **DELETE**: Remove application |
| applies_to_reference: {type: "...", id: "..."} |  |
| parameter_overrides[] |  |
| override_reason |  |
| mandatory |  |
| effective_from_date |  |
| effective_to_date |  |

---

## 5. ENGINEERING MASTER DATA

### Bill of Materials (BOM)
**Purpose**: Define product structure and components

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| bom_id | **CREATE**: New BOM version |
| parent_product_reference | **UPDATE**: Not allowed - create new version |
| version_number | **DELETE**: Not allowed - expire instead |
| parent_version_reference |  |
| effective_from_date |  |
| effective_to_date |  |
| is_active |  |

### BOM Components
**Purpose**: Individual components within a BOM

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| bom_id | **CREATE**: Add component |
| component_sequence | **UPDATE**: Quantity changes only |
| component_item_reference: {type: "item", id: "..."} | **DELETE**: Not allowed |
| quantity_per |  |
| uom_reference: {type: "uom", id: "..."} |  |
| scrap_allowance |  |

### Manufacturing Route
**Purpose**: Define how products are manufactured

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| route_id | **CREATE**: New manufacturing method |
| product_reference | **UPDATE**: Not allowed - version instead |
| route_name | **DELETE**: Not allowed - expire instead |
| route_type (primary/alternate/rework) |  |
| version_number |  |
| parent_version_reference |  |
| is_default_route |  |
| effective_from_date |  |
| effective_to_date |  |

### Route Operation (ENHANCED)
**Purpose**: Individual manufacturing steps with quality integration

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| route_id | **CREATE**: Add operation to route |
| operation_sequence | **UPDATE**: Time changes only |
| operation_description | **DELETE**: Not allowed |
| operation_type |  |
| resource_capability_required |  |
| preferred_resource_reference |  |
| alternate_resources[] |  |
| standard_setup_time |  |
| standard_run_time |  |
| time_uom_reference: {type: "uom", id: "..."} |  |
| quality_check_points[] |  |
| specification_references[] |  |

**Starting Simple**:
```json
{
  "operation_sequence": 10,
  "operation_description": "Cut material",
  "estimated_time": 30
}
```

**Growing Sophistication**:
```json
{
  "operation_sequence": 10,
  "operation_description": "Cut material",
  "preferred_resource_reference": {"type": "resource", "id": "SAW-01"},
  "alternate_resources": ["SAW-02", "SAW-03"],
  "standard_setup_time": 15,
  "standard_run_time": 2,
  "quality_check_points": ["first_piece", "every_50th"],
  "specification_references": ["SPEC-LENGTH-001"]
}
```

---

## 6. INVENTORY & MATERIAL OBJECTS

### Item/SKU Master
**Purpose**: Define materials, components, and products at the SKU level

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| item_id | **CREATE**: New item setup |
| product_reference: {type: "product", id: "..."} | **UPDATE**: Attribute maintenance |
| description | **DELETE**: Obsolete (status change) |
| item_type (raw/WIP/finished) |  |
| base_uom_reference: {type: "uom", id: "..."} |  |
| weight |  |
| weight_uom_reference: {type: "uom", id: "..."} |  |
| dimensions |  |
| dimension_uom_reference: {type: "uom", id: "..."} |  |
| shelf_life_days |  |
| lot_controlled |  |
| serial_controlled |  |
| abc_classification |  |
| effective_from_date |  |
| effective_to_date |  |

### Lot/Batch
**Purpose**: Track material genealogy and quality

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| lot_number (immutable) | **CREATE**: Receiving, production |
| item_reference: {type: "item", id: "..."} (immutable) | **UPDATE**: Quality status only |
| manufacture_date (immutable) | **DELETE**: Not allowed |
| expiration_date (immutable) |  |
| supplier_lot (immutable) |  |
| quantity_received (immutable) |  |
| quality_status |  |
| environmental_exposure_log[] |  |

### Serial Number
**Purpose**: Track individual units

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| serial_number (immutable) | **CREATE**: Production, receiving |
| item_reference: {type: "item", id: "..."} (immutable) | **UPDATE**: Status, location changes |
| lot_reference: {type: "lot", id: "..."} (immutable) | **DELETE**: Not allowed |
| manufacture_date (immutable) |  |
| commission_date (immutable) |  |
| current_status |  |
| location_reference: {type: "location", id: "..."} |  |
| handling_unit_reference: {type: "handling_unit", id: "..."} |  |
| warranty_expiration |  |

### Inventory Record (ENHANCED)
**Purpose**: Current inventory status with quality integration

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| inventory_id | **CREATE**: First receipt to location |  |
| item_reference: {type: "item", id: "..."} | **UPDATE**: Every movement, count |  |
| lot_reference: {type: "lot", id: "..."} | **DELETE**: When quantity reaches zero |  |
| location_reference: {type: "location", id: "..."} |  |  |
| quantity_on_hand |  |  |
| quantity_available |  |  |
| quantity_allocated |  |  |
| uom_reference: {type: "uom", id: "..."} |  |  |
| quality_status | **UPDATE**: Quality changes | Available → Hold → Quarantine → Released/Rejected |
| environmental_status |  |  |
| last_movement_date (immutable) |  |  |
| last_count_date (immutable) |  |  |

---

## 7. CONTAINER & LOCATION OBJECTS

### Location
**Purpose**: Define physical and logical storage locations

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| location_id | **CREATE**: New location setup |  |
| warehouse_id (immutable) | **UPDATE**: Capacity, type changes |  |
| zone | **DELETE**: Decommission |  |
| aisle |  |  |
| bin |  |  |
| location_type |  |  |
| location_subtype |  |  |
| capacity_volume |  |  |
| capacity_volume_uom |  |  |
| capacity_weight |  |  |
| capacity_weight_uom |  |  |
| capacity_handling_units |  |  |
| single_handling_unit_only |  |  |
| environmental_requirements |  |  |
| monitoring_points[] |  |  |
| status | **UPDATE**: Status changes | Active → Maintenance → Inactive → Decommissioned |
| effective_from_date |  |  |
| effective_to_date |  |  |

### Handling Unit
**Purpose**: Containers that hold and move materials

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| handling_unit_id | **CREATE**: Receiving, packing |  |
| handling_unit_type | **UPDATE**: Location, parent, status |  |
| parent_reference: {type: "handling_unit", id: "..."} | **DELETE**: Unpacking |  |
| planned_location_reference: {type: "location", id: "..."} |  |  |
| actual_location_reference: {type: "location", id: "..."} |  |  |
| status | **UPDATE**: Lifecycle changes | Empty → In Use → Sealed → Shipped → Consumed |
| created_date (immutable) |  |  |
| sealed_flag |  |  |

### Handling Unit Contents
**Purpose**: What's inside each handling unit

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| handling_unit_reference: {type: "handling_unit", id: "..."} | **CREATE**: Pack items |
| item_reference: {type: "item", id: "..."} | **UPDATE**: Quantity only |
| lot_reference: {type: "lot", id: "..."} | **DELETE**: Unpack |
| quantity |  |
| uom_reference: {type: "uom", id: "..."} |  |
| pack_date (immutable) |  |
| serial_numbers[] |  |

---

## 8. RESOURCE OBJECTS

### Resource (Hierarchical)
**Purpose**: Equipment and work centers in ISA-95 hierarchy

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| resource_id | **CREATE**: New resource |  |
| resource_name | **UPDATE**: Capabilities, status |  |
| resource_type | **DELETE**: Decommission |  |
| parent_reference: {type: "resource", id: "..."} |  |  |
| level (enterprise/site/area/line/unit) |  |  |
| general_capabilities[] |  |  |
| capability_notes |  |  |
| capacity_per_hour |  |  |
| capacity_uom_reference: {type: "uom", id: "..."} |  |  |
| current_status |  |  |
| status_since_timestamp |  |  |
| monitoring_points[] |  |  |
| status | **UPDATE**: Availability changes | Available → Running → Setup → Idle → Down → Maintenance → Offline |
| effective_from_date |  |  |
| effective_to_date |  |  |

**Starting Simple**:
```json
{
  "resource_id": "MILL-5",
  "resource_name": "CNC Mill #5",
  "resource_type": "milling_machine",
  "general_capabilities": ["3-axis", "aluminum", "steel"],
  "status": "available"
}
```

### Personnel Class (NEW)
**Purpose**: Define job roles and positions

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| personnel_class_id | **CREATE**: New role definition |
| class_name | **UPDATE**: Requirements |
| class_description | **DELETE**: Not if people assigned |
| required_skills[] |  |
| required_certifications[] |  |
| typical_wage_grade |  |

### Person (formerly Operator)
**Purpose**: Individual personnel with skills and assignments

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| person_id | **CREATE**: New employee |
| name | **UPDATE**: Skills, assignments |
| personnel_class_reference: {type: "personnel_class", id: "..."} | **DELETE**: Termination |
| skills[] |  |
| skill_proficiency_levels |  |
| certifications[] |  |
| home_resource_reference: {type: "resource", id: "..."} |  |
| planned_resource_reference: {type: "resource", id: "..."} |  |
| actual_resource_reference: {type: "resource", id: "..."} |  |
| availability_status |  |

---

## 9. ORDER OBJECTS

### Order Header
**Purpose**: Common structure for all order types

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| order_id | **CREATE**: Order entry |  |
| order_type (purchase/sales/transfer) | **UPDATE**: Status, dates |  |
| from_party_reference: {type: "...", id: "..."} | **DELETE**: Cancel only |  |
| to_party_reference: {type: "...", id: "..."} |  |  |
| external_references |  |  |
| order_date (immutable) |  |  |
| requested_date |  |  |
| promised_date |  |  |
| planned_ship_date |  |  |
| actual_ship_date |  |  |
| planned_receipt_date |  |  |
| actual_receipt_date |  |  |
| status | **UPDATE**: Progress through lifecycle | Draft → Open → Allocated → Picking → Packed → Shipped → Delivered → Closed |

### Order Line
**Purpose**: Individual items on orders

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| order_id | **CREATE**: Line addition |  |
| line_number | **UPDATE**: Quantities, status |  |
| item_reference: {type: "item", id: "..."} | **DELETE**: Line cancel |  |
| quantity_ordered |  |  |
| quantity_shipped |  |  |
| quantity_received |  |  |
| quantity_backordered |  |  |
| uom_reference: {type: "uom", id: "..."} |  |  |
| unit_price (immutable) |  |  |
| requested_date |  |  |
| promised_date |  |  |
| status | **UPDATE**: Status changes | Open → Allocated → Picked → Packed → Shipped → Delivered → Closed |

---

## 10. PRODUCTION EXECUTION OBJECTS

### Work Order (ENHANCED)
**Purpose**: Execute production with flexible resource assignment

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| work_order_id | **CREATE**: Order release |  |
| work_order_type (scheduled/performed) | **UPDATE**: Progress, status |  |
| product_reference: {type: "product", id: "..."} | **DELETE**: Not allowed |  |
| external_references |  |  |
| scheduling_level (basic/scheduled/dispatched) |  |  |
| planned_route_reference: {type: "route", id: "..."} |  |  |
| actual_route_reference: {type: "route", id: "..."} |  |  |
| resource_flexibility |  |  |
| planned_quantity |  |  |
| actual_quantity_good |  |  |
| actual_quantity_scrap |  |  |
| planned_start_date |  |  |
| actual_start_date |  |  |
| planned_complete_date |  |  |
| actual_complete_date |  |  |
| status | **UPDATE**: Production progress | Created → Released → Started → In Process → Completed |

**Progressive Implementation**:
```json
// Year 1: Basic
{
  "work_order_id": "WO-001",
  "product_reference": {"type": "product", "id": "WIDGET-A"},
  "quantity": 100,
  "due_date": "2024-03-20"
}

// Year 2: With routing
{
  "work_order_id": "WO-001",
  "product_reference": {"type": "product", "id": "WIDGET-A"},
  "planned_route_reference": {"type": "route", "id": "ROUTE-WIDGET-STD"},
  "quantity": 100,
  "scheduled_start": "2024-03-18T08:00:00Z"
}
```

### Work Order Operation
**Purpose**: Track execution at operation level when routes are used

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| work_order_id | **CREATE**: WO release |  |
| operation_sequence | **UPDATE**: Progress |  |
| route_operation_reference | **DELETE**: Not allowed |  |
| planned_resource_reference: {type: "resource", id: "..."} |  |  |
| actual_resource_reference: {type: "resource", id: "..."} |  |  |
| resource_assignment_notes |  |  |
| planned_vs_actual_tracking |  |  |
| quality_checks_required[] |  |  |
| quality_checks_completed[] |  |  |
| status | **UPDATE**: Operation progress | Pending → Setup → Running → Complete |

### Material Consumption
**Purpose**: Track actual material usage

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| consumption_id | **CREATE**: Issue transaction |
| work_order_reference: {type: "work_order", id: "..."} | **UPDATE**: Corrections only |
| operation_sequence | **DELETE**: Reversal only |
| item_reference: {type: "item", id: "..."} (immutable) |  |
| lot_reference: {type: "lot", id: "..."} (immutable) |  |
| planned_quantity |  |
| actual_quantity |  |
| consumption_date (immutable) |  |
| transaction_reference: {type: "transaction_log", id: "..."} (immutable) |  |

### Production Declaration
**Purpose**: Report production output

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| declaration_id | **CREATE**: Report production |
| work_order_reference: {type: "work_order", id: "..."} | **UPDATE**: Quantity corrections |
| operation_reference: {type: "work_order_operation", id: "..."} | **DELETE**: Not allowed |
| item_reference: {type: "item", id: "..."} |  |
| lot_number (immutable) |  |
| serial_numbers[] |  |
| quantity_good |  |
| quantity_scrap |  |
| location_reference: {type: "location", id: "..."} |  |
| declaration_timestamp (immutable) |  |
| triggers_consumption[] |  |

---

## 11. LABOR TRACKING OBJECTS

### Labor Assignment
**Purpose**: Track who works on what

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| assignment_id | **CREATE**: Operator assigned to work |  |
| person_reference: {type: "person", id: "..."} | **UPDATE**: Assignment changes |  |
| work_order_reference: {type: "work_order", id: "..."} | **DELETE**: Assignment end |  |
| operation_sequence |  |  |
| resource_reference: {type: "resource", id: "..."} |  |  |
| planned_start_time |  |  |
| actual_start_time |  |  |
| planned_end_time |  |  |
| actual_end_time |  |  |
| status | **UPDATE**: Assignment progress | Assigned → Active → Paused → Completed |

### Labor Time Entry
**Purpose**: Operational time tracking

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| time_entry_id | **CREATE**: Time recording |
| person_reference: {type: "person", id: "..."} (immutable) | **UPDATE**: Corrections only |
| entry_type (clock_in/clock_out/job_start/job_end/break_start/break_end) | **DELETE**: Corrections only |
| timestamp (immutable) |  |
| work_order_reference: {type: "work_order", id: "..."} |  |
| operation_sequence |  |
| resource_reference: {type: "resource", id: "..."} |  |
| entry_method (manual/barcode/badge) (immutable) |  |

---

## 12. MAINTENANCE OBJECTS

### Maintenance Schedule
**Purpose**: Preventive maintenance planning

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| schedule_id | **CREATE**: PM program setup |  |
| resource_reference: {type: "resource", id: "..."} | **UPDATE**: Frequency changes |  |
| maintenance_type (preventive/inspection/calibration) | **DELETE**: Deactivate |  |
| frequency_days |  |  |
| frequency_hours |  |  |
| frequency_cycles |  |  |
| last_completed_date |  |  |
| next_due_date |  |  |
| description |  |  |
| instructions |  |  |
| status | **UPDATE**: Status changes | Active → Suspended → Inactive |

### Maintenance Work Order
**Purpose**: Track maintenance activities

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| maintenance_wo_id | **CREATE**: Maintenance needed |  |
| maintenance_type (preventive/corrective/emergency) | **UPDATE**: Progress tracking |  |
| resource_reference: {type: "resource", id: "..."} | **DELETE**: Not allowed |  |
| schedule_reference: {type: "maintenance_schedule", id: "..."} |  |  |
| problem_description |  |  |
| priority (low/medium/high/emergency) |  |  |
| requested_date |  |  |
| planned_start_date |  |  |
| actual_start_date |  |  |
| planned_completion_date |  |  |
| actual_completion_date |  |  |
| assigned_technician_reference: {type: "person", id: "..."} |  |  |
| work_performed |  |  |
| status | **UPDATE**: Work progress | Created → Assigned → In Progress → Completed → Closed |

---

## 13. SHOP FLOOR CONTROL OBJECTS

### Equipment Status Event
**Purpose**: Real-time equipment state tracking

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| status_event_id | **CREATE**: Status change |
| resource_reference: {type: "resource", id: "..."} (immutable) | **UPDATE**: Not allowed |
| event_timestamp (immutable) | **DELETE**: Not allowed |
| old_status |  |
| new_status |  |
| person_reference: {type: "person", id: "..."} (immutable) |  |
| reason_code |  |
| notes |  |

### Downtime Record
**Purpose**: Track equipment stoppages

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| downtime_id | **CREATE**: Equipment stops |  |
| resource_reference: {type: "resource", id: "..."} (immutable) | **UPDATE**: Resolution details |  |
| start_timestamp (immutable) | **DELETE**: Not allowed |  |
| end_timestamp |  |  |
| duration_minutes |  |  |
| downtime_category (planned/unplanned) (immutable) |  |  |
| reason_code |  |  |
| reason_description |  |  |
| person_reference: {type: "person", id: "..."} (immutable) |  |  |
| resolution_notes |  |  |
| status | **UPDATE**: Resolution progress | Open → Investigating → Resolved |

### OEE Calculation
**Purpose**: Equipment efficiency metrics

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| oee_id | **CREATE**: Periodic calculation |
| resource_reference: {type: "resource", id: "..."} | **UPDATE**: Recalculation |
| calculation_period_start | **DELETE**: Data retention |
| calculation_period_end |  |
| planned_production_time_minutes |  |
| actual_production_time_minutes |  |
| availability_percentage |  |
| performance_percentage |  |
| quality_percentage |  |
| oee_percentage |  |
| pieces_planned |  |
| pieces_produced |  |
| pieces_good |  |
| pieces_scrap |  |

---

## 14. INVENTORY CONTROL OBJECTS

### Cycle Count Plan
**Purpose**: Define counting strategies

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| plan_id | **CREATE**: New strategy |
| plan_name | **UPDATE**: Rules, frequency |
| abc_a_frequency_days | **DELETE**: Deactivate |
| abc_b_frequency_days |  |
| abc_c_frequency_days |  |
| location_frequency_days |  |
| value_threshold |  |
| effective_from_date |  |
| effective_to_date |  |

### Cycle Count Schedule
**Purpose**: Plan count activities

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| schedule_id | **CREATE**: Generated from plan |  |
| plan_id | **UPDATE**: Assignments |  |
| scheduled_date | **DELETE**: Cancel |  |
| location_reference: {type: "location", id: "..."} |  |  |
| item_reference: {type: "item", id: "..."} |  |  |
| assigned_person_reference: {type: "person", id: "..."} |  |  |
| status | **UPDATE**: Status changes | Scheduled → Assigned → In Progress → Completed → Cancelled |

### Cycle Count Record
**Purpose**: Record count results

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| count_id | **CREATE**: Count execution |  |
| schedule_id | **UPDATE**: Approval only |  |
| location_reference: {type: "location", id: "..."} | **DELETE**: Not allowed |  |
| handling_unit_reference: {type: "handling_unit", id: "..."} |  |  |
| item_reference: {type: "item", id: "..."} |  |  |
| lot_reference: {type: "lot", id: "..."} |  |  |
| expected_quantity (immutable) |  |  |
| counted_quantity |  |  |
| variance |  |  |
| count_date (immutable) |  |  |
| counter_reference: {type: "person", id: "..."} |  |  |
| status | **UPDATE**: Count progress | Assigned → In Progress → Completed → Reviewed → Approved → Posted |

---

## 15. QUALITY OBJECTS

### Quality Plan (NEW)
**Purpose**: Define quality requirements for products

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| quality_plan_id | **CREATE**: New plan |
| plan_name | **UPDATE**: Requirements |
| product_family_reference | **DELETE**: Not allowed |
| product_reference |  |
| inspection_points[] |  |
| applicable_specifications[] |  |
| sampling_plans[] |  |
| version |  |
| effective_from_date |  |
| effective_to_date |  |

### Quality Inspection (ENHANCED)
**Purpose**: Record quality checks at any level

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| inspection_id | **CREATE**: Inspection event |  |
| inspection_type (receiving/in_process/final/equipment_check/environmental) | **UPDATE**: Not allowed |  |
| inspection_level (product/process/environmental) | **DELETE**: Not allowed |  |
| context_reference: {type: "...", id: "..."} |  |  |
| specification_references[] |  |  |
| measurements[] |  |  |
| overall_result |  |  |
| disposition |  |  |
| inspector_reference: {type: "person", id: "..."} (immutable) |  |  |
| inspection_date (immutable) |  |  |
| requires_alert |  |  |
| status | **UPDATE**: Status changes | Planned → In Progress → Complete → Approved |

**Three Types Example**:
```json
// Product Quality
{
  "inspection_type": "final",
  "inspection_level": "product",
  "context_reference": {"type": "production_declaration", "id": "PD-001"},
  "measurements": [{"characteristic": "diameter", "value": 10.05}]
}

// Process Quality
{
  "inspection_type": "in_process",
  "inspection_level": "process",
  "context_reference": {"type": "work_order_operation", "id": "WO-OP-001"},
  "measurements": [{"parameter": "temperature", "value": 185}]
}

// Environmental Quality
{
  "inspection_type": "environmental",
  "inspection_level": "environmental",
  "context_reference": {"type": "monitoring_point", "id": "MP-001"},
  "measurements": [{"condition": "humidity", "value": 65}]
}
```

### Non-Conformance
**Purpose**: Track quality issues

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| ncr_number | **CREATE**: Issue found |  |
| item_reference: {type: "item", id: "..."} | **UPDATE**: Investigation |  |
| lot_reference: {type: "lot", id: "..."} | **DELETE**: Not allowed |  |
| quantity_affected (immutable) |  |  |
| defect_code (immutable) |  |  |
| disposition |  |  |
| root_cause |  |  |
| status | **UPDATE**: Status changes | Open → Investigating → Resolved → Closed |

---

## 16. ENVIRONMENTAL MONITORING OBJECTS (NEW)

### Environmental Monitoring Point
**Purpose**: Define locations requiring environmental monitoring

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| monitoring_point_id | **CREATE**: New monitoring location |
| description | **UPDATE**: Requirements, limits |
| location_reference: {type: "...", id: "..."} | **DELETE**: Deactivate only |
| isa95_level (enterprise/site/area/line/unit) |  |
| monitoring_type (temperature/humidity/pressure/particles/light) |  |
| sensor_reference: {type: "sensor", id: "..."} |  |
| manual_check_required |  |
| check_frequency |  |
| specifications[] |  |
| alert_conditions[] |  |
| compliance_references[] |  |

**Real-World Example**:
```json
{
  "monitoring_point_id": "MP-FREEZER-01",
  "description": "Main freezer temperature",
  "location_reference": {"type": "area", "id": "COLD-STORAGE"},
  "monitoring_type": "temperature",
  "specifications": [{
    "min_value": -20,
    "max_value": -15,
    "units": "celsius"
  }],
  "alert_conditions": [{
    "condition": "out_of_range",
    "duration": "15_minutes",
    "severity": "critical"
  }]
}
```

### Environmental Monitoring Record
**Purpose**: Track environmental conditions

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| record_id | **CREATE**: Reading taken |
| monitoring_point_reference: {type: "monitoring_point", id: "..."} | **UPDATE**: Corrections only |
| timestamp (immutable) | **DELETE**: Not allowed |
| measurements |  |
| status (in_spec/warning/out_of_spec) |  |
| data_source (automatic/manual) |  |
| verified_by |  |
| affected_inventory[] |  |
| alert_triggered |  |

### Environmental Alert (NEW)
**Purpose**: Proactive notifications for environmental deviations

| **Attributes** | **Transaction Effects** | **State Transitions** |
|----------------|------------------------|-----------------------|
| alert_id | **CREATE**: Condition detected |  |
| alert_type (deviation/trend/prediction) | **UPDATE**: Acknowledgment |  |
| severity (info/warning/critical) | **DELETE**: Not allowed |  |
| source_reference: {type: "...", id: "..."} |  |  |
| condition_description |  |  |
| affected_objects[] |  |  |
| recommended_actions[] |  |  |
| notifications_sent[] |  |  |
| acknowledged_by |  |  |
| acknowledged_at |  |  |
| resolution_notes |  |  |
| status | **UPDATE**: Alert lifecycle | Active → Acknowledged → Resolved |

**Alert Examples**:
```json
// Immediate Alert
{
  "alert_type": "deviation",
  "severity": "critical",
  "source_reference": {"type": "monitoring_point", "id": "MP-FREEZER-01"},
  "condition_description": "Temperature -10°C exceeds limit of -15°C",
  "affected_objects": [
    {"type": "lot", "id": "LOT-001", "quantity": 500}
  ],
  "recommended_actions": ["Check freezer unit", "Verify product quality"]
}

// Trend Alert
{
  "alert_type": "trend",
  "severity": "warning",
  "condition_description": "Temperature rising 1°C per hour",
  "prediction": "Will exceed limit in 2 hours"
}
```

---

## 17. TRANSACTION & AUDIT OBJECTS

### Transaction Log
**Purpose**: Immutable audit trail of all changes

| **Attributes** | **Transaction Effects** |
|----------------|------------------------|
| transaction_id (immutable) | **CREATE**: Every change |
| event_timestamp (immutable) | **UPDATE**: Not allowed |
| event_type (immutable) | **DELETE**: Archive only |
| operational_category (production/maintenance/quality/inventory) |  |
| object_reference: {type: "...", id: "..."} (immutable) |  |
| attribute_changes[] (immutable) |  |
| caused_by_reference: {type: "...", id: "..."} (immutable) |  |
| user_id (immutable) |  |
| source_system (immutable) |  |
| reversal_of_reference: {type: "transaction_log", id: "..."} |  |

**Event Type Categories**:
```
Production Events: released, started, completed, scrapped
Inventory Events: received, moved, consumed, adjusted, counted
Quality Events: inspected, held, released, rejected
Maintenance Events: scheduled, started, completed
Environmental Events: reading_taken, limit_exceeded, condition_restored
```

---

## Implementation Principles

### Starting Simple
Every object can begin with minimal required fields and grow over time:
1. **Year 1**: Basic tracking (what, where, how much)
2. **Year 2**: Add scheduling and assignment
3. **Year 3**: Add specifications and compliance
4. **Year 4**: Full optimization and analytics

### Progressive Enhancement
Objects support incremental sophistication:
- Start with manual data entry
- Add barcode scanning
- Integrate sensors
- Enable predictive analytics

### Flexibility First
- Every planned field has an actual counterpart
- Manual overrides always allowed
- Notes fields for tribal knowledge
- Support for incomplete data

This catalog provides the complete foundation for manufacturing digitization, from basic inventory tracking to sophisticated quality and environmental compliance, all while maintaining practical flexibility for real-world implementation.