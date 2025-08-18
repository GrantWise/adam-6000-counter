# OEE System Simplification Plan

**Version**: 1.0  
**Date**: 2025-08-18  
**Objective**: Simplify OEE system back to core purpose of monitoring equipment performance while preserving essential functionality

## Executive Summary

The current OEE implementation has grown beyond its intended scope with complex batch tracking, advanced scheduling, and over-engineered canonical patterns that obscure the core purpose: monitoring equipment performance through ADAM device integration. This plan provides a surgical approach to remove complexity while preserving the valuable Phase 1 & 2 functionality.

## Current State Analysis

### Over-Implemented Features (Remove)
- **Batch Tracking System** - Complete batch lifecycle, genealogy, quality tracking
- **Advanced Job Scheduling** - Complex dependency management, optimization algorithms  
- **Shift Management** - Employee shift tracking (wrong concept for equipment patterns)
- **Complex Canonical Patterns** - Over-engineered reference system
- **Advanced Quality System** - Inspection workflows beyond simple quality gates

### Core Functionality (Preserve & Enhance)
- **Equipment Line Mapping** - ADAM device to production line relationship
- **Work Order Context** - Business context over counter data
- **Basic Stoppage Detection** - Equipment downtime monitoring
- **Real-time OEE Calculation** - Core availability × performance × quality
- **SignalR Notifications** - Real-time stoppage alerts
- **Simple Job Sequencing** - Basic work order queue management

## Implementation Strategy

### Phase 1: Analysis and Planning (Days 1-2)
1. **Dependency Mapping** - Identify all references to entities being removed
2. **Data Migration Planning** - Plan for safe removal of Phase 3 data
3. **API Impact Assessment** - Identify breaking changes to endpoints
4. **Test Strategy** - Ensure Phase 1 & 2 functionality remains intact

### Phase 2: Entity Simplification (Days 3-5)
1. **Remove Complex Entities** - Delete Batch.cs, JobSchedule.cs, Shift.cs
2. **Simplify WorkOrder.cs** - Remove Phase 3 enhancements
3. **Create Simple Replacements** - Implement SimpleJobQueue, QualityRecord
4. **Update Value Objects** - Remove canonical complexity

### Phase 3: Service Layer Cleanup (Days 6-8)
1. **Remove Advanced Services** - Delete batch, scheduling, shift services
2. **Simplify Domain Services** - Keep core OEE calculation logic
3. **Update Application Services** - Remove CQRS handlers for removed entities
4. **Create Equipment Availability Interface** - For external scheduling integration

### Phase 4: Infrastructure Updates (Days 9-11)
1. **Database Migration Rollback** - Remove Phase 3 tables safely
2. **Repository Cleanup** - Remove repositories for deleted entities
3. **API Controller Updates** - Remove/simplify endpoints
4. **Configuration Simplification** - Remove unnecessary config sections

### Phase 5: Testing and Validation (Days 12-14)
1. **Unit Test Updates** - Ensure all tests pass with simplified model
2. **Integration Test Verification** - Validate core OEE functionality
3. **Performance Testing** - Ensure no degradation in core operations
4. **Manual Testing** - Validate ADAM device integration still works

## Detailed Implementation Plan

### 1. File-by-File Analysis

#### REMOVE COMPLETELY
```
Domain/Entities/
├── Batch.cs                           ❌ DELETE - Complete batch tracking system
├── JobSchedule.cs                     ❌ DELETE - Advanced scheduling complexity  
├── Shift.cs                           ❌ DELETE - Wrong concept (employee shifts)
├── QualityInspection.cs              ❌ DELETE - Over-engineered quality system
└── JobCompletionIssue.cs             ❌ DELETE - Related to complex scheduling

Domain/Services/
├── BatchManagementService.cs          ❌ DELETE - Batch-related service
├── AdvancedJobSchedulingService.cs    ❌ DELETE - Complex scheduling algorithms
└── (Any shift-related services)       ❌ DELETE - Shift management services

Domain/ValueObjects/
├── CanonicalReference.cs              ❌ DELETE - Over-engineered reference system
├── StateTransition.cs                 ❌ DELETE - Complex state management  
├── TransactionLog.cs                  ❌ DELETE - Audit trail complexity
└── (Related complex value objects)    ❌ DELETE

Application/Commands/
├── CreateBatchCommand.cs              ❌ DELETE - Batch creation
├── CreateJobScheduleCommand.cs        ❌ DELETE - Scheduling commands
└── (Shift-related commands)           ❌ DELETE

Application/Queries/
├── GetBatchSummaryQuery.cs            ❌ DELETE - Batch queries
├── GetScheduleOptimizationQuery.cs    ❌ DELETE - Scheduling queries
└── (Shift-related queries)            ❌ DELETE

Infrastructure/Data/Migrations/
├── 006-create-phase3-batch-tracking.sql      ❌ ROLLBACK - Phase 3 migrations
└── 007-create-phase3-shift-management.sql    ❌ ROLLBACK

WebApi/Controllers/
├── (Batch-related controllers)        ❌ DELETE - Remove batch endpoints
├── (Schedule-related controllers)      ❌ DELETE - Remove scheduling endpoints  
└── (Shift-related controllers)        ❌ DELETE - Remove shift endpoints
```

#### PRESERVE & ENHANCE
```
Domain/Entities/
├── EquipmentLine.cs                   ✅ KEEP - Core ADAM mapping
├── WorkOrder.cs                       🔄 SIMPLIFY - Remove Phase 3 additions
├── EquipmentStoppage.cs              ✅ KEEP - Core stoppage detection
├── StoppageReasonCategory.cs         ✅ KEEP - Reason code system
├── StoppageReasonSubcode.cs          ✅ KEEP - Detailed reason codes  
├── OeeCalculation.cs                 ✅ KEEP - Core OEE math
└── QualityGate.cs                    🔄 SIMPLIFY - Basic quality tracking

Domain/Services/
├── OeeCalculationService.cs          ✅ KEEP - Core OEE calculations
├── StoppageDetectionService.cs       ✅ KEEP - Equipment monitoring
├── EquipmentLineService.cs           ✅ KEEP - ADAM device management
├── JobSequencingService.cs           🔄 SIMPLIFY - Basic job queue only
└── (Quality/Performance services)     ✅ KEEP - Core calculation services

Application/Commands/
├── StartWorkOrderCommand.cs          ✅ KEEP - Core work order lifecycle
├── CompleteWorkOrderCommand.cs       ✅ KEEP - Work order completion
└── UpdateWorkOrderCommand.cs         🔄 SIMPLIFY - Remove Phase 3 fields

Application/Queries/
├── GetActiveWorkOrderQuery.cs        ✅ KEEP - Current job status
├── CalculateCurrentOeeQuery.cs       ✅ KEEP - Real-time OEE
├── GetOeeHistoryQuery.cs            ✅ KEEP - Historical reporting
├── GetStoppageHistoryQuery.cs       ✅ KEEP - Stoppage analysis
└── GetWorkOrderProgressQuery.cs     ✅ KEEP - Production monitoring

Infrastructure/
├── StoppageNotificationHub.cs       ✅ KEEP - Real-time alerts
├── StoppageNotificationService.cs   ✅ KEEP - Notification logic
└── WorkOrderRepository.cs           🔄 SIMPLIFY - Remove Phase 3 fields

WebApi/Controllers/
├── OeeController.cs                  ✅ KEEP - Core OEE endpoints
├── StoppagesController.cs            ✅ KEEP - Stoppage management
└── JobsController.cs                🔄 SIMPLIFY - Basic job management
```

#### CREATE NEW (Simple Replacements)
```
Domain/Entities/
└── SimpleJobQueue.cs                 ✨ CREATE - Replace complex scheduling

Domain/ValueObjects/
└── QualityRecord.cs                  ✨ CREATE - Replace complex quality system

Domain/Services/
└── SimpleJobQueueService.cs          ✨ CREATE - Basic job sequencing

Domain/Interfaces/
└── IEquipmentAvailabilityService.cs  ✨ CREATE - External scheduling interface

Application/Services/
└── SimpleJobQueueApplicationService.cs ✨ CREATE - Job queue management
```

### 2. Entity Refactoring Plan

#### WorkOrder.cs Simplification
**Remove Phase 3 Properties:**
```csharp
// REMOVE these Phase 3 additions:
public int Priority { get; private set; }
public string? ShiftId { get; private set; }
public string? JobScheduleId { get; private set; }
public bool BatchTrackingEnabled { get; private set; }
public decimal? PlannedBatchSize { get; private set; }
public decimal SetupTimeMinutes { get; private set; }
public decimal TeardownTimeMinutes { get; private set; }
public int ComplexityScore { get; private set; }
public string? CustomerPriority { get; private set; }
public DateTime? DueDate { get; private set; }
private readonly List<string> _batchIds = new();
private readonly List<string> _requiredQualityGateIds = new();
private readonly List<WorkOrderMaterialRequirement> _materialRequirements = new();
private readonly List<WorkOrderNote> _notes = new();

// REMOVE these methods:
UpdatePriority(), UpdateShift(), UpdateJobSchedule(), AddBatch(), 
AddRequiredQualityGate(), AddMaterialRequirement(), EnableBatchTracking(), 
CalculateUrgencyScore(), etc.
```

**Keep Core Properties:**
```csharp
// KEEP these core properties:
public string WorkOrderDescription { get; private set; }
public string ProductId { get; private set; }
public string ProductDescription { get; private set; }
public decimal PlannedQuantity { get; private set; }
public string UnitOfMeasure { get; private set; }
public DateTime ScheduledStartTime { get; private set; }
public DateTime ScheduledEndTime { get; private set; }
public string ResourceReference { get; private set; }
public WorkOrderStatus Status { get; private set; }
public decimal ActualQuantityGood { get; private set; }
public decimal ActualQuantityScrap { get; private set; }
public DateTime? ActualStartTime { get; private set; }
public DateTime? ActualEndTime { get; private set; }
```

#### New SimpleJobQueue.cs
```csharp
public sealed class SimpleJobQueue : Entity<int>, IAggregateRoot
{
    public string LineId { get; private set; }
    private readonly List<QueuedJob> _jobs = new();
    public IReadOnlyList<QueuedJob> Jobs => _jobs.AsReadOnly();
    
    public void AddJob(string workOrderId, int priority = 5);
    public QueuedJob? GetNextJob();
    public void StartJob(string workOrderId, string operatorId);
    public void CompleteJob(string workOrderId);
    public void RemoveJob(string workOrderId);
    public int GetQueuePosition(string workOrderId);
}

public record QueuedJob(
    string WorkOrderId,
    string ProductDescription,
    int Priority,
    DateTime QueuedAt,
    string? OperatorId = null,
    DateTime? StartedAt = null
);
```

#### New QualityRecord.cs (Value Object)
```csharp
public sealed class QualityRecord : ValueObject
{
    public string WorkOrderId { get; private set; }
    public int GoodCount { get; private set; }
    public int ScrapCount { get; private set; }
    public string? ScrapReasonCode { get; private set; }
    public DateTime RecordedAt { get; private set; }
    
    public decimal YieldPercentage => 
        TotalCount == 0 ? 100m : (GoodCount / (decimal)TotalCount) * 100m;
    
    public int TotalCount => GoodCount + ScrapCount;
}
```

### 3. Service Layer Cleanup

#### Remove These Services Completely:
- `BatchManagementService.cs`
- `AdvancedJobSchedulingService.cs` 
- Any shift-related services
- Complex canonical reference services

#### Simplify These Services:
- `JobSequencingService.cs` → Focus on basic FIFO/priority queue only
- `WorkOrderValidationService.cs` → Remove batch/shift validations
- `WorkOrderProgressService.cs` → Remove complex progress calculations

#### Create New Simple Services:
```csharp
public class SimpleJobQueueService
{
    public async Task<QueuedJob?> GetNextJobAsync(string lineId);
    public async Task AddJobToQueueAsync(string lineId, string workOrderId, int priority = 5);
    public async Task StartJobAsync(string lineId, string workOrderId, string operatorId);
    public async Task CompleteJobAsync(string lineId, string workOrderId);
    public async Task<List<QueuedJob>> GetQueueStatusAsync(string lineId);
}
```

#### Equipment Availability Service Interface:
```csharp
public interface IEquipmentAvailabilityService
{
    Task<bool> IsPlannedOperatingAsync(string lineId, DateTime timestamp);
    Task<PlannedHours> GetPlannedHoursAsync(string lineId, DateTime date);  
    Task<AvailabilitySchedule> GetWeeklyScheduleAsync(string lineId, DateTime weekStart);
}

public record PlannedHours(
    string LineId,
    DateTime Date,
    decimal PlannedOperatingHours,
    List<PlannedDowntime> PlannedDowntimes
);

public record PlannedDowntime(
    string Reason,
    DateTime StartTime,
    DateTime EndTime,
    string Category
);

public record AvailabilitySchedule(
    string LineId,
    DateTime WeekStartDate,
    List<DailyAvailability> DailySchedules
);

public record DailyAvailability(
    DateTime Date,
    decimal PlannedHours,
    List<PlannedDowntime> PlannedDowntimes
);
```

### 4. Database Migration Strategy

#### Rollback Phase 3 Migrations Safely:
```sql
-- 1. First, backup any data that should be preserved
CREATE TEMP TABLE work_order_backup AS
SELECT work_order_id, product_description, planned_quantity, 
       actual_quantity_good, actual_quantity_scrap,
       scheduled_start_time, scheduled_end_time, resource_reference,
       status, actual_start_time, actual_end_time
FROM work_orders;

-- 2. Remove Phase 3 foreign key constraints
ALTER TABLE batches DROP CONSTRAINT IF EXISTS fk_batches_work_order;
ALTER TABLE shift_planned_work_orders DROP CONSTRAINT IF EXISTS fk_shift_work_orders_work_order;

-- 3. Drop Phase 3 tables in correct order
DROP TABLE IF EXISTS batch_notes CASCADE;
DROP TABLE IF EXISTS batch_quality_checks CASCADE;  
DROP TABLE IF EXISTS batch_material_consumptions CASCADE;
DROP TABLE IF EXISTS batches CASCADE;

DROP TABLE IF EXISTS shift_handover_notes CASCADE;
DROP TABLE IF EXISTS shift_performance_metrics CASCADE;
DROP TABLE IF EXISTS shift_planned_work_orders CASCADE;
DROP TABLE IF EXISTS shift_equipment_lines CASCADE; 
DROP TABLE IF EXISTS shift_operators CASCADE;
DROP TABLE IF EXISTS shifts CASCADE;

-- 4. Remove Phase 3 columns from work_orders
ALTER TABLE work_orders 
DROP COLUMN IF EXISTS priority,
DROP COLUMN IF EXISTS shift_id,
DROP COLUMN IF EXISTS job_schedule_id,
DROP COLUMN IF EXISTS batch_tracking_enabled,
DROP COLUMN IF EXISTS planned_batch_size,
DROP COLUMN IF EXISTS setup_time_minutes,
DROP COLUMN IF EXISTS teardown_time_minutes,
DROP COLUMN IF EXISTS complexity_score,
DROP COLUMN IF EXISTS customer_priority,
DROP COLUMN IF EXISTS due_date;

-- 5. Create new simple_job_queues table
CREATE TABLE simple_job_queues (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(100) NOT NULL,
    work_order_id VARCHAR(255) NOT NULL,
    priority INTEGER DEFAULT 5,
    queued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operator_id VARCHAR(100),
    started_at TIMESTAMPTZ,
    
    CONSTRAINT fk_simple_queue_work_order 
        FOREIGN KEY (work_order_id) REFERENCES work_orders(work_order_id),
    CONSTRAINT uk_line_work_order 
        UNIQUE (line_id, work_order_id),
    CONSTRAINT chk_priority 
        CHECK (priority >= 1 AND priority <= 10)
);

CREATE INDEX idx_simple_job_queues_line_id ON simple_job_queues(line_id);
CREATE INDEX idx_simple_job_queues_priority ON simple_job_queues(priority);
CREATE INDEX idx_simple_job_queues_queued_at ON simple_job_queues(queued_at);
```

### 5. API Endpoint Cleanup

#### Remove These Controllers/Endpoints:
- All batch-related endpoints
- All advanced scheduling endpoints  
- All shift management endpoints
- Complex canonical reference endpoints

#### Simplify These Endpoints:
```csharp
// JobsController.cs - Keep only basic operations
[HttpGet("queue/{lineId}")]
public async Task<IActionResult> GetJobQueue(string lineId)

[HttpPost("queue/{lineId}")]  
public async Task<IActionResult> AddJobToQueue(string lineId, AddJobRequest request)

[HttpPut("queue/{lineId}/start")]
public async Task<IActionResult> StartNextJob(string lineId, StartJobRequest request)

[HttpPut("queue/{lineId}/complete/{workOrderId}")]
public async Task<IActionResult> CompleteJob(string lineId, string workOrderId)

// Remove complex scheduling, batch, and shift endpoints
```

#### Keep These Core Endpoints:
- `GET /api/oee/current/{lineId}` - Current OEE calculation
- `GET /api/oee/history/{lineId}` - Historical OEE data  
- `GET /api/stoppages/current/{lineId}` - Current stoppages
- `POST /api/stoppages/{lineId}/end` - End stoppage
- `GET /api/workorders/active/{lineId}` - Active work order
- `PUT /api/workorders/{workOrderId}/start` - Start work order
- `PUT /api/workorders/{workOrderId}/complete` - Complete work order

### 6. Testing Strategy

#### Unit Tests - Update/Create:
```csharp
// Remove all tests for deleted entities
❌ BatchTests.cs
❌ JobScheduleTests.cs  
❌ ShiftTests.cs
❌ AdvancedJobSchedulingServiceTests.cs
❌ BatchManagementServiceTests.cs

// Keep and update core tests
✅ EquipmentLineTests.cs
🔄 WorkOrderTests.cs - Remove Phase 3 test methods
✅ OeeCalculationTests.cs
✅ StoppageDetectionServiceTests.cs

// Create new tests
✨ SimpleJobQueueTests.cs
✨ SimpleJobQueueServiceTests.cs
```

#### Integration Tests:
```csharp
// Focus on verifying core OEE functionality still works
✅ OeeCalculationIntegrationTests.cs
✅ WorkOrderLifecycleIntegrationTests.cs  
✅ StoppageDetectionIntegrationTests.cs
✅ SignalRNotificationIntegrationTests.cs
```

#### Performance Tests:
- Ensure OEE calculation performance is not degraded
- Verify ADAM device integration latency unchanged
- Test work order throughput performance

#### Manual Test Scenarios:
1. **ADAM Device Integration**: Verify counter data still flows correctly
2. **Work Order Lifecycle**: Create, start, update quantities, complete work order
3. **Stoppage Detection**: Trigger stoppage, verify notification, resolve stoppage  
4. **OEE Calculation**: Verify availability, performance, quality calculations
5. **Real-time Updates**: Verify SignalR notifications for stoppages
6. **Job Queue**: Add jobs to queue, start next job, complete job

### 7. Integration Points

#### Equipment Availability Integration:
The simplified OEE system will consume equipment availability data through the new interface:

```csharp
// In OeeCalculationService.cs
public class OeeCalculationService
{
    private readonly IEquipmentAvailabilityService _availabilityService;
    
    public async Task<OeeMetrics> CalculateCurrentOeeAsync(string lineId)
    {
        var now = DateTime.UtcNow;
        
        // Get planned operating status from external scheduling system
        var isPlannedOperating = await _availabilityService.IsPlannedOperatingAsync(lineId, now);
        
        if (!isPlannedOperating)
        {
            // Equipment not planned to operate - don't count as availability loss
            return new OeeMetrics(lineId, 100m, 100m, 100m, 100m, now);
        }
        
        // Continue with normal OEE calculation...
        var plannedHours = await _availabilityService.GetPlannedHoursAsync(lineId, now.Date);
        // ... rest of calculation logic
    }
}
```

#### External System Integration Flow:
1. **Equipment Scheduling System** → Provides planned operating hours and maintenance windows
2. **OEE System** → Consumes availability data for accurate OEE calculations  
3. **MES/ERP System** → Provides work orders and production schedules
4. **ADAM Devices** → Provide real-time counter data
5. **Operator Interface** → Receives real-time OEE metrics and stoppage alerts

## Risk Assessment & Mitigation

### High Risk Items:
1. **Data Loss** - Phase 3 data removal could be irreversible
   - **Mitigation**: Full database backup before migration, phased rollout

2. **API Breaking Changes** - External systems may depend on removed endpoints  
   - **Mitigation**: Deprecation notices, maintain compatibility layer during transition

3. **Business Logic Dependencies** - Hidden dependencies on removed entities
   - **Mitigation**: Comprehensive dependency analysis, extensive testing

### Medium Risk Items:
1. **Performance Impact** - Simplified system should be faster but need validation
   - **Mitigation**: Performance benchmarking before/after changes

2. **Integration Disruption** - ADAM device integration disruption
   - **Mitigation**: Integration tests, gradual rollout with rollback plan

### Low Risk Items:
1. **UI Updates** - Frontend applications may need updates
   - **Mitigation**: Coordinate with frontend teams, provide migration guides

## Success Criteria

### Technical Metrics:
- [ ] All Phase 1 & 2 functionality preserved and working
- [ ] Phase 3 database tables successfully removed  
- [ ] ADAM device integration maintains <100ms response time
- [ ] OEE calculation accuracy unchanged (within 0.01%)
- [ ] Memory usage reduced by >30% (removal of complex entities)
- [ ] Code complexity reduced (measured by cyclomatic complexity)

### Functional Validation:
- [ ] Equipment lines can be created and mapped to ADAM devices
- [ ] Work orders can be created, started, updated with counter data, completed
- [ ] Stoppage detection triggers correctly and sends SignalR notifications
- [ ] OEE calculations provide accurate availability, performance, quality metrics
- [ ] Simple job queue allows basic work order sequencing
- [ ] Real-time dashboard continues to function with simplified data model

### Business Value:
- [ ] OEE system is easier to understand and maintain
- [ ] Onboarding time for new developers reduced
- [ ] System performance improved due to reduced complexity
- [ ] Focus returned to core equipment monitoring purpose
- [ ] Foundation prepared for proper equipment scheduling integration

## Timeline & Milestones

### Week 1: Analysis and Planning
- **Day 1-2**: Dependency analysis and detailed planning
- **Day 3**: Database backup and rollback script preparation  
- **Day 4-5**: Create branch, begin entity simplification

### Week 2: Core Implementation
- **Day 6-8**: Remove complex entities, update WorkOrder.cs
- **Day 9-10**: Create simple replacements (SimpleJobQueue, QualityRecord)
- **Day 11-12**: Update service layer, remove complex services

### Week 3: Infrastructure and API Updates  
- **Day 13-15**: Database migration rollback execution
- **Day 16-17**: API controller updates and endpoint removal
- **Day 18-19**: Update configuration and infrastructure services

### Week 4: Testing and Validation
- **Day 20-22**: Unit test updates, integration testing
- **Day 23-24**: Performance testing and optimization
- **Day 25**: Manual testing and validation
- **Day 26**: Documentation updates and deployment preparation

## Conclusion

This simplification plan provides a systematic approach to returning the OEE system to its core purpose while maintaining all essential functionality. The surgical removal of over-engineered features will result in a more maintainable, understandable, and performant system that properly focuses on equipment performance monitoring.

The plan prioritizes safety through comprehensive testing and gradual rollout, ensuring that the valuable Phase 1 and Phase 2 functionality remains intact while removing the complexity that has obscured the system's primary value proposition.

By implementing the `IEquipmentAvailabilityService` interface, the simplified OEE system will be properly positioned to integrate with external equipment scheduling systems, maintaining separation of concerns while enabling accurate availability calculations based on planned operating schedules.