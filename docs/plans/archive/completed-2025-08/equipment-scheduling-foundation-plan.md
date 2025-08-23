# Equipment Scheduling System - Technical Foundation Plan

## Executive Summary

This document outlines the comprehensive technical foundation for the Equipment Scheduling System, an 8-week business initiative designed to provide equipment availability scheduling for manufacturing facilities. The system will serve as the authoritative data source for OEE systems, following Clean Architecture principles and integrating seamlessly with the existing TimescaleDB infrastructure.

### Vision
Build a complete equipment scheduling foundation that delivers immediate value in Phase 1 while enabling seamless expansion to workforce management in Phase 2, without requiring any refactoring or data migration.

## 1. Architecture Overview

### Design Principles
- **Complete Foundation**: Build full architecture from day one, activate features progressively
- **Zero Migration**: Phase 2 activates through configuration, not deployment
- **API Stability**: Version 1 endpoints remain unchanged when Version 2 extends functionality
- **Logger Foundation**: Built on Industrial.Adam.Logger's proven data collection architecture
- **Integration First**: Seamless integration with existing OEE system via shared TimescaleDB

### System Context within Industrial ADAM Ecosystem

```
┌─────────────────────────────────────────────────────────────┐
│                Industrial ADAM Ecosystem                    │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ Counter Frontend │  │  OEE Frontend    │                │
│  │   (Port 3000)    │  │   (Port 3001)    │                │
│  │ Device Management│  │ Manufacturing    │                │
│  └──────────────────┘  │ Analytics        │                │
│                         └──────────────────┘                │
│                                   ↓                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Backend Services (.NET 9)              │   │
│  │                                                     │   │
│  │ ┌─────────────────┐ ┌─────────────────┐ ┌─────────┐ │   │
│  │ │Industrial.Adam  │ │Industrial.Adam  │ │Equipment│ │   │
│  │ │.Logger          │ │.Oee             │ │Scheduling│ │   │
│  │ │(Foundation)     │ │(Monitoring)     │ │(Planning)│ │   │
│  │ └─────────────────┘ └─────────────────┘ └─────────┘ │   │
│  └─────────────────────────────────────────────────────┘   │
│                                   ↓                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                TimescaleDB                          │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │   │
│  │  │ Counter Data │ │  oee_* tables│ │sched_* tables│ │   │
│  │  │(Time Series) │ │ (Relational) │ │(Relational)  │ │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

Data Flow: ADAM Devices → Logger → TimescaleDB → OEE + Equipment Scheduling
```

### System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Equipment Scheduling System                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                  Web API Layer                       │   │
│  │  ├── Controllers (RESTful endpoints)                │   │
│  │  ├── SignalR Hubs (Real-time notifications)        │   │
│  │  └── OpenAPI Documentation                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Application Layer (CQRS)               │   │
│  │  ├── Commands (Create, Update, Generate)            │   │
│  │  ├── Queries (Read, Report)                         │   │
│  │  ├── Handlers (Business Logic)                      │   │
│  │  └── Validators (Business Rules)                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                 Domain Layer                         │   │
│  │  ├── Entities (Resource, Pattern, Schedule)         │   │
│  │  ├── Value Objects (TimeRange, Availability)        │   │
│  │  ├── Domain Services (Schedule Generation)          │   │
│  │  └── Events (Schedule Changed)                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │             Infrastructure Layer                     │   │
│  │  ├── Repositories (EF Core + TimescaleDB)           │   │
│  │  ├── Background Services (Schedule Generation)      │   │
│  │  ├── External APIs (OEE Integration)                │   │
│  │  └── Caching (Redis)                                │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              │        Shared TimescaleDB     │
              │  ├── sched_* tables (Active)  │
              │  ├── emp_* tables (Dormant)   │
              │  └── OEE tables (Existing)    │
              └───────────────────────────────┘
```

## 2. Module Structure Plan

### Project Organization

```
src/Industrial.Adam.EquipmentScheduling/
├── Domain/
│   ├── Entities/
│   │   ├── Resource.cs                    # ISA-95 equipment hierarchy
│   │   ├── OperatingPattern.cs            # When equipment operates
│   │   ├── PatternAssignment.cs           # Pattern → Equipment mapping
│   │   ├── EquipmentSchedule.cs           # Generated availability records
│   │   ├── ScheduleException.cs           # Maintenance/holiday overrides
│   │   ├── HolidayCalendar.cs             # Regional holidays
│   │   └── Employee.cs                    # Phase 2: Workforce (Dormant)
│   ├── ValueObjects/
│   │   ├── TimeRange.cs                   # Start/End time periods
│   │   ├── ShiftConfiguration.cs          # Shift definitions
│   │   ├── Availability.cs                # Equipment availability state
│   │   └── IsaHierarchyPath.cs            # ISA-95 hierarchy path
│   ├── Services/
│   │   ├── ScheduleGenerationService.cs   # Core schedule generation
│   │   ├── PatternInheritanceService.cs   # Hierarchy pattern resolution
│   │   ├── HolidayCalendarService.cs      # Holiday processing
│   │   └── CoverageAnalysisService.cs     # Phase 2: Employee coverage
│   └── Events/
│       ├── ScheduleGeneratedEvent.cs      # Schedule creation/update
│       ├── PatternAssignedEvent.cs        # Pattern assignment changes
│       └── ExceptionCreatedEvent.cs       # Maintenance/holiday exceptions
├── Application/
│   ├── Commands/
│   │   ├── CreateResourceCommand.cs
│   │   ├── AssignPatternCommand.cs
│   │   ├── GenerateSchedulesCommand.cs
│   │   └── CreateExceptionCommand.cs
│   ├── Queries/
│   │   ├── GetResourceHierarchyQuery.cs
│   │   ├── GetAvailabilityQuery.cs
│   │   ├── GetScheduleChangesQuery.cs
│   │   └── GetPatternAssignmentsQuery.cs
│   ├── DTOs/
│   │   ├── ResourceDto.cs
│   │   ├── PatternDto.cs
│   │   ├── ScheduleDto.cs
│   │   └── AvailabilityDto.cs
│   └── Validators/
│       ├── CreateResourceValidator.cs
│       ├── AssignPatternValidator.cs
│       └── GenerateSchedulesValidator.cs
├── Infrastructure/
│   ├── Data/
│   │   ├── SchedulingDbContext.cs         # EF Core context
│   │   ├── Configurations/               # Entity configurations
│   │   └── Migrations/                   # Complete schema from day 1
│   ├── Repositories/
│   │   ├── ResourceRepository.cs
│   │   ├── PatternRepository.cs
│   │   ├── ScheduleRepository.cs
│   │   └── EmployeeRepository.cs         # Phase 2: Dormant
│   ├── Services/
│   │   ├── ScheduleGenerationEngine.cs   # High-performance generation
│   │   ├── WebhookNotificationService.cs # OEE system notifications
│   │   └── CacheService.cs              # Redis caching
│   └── BackgroundServices/
│       ├── ScheduleGenerationService.cs  # Async schedule processing
│       └── ChangeNotificationService.cs  # Webhook delivery
└── WebApi/
    ├── Controllers/
    │   ├── ResourcesController.cs        # Equipment hierarchy API
    │   ├── PatternsController.cs         # Pattern management API
    │   ├── SchedulesController.cs        # Schedule generation API
    │   ├── OeeController.cs              # OEE system integration
    │   └── EmployeesController.cs        # Phase 2: Dormant
    ├── SignalR/
    │   └── ScheduleNotificationHub.cs    # Real-time updates
    └── Models/
        ├── ApiResponse.cs
        ├── CreateResourceRequest.cs
        └── GenerateSchedulesRequest.cs
```

### Dependencies

```xml
<!-- Industrial.Adam.EquipmentScheduling.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>

<!-- Industrial.Adam.EquipmentScheduling.Application.csproj -->
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="FluentValidation" Version="11.11.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />

<!-- Industrial.Adam.EquipmentScheduling.Infrastructure.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
<PackageReference Include="Hangfire.Core" Version="1.8.19" />

<!-- Industrial.Adam.EquipmentScheduling.WebApi.csproj -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.4" />
```

## 3. Domain Model Design

### Core Entities

#### Resource (ISA-95 Equipment Hierarchy)

```csharp
public sealed class Resource : Entity<long>, IAggregateRoot
{
    public string ResourceId { get; private set; }           // Business ID
    public string ResourceName { get; private set; }         // Display name
    public ResourceType ResourceType { get; private set; }   // E, S, A, WC, WU
    public long? ParentResourceId { get; private set; }      // Hierarchy parent
    public IsaHierarchyPath HierarchyPath { get; private set; } // Computed path
    public bool RequiresScheduling { get; private set; }     // Scheduling flag
    public bool IsActive { get; private set; }               // Soft delete
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation properties
    public Resource? Parent { get; private set; }
    public List<Resource> Children { get; private set; } = [];
    public List<PatternAssignment> PatternAssignments { get; private set; } = [];

    // Domain methods
    public void AssignPattern(long patternId, DateTime effectiveDate, string assignedBy);
    public void RemovePattern(DateTime effectiveDate);
    public OperatingPattern? GetEffectivePattern(DateTime date);
    public void UpdateSchedulingRequirement(bool requiresScheduling);
}

public enum ResourceType
{
    Enterprise = 1,  // Level 1
    Site = 2,        // Level 2  
    Area = 3,        // Level 3
    WorkCenter = 4,  // Level 4
    WorkUnit = 5     // Level 5
}
```

#### OperatingPattern (When Equipment Should Operate)

```csharp
public sealed class OperatingPattern : Entity<long>
{
    public string PatternName { get; private set; }
    public string PatternCode { get; private set; }
    public ComplexityLevel Complexity { get; private set; }     // Simple/Advanced/Expert
    public PatternType PatternType { get; private set; }        // Continuous/Shift/Custom
    public int CycleDays { get; private set; }                  // 7 for weekly, 14 for bi-weekly
    public decimal WeeklyHours { get; private set; }            // Average hours per week
    public bool IsVisible { get; private set; }                 // Phase control
    public PatternConfiguration Configuration { get; private set; } // JSON configuration
    public DateTime CreatedAt { get; private set; }
    public bool IsSystemPattern { get; private set; }           // Built-in patterns

    // Domain methods
    public List<ShiftConfiguration> GetShiftsForDate(DateTime date);
    public decimal CalculateWeeklyHours();
    public bool IsActiveOnDay(DayOfWeek dayOfWeek);
}

public enum ComplexityLevel
{
    Simple = 1,      // Phase 1: 24/7, Two-Shift, Day-Only, Extended, Custom
    Advanced = 2,    // Phase 2: DuPont, Pitman, Continental
    Expert = 3       // Phase 2: Complex rotations
}

public enum PatternType
{
    Continuous,      // 24/7 operation
    Shift,          // Multiple shifts
    DayOnly,        // Single day shift
    Custom          // User-defined
}
```

#### PatternAssignment (Which Equipment Uses Which Pattern)

```csharp
public sealed class PatternAssignment : Entity<long>
{
    public long ResourceId { get; private set; }
    public long PatternId { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsOverride { get; private set; }              // True if overriding inherited
    public long? InheritedFrom { get; private set; }          // Parent resource that provided pattern
    public string AssignedBy { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public Resource Resource { get; private set; } = null!;
    public OperatingPattern Pattern { get; private set; } = null!;

    // Domain methods
    public bool IsActiveOn(DateTime date);
    public void Terminate(DateTime endDate);
    public void ExtendTo(DateTime newEndDate);
}
```

#### EquipmentSchedule (Generated Availability Records)

```csharp
public sealed class EquipmentSchedule : Entity<long>
{
    public long ResourceId { get; private set; }
    public DateTime ScheduleDate { get; private set; }
    public string? ShiftCode { get; private set; }            // D, A, N for OEE integration
    public DateTime PlannedStartTime { get; private set; }
    public DateTime PlannedEndTime { get; private set; }
    public decimal PlannedHours { get; private set; }
    public ScheduleStatus Status { get; private set; }        // Operating/Maintenance/Holiday
    public long PatternId { get; private set; }               // Source pattern
    public bool IsException { get; private set; }             // Overridden by exception
    public long? ExceptionId { get; private set; }            // Reference to exception
    public DateTime GeneratedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation properties
    public Resource Resource { get; private set; } = null!;
    public OperatingPattern Pattern { get; private set; } = null!;
    public ScheduleException? Exception { get; private set; }

    // Domain methods
    public void ApplyException(ScheduleException exception);
    public void ClearException();
    public Availability ToAvailability();
}

public enum ScheduleStatus
{
    Operating,       // Normal production
    Maintenance,     // Planned maintenance
    Holiday,         // Holiday/non-working day
    Shutdown,        // Planned shutdown
    Breakdown        // Unplanned downtime
}
```

#### ScheduleException (Maintenance Windows, Holidays)

```csharp
public sealed class ScheduleException : Entity<long>
{
    public long ResourceId { get; private set; }
    public ExceptionType ExceptionType { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }
    public string? RecurrenceRule { get; private set; }       // RRULE format
    public ImpactType Impact { get; private set; }            // NoOperation/Reduced/Shifted
    public string Description { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public Resource Resource { get; private set; } = null!;

    // Domain methods
    public bool AffectsDate(DateTime date);
    public List<DateTime> GenerateOccurrences(DateTime startDate, DateTime endDate);
    public void Deactivate();
}

public enum ExceptionType
{
    Maintenance,     // Planned maintenance
    Breakdown,       // Unplanned downtime
    Holiday,         // Holiday
    Shutdown,        // Planned shutdown
    Other           // Other reasons
}

public enum ImpactType
{
    NoOperation,     // Equipment stops completely
    Reduced,         // Reduced capacity
    Shifted          // Time shifted
}
```

### Value Objects

#### TimeRange

```csharp
public sealed record TimeRange
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public TimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time");
            
        StartTime = startTime;
        EndTime = endTime;
    }

    public decimal CalculateHours()
    {
        var duration = EndTime - StartTime;
        return (decimal)duration.TotalHours;
    }

    public bool Overlaps(TimeRange other)
    {
        return StartTime < other.EndTime && EndTime > other.StartTime;
    }
}
```

#### Availability

```csharp
public sealed record Availability
{
    public DateTime Date { get; }
    public decimal PlannedHours { get; }
    public List<string> ShiftCodes { get; }
    public ScheduleStatus Status { get; }

    public Availability(DateTime date, decimal plannedHours, List<string> shiftCodes, ScheduleStatus status)
    {
        Date = date.Date;
        PlannedHours = plannedHours;
        ShiftCodes = shiftCodes.ToList();
        Status = status;
    }
}
```

## 4. Database Schema Specifications

### Complete SQL Schema

```sql
-- ================================================
-- Equipment Scheduling System Database Schema
-- Phase 1: Active tables (prefix: sched_)
-- Phase 2: Dormant tables (prefix: sched_emp_)
-- ================================================

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- ================================================
-- PHASE 1: EQUIPMENT SCHEDULING (ACTIVE)
-- ================================================

-- ISA-95 Resource Hierarchy
CREATE TABLE sched_resources (
    id BIGSERIAL PRIMARY KEY,
    resource_id VARCHAR(100) UNIQUE NOT NULL,           -- Business identifier
    resource_name VARCHAR(200) NOT NULL,                -- Display name
    resource_type INTEGER NOT NULL,                     -- 1=Enterprise, 2=Site, 3=Area, 4=WorkCenter, 5=WorkUnit
    parent_resource_id BIGINT REFERENCES sched_resources(id) ON DELETE CASCADE,
    hierarchy_path VARCHAR(500) NOT NULL,               -- 'Enterprise.Site.Area.WorkCenter.WorkUnit'
    hierarchy_level INTEGER NOT NULL CHECK (hierarchy_level BETWEEN 1 AND 5),
    requires_scheduling BOOLEAN DEFAULT FALSE,           -- Does this resource need scheduling?
    is_active BOOLEAN DEFAULT TRUE,
    metadata JSONB DEFAULT '{}',                        -- Flexible attributes
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT sched_resources_resource_id_not_empty CHECK (resource_id != ''),
    CONSTRAINT sched_resources_resource_name_not_empty CHECK (resource_name != ''),
    CONSTRAINT sched_resources_hierarchy_path_not_empty CHECK (hierarchy_path != ''),
    CONSTRAINT sched_resources_hierarchy_level_type_match CHECK (
        (hierarchy_level = 1 AND resource_type = 1) OR    -- Enterprise
        (hierarchy_level = 2 AND resource_type = 2) OR    -- Site
        (hierarchy_level = 3 AND resource_type = 3) OR    -- Area  
        (hierarchy_level = 4 AND resource_type = 4) OR    -- WorkCenter
        (hierarchy_level = 5 AND resource_type = 5)       -- WorkUnit
    )
);

-- Operating Patterns Library
CREATE TABLE sched_operating_patterns (
    id BIGSERIAL PRIMARY KEY,
    pattern_name VARCHAR(100) NOT NULL,
    pattern_code VARCHAR(50) UNIQUE NOT NULL,
    complexity_level INTEGER NOT NULL DEFAULT 1,        -- 1=Simple, 2=Advanced, 3=Expert
    pattern_type INTEGER NOT NULL,                      -- 1=Continuous, 2=Shift, 3=DayOnly, 4=Custom
    cycle_days INTEGER NOT NULL DEFAULT 7,              -- 7=weekly, 14=bi-weekly, etc.
    weekly_hours DECIMAL(5,2) NOT NULL DEFAULT 0,       -- Average hours per week
    is_visible BOOLEAN DEFAULT TRUE,                     -- Phase control flag
    is_system_pattern BOOLEAN DEFAULT FALSE,             -- Built-in vs custom
    configuration JSONB NOT NULL DEFAULT '{}',          -- Pattern definition
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT sched_patterns_name_not_empty CHECK (pattern_name != ''),
    CONSTRAINT sched_patterns_code_not_empty CHECK (pattern_code != ''),
    CONSTRAINT sched_patterns_cycle_days_positive CHECK (cycle_days > 0),
    CONSTRAINT sched_patterns_weekly_hours_valid CHECK (weekly_hours >= 0 AND weekly_hours <= 168),
    CONSTRAINT sched_patterns_complexity_valid CHECK (complexity_level BETWEEN 1 AND 3),
    CONSTRAINT sched_patterns_type_valid CHECK (pattern_type BETWEEN 1 AND 4)
);

-- Pattern Schedule Rules (detailed shift definitions)
CREATE TABLE sched_pattern_rules (
    id BIGSERIAL PRIMARY KEY,
    pattern_id BIGINT NOT NULL REFERENCES sched_operating_patterns(id) ON DELETE CASCADE,
    day_of_week INTEGER NOT NULL CHECK (day_of_week BETWEEN 0 AND 6), -- 0=Sunday
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_operating BOOLEAN NOT NULL DEFAULT TRUE,
    shift_code VARCHAR(10),                             -- 'D', 'A', 'N' for OEE
    sequence_order INTEGER NOT NULL DEFAULT 1,          -- For multiple shifts per day
    
    CONSTRAINT sched_pattern_rules_times_valid CHECK (end_time > start_time),
    UNIQUE(pattern_id, day_of_week, sequence_order)
);

-- Resource Pattern Assignments (which equipment uses which pattern)
CREATE TABLE sched_resource_pattern_assignments (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    pattern_id BIGINT NOT NULL REFERENCES sched_operating_patterns(id) ON DELETE CASCADE,
    effective_date DATE NOT NULL,
    end_date DATE,                                      -- NULL = current assignment
    is_override BOOLEAN DEFAULT FALSE,                  -- Overriding inherited pattern
    inherited_from BIGINT REFERENCES sched_resources(id), -- Parent resource
    assigned_by VARCHAR(100) NOT NULL,                  -- User who made assignment
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes TEXT,
    
    -- Constraints
    CONSTRAINT sched_assignments_dates_valid CHECK (
        end_date IS NULL OR end_date >= effective_date
    ),
    CONSTRAINT sched_assignments_assigned_by_not_empty CHECK (assigned_by != '')
);

-- Generated Equipment Schedules (TimescaleDB hypertable)
CREATE TABLE sched_equipment_schedules (
    time TIMESTAMPTZ NOT NULL,                          -- Schedule date+time (for TimescaleDB)
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    schedule_date DATE NOT NULL,                        -- Business date
    shift_code VARCHAR(10),                             -- OEE shift identifier
    planned_start_time TIMESTAMPTZ NOT NULL,
    planned_end_time TIMESTAMPTZ NOT NULL,
    planned_hours DECIMAL(5,2) NOT NULL,
    schedule_status INTEGER NOT NULL DEFAULT 1,         -- 1=Operating, 2=Maintenance, 3=Holiday, 4=Shutdown
    pattern_id BIGINT NOT NULL REFERENCES sched_operating_patterns(id),
    is_exception BOOLEAN DEFAULT FALSE,
    exception_id BIGINT,                                -- Reference to exception (FK added later)
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_at TIMESTAMPTZ,
    
    -- TimescaleDB requirements
    PRIMARY KEY (time, resource_id),
    
    -- Constraints
    CONSTRAINT sched_schedules_times_valid CHECK (planned_end_time > planned_start_time),
    CONSTRAINT sched_schedules_hours_positive CHECK (planned_hours >= 0),
    CONSTRAINT sched_schedules_status_valid CHECK (schedule_status BETWEEN 1 AND 5)
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('sched_equipment_schedules', 'time', if_not_exists => TRUE);

-- Schedule Exceptions (maintenance windows, holidays, breakdowns)
CREATE TABLE sched_schedule_exceptions (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    exception_type INTEGER NOT NULL,                    -- 1=Maintenance, 2=Breakdown, 3=Holiday, 4=Shutdown, 5=Other
    start_datetime TIMESTAMPTZ NOT NULL,
    end_datetime TIMESTAMPTZ,
    recurrence_rule VARCHAR(200),                       -- RRULE format for recurring
    impact_type INTEGER NOT NULL DEFAULT 1,            -- 1=NoOperation, 2=Reduced, 3=Shifted
    description TEXT NOT NULL,
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Constraints
    CONSTRAINT sched_exceptions_times_valid CHECK (
        end_datetime IS NULL OR end_datetime > start_datetime
    ),
    CONSTRAINT sched_exceptions_type_valid CHECK (exception_type BETWEEN 1 AND 5),
    CONSTRAINT sched_exceptions_impact_valid CHECK (impact_type BETWEEN 1 AND 3),
    CONSTRAINT sched_exceptions_description_not_empty CHECK (description != ''),
    CONSTRAINT sched_exceptions_created_by_not_empty CHECK (created_by != '')
);

-- Add foreign key to schedules table
ALTER TABLE sched_equipment_schedules 
ADD CONSTRAINT fk_sched_schedules_exception 
FOREIGN KEY (exception_id) REFERENCES sched_schedule_exceptions(id);

-- Holiday Calendars
CREATE TABLE sched_holiday_calendars (
    id SERIAL PRIMARY KEY,
    calendar_name VARCHAR(100) NOT NULL,
    country_code CHAR(2) NOT NULL,                     -- ISO country code
    region VARCHAR(50),                                -- State/Province
    is_default BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT sched_calendars_name_not_empty CHECK (calendar_name != '')
);

-- Holidays
CREATE TABLE sched_holidays (
    id SERIAL PRIMARY KEY,
    calendar_id INTEGER NOT NULL REFERENCES sched_holiday_calendars(id) ON DELETE CASCADE,
    holiday_name VARCHAR(100) NOT NULL,
    holiday_date DATE,                                 -- NULL if recurring
    recurrence_rule VARCHAR(200),                      -- RRULE format
    affects_scheduling BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT sched_holidays_name_not_empty CHECK (holiday_name != ''),
    CONSTRAINT sched_holidays_date_or_rule CHECK (
        (holiday_date IS NOT NULL AND recurrence_rule IS NULL) OR
        (holiday_date IS NULL AND recurrence_rule IS NOT NULL)
    )
);

-- Resource Calendar Assignments
CREATE TABLE sched_resource_calendar_assignments (
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    calendar_id INTEGER NOT NULL REFERENCES sched_holiday_calendars(id) ON DELETE CASCADE,
    priority INTEGER NOT NULL DEFAULT 1,
    
    PRIMARY KEY (resource_id, calendar_id)
);

-- ================================================
-- PHASE 2: EMPLOYEE SCHEDULING (DORMANT)
-- ================================================

-- Departments
CREATE TABLE sched_emp_departments (
    id SERIAL PRIMARY KEY,
    department_name VARCHAR(100) NOT NULL,
    department_code VARCHAR(20) UNIQUE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT sched_departments_name_not_empty CHECK (department_name != ''),
    CONSTRAINT sched_departments_code_not_empty CHECK (department_code != '')
);

-- Positions
CREATE TABLE sched_emp_positions (
    id SERIAL PRIMARY KEY,
    position_name VARCHAR(100) NOT NULL,
    position_code VARCHAR(20) UNIQUE NOT NULL,
    department_id INTEGER NOT NULL REFERENCES sched_emp_departments(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT sched_positions_name_not_empty CHECK (position_name != ''),
    CONSTRAINT sched_positions_code_not_empty CHECK (position_code != '')
);

-- Employees (Phase 2: Built but empty)
CREATE TABLE sched_emp_employees (
    id BIGSERIAL PRIMARY KEY,
    employee_number VARCHAR(50) UNIQUE NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(200),
    department_id INTEGER NOT NULL REFERENCES sched_emp_departments(id),
    position_id INTEGER NOT NULL REFERENCES sched_emp_positions(id),
    hire_date DATE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    skills JSONB DEFAULT '{}',                         -- Skill matrix
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT sched_employees_number_not_empty CHECK (employee_number != ''),
    CONSTRAINT sched_employees_first_name_not_empty CHECK (first_name != ''),
    CONSTRAINT sched_employees_last_name_not_empty CHECK (last_name != '')
);

-- Employee Resource Qualifications (who can operate what)
CREATE TABLE sched_emp_resource_qualifications (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL REFERENCES sched_emp_employees(id) ON DELETE CASCADE,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    qualification_level INTEGER NOT NULL DEFAULT 1,    -- 1=Basic, 2=Advanced, 3=Expert
    certification_date DATE NOT NULL,
    expiry_date DATE,
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Constraints
    CONSTRAINT sched_qualifications_level_valid CHECK (qualification_level BETWEEN 1 AND 3),
    CONSTRAINT sched_qualifications_dates_valid CHECK (
        expiry_date IS NULL OR expiry_date > certification_date
    ),
    UNIQUE(employee_id, resource_id)
);

-- Teams (work teams for rotation)
CREATE TABLE sched_emp_teams (
    id SERIAL PRIMARY KEY,
    team_name VARCHAR(50) NOT NULL,
    team_code CHAR(1) NOT NULL,                       -- 'A', 'B', 'C', 'D'
    pattern_id BIGINT REFERENCES sched_operating_patterns(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT sched_teams_name_not_empty CHECK (team_name != ''),
    CONSTRAINT sched_teams_code_not_empty CHECK (team_code != '')
);

-- Employee Team Assignments
CREATE TABLE sched_emp_team_assignments (
    employee_id BIGINT NOT NULL REFERENCES sched_emp_employees(id) ON DELETE CASCADE,
    team_id INTEGER NOT NULL REFERENCES sched_emp_teams(id) ON DELETE CASCADE,
    start_date DATE NOT NULL,
    end_date DATE,
    is_primary BOOLEAN DEFAULT TRUE,
    
    PRIMARY KEY (employee_id, team_id, start_date),
    
    CONSTRAINT sched_team_assignments_dates_valid CHECK (
        end_date IS NULL OR end_date >= start_date
    )
);

-- Employee Schedules (individual work schedules)
CREATE TABLE sched_emp_schedules (
    time TIMESTAMPTZ NOT NULL,                         -- For TimescaleDB
    employee_id BIGINT NOT NULL REFERENCES sched_emp_employees(id) ON DELETE CASCADE,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id) ON DELETE CASCADE,
    schedule_date DATE NOT NULL,
    shift_start_time TIMESTAMPTZ NOT NULL,
    shift_end_time TIMESTAMPTZ NOT NULL,
    schedule_status INTEGER NOT NULL DEFAULT 1,        -- 1=Scheduled, 2=Worked, 3=Absent, 4=Leave
    team_id INTEGER REFERENCES sched_emp_teams(id),
    generated_from BIGINT REFERENCES sched_equipment_schedules(time, resource_id),
    
    PRIMARY KEY (time, employee_id, resource_id),
    
    CONSTRAINT sched_emp_schedules_times_valid CHECK (shift_end_time > shift_start_time),
    CONSTRAINT sched_emp_schedules_status_valid CHECK (schedule_status BETWEEN 1 AND 4)
);

-- Convert employee schedules to hypertable
SELECT create_hypertable('sched_emp_schedules', 'time', if_not_exists => TRUE);

-- ================================================
-- SYSTEM CONFIGURATION & AUDIT
-- ================================================

-- System Configuration (feature flags and settings)
CREATE TABLE sched_system_configuration (
    config_key VARCHAR(100) PRIMARY KEY,
    config_value TEXT NOT NULL,
    config_type INTEGER NOT NULL DEFAULT 1,            -- 1=Feature, 2=Setting, 3=Integration
    is_active BOOLEAN DEFAULT TRUE,
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_by VARCHAR(100) NOT NULL,
    
    CONSTRAINT sched_config_key_not_empty CHECK (config_key != ''),
    CONSTRAINT sched_config_value_not_empty CHECK (config_value != ''),
    CONSTRAINT sched_config_type_valid CHECK (config_type BETWEEN 1 AND 3),
    CONSTRAINT sched_config_modified_by_not_empty CHECK (modified_by != '')
);

-- Audit Log
CREATE TABLE sched_audit_log (
    id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    record_id VARCHAR(100) NOT NULL,                   -- Can be BIGINT or composite key
    action VARCHAR(10) NOT NULL,                       -- INSERT, UPDATE, DELETE
    changed_by VARCHAR(100) NOT NULL,
    changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    
    CONSTRAINT sched_audit_table_not_empty CHECK (table_name != ''),
    CONSTRAINT sched_audit_action_valid CHECK (action IN ('INSERT', 'UPDATE', 'DELETE')),
    CONSTRAINT sched_audit_changed_by_not_empty CHECK (changed_by != '')
);

-- Convert audit log to hypertable for performance
SELECT create_hypertable('sched_audit_log', 'changed_at', if_not_exists => TRUE);

-- ================================================
-- PERFORMANCE INDEXES
-- ================================================

-- Resource hierarchy indexes
CREATE INDEX idx_sched_resources_parent ON sched_resources(parent_resource_id) WHERE is_active = true;
CREATE INDEX idx_sched_resources_hierarchy ON sched_resources USING GIN(to_tsvector('english', hierarchy_path));
CREATE INDEX idx_sched_resources_scheduling ON sched_resources(requires_scheduling, is_active) WHERE requires_scheduling = true AND is_active = true;
CREATE INDEX idx_sched_resources_type_level ON sched_resources(resource_type, hierarchy_level) WHERE is_active = true;

-- Pattern indexes
CREATE INDEX idx_sched_patterns_complexity ON sched_operating_patterns(complexity_level, is_visible) WHERE is_visible = true;
CREATE INDEX idx_sched_patterns_type ON sched_operating_patterns(pattern_type, is_visible) WHERE is_visible = true;
CREATE INDEX idx_sched_pattern_rules_pattern_day ON sched_pattern_rules(pattern_id, day_of_week);

-- Assignment indexes
CREATE INDEX idx_sched_assignments_resource_effective ON sched_resource_pattern_assignments(resource_id, effective_date, end_date);
CREATE INDEX idx_sched_assignments_pattern ON sched_resource_pattern_assignments(pattern_id);
CREATE INDEX idx_sched_assignments_current ON sched_resource_pattern_assignments(resource_id, effective_date) WHERE end_date IS NULL;

-- Schedule indexes (TimescaleDB optimized)
CREATE INDEX idx_sched_schedules_resource_date ON sched_equipment_schedules(resource_id, schedule_date);
CREATE INDEX idx_sched_schedules_date_status ON sched_equipment_schedules(schedule_date, schedule_status);
CREATE INDEX idx_sched_schedules_exceptions ON sched_equipment_schedules(exception_id) WHERE is_exception = true;

-- Exception indexes
CREATE INDEX idx_sched_exceptions_resource_time ON sched_schedule_exceptions(resource_id, start_datetime, end_datetime) WHERE is_active = true;
CREATE INDEX idx_sched_exceptions_type_active ON sched_schedule_exceptions(exception_type, is_active) WHERE is_active = true;

-- Holiday indexes
CREATE INDEX idx_sched_holidays_calendar_date ON sched_holidays(calendar_id, holiday_date) WHERE holiday_date IS NOT NULL;
CREATE INDEX idx_sched_resource_calendars ON sched_resource_calendar_assignments(resource_id, priority);

-- Employee indexes (Phase 2, built but unused)
CREATE INDEX idx_sched_employees_department ON sched_emp_employees(department_id, is_active) WHERE is_active = true;
CREATE INDEX idx_sched_employees_position ON sched_emp_employees(position_id, is_active) WHERE is_active = true;
CREATE INDEX idx_sched_qualifications_resource ON sched_emp_resource_qualifications(resource_id, qualification_level) WHERE is_active = true;
CREATE INDEX idx_sched_team_assignments_team ON sched_emp_team_assignments(team_id, start_date, end_date);

-- System indexes
CREATE INDEX idx_sched_config_type_active ON sched_system_configuration(config_type, is_active) WHERE is_active = true;
CREATE INDEX idx_sched_audit_table_time ON sched_audit_log(table_name, changed_at);
CREATE INDEX idx_sched_audit_user_time ON sched_audit_log(changed_by, changed_at);

-- ================================================
-- INSERT INITIAL DATA
-- ================================================

-- Insert system configuration for phase control
INSERT INTO sched_system_configuration (config_key, config_value, config_type, modified_by) VALUES
('phase2_enabled', 'false', 1, 'SYSTEM'),
('employee_module_enabled', 'false', 1, 'SYSTEM'),
('pattern_complexity_level', 'Simple', 2, 'SYSTEM'),
('schedule_generation_batch_size', '1000', 2, 'SYSTEM'),
('webhook_retry_attempts', '3', 2, 'SYSTEM'),
('cache_ttl_minutes', '60', 2, 'SYSTEM');

-- Insert built-in operating patterns
INSERT INTO sched_operating_patterns (pattern_name, pattern_code, complexity_level, pattern_type, cycle_days, weekly_hours, is_visible, is_system_pattern, configuration) VALUES
('24/7 Continuous', '24_7', 1, 1, 7, 168, true, true, '{"description": "Equipment runs continuously, 24 hours a day, 7 days a week"}'),
('Two-Shift', 'TWO_SHIFT', 1, 2, 7, 80, true, true, '{"description": "Two 8-hour shifts, Monday-Friday", "shifts": [{"day": 1, "shifts": [{"start": "06:00", "end": "14:00", "code": "D"}, {"start": "14:00", "end": "22:00", "code": "A"}]}]}'),
('Day-Only', 'DAY_ONLY', 1, 3, 7, 40, true, true, '{"description": "Single day shift, Monday-Friday 8am-5pm"}'),
('Extended Hours', 'EXTENDED', 1, 2, 7, 96, true, true, '{"description": "Extended hours, Monday-Saturday 6am-2am"}'),
('Custom Pattern', 'CUSTOM', 1, 4, 7, 0, true, true, '{"description": "User-defined custom pattern"}');

-- Insert pattern rules for built-in patterns
-- 24/7 Continuous (all days, all hours)
INSERT INTO sched_pattern_rules (pattern_id, day_of_week, start_time, end_time, is_operating, shift_code, sequence_order) 
SELECT 1, d.day, '00:00', '23:59', true, 'C', 1 
FROM generate_series(0, 6) AS d(day);

-- Two-Shift (Monday-Friday, Day & Afternoon shifts)
INSERT INTO sched_pattern_rules (pattern_id, day_of_week, start_time, end_time, is_operating, shift_code, sequence_order) VALUES
(2, 1, '06:00', '14:00', true, 'D', 1), -- Monday Day
(2, 1, '14:00', '22:00', true, 'A', 2), -- Monday Afternoon
(2, 2, '06:00', '14:00', true, 'D', 1), -- Tuesday Day
(2, 2, '14:00', '22:00', true, 'A', 2), -- Tuesday Afternoon
(2, 3, '06:00', '14:00', true, 'D', 1), -- Wednesday Day
(2, 3, '14:00', '22:00', true, 'A', 2), -- Wednesday Afternoon
(2, 4, '06:00', '14:00', true, 'D', 1), -- Thursday Day
(2, 4, '14:00', '22:00', true, 'A', 2), -- Thursday Afternoon
(2, 5, '06:00', '14:00', true, 'D', 1), -- Friday Day
(2, 5, '14:00', '22:00', true, 'A', 2); -- Friday Afternoon

-- Day-Only (Monday-Friday, 8am-5pm)
INSERT INTO sched_pattern_rules (pattern_id, day_of_week, start_time, end_time, is_operating, shift_code, sequence_order) 
SELECT 3, d.day, '08:00', '17:00', true, 'D', 1 
FROM generate_series(1, 5) AS d(day);

-- Insert default holiday calendars
INSERT INTO sched_holiday_calendars (calendar_name, country_code, region, is_default) VALUES
('South Africa Public Holidays', 'ZA', NULL, true),
('United States Federal Holidays', 'US', NULL, false),
('United Kingdom Bank Holidays', 'GB', NULL, false),
('Canada Federal Holidays', 'CA', NULL, false);

-- Insert sample holidays for South Africa
INSERT INTO sched_holidays (calendar_id, holiday_name, holiday_date, affects_scheduling) VALUES
(1, 'New Year''s Day', '2025-01-01', true),
(1, 'Human Rights Day', '2025-03-21', true),
(1, 'Freedom Day', '2025-04-27', true),
(1, 'Workers'' Day', '2025-05-01', true),
(1, 'Youth Day', '2025-06-16', true),
(1, 'National Women''s Day', '2025-08-09', true),
(1, 'Heritage Day', '2025-09-24', true),
(1, 'Day of Reconciliation', '2025-12-16', true),
(1, 'Christmas Day', '2025-12-25', true),
(1, 'Day of Goodwill', '2025-12-26', true);

-- ================================================
-- TABLE COMMENTS FOR DOCUMENTATION
-- ================================================

COMMENT ON TABLE sched_resources IS 'ISA-95 compliant equipment hierarchy with 5 levels';
COMMENT ON TABLE sched_operating_patterns IS 'Library of operating patterns (Simple in Phase 1, Advanced in Phase 2)';
COMMENT ON TABLE sched_resource_pattern_assignments IS 'Maps patterns to equipment with inheritance support';
COMMENT ON TABLE sched_equipment_schedules IS 'TimescaleDB hypertable storing generated equipment availability schedules';
COMMENT ON TABLE sched_schedule_exceptions IS 'Maintenance windows, holidays, and other schedule overrides';
COMMENT ON TABLE sched_holiday_calendars IS 'Regional holiday calendar definitions';
COMMENT ON TABLE sched_emp_employees IS 'Phase 2: Employee records (built but dormant)';
COMMENT ON TABLE sched_emp_schedules IS 'Phase 2: Individual employee work schedules (built but dormant)';
COMMENT ON TABLE sched_system_configuration IS 'Feature flags and system settings for phase control';

-- ================================================
-- FUNCTIONS AND TRIGGERS
-- ================================================

-- Update modified timestamp trigger
CREATE OR REPLACE FUNCTION update_modified_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply update triggers to relevant tables
CREATE TRIGGER tg_sched_resources_updated 
    BEFORE UPDATE ON sched_resources 
    FOR EACH ROW EXECUTE FUNCTION update_modified_timestamp();

CREATE TRIGGER tg_sched_patterns_updated 
    BEFORE UPDATE ON sched_operating_patterns 
    FOR EACH ROW EXECUTE FUNCTION update_modified_timestamp();

-- Audit logging trigger
CREATE OR REPLACE FUNCTION audit_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO sched_audit_log (table_name, record_id, action, changed_by, old_values)
        VALUES (TG_TABLE_NAME, OLD.id::text, 'DELETE', COALESCE(current_setting('app.current_user', true), 'SYSTEM'), to_jsonb(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO sched_audit_log (table_name, record_id, action, changed_by, old_values, new_values)
        VALUES (TG_TABLE_NAME, NEW.id::text, 'UPDATE', COALESCE(current_setting('app.current_user', true), 'SYSTEM'), to_jsonb(OLD), to_jsonb(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO sched_audit_log (table_name, record_id, action, changed_by, new_values)
        VALUES (TG_TABLE_NAME, NEW.id::text, 'INSERT', COALESCE(current_setting('app.current_user', true), 'SYSTEM'), to_jsonb(NEW));
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Apply audit triggers to key tables
CREATE TRIGGER tg_sched_resources_audit AFTER INSERT OR UPDATE OR DELETE ON sched_resources FOR EACH ROW EXECUTE FUNCTION audit_changes();
CREATE TRIGGER tg_sched_assignments_audit AFTER INSERT OR UPDATE OR DELETE ON sched_resource_pattern_assignments FOR EACH ROW EXECUTE FUNCTION audit_changes();
CREATE TRIGGER tg_sched_exceptions_audit AFTER INSERT OR UPDATE OR DELETE ON sched_schedule_exceptions FOR EACH ROW EXECUTE FUNCTION audit_changes();

-- Performance monitoring view
CREATE VIEW v_sched_system_health AS
SELECT 
    'Resources' as entity_type,
    COUNT(*) as total_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count,
    COUNT(*) FILTER (WHERE requires_scheduling = true) as schedulable_count
FROM sched_resources
UNION ALL
SELECT 
    'Patterns' as entity_type,
    COUNT(*) as total_count,
    COUNT(*) FILTER (WHERE is_visible = true) as active_count,
    COUNT(*) FILTER (WHERE complexity_level = 1) as schedulable_count
FROM sched_operating_patterns
UNION ALL
SELECT 
    'Assignments' as entity_type,
    COUNT(*) as total_count,
    COUNT(*) FILTER (WHERE end_date IS NULL) as active_count,
    COUNT(DISTINCT resource_id) as schedulable_count
FROM sched_resource_pattern_assignments;

-- Schedule generation summary view
CREATE VIEW v_sched_generation_summary AS
SELECT 
    resource_id,
    r.resource_name,
    COUNT(*) as schedule_count,
    MIN(schedule_date) as earliest_date,
    MAX(schedule_date) as latest_date,
    SUM(planned_hours) as total_planned_hours,
    COUNT(*) FILTER (WHERE is_exception = true) as exception_count,
    MAX(generated_at) as last_generated
FROM sched_equipment_schedules s
JOIN sched_resources r ON r.id = s.resource_id
GROUP BY resource_id, r.resource_name;
```

## 5. API Design Specifications

### RESTful Endpoint Structure

```
/api/v1/
├── /resources/                     # Equipment hierarchy management
│   ├── GET    /                   # List resources with filtering
│   ├── POST   /                   # Create new resource
│   ├── GET    /{id}              # Get resource details
│   ├── PUT    /{id}              # Update resource
│   ├── DELETE /{id}              # Delete resource
│   ├── GET    /hierarchy         # Get full ISA-95 tree
│   ├── POST   /import            # Bulk import from CSV/Excel
│   └── GET    /{id}/children     # Get child resources
├── /patterns/                     # Operating pattern management
│   ├── GET    /                   # List patterns (filtered by complexity)
│   ├── POST   /                   # Create custom pattern
│   ├── GET    /{id}              # Get pattern details
│   ├── PUT    /{id}              # Update pattern
│   ├── DELETE /{id}              # Delete pattern (if not in use)
│   └── GET    /{id}/preview      # Preview pattern schedule
├── /assignments/                  # Pattern assignment management
│   ├── GET    /resources/{id}/pattern    # Get resource pattern
│   ├── POST   /resources/{id}/pattern    # Assign pattern
│   ├── DELETE /resources/{id}/pattern    # Remove pattern
│   ├── POST   /bulk-assign              # Bulk pattern assignment
│   └── GET    /inheritance-tree         # Pattern inheritance view
├── /schedules/                    # Schedule generation and queries
│   ├── POST   /generate          # Generate schedules
│   ├── GET    /                  # Query schedules with filtering
│   ├── GET    /resources/{id}    # Get resource schedules
│   ├── DELETE /resources/{id}    # Clear resource schedules
│   └── GET    /changes           # Recent schedule changes
├── /exceptions/                   # Exception management
│   ├── GET    /                  # List exceptions
│   ├── POST   /                  # Create exception
│   ├── GET    /{id}             # Get exception details
│   ├── PUT    /{id}             # Update exception
│   ├── DELETE /{id}             # Delete exception
│   └── POST   /bulk-create      # Bulk exception creation
├── /calendars/                   # Holiday calendar management
│   ├── GET    /                  # List calendars
│   ├── GET    /{id}/holidays    # Get calendar holidays
│   ├── POST   /{id}/holidays    # Add holiday
│   └── GET    /resources/{id}/calendars # Get resource calendars
└── /oee/                         # OEE system integration
    ├── GET    /availability      # Equipment availability data
    ├── GET    /changes          # Schedule changes for OEE
    ├── POST   /webhooks         # Register webhook
    ├── GET    /webhooks         # List webhooks
    └── DELETE /webhooks/{id}    # Remove webhook

/api/v2/ (Phase 2: Additive Extensions)
├── /employees/                   # Employee management
│   ├── GET    /                 # List employees
│   ├── POST   /                 # Create employee
│   ├── GET    /{id}/schedule    # Employee schedule
│   └── PUT    /{id}/skills      # Update skills
├── /teams/                      # Team management
│   ├── GET    /                 # List teams
│   ├── GET    /{id}/members     # Team members
│   └── GET    /{id}/rotation    # Team rotation pattern
├── /coverage/                   # Coverage analysis
│   ├── GET    /gaps             # Coverage gaps
│   ├── GET    /requirements     # Staffing requirements
│   └── GET    /optimization     # Optimization suggestions
└── /patterns/                   # Enhanced patterns (v1 compatible)
    ├── GET    /                 # Includes advanced patterns
    └── GET    /{id}             # Enhanced response with employee info
```

### Sample API Specifications

#### Create Resource
```http
POST /api/v1/resources
Content-Type: application/json

{
  "resourceId": "LINE-A-001",
  "resourceName": "Production Line A",
  "resourceType": 4,
  "parentResourceId": 5,
  "requiresScheduling": true,
  "metadata": {
    "location": "Building 1",
    "capacity": "100 units/hour"
  }
}

Response: 201 Created
{
  "success": true,
  "data": {
    "id": 123,
    "resourceId": "LINE-A-001",
    "resourceName": "Production Line A",
    "resourceType": 4,
    "hierarchyPath": "Plant.Building1.Area1.LineA",
    "requiresScheduling": true,
    "isActive": true,
    "createdAt": "2025-02-01T10:00:00Z"
  }
}
```

#### Generate Schedules
```http
POST /api/v1/schedules/generate
Content-Type: application/json

{
  "startDate": "2025-02-01",
  "endDate": "2025-12-31",
  "resourceIds": null,
  "regenerate": false,
  "applyHolidays": true,
  "notifyOeeSystems": true
}

Response: 200 OK
{
  "success": true,
  "data": {
    "generatedCount": 254320,
    "resourceCount": 847,
    "dateRange": {
      "start": "2025-02-01",
      "end": "2025-12-31"
    },
    "generationTimeMs": 4523,
    "scheduledWebhooks": 3
  }
}
```

#### Get Equipment Availability (OEE Integration)
```http
GET /api/v1/oee/availability?resourceIds=123,124&startDate=2025-02-01&endDate=2025-02-28&includeShifts=true

Response: 200 OK
{
  "success": true,
  "data": {
    "period": {
      "start": "2025-02-01",
      "end": "2025-02-28"
    },
    "resources": [
      {
        "resourceId": 123,
        "resourceName": "Line A",
        "hierarchyPath": "Plant.Area1.LineA",
        "totalHours": 352,
        "availableHours": 320,
        "maintenanceHours": 32,
        "availability": [
          {
            "date": "2025-02-01",
            "plannedHours": 16,
            "shifts": [
              {
                "shiftCode": "D",
                "startTime": "2025-02-01T06:00:00Z",
                "endTime": "2025-02-01T14:00:00Z",
                "hours": 8
              },
              {
                "shiftCode": "A", 
                "startTime": "2025-02-01T14:00:00Z",
                "endTime": "2025-02-01T22:00:00Z",
                "hours": 8
              }
            ],
            "status": "Operating"
          }
        ]
      }
    ]
  }
}
```

## 6. Pattern Management System

### Simple Patterns (Phase 1)

```csharp
public static class BuiltInPatterns
{
    public static readonly List<OperatingPattern> SimplePatterns = new()
    {
        new OperatingPattern
        {
            PatternName = "24/7 Continuous",
            PatternCode = "24_7", 
            Complexity = ComplexityLevel.Simple,
            PatternType = PatternType.Continuous,
            CycleDays = 7,
            WeeklyHours = 168,
            Configuration = new PatternConfiguration
            {
                Rules = Enumerable.Range(0, 7).Select(day => new PatternRule
                {
                    DayOfWeek = day,
                    StartTime = TimeOnly.MinValue,
                    EndTime = new TimeOnly(23, 59),
                    IsOperating = true,
                    ShiftCode = "C"
                }).ToList()
            }
        },
        
        new OperatingPattern
        {
            PatternName = "Two-Shift",
            PatternCode = "TWO_SHIFT",
            Complexity = ComplexityLevel.Simple,
            PatternType = PatternType.Shift,
            CycleDays = 7,
            WeeklyHours = 80,
            Configuration = new PatternConfiguration
            {
                Rules = new List<PatternRule>
                {
                    // Monday - Day Shift
                    new() { DayOfWeek = 1, StartTime = new(6, 0), EndTime = new(14, 0), ShiftCode = "D" },
                    // Monday - Afternoon Shift  
                    new() { DayOfWeek = 1, StartTime = new(14, 0), EndTime = new(22, 0), ShiftCode = "A" },
                    // Tuesday - Day Shift
                    new() { DayOfWeek = 2, StartTime = new(6, 0), EndTime = new(14, 0), ShiftCode = "D" },
                    // Tuesday - Afternoon Shift
                    new() { DayOfWeek = 2, StartTime = new(14, 0), EndTime = new(22, 0), ShiftCode = "A" },
                    // ... Wednesday, Thursday, Friday
                }
            }
        },
        
        new OperatingPattern
        {
            PatternName = "Day-Only",
            PatternCode = "DAY_ONLY",
            Complexity = ComplexityLevel.Simple,
            PatternType = PatternType.DayOnly,
            CycleDays = 7,
            WeeklyHours = 40,
            Configuration = new PatternConfiguration
            {
                Rules = Enumerable.Range(1, 5).Select(day => new PatternRule
                {
                    DayOfWeek = day,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(17, 0),
                    IsOperating = true,
                    ShiftCode = "D"
                }).ToList()
            }
        }
    };
}
```

### Advanced Patterns (Phase 2 - Dormant)

```csharp
public static class AdvancedPatterns
{
    // DuPont 12-hour rotation (built but hidden)
    public static readonly OperatingPattern DuPont = new()
    {
        PatternName = "DuPont 12-Hour Rotation",
        PatternCode = "DUPONT",
        Complexity = ComplexityLevel.Advanced,
        PatternType = PatternType.Shift,
        CycleDays = 28, // 4-week cycle
        WeeklyHours = 42,
        IsVisible = false, // Hidden until Phase 2
        Configuration = new PatternConfiguration
        {
            Description = "4-team rotation: 4 days, 3 off, 3 nights, 1 off, 3 days, 3 off, 4 nights, 7 off",
            Teams = new[] { "A", "B", "C", "D" },
            RotationCycle = GenerateDuPontCycle()
        }
    };

    private static List<RotationDay> GenerateDuPontCycle()
    {
        // Implementation would generate 28-day cycle
        // This is built but dormant until Phase 2 activation
        return new List<RotationDay>();
    }
}
```

### Pattern Inheritance Logic

```csharp
public class PatternInheritanceService : IPatternInheritanceService
{
    public async Task<OperatingPattern?> ResolveEffectivePattern(long resourceId, DateTime date)
    {
        // 1. Check direct assignment
        var directAssignment = await GetDirectAssignment(resourceId, date);
        if (directAssignment != null)
            return directAssignment.Pattern;
            
        // 2. Walk up hierarchy for inherited pattern
        var resource = await _resourceRepository.GetByIdAsync(resourceId);
        while (resource?.ParentResourceId != null)
        {
            var parentAssignment = await GetDirectAssignment(resource.ParentResourceId.Value, date);
            if (parentAssignment != null)
                return parentAssignment.Pattern;
                
            resource = await _resourceRepository.GetByIdAsync(resource.ParentResourceId.Value);
        }
        
        return null; // No pattern found
    }

    public async Task ApplyInheritance(long parentResourceId, long patternId, DateTime effectiveDate, bool applyToChildren)
    {
        if (!applyToChildren) return;
        
        var children = await _resourceRepository.GetChildrenAsync(parentResourceId);
        
        foreach (var child in children)
        {
            // Only apply if child doesn't have override
            var existingAssignment = await GetDirectAssignment(child.Id, effectiveDate);
            if (existingAssignment?.IsOverride != true)
            {
                await _patternAssignmentRepository.CreateAsync(new PatternAssignment(
                    child.Id,
                    patternId,
                    effectiveDate,
                    null, // No end date
                    false, // Not an override
                    parentResourceId, // Inherited from
                    "SYSTEM"
                ));
            }
            
            // Recursively apply to grandchildren
            await ApplyInheritance(child.Id, patternId, effectiveDate, true);
        }
    }
}
```

## 7. Schedule Generation Engine

### High-Performance Generation

```csharp
public class ScheduleGenerationEngine : IScheduleGenerationEngine
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IPatternRepository _patternRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IHolidayCalendarService _holidayService;
    private readonly ILogger<ScheduleGenerationEngine> _logger;

    public async Task<ScheduleGenerationResult> GenerateSchedulesAsync(
        DateTime startDate, 
        DateTime endDate, 
        IEnumerable<long>? resourceIds = null,
        bool regenerate = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 1. Get resources to schedule
        var resources = resourceIds?.Any() == true 
            ? await _resourceRepository.GetByIdsAsync(resourceIds, cancellationToken)
            : await _resourceRepository.GetSchedulableResourcesAsync(cancellationToken);
            
        _logger.LogInformation("Generating schedules for {ResourceCount} resources from {StartDate} to {EndDate}", 
            resources.Count, startDate, endDate);
            
        var generatedCount = 0;
        var batchSize = 1000;
        
        // 2. Process in batches for memory efficiency
        var resourceBatches = resources.Chunk(batchSize);
        
        foreach (var batch in resourceBatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var schedules = new List<EquipmentSchedule>();
            
            // 3. Generate schedules for batch
            await foreach (var schedule in GenerateSchedulesForBatch(batch, startDate, endDate, cancellationToken))
            {
                schedules.Add(schedule);
                
                if (schedules.Count >= batchSize)
                {
                    await _scheduleRepository.BulkInsertAsync(schedules, cancellationToken);
                    generatedCount += schedules.Count;
                    schedules.Clear();
                    
                    _logger.LogDebug("Generated {GeneratedCount} schedules so far", generatedCount);
                }
            }
            
            // 4. Insert remaining schedules
            if (schedules.Any())
            {
                await _scheduleRepository.BulkInsertAsync(schedules, cancellationToken);
                generatedCount += schedules.Count;
            }
        }
        
        stopwatch.Stop();
        
        _logger.LogInformation("Generated {GeneratedCount} schedules in {ElapsedMs}ms", 
            generatedCount, stopwatch.ElapsedMilliseconds);
            
        return new ScheduleGenerationResult
        {
            GeneratedCount = generatedCount,
            ResourceCount = resources.Count,
            GenerationTimeMs = stopwatch.ElapsedMilliseconds,
            DateRange = new DateRange(startDate, endDate)
        };
    }

    private async IAsyncEnumerable<EquipmentSchedule> GenerateSchedulesForBatch(
        IEnumerable<Resource> resources,
        DateTime startDate,
        DateTime endDate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            foreach (var resource in resources)
            {
                // 1. Get effective pattern for date
                var pattern = await _patternInheritanceService.ResolveEffectivePattern(resource.Id, currentDate);
                if (pattern == null) continue;
                
                // 2. Check for exceptions
                var exceptions = await _scheduleRepository.GetExceptionsForDate(resource.Id, currentDate);
                
                // 3. Check holidays
                var holidays = await _holidayService.GetHolidaysForDate(resource.Id, currentDate);
                
                // 4. Generate shifts for the day
                var shifts = pattern.GetShiftsForDate(currentDate);
                
                foreach (var shift in shifts)
                {
                    var status = DetermineScheduleStatus(exceptions, holidays, shift);
                    
                    var schedule = new EquipmentSchedule
                    {
                        ResourceId = resource.Id,
                        ScheduleDate = currentDate,
                        ShiftCode = shift.ShiftCode,
                        PlannedStartTime = currentDate.Add(shift.StartTime.ToTimeSpan()),
                        PlannedEndTime = currentDate.Add(shift.EndTime.ToTimeSpan()),
                        PlannedHours = shift.CalculateHours(),
                        Status = status,
                        PatternId = pattern.Id,
                        IsException = exceptions.Any(),
                        GeneratedAt = DateTime.UtcNow
                    };
                    
                    yield return schedule;
                }
            }
            
            currentDate = currentDate.AddDays(1);
        }
    }

    private ScheduleStatus DetermineScheduleStatus(
        List<ScheduleException> exceptions, 
        List<Holiday> holidays, 
        ShiftConfiguration shift)
    {
        // Priority order: Exception > Holiday > Normal
        if (exceptions.Any(e => e.Impact == ImpactType.NoOperation))
            return ScheduleStatus.Maintenance;
            
        if (holidays.Any(h => h.AffectsScheduling))
            return ScheduleStatus.Holiday;
            
        return ScheduleStatus.Operating;
    }
}

public record ScheduleGenerationResult
{
    public int GeneratedCount { get; init; }
    public int ResourceCount { get; init; }
    public long GenerationTimeMs { get; init; }
    public DateRange DateRange { get; init; }
}
```

## 8. Integration Architecture

### OEE System Integration

```csharp
public class OeeIntegrationController : ControllerBase
{
    private readonly IScheduleQueryService _scheduleService;
    private readonly IWebhookService _webhookService;

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<AvailabilityResponse>>> GetAvailability(
        [FromQuery] string? resourceIds,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeShifts = false,
        [FromQuery] string format = "json")
    {
        var resourceIdList = resourceIds?.Split(',').Select(long.Parse).ToList();
        
        var availability = await _scheduleService.GetAvailabilityAsync(
            resourceIdList, startDate, endDate, includeShifts);
            
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = _csvExporter.Export(availability);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "availability.csv");
        }
        
        return Ok(ApiResponse.Success(availability));
    }

    [HttpGet("changes")]  
    public async Task<ActionResult<ApiResponse<List<ScheduleChange>>>> GetChanges(
        [FromQuery] DateTime? since,
        [FromQuery] string? resourceIds,
        [FromQuery] string? changeTypes)
    {
        var changes = await _scheduleService.GetChangesAsync(since, resourceIds, changeTypes);
        return Ok(ApiResponse.Success(changes));
    }

    [HttpPost("webhooks")]
    public async Task<ActionResult<ApiResponse<WebhookRegistration>>> RegisterWebhook(
        [FromBody] RegisterWebhookRequest request)
    {
        var registration = await _webhookService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetWebhook), new { id = registration.Id }, ApiResponse.Success(registration));
    }
}
```

### Webhook System

```csharp
public class WebhookNotificationService : IWebhookNotificationService
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationService> _logger;

    public async Task NotifyScheduleChanged(ScheduleChangedEvent @event)
    {
        var webhooks = await _webhookRepository.GetActiveWebhooksAsync(@event.ResourceId);
        
        var tasks = webhooks.Select(webhook => NotifyWebhook(webhook, @event));
        await Task.WhenAll(tasks);
    }

    private async Task NotifyWebhook(WebhookRegistration webhook, ScheduleChangedEvent @event)
    {
        try
        {
            var payload = CreateWebhookPayload(@event);
            var signature = CreateSignature(payload, webhook.Secret);
            
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", @event.EventType);
            request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            
            var response = await client.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook delivery failed: {Url}, Status: {StatusCode}", 
                    webhook.Url, response.StatusCode);
                await _webhookRepository.RecordDeliveryFailureAsync(webhook.Id);
            }
            else
            {
                await _webhookRepository.RecordDeliverySuccessAsync(webhook.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery error: {Url}", webhook.Url);
            await _webhookRepository.RecordDeliveryFailureAsync(webhook.Id);
        }
    }

    private string CreateWebhookPayload(ScheduleChangedEvent @event)
    {
        var payload = new
        {
            @event = @event.EventType,
            timestamp = @event.Timestamp,
            data = new
            {
                resourceId = @event.ResourceId,
                date = @event.Date,
                oldValue = @event.OldValue,
                newValue = @event.NewValue
            }
        };
        
        return JsonSerializer.Serialize(payload);
    }

    private string CreateSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLower();
    }
}
```

## 9. Implementation Timeline

### Week 1-2: Foundation & Database

#### Week 1: Project Setup & Core Infrastructure
**Days 1-3: Project Structure**
- [ ] Create solution structure following Clean Architecture
- [ ] Set up project dependencies and NuGet packages
- [ ] Configure logging, configuration, and DI containers
- [ ] Set up development environment and Docker containers

**Days 4-5: Database Schema**
- [ ] Create complete TimescaleDB schema (Phase 1 + Phase 2 tables)
- [ ] Set up Entity Framework Core models and configurations
- [ ] Create database migrations and seed data
- [ ] Test database connectivity and performance

#### Week 2: Domain Layer & Core Services  
**Days 1-3: Domain Entities**
- [ ] Implement core entities: Resource, OperatingPattern, PatternAssignment
- [ ] Create value objects: TimeRange, Availability, IsaHierarchyPath
- [ ] Build domain services: PatternInheritanceService, HolidayCalendarService
- [ ] Write comprehensive unit tests for domain logic

**Days 4-5: Repository Pattern**
- [ ] Implement repository interfaces and EF Core implementations
- [ ] Create base repository with common CRUD operations
- [ ] Set up caching layer with Redis integration
- [ ] Performance test with large datasets (10,000+ resources)

### Week 3-4: Application Layer & Pattern Management

#### Week 3: CQRS & MediatR Implementation
**Days 1-3: Commands & Queries**
- [ ] Implement CQRS commands: CreateResource, AssignPattern, GenerateSchedules
- [ ] Build query handlers: GetResourceHierarchy, GetAvailability
- [ ] Add FluentValidation for all requests
- [ ] Set up command/query pipelines with logging and caching

**Days 4-5: Pattern Management**
- [ ] Create built-in pattern library (24/7, Two-Shift, Day-Only, Extended, Custom)
- [ ] Implement pattern inheritance resolution logic
- [ ] Build pattern assignment workflow with override support
- [ ] Test pattern inheritance through 5-level ISA-95 hierarchy

#### Week 4: Schedule Generation Engine
**Days 1-3: Core Generation Logic**
- [ ] Build high-performance schedule generation engine
- [ ] Implement batch processing for large equipment counts
- [ ] Add exception handling (maintenance windows, holidays)
- [ ] Create async background service for long-running generations

**Days 4-5: Holiday Integration**
- [ ] Implement holiday calendar system
- [ ] Create regional calendar support (ZA, US, UK, CA)
- [ ] Build holiday impact on schedule generation
- [ ] Test with complex holiday scenarios

### Week 5-6: API Layer & OEE Integration

#### Week 5: RESTful API Development
**Days 1-3: Core Controllers**
- [ ] Build ResourcesController with ISA-95 hierarchy endpoints
- [ ] Implement PatternsController with filtering and preview
- [ ] Create SchedulesController with generation and query endpoints
- [ ] Add comprehensive API validation and error handling

**Days 4-5: Advanced API Features**
- [ ] Implement bulk operations (import, assign, generate)
- [ ] Add advanced filtering and sorting capabilities
- [ ] Create export functionality (CSV, Excel, JSON)
- [ ] Set up OpenAPI/Swagger documentation

#### Week 6: OEE System Integration
**Days 1-3: Integration Endpoints**
- [ ] Build OeeController with availability data endpoints
- [ ] Implement webhook registration and management system
- [ ] Create real-time change notification system with SignalR
- [ ] Add API key authentication for external systems

**Days 4-5: Webhook System**
- [ ] Implement webhook delivery with retry logic
- [ ] Add webhook signature verification for security  
- [ ] Create delivery tracking and failure handling
- [ ] Test webhook integration with sample OEE systems

### Week 7-8: Testing, Performance & Deployment

#### Week 7: Comprehensive Testing
**Days 1-3: Integration Testing**
- [ ] Create end-to-end API tests covering all endpoints
- [ ] Build integration tests for database operations
- [ ] Test schedule generation performance with 10,000+ equipment
- [ ] Validate pattern inheritance across complex hierarchies

**Days 4-5: System Testing**
- [ ] Load test API endpoints for 500+ concurrent users
- [ ] Test webhook delivery under high volume
- [ ] Validate schedule generation performance (< 5 seconds for 1000 items)
- [ ] Memory and resource usage optimization

#### Week 8: Deployment & Documentation
**Days 1-3: Production Preparation**
- [ ] Set up CI/CD pipelines with automated testing
- [ ] Create Docker containers and Kubernetes deployments
- [ ] Configure monitoring with Prometheus and Grafana
- [ ] Set up centralized logging with structured logs

**Days 4-5: Go-Live Preparation**
- [ ] Create comprehensive API documentation
- [ ] Build user training materials and quick start guides
- [ ] Set up staging environment for customer testing
- [ ] Prepare rollback procedures and emergency contacts

## 10. Phase 2 Activation Plan

### Activation Prerequisites
- [ ] Phase 1 stable for 3+ months with <0.1% error rate
- [ ] Customer demand validated (3+ requests for employee features)
- [ ] Development team capacity available
- [ ] Business case showing positive ROI

### Technical Activation Steps

#### Step 1: Configuration Changes (No Deployment)
```sql
-- Enable Phase 2 features
UPDATE sched_system_configuration 
SET config_value = 'true', modified_by = 'ADMIN', modified_at = NOW()
WHERE config_key = 'phase2_enabled';

UPDATE sched_system_configuration 
SET config_value = 'true', modified_by = 'ADMIN', modified_at = NOW()
WHERE config_key = 'employee_module_enabled';

UPDATE sched_system_configuration 
SET config_value = 'Advanced', modified_by = 'ADMIN', modified_at = NOW()
WHERE config_key = 'pattern_complexity_level';

-- Make advanced patterns visible
UPDATE sched_operating_patterns 
SET is_visible = true, updated_at = NOW()
WHERE complexity_level IN (2, 3); -- Advanced and Expert patterns
```

#### Step 2: Data Population
```sql
-- Begin populating employee tables
INSERT INTO sched_emp_departments (department_name, department_code) VALUES
('Production', 'PROD'),
('Maintenance', 'MAINT'),
('Quality', 'QC');

INSERT INTO sched_emp_positions (position_name, position_code, department_id) VALUES
('Operator', 'OP', 1),
('Lead Operator', 'LEAD_OP', 1),
('Maintenance Technician', 'TECH', 2);

-- Add sample employees (migration from existing HR systems)
-- This would typically be done via bulk import from HR systems
```

#### Step 3: UI Module Activation
- Deploy frontend with Phase 2 modules enabled (feature flags)
- No API changes required - endpoints already exist
- Employee modules become visible in navigation
- Advanced patterns appear in pattern library

#### Step 4: Advanced Pattern Activation
```csharp
// Advanced patterns that were built but hidden become available
public static class AdvancedPatternLibrary
{
    public static readonly List<OperatingPattern> Phase2Patterns = new()
    {
        new OperatingPattern
        {
            PatternName = "DuPont 12-Hour",
            PatternCode = "DUPONT",
            Complexity = ComplexityLevel.Advanced,
            PatternType = PatternType.Shift,
            CycleDays = 28,
            IsVisible = true, // Now visible
            Configuration = DuPontConfiguration
        },
        
        new OperatingPattern
        {
            PatternName = "Pitman 14-Day",
            PatternCode = "PITMAN",
            Complexity = ComplexityLevel.Advanced,
            PatternType = PatternType.Shift,
            CycleDays = 14,
            IsVisible = true, // Now visible
            Configuration = PitmanConfiguration
        }
    };
}
```

### Migration Verification Checklist
- [ ] All Phase 1 functionality remains unchanged
- [ ] Existing API endpoints return same responses
- [ ] Employee tables populated successfully
- [ ] Advanced patterns visible in UI
- [ ] v2 API endpoints accessible
- [ ] Coverage analysis features operational
- [ ] Team management functional
- [ ] Performance metrics maintained

## 11. Success Metrics & Monitoring

### Phase 1 Success Criteria (Months 1-3)

#### Technical Metrics
```yaml
Performance Targets:
  - API Response Time: < 200ms (95th percentile)
  - Schedule Generation: < 5s for 1000 equipment items
  - System Uptime: > 99.9%
  - Database Query Performance: < 100ms average
  - Memory Usage: < 2GB under normal load
  - Error Rate: < 0.1%

Scale Targets:
  - Equipment Items: 10,000+ per installation
  - Schedule Records: 10M+ total records  
  - Concurrent Users: 500+ simultaneous
  - API Throughput: 10,000 requests/hour
  - Webhook Delivery: < 30 seconds notification time
```

#### Business Metrics
```yaml
Adoption Metrics:
  - Setup Time: < 30 minutes average
  - Active Customers: 5+ in first quarter
  - User Satisfaction: > 80% prefer over Excel
  - Support Tickets: < 5 per week
  - Feature Usage: > 90% of equipment scheduled

Quality Metrics:
  - Schedule Accuracy: > 99.9%
  - Data Integrity: Zero data loss
  - OEE Integration Success: 100% without custom development
  - Customer Retention: > 95%
```

### Monitoring Implementation

#### Application Performance Monitoring
```csharp
public class PerformanceMetrics
{
    private static readonly Counter ScheduleGenerationCounter = Metrics
        .CreateCounter("schedule_generation_total", "Total schedule generation operations");
        
    private static readonly Histogram ScheduleGenerationDuration = Metrics
        .CreateHistogram("schedule_generation_duration_seconds", "Schedule generation duration");
        
    private static readonly Gauge ActiveResourcesGauge = Metrics
        .CreateGauge("active_resources_total", "Number of active resources");
        
    private static readonly Counter ApiRequestCounter = Metrics
        .CreateCounter("api_requests_total", "Total API requests", "method", "endpoint");
        
    private static readonly Counter WebhookDeliveryCounter = Metrics
        .CreateCounter("webhook_delivery_total", "Webhook delivery attempts", "status");

    public static void RecordScheduleGeneration(int resourceCount, double durationSeconds)
    {
        ScheduleGenerationCounter.Inc();
        ScheduleGenerationDuration.Observe(durationSeconds);
        _logger.LogInformation("Generated schedules for {ResourceCount} resources in {Duration}s", 
            resourceCount, durationSeconds);
    }
}
```

#### Health Checks
```csharp
public class SchedulingSystemHealthChecks
{
    public static IServiceCollection AddSchedulingHealthChecks(this IServiceCollection services)
    {
        return services
            .AddHealthChecks()
            .AddDbContextCheck<SchedulingDbContext>("database")
            .AddRedis("redis")
            .AddCheck<PatternLibraryHealthCheck>("pattern_library")
            .AddCheck<ScheduleGenerationHealthCheck>("schedule_generation")
            .AddCheck<WebhookDeliveryHealthCheck>("webhook_delivery");
    }
}

public class PatternLibraryHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var patternCount = await _patternRepository.GetActivePatternCountAsync();
        
        if (patternCount < 5) // Should have at least built-in patterns
        {
            return HealthCheckResult.Unhealthy($"Only {patternCount} patterns available");
        }
        
        return HealthCheckResult.Healthy($"{patternCount} patterns available");
    }
}
```

#### Grafana Dashboard Configuration
```yaml
# Equipment Scheduling System Dashboard
dashboard:
  title: "Equipment Scheduling System"
  panels:
    - title: "System Overview"
      metrics:
        - active_resources_total
        - schedule_generation_total
        - api_requests_total
        - webhook_delivery_total
        
    - title: "Performance Metrics"
      metrics:
        - api_response_duration_seconds
        - schedule_generation_duration_seconds
        - database_query_duration_seconds
        
    - title: "Error Tracking"
      metrics:
        - api_requests_total{status=~"4.."}
        - api_requests_total{status=~"5.."}
        - webhook_delivery_total{status="failed"}
        
    - title: "Business Metrics"
      metrics:
        - resources_with_patterns_total
        - schedules_generated_daily
        - active_webhooks_total
```

## 12. Risk Management & Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| **Database Performance Issues** | Medium | High | - Strategic indexing on all query patterns<br>- TimescaleDB partitioning by date<br>- Read replicas for query-heavy operations<br>- Connection pooling and query optimization |
| **Schedule Generation Timeouts** | Medium | High | - Batch processing with progress tracking<br>- Async background jobs with queuing<br>- Performance testing with 10,000+ resources<br>- Timeout handling and partial completion |
| **API Integration Failures** | Low | High | - Comprehensive webhook retry logic<br>- Circuit breaker pattern for external calls<br>- API versioning for backward compatibility<br>- Extensive integration testing |
| **Memory Exhaustion** | Low | Medium | - Streaming data processing<br>- Garbage collection optimization<br>- Memory profiling and leak detection<br>- Resource usage monitoring |

### Business Risks

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| **Low Customer Adoption** | Medium | High | - Excel import for easy migration<br>- 30-minute setup target<br>- Comprehensive training materials<br>- Customer success program |
| **Scope Creep** | High | Medium | - Strict Phase 1 boundary enforcement<br>- Feature flag system for controlled expansion<br>- Clear roadmap communication<br>- Change request process |
| **Competition** | Low | Medium | - Fast delivery (8-week timeline)<br>- Superior OEE integration<br>- Customer feedback incorporation<br>- Continuous feature enhancement |
| **Phase 2 Activation Issues** | Medium | Medium | - Complete Phase 2 testing in development<br>- Phased rollout with rollback capability<br>- Customer communication plan<br>- Support team preparation |

### Mitigation Implementation

#### Database Performance
```sql
-- Proactive performance monitoring
CREATE VIEW v_performance_metrics AS
SELECT 
    schemaname,
    tablename,
    attname,
    n_distinct,
    correlation,
    most_common_vals
FROM pg_stats 
WHERE schemaname = 'public' 
  AND tablename LIKE 'sched_%';

-- Automated index monitoring  
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes 
WHERE idx_scan < 100  -- Unused indexes
ORDER BY schemaname, tablename;
```

#### Circuit Breaker Implementation
```csharp
public class WebhookCircuitBreaker
{
    private readonly CircuitBreakerPolicy _circuitBreaker;

    public WebhookCircuitBreaker()
    {
        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) => 
                    _logger.LogWarning("Webhook circuit breaker opened for {Duration}", duration),
                onReset: () => 
                    _logger.LogInformation("Webhook circuit breaker reset"));
    }

    public async Task<bool> ExecuteWebhookAsync(Func<Task> webhookCall)
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(webhookCall);
            return true;
        }
        catch (CircuitBreakerOpenException)
        {
            _logger.LogWarning("Webhook call skipped - circuit breaker open");
            return false;
        }
    }
}
```

## 13. Quality Assurance Plan

### Testing Strategy

#### Unit Testing (Target: 90% Coverage)
```csharp
// Domain entity tests
[TestClass]
public class ResourceTests
{
    [TestMethod]
    public void CreateResource_WithValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange
        var resourceId = "LINE-A-001";
        var resourceName = "Production Line A";
        var resourceType = ResourceType.WorkUnit;

        // Act
        var resource = new Resource(resourceId, resourceName, resourceType);

        // Assert
        Assert.AreEqual(resourceId, resource.ResourceId);
        Assert.AreEqual(resourceName, resource.ResourceName);
        Assert.AreEqual(resourceType, resource.ResourceType);
        Assert.IsTrue(resource.IsActive);
    }

    [TestMethod]
    public void AssignPattern_WithValidPattern_ShouldCreateAssignment()
    {
        // Arrange
        var resource = CreateTestResource();
        var patternId = 1L;
        var effectiveDate = DateTime.Today;

        // Act
        resource.AssignPattern(patternId, effectiveDate, "TestUser");

        // Assert
        Assert.AreEqual(1, resource.PatternAssignments.Count);
        Assert.AreEqual(patternId, resource.PatternAssignments[0].PatternId);
    }
}

// Service integration tests
[TestClass]
public class ScheduleGenerationServiceTests
{
    [TestMethod]
    public async Task GenerateSchedules_For1000Resources_ShouldCompleteUnder5Seconds()
    {
        // Arrange
        var resources = CreateTestResources(1000);
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddMonths(12);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _scheduleService.GenerateSchedulesAsync(startDate, endDate);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Generation took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        Assert.AreEqual(1000, result.ResourceCount);
    }
}
```

#### Integration Testing
```csharp
[TestClass]
public class SchedulingApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public SchedulingApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [TestMethod]
    public async Task CreateResource_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateResourceRequest
        {
            ResourceId = "TEST-001",
            ResourceName = "Test Resource",
            ResourceType = 5,
            RequiresScheduling = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/resources", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResourceDto>>();
        Assert.IsTrue(result.Success);
        Assert.AreEqual("TEST-001", result.Data.ResourceId);
    }

    [TestMethod]
    public async Task GenerateSchedules_WithLargeDataset_ShouldMaintainPerformance()
    {
        // Arrange
        await SeedTestResources(5000);
        var request = new GenerateSchedulesRequest
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1)
        };

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.PostAsJsonAsync("/api/v1/schedules/generate", request);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(duration.TotalSeconds < 10, $"Generation took {duration.TotalSeconds}s");
    }
}
```

#### Load Testing (JMeter/NBomber)
```csharp
public class SchedulingApiLoadTests
{
    [Fact]
    public void LoadTest_ConcurrentUsers_ShouldMaintainPerformance()
    {
        var scenario = Scenario.Create("api_load_test", async context =>
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://localhost:5001/api/v1/resources");
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5)),
            Simulation.KeepConstant(copies: 500, during: TimeSpan.FromMinutes(10))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
```

### Performance Benchmarks
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ScheduleGenerationBenchmarks
{
    private List<Resource> _resources;
    private ScheduleGenerationService _service;

    [GlobalSetup]
    public void Setup()
    {
        _resources = GenerateTestResources(1000);
        _service = CreateScheduleService();
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)] 
    [Arguments(10000)]
    public async Task GenerateSchedules(int resourceCount)
    {
        var resources = _resources.Take(resourceCount);
        await _service.GenerateSchedulesAsync(
            DateTime.Today, 
            DateTime.Today.AddYears(1), 
            resources.Select(r => r.Id));
    }

    [Benchmark]
    public async Task PatternInheritanceResolution()
    {
        foreach (var resource in _resources.Take(100))
        {
            await _service.ResolveEffectivePattern(resource.Id, DateTime.Today);
        }
    }
}
```

## 14. Documentation Plan

### Technical Documentation

#### API Documentation (OpenAPI 3.0)
```yaml
openapi: 3.0.0
info:
  title: Equipment Scheduling System API
  version: 1.0.0
  description: |
    Equipment availability scheduling for manufacturing OEE systems.
    
    ## Authentication
    Use API key in header: `X-API-Key: your_api_key`
    
    ## Rate Limits
    - 1000 requests/hour for API keys
    - 500 requests/hour for OAuth tokens

paths:
  /api/v1/resources:
    get:
      summary: List equipment resources
      parameters:
        - name: search
          in: query
          schema:
            type: string
          description: Search term for name/code
        - name: requiresScheduling
          in: query
          schema:
            type: boolean
          description: Filter by scheduling requirement
      responses:
        200:
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/ResourceDto'
```

#### Database Schema Documentation
```sql
-- Generate schema documentation
SELECT 
    t.table_name,
    t.table_comment,
    c.column_name,
    c.data_type,
    c.is_nullable,
    c.column_default,
    c.column_comment
FROM information_schema.tables t
JOIN information_schema.columns c ON t.table_name = c.table_name
WHERE t.table_schema = 'public' 
  AND t.table_name LIKE 'sched_%'
ORDER BY t.table_name, c.ordinal_position;
```

#### Architecture Decision Records (ADRs)
```markdown
# ADR-001: Use TimescaleDB for Schedule Storage

## Status
Accepted

## Context
Need to store millions of schedule records with time-series characteristics.
Requirements: High performance queries, efficient storage, time-based partitioning.

## Decision
Use TimescaleDB extension on PostgreSQL for schedule storage.

## Consequences
- Pros: Automatic partitioning, time-series optimizations, SQL compatibility
- Cons: Additional operational complexity, PostgreSQL dependency

## Implementation
- Convert sched_equipment_schedules to hypertable
- Use time-based partitioning on schedule_date
- Leverage TimescaleDB aggregation functions
```

### User Documentation

#### Quick Start Guide
```markdown
# Equipment Scheduling Quick Start

## 1. Setup (5 minutes)
1. Access the system at `https://your-domain.com`
2. Login with your credentials
3. Navigate to **Resources** > **Import**
4. Download the Excel template

## 2. Import Equipment (10 minutes)
1. Fill the template with your equipment hierarchy
2. Upload the completed file
3. Review and fix any validation errors
4. Click **Import** to create resources

## 3. Assign Patterns (10 minutes)
1. Go to **Patterns** > **Library**
2. Select a pattern (e.g., "Two-Shift")
3. Drag the pattern to your root area
4. Confirm inheritance to child equipment

## 4. Generate Schedules (5 minutes)
1. Navigate to **Schedules** > **Generate**
2. Select date range (recommend 12 months)
3. Click **Generate Schedules**
4. Monitor progress in the dashboard

## 5. Verify OEE Integration
1. Check **Integration** > **OEE Systems**
2. Verify webhook delivery status
3. Test availability API endpoint
4. Confirm data appears in your OEE system
```

#### User Training Materials
```markdown
# Equipment Scheduling Training Manual

## Module 1: Understanding ISA-95 Hierarchy
- Enterprise > Site > Area > Work Center > Work Unit
- Each level can have its own operating pattern
- Child equipment inherits parent patterns automatically
- Override inheritance when needed for specific equipment

## Module 2: Operating Patterns
- **24/7 Continuous**: Equipment runs all the time
- **Two-Shift**: Day and afternoon shifts, Monday-Friday
- **Day-Only**: Single shift, business hours only
- **Extended**: Longer hours, may include weekends
- **Custom**: Define your own operating schedule

## Module 3: Schedule Generation
- Generates availability data for OEE calculations
- Considers holidays and maintenance windows
- Updates OEE systems automatically via webhooks
- Can regenerate without duplicating data

## Module 4: Exception Management
- Maintenance Windows: Planned downtime
- Breakdowns: Unplanned equipment failures
- Holidays: Regional non-working days
- All exceptions automatically update schedules
```

### Developer Documentation

#### Contributing Guide
```markdown
# Contributing to Equipment Scheduling System

## Development Setup
1. Clone repository: `git clone https://github.com/company/equipment-scheduling`
2. Install .NET 9 SDK
3. Start dependencies: `docker-compose up -d`
4. Run migrations: `dotnet ef database update`
5. Start application: `dotnet run`

## Code Standards
- Follow Clean Architecture principles
- Use CQRS pattern for application layer
- Write unit tests for all business logic
- Document public APIs with XML comments
- Use FluentValidation for input validation

## Pull Request Process
1. Create feature branch from `main`
2. Write tests for new functionality
3. Ensure all tests pass locally
4. Update API documentation if needed
5. Submit PR with clear description
```

## 15. Conclusion

This comprehensive technical foundation plan provides a complete roadmap for implementing the Equipment Scheduling System. The design follows Clean Architecture principles, leverages the existing TimescaleDB infrastructure, and provides a seamless path from Phase 1 equipment scheduling to Phase 2 workforce management.

### Key Success Factors

1. **Complete Architecture from Day One**: Both Phase 1 and Phase 2 components are built initially, with Phase 2 features remaining dormant until activation.

2. **Integration-First Design**: Seamless integration with existing OEE systems through shared database and well-defined APIs.

3. **Performance by Design**: TimescaleDB hypertables, strategic indexing, and batch processing ensure sub-5-second schedule generation for 1000+ equipment items.

4. **Zero-Migration Activation**: Phase 2 activation requires only configuration changes, not database migrations or code deployments.

5. **Production-Ready Foundation**: Comprehensive monitoring, error handling, testing, and documentation ensure enterprise-grade reliability.

The 8-week implementation timeline is aggressive but achievable with the detailed technical specifications provided. The system will deliver immediate value in Phase 1 while providing a solid foundation for future workforce management expansion.

### Next Steps

1. **Team Formation**: Assemble development team with Clean Architecture and TimescaleDB experience
2. **Environment Setup**: Provision development, testing, and staging environments  
3. **Stakeholder Alignment**: Review plan with business stakeholders and OEE system owners
4. **Phase 1 Kickoff**: Begin Week 1 implementation following the detailed timeline

This foundation plan ensures the Equipment Scheduling System will become the authoritative source for manufacturing equipment availability, directly supporting accurate OEE calculations and enabling data-driven operational improvements.