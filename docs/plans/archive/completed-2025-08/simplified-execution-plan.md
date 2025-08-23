# Simplified Execution Plan - OEE Cleanup & Module Separation

## Executive Summary

Since the OEE system is not yet in production, we can take a much simpler, more aggressive approach to cleanup and module separation. No data migration, no rollback procedures, no production concerns - just clean code restructuring.

## Complete System Context

### Current Architecture Foundation

```
Industrial.Adam.Logger (Existing - No Changes Needed)
├── Device communication and data collection
├── TimescaleDB counter data storage  
├── REST API for counter data queries
└── Foundation for all manufacturing modules

Frontend Applications (Existing - No Changes Needed)
├── /adam-counter-frontend (Port 3000) - Device management
├── /oee-app/oee-interface (Port 3001) - Manufacturing analytics  
└── Future: Equipment Scheduling frontend

Industrial.Adam.Oee (Current - Requires Cleanup)
├── Over-implemented Phase 3 features
├── Wrong shift management concept
├── Complex scheduling that belongs elsewhere
└── Target: Simple performance monitoring

Industrial.Adam.EquipmentScheduling (Future - To Be Created)
└── Equipment operating patterns and schedule generation
```

## GitHub Strategy

### Branch Structure

```
main
├── feature/oee-cleanup              # OEE simplification work
├── feature/equipment-scheduling     # New Equipment Scheduling module
└── feature/integration              # Integration between modules

Note: Logger and Frontend applications remain unchanged
```

### Pull Request Strategy

#### PR #1: OEE Cleanup (Week 1)
```bash
Branch: feature/oee-cleanup
Title: "refactor: Remove Phase 3 over-implementations from OEE module"
Description: 
  - Remove batch tracking
  - Remove wrong shift implementation  
  - Remove complex job scheduling
  - Remove over-engineered canonical patterns
  - Simplify to core OEE monitoring
```

#### PR #2: Equipment Scheduling Foundation (Week 2-3)
```bash
Branch: feature/equipment-scheduling
Title: "feat: Add Equipment Scheduling System foundation"
Description:
  - Create new Industrial.Adam.EquipmentScheduling module
  - Add ISA-95 equipment hierarchy
  - Add basic operating patterns
  - Add database schema with sched_* prefix
  - Add API foundation
```

#### PR #3: Integration (Week 4)
```bash
Branch: feature/integration
Title: "feat: Integrate OEE with Equipment Scheduling"
Description:
  - Add IEquipmentAvailabilityService interface
  - Update OEE calculations to use planned availability
  - Add API client in OEE for Equipment Scheduling
  - Update documentation
```

### Commit Strategy

```bash
# Use conventional commits
feat: Add Equipment Scheduling module structure
refactor: Remove batch tracking from OEE
fix: Update OEE to use simple job queue
docs: Update architecture documentation
test: Add integration tests for module communication
chore: Remove unused Phase 3 migrations
```

## Simplified Technical Execution

### System Component Status

**✅ No Changes Required:**
- **Industrial.Adam.Logger**: Solid foundation, working perfectly
- **Counter Frontend** (`/adam-counter-frontend`): Device management interface complete
- **OEE Frontend** (`/oee-app/oee-interface`): Analytics interface ready for simplified backend
- **TimescaleDB**: Counter data collection working, ready for OEE consumption

**🔧 Requires Cleanup:**
- **Industrial.Adam.Oee**: Remove over-implementations, focus on performance monitoring

**🆕 To Be Created:**
- **Industrial.Adam.EquipmentScheduling**: New module for equipment operating patterns

### Week 1: Aggressive OEE Cleanup

#### Day 1-2: Delete Phase 3 Over-implementations
```bash
# Simply delete these files - no migration needed
# Logger and Frontend applications remain untouched
rm -rf src/Industrial.Adam.Oee/Domain/Entities/Batch.cs
rm -rf src/Industrial.Adam.Oee/Domain/Entities/Shift.cs
rm -rf src/Industrial.Adam.Oee/Domain/Entities/JobSchedule.cs
rm -rf src/Industrial.Adam.Oee/Domain/Entities/QualityInspection.cs
rm -rf src/Industrial.Adam.Oee/Domain/ValueObjects/CanonicalReference.cs
rm -rf src/Industrial.Adam.Oee/Domain/ValueObjects/TransactionLog.cs
rm -rf src/Industrial.Adam.Oee/Domain/ValueObjects/StateTransition.cs
rm -rf src/Industrial.Adam.Oee/Domain/Services/BatchManagementService.cs
rm -rf src/Industrial.Adam.Oee/Domain/Services/AdvancedJobSchedulingService.cs
rm -rf src/Industrial.Adam.Oee/Infrastructure/Data/Migrations/006-*.sql
rm -rf src/Industrial.Adam.Oee/Infrastructure/Data/Migrations/007-*.sql
```

#### Day 3: Create Simple Replacements
```csharp
// Create SimpleJobQueue.cs
public class SimpleJobQueue : Entity<int>
{
    public string LineId { get; private set; }
    public Queue<QueuedJob> Jobs { get; private set; } = new();
    
    public void AddJob(string workOrderId, int priority = 0)
    {
        Jobs.Enqueue(new QueuedJob(workOrderId, priority));
    }
    
    public QueuedJob? GetNextJob()
    {
        return Jobs.Count > 0 ? Jobs.Dequeue() : null;
    }
}

// Create BasicQualityTracking.cs
public class QualityRecord : Entity<int>
{
    public string WorkOrderId { get; private set; }
    public int GoodCount { get; private set; }
    public int ScrapCount { get; private set; }
    public decimal QualityRate => GoodCount / (decimal)(GoodCount + ScrapCount);
}
```

#### Day 4-5: Update Dependencies and Tests
- Fix all compilation errors from deletions
- Update services to use simplified entities
- Ensure all Phase 1 & 2 tests pass
- Remove tests for deleted functionality

### Week 2-3: Equipment Scheduling Foundation

#### Create New Module Structure
```bash
# Create module structure
mkdir -p src/Industrial.Adam.EquipmentScheduling/{Domain,Application,Infrastructure,WebApi}
mkdir -p src/Industrial.Adam.EquipmentScheduling/Domain/{Entities,ValueObjects,Interfaces,Services}
mkdir -p src/Industrial.Adam.EquipmentScheduling/Application/{Commands,Queries,DTOs}
mkdir -p src/Industrial.Adam.EquipmentScheduling/Infrastructure/{Data,Repositories}
mkdir -p src/Industrial.Adam.EquipmentScheduling/WebApi/Controllers
```

#### Core Entities (Week 2)
```csharp
// Resource.cs - ISA-95 Equipment Hierarchy
public class Resource : Entity<long>
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public ResourceType Type { get; private set; } // Enterprise, Site, Area, WorkCenter, WorkUnit
    public long? ParentId { get; private set; }
    public string HierarchyPath { get; private set; }
    public bool RequiresScheduling { get; private set; }
}

// OperatingPattern.cs - When equipment operates
public class OperatingPattern : Entity<int>
{
    public string Name { get; private set; }
    public PatternType Type { get; private set; } // Continuous, TwoShift, DayOnly, Extended, Custom
    public int CycleDays { get; private set; }
    public decimal WeeklyHours { get; private set; }
    public JsonDocument Configuration { get; private set; }
}

// PatternAssignment.cs - Which equipment uses which pattern
public class PatternAssignment : Entity<long>
{
    public long ResourceId { get; private set; }
    public int PatternId { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsOverride { get; private set; }
}
```

#### Database Schema (Week 2)
```sql
-- Equipment Scheduling Tables (sched_* prefix)
CREATE TABLE sched_resources (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    resource_type VARCHAR(20) NOT NULL,
    parent_id BIGINT REFERENCES sched_resources(id),
    hierarchy_path VARCHAR(500),
    requires_scheduling BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE sched_operating_patterns (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    pattern_type VARCHAR(20) NOT NULL,
    cycle_days INT NOT NULL,
    weekly_hours DECIMAL(5,2),
    configuration JSONB NOT NULL,
    is_visible BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE sched_pattern_assignments (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id),
    pattern_id INT NOT NULL REFERENCES sched_operating_patterns(id),
    effective_date DATE NOT NULL,
    end_date DATE,
    is_override BOOLEAN DEFAULT FALSE,
    assigned_by VARCHAR(100),
    assigned_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE sched_equipment_schedules (
    id BIGSERIAL PRIMARY KEY,
    resource_id BIGINT NOT NULL REFERENCES sched_resources(id),
    schedule_date DATE NOT NULL,
    shift_code VARCHAR(10),
    planned_start_time TIMESTAMPTZ,
    planned_end_time TIMESTAMPTZ,
    planned_hours DECIMAL(4,2),
    schedule_status VARCHAR(20),
    pattern_id INT REFERENCES sched_operating_patterns(id),
    is_exception BOOLEAN DEFAULT FALSE,
    generated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_sched_resources_parent ON sched_resources(parent_id);
CREATE INDEX idx_sched_resources_hierarchy ON sched_resources(hierarchy_path);
CREATE INDEX idx_sched_schedules_resource_date ON sched_equipment_schedules(resource_id, schedule_date);
CREATE INDEX idx_sched_assignments_resource ON sched_pattern_assignments(resource_id);
```

#### API Foundation (Week 3)
```csharp
[ApiController]
[Route("api/equipment-scheduling/[controller]")]
public class AvailabilityController : ControllerBase
{
    [HttpGet("equipment/{lineId}/availability")]
    public async Task<IActionResult> GetAvailability(
        string lineId, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        // Return planned availability for OEE consumption
        var availability = await _service.GetPlannedAvailabilityAsync(lineId, startDate, endDate);
        return Ok(availability);
    }
    
    [HttpGet("equipment/{lineId}/is-operating")]
    public async Task<IActionResult> IsOperating(string lineId, [FromQuery] DateTime timestamp)
    {
        var isOperating = await _service.IsPlannedOperatingAsync(lineId, timestamp);
        return Ok(new { lineId, timestamp, isOperating });
    }
}
```

### Week 4: Integration

#### Complete Integration Architecture
```csharp
// OEE consumes data from both Logger and Equipment Scheduling
public class EnhancedOeeService
{
    private readonly ICounterDataService _counterData;           // From Logger
    private readonly IEquipmentAvailabilityService _availability; // From Equipment Scheduling
    
    public async Task<OeeMetrics> CalculateAsync(string lineId, DateTime date)
    {
        // Get actual counter data from Logger
        var actualData = await _counterData.GetProductionDataAsync(lineId, date);
        
        // Get planned availability from Equipment Scheduling  
        var plannedHours = await _availability.GetPlannedHoursAsync(lineId, date);
        
        // Calculate OEE: Availability × Performance × Quality
        return new OeeMetrics
        {
            Availability = actualData.RunTime / plannedHours.TotalHours,
            Performance = actualData.ActualOutput / actualData.TheoreticalOutput,
            Quality = actualData.GoodCount / actualData.TotalCount
        };
    }
}

// Integration interfaces
public interface IEquipmentAvailabilityService
{
    Task<bool> IsPlannedOperatingAsync(string lineId, DateTime timestamp);
    Task<PlannedHours> GetPlannedHoursAsync(string lineId, DateTime date);
}

public interface ICounterDataService  // Existing Logger integration
{
    Task<ProductionData> GetProductionDataAsync(string lineId, DateTime date);
    Task<CounterReading[]> GetCounterHistoryAsync(string lineId, DateTime start, DateTime end);
}
```

#### Update OEE Calculations
```csharp
public class EnhancedOeeCalculationService
{
    private readonly IEquipmentAvailabilityService _availabilityService;
    
    public async Task<OeeMetrics> CalculateOeeAsync(string lineId, DateTime date)
    {
        // Get planned hours from Equipment Scheduling
        var plannedHours = await _availabilityService.GetPlannedHoursAsync(lineId, date);
        
        // Get actual production data
        var actualData = await _repository.GetProductionDataAsync(lineId, date);
        
        // Calculate OEE
        var availability = actualData.RunTime / plannedHours.TotalHours;
        var performance = actualData.ActualOutput / actualData.TheoreticalOutput;
        var quality = actualData.GoodCount / actualData.TotalCount;
        
        return new OeeMetrics
        {
            Availability = availability,
            Performance = performance,
            Quality = quality,
            OEE = availability * performance * quality
        };
    }
}
```

## Validation & Testing

### Week 4: Comprehensive Testing
1. **Unit Tests**: Each module independently
2. **Integration Tests**: API communication between modules
3. **End-to-End Tests**: Complete OEE calculation with planned availability
4. **Performance Tests**: 1000+ equipment items

## Benefits of Simplified Approach

1. **No Migration Complexity**: Just delete and recreate
2. **Clean Separation**: Start fresh with proper boundaries
3. **Faster Execution**: 4 weeks instead of 16
4. **Lower Risk**: No production data to preserve
5. **Better Code Quality**: Clean slate approach

## Success Criteria

- [ ] All Phase 3 over-implementations removed
- [ ] OEE simplified to core monitoring purpose
- [ ] Equipment Scheduling module foundation created
- [ ] Integration working between modules
- [ ] All tests passing
- [ ] Documentation updated

## Git Commands for Execution

```bash
# Week 1: OEE Cleanup
git checkout -b feature/oee-cleanup
# ... make changes ...
git add -A
git commit -m "refactor: Remove Phase 3 over-implementations"
git push origin feature/oee-cleanup
# Create PR #1

# Week 2-3: Equipment Scheduling
git checkout main
git pull
git checkout -b feature/equipment-scheduling
# ... create new module ...
git add -A
git commit -m "feat: Add Equipment Scheduling System foundation"
git push origin feature/equipment-scheduling
# Create PR #2

# Week 4: Integration
git checkout main
git pull
git checkout -b feature/integration
# ... add integration ...
git add -A
git commit -m "feat: Integrate OEE with Equipment Scheduling"
git push origin feature/integration
# Create PR #3

# After all PRs approved
git checkout main
git merge feature/oee-cleanup
git merge feature/equipment-scheduling
git merge feature/integration
git push origin main
```

## Timeline Summary

| Week | Focus | Deliverable | PR |
|------|-------|-------------|-----|
| 1 | OEE Cleanup | Simplified OEE module | PR #1 |
| 2-3 | Equipment Scheduling | New module foundation | PR #2 |
| 4 | Integration | Connected systems | PR #3 |

Total: **4 weeks** with clean code, proper separation, and no technical debt.