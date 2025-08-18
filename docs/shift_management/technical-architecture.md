# Technical Architecture Document - Equipment Scheduling System

## 1. System Architecture Overview

### Design Principles
- **Complete Foundation**: Build full schema and architecture from day one
- **Progressive Disclosure**: Expose features through configuration, not deployment
- **Zero Migration**: Phase 2 activates without database changes
- **API Stability**: Version 1 endpoints remain unchanged when Version 2 adds features

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Web Application                       │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Phase 1: Equipment Modules              │  │
│  │  ├── Equipment Manager                           │  │
│  │  ├── Pattern Library (Simple)                    │  │
│  │  └── Schedule Generator                          │  │
│  ├──────────────────────────────────────────────────┤  │
│  │     Phase 2: Employee Modules (Hidden)           │  │
│  │  ├── Employee Manager (Dormant)                  │  │
│  │  ├── Pattern Library (Advanced)                  │  │
│  │  └── Coverage Analyzer (Dormant)                 │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                            │
                  ┌─────────┴──────────┐
                  │   REST API Layer    │
                  │  ├── /v1 endpoints  │
                  │  └── /v2 ready      │
                  └─────────┬──────────┘
                            │
            ┌───────────────┴────────────────┐
            │      Service Layer             │
            │  ├── ScheduleService           │
            │  ├── PatternService            │
            │  ├── EquipmentService          │
            │  └── EmployeeService (Dormant) │
            └───────────────┬────────────────┘
                            │
            ┌───────────────┴────────────────┐
            │      Data Access Layer         │
            │   Complete Schema from Day 1   │
            └───────────────┬────────────────┘
                            │
            ┌───────────────┴────────────────┐
            │         PostgreSQL             │
            │   ├── Active Tables (Phase 1)  │
            │   └── Dormant Tables (Phase 2) │
            └─────────────────────────────────┘
```

## 2. Database Design

### Schema Strategy
- **Complete Schema**: All tables created in initial deployment
- **Phase Markers**: Tables tagged with activation phase
- **Data Isolation**: Phase 2 tables remain empty until activated
- **Referential Integrity**: Foreign keys support future relationships

### Phase 1: Active Tables

#### Core Equipment Tables

**Resources** - ISA-95 Equipment Hierarchy
```
Table: Resources
├── ResourceID: BIGINT PRIMARY KEY
├── ResourceName: VARCHAR(200) NOT NULL
├── ResourceCode: VARCHAR(50) UNIQUE
├── ResourceTypeID: INT FK → ResourceTypes
├── ParentResourceID: BIGINT FK → Resources (self)
├── HierarchyPath: VARCHAR(500) -- 'Enterprise.Site.Area.WorkCenter'
├── RequiresScheduling: BOOLEAN DEFAULT FALSE
├── IsActive: BOOLEAN DEFAULT TRUE
├── CreatedDate: TIMESTAMP
├── ModifiedDate: TIMESTAMP
├── Metadata: JSONB -- Flexible attributes

Indexes:
├── idx_resources_parent (ParentResourceID)
├── idx_resources_hierarchy (HierarchyPath)
├── idx_resources_scheduling (RequiresScheduling, IsActive)
```

**ResourceTypes** - ISA-95 Levels
```
Table: ResourceTypes
├── ResourceTypeID: INT PRIMARY KEY
├── TypeName: VARCHAR(50) -- 'Enterprise', 'Site', 'Area', 'WorkCenter', 'WorkUnit'
├── ISALevel: CHAR(2) -- 'E', 'S', 'A', 'WC', 'WU'
├── HierarchyLevel: INT -- 1-5
├── Description: TEXT
```

#### Pattern Management Tables

**ShiftPatternTemplates** - Pattern Library
```
Table: ShiftPatternTemplates
├── PatternID: INT PRIMARY KEY
├── PatternName: VARCHAR(100) NOT NULL
├── PatternCode: VARCHAR(20) UNIQUE
├── ComplexityLevel: ENUM('Simple', 'Advanced', 'Expert')
├── PatternType: ENUM('Continuous', 'Shift', 'DayOnly', 'Custom')
├── CycleDays: INT -- 7 for weekly, 14 for bi-weekly, etc.
├── WeeklyHours: DECIMAL(5,2) -- Average hours per week
├── IsVisible: BOOLEAN DEFAULT TRUE -- Phase control
├── Configuration: JSONB -- Pattern details
├── CreatedDate: TIMESTAMP
├── ModifiedDate: TIMESTAMP

-- Phase 1: Only ComplexityLevel='Simple' are visible
-- Phase 2: 'Advanced' and 'Expert' become visible
```

**PatternScheduleRules** - When Equipment Operates
```
Table: PatternScheduleRules
├── RuleID: BIGINT PRIMARY KEY
├── PatternID: INT FK → ShiftPatternTemplates
├── DayOfWeek: INT -- 0=Sunday, 6=Saturday
├── StartTime: TIME
├── EndTime: TIME
├── IsOperating: BOOLEAN
├── ShiftCode: VARCHAR(10) -- 'D', 'A', 'N' for OEE
```

**ResourcePatternAssignments** - Active Pattern per Equipment
```
Table: ResourcePatternAssignments
├── AssignmentID: BIGINT PRIMARY KEY
├── ResourceID: BIGINT FK → Resources
├── PatternID: INT FK → ShiftPatternTemplates
├── EffectiveDate: DATE NOT NULL
├── EndDate: DATE -- NULL for current
├── IsOverride: BOOLEAN DEFAULT FALSE
├── InheritedFrom: BIGINT FK → Resources -- Parent that provided pattern
├── AssignedBy: VARCHAR(100)
├── AssignedDate: TIMESTAMP
├── Notes: TEXT
```

#### Schedule Output Tables

**EquipmentSchedules** - Generated Availability
```
Table: EquipmentSchedules
├── ScheduleID: BIGINT PRIMARY KEY
├── ResourceID: BIGINT FK → Resources
├── ScheduleDate: DATE NOT NULL
├── ShiftCode: VARCHAR(10)
├── PlannedStartTime: TIMESTAMP
├── PlannedEndTime: TIMESTAMP
├── PlannedHours: DECIMAL(4,2)
├── ScheduleStatus: ENUM('Operating', 'Maintenance', 'Holiday', 'Shutdown')
├── PatternID: INT FK → ShiftPatternTemplates -- Source pattern
├── IsException: BOOLEAN DEFAULT FALSE
├── GeneratedDate: TIMESTAMP
├── ModifiedDate: TIMESTAMP

Indexes:
├── idx_schedules_resource_date (ResourceID, ScheduleDate)
├── idx_schedules_date_status (ScheduleDate, ScheduleStatus)
├── idx_schedules_exceptions (IsException) WHERE IsException = TRUE
```

**ScheduleExceptions** - Maintenance and Downtime
```
Table: ScheduleExceptions
├── ExceptionID: BIGINT PRIMARY KEY
├── ResourceID: BIGINT FK → Resources
├── ExceptionType: ENUM('Maintenance', 'Breakdown', 'Holiday', 'Other')
├── StartDateTime: TIMESTAMP NOT NULL
├── EndDateTime: TIMESTAMP
├── RecurrenceRule: VARCHAR(200) -- RRULE format for recurring
├── ImpactType: ENUM('NoOperation', 'Reduced', 'Shifted')
├── Description: TEXT
├── CreatedBy: VARCHAR(100)
├── CreatedDate: TIMESTAMP
├── IsActive: BOOLEAN DEFAULT TRUE
```

#### Support Tables

**HolidayCalendars** - Regional Holidays
```
Table: HolidayCalendars
├── CalendarID: INT PRIMARY KEY
├── CalendarName: VARCHAR(100)
├── Country: CHAR(2) -- ISO country code
├── Region: VARCHAR(50) -- State/Province
├── IsDefault: BOOLEAN
```

**Holidays** - Specific Holiday Dates
```
Table: Holidays
├── HolidayID: INT PRIMARY KEY
├── CalendarID: INT FK → HolidayCalendars
├── HolidayName: VARCHAR(100)
├── HolidayDate: DATE -- NULL if recurring
├── RecurrenceRule: VARCHAR(200) -- RRULE format
├── AffectsScheduling: BOOLEAN DEFAULT TRUE
```

**ResourceCalendarAssignments** - Which Calendar per Equipment
```
Table: ResourceCalendarAssignments
├── ResourceID: BIGINT FK → Resources
├── CalendarID: INT FK → HolidayCalendars
├── Priority: INT -- For multiple calendars
├── PRIMARY KEY (ResourceID, CalendarID)
```

### Phase 2: Dormant Tables (Built but Empty)

#### Employee Management Tables

**Employees** - Worker Records
```
Table: Employees
├── EmployeeID: BIGINT PRIMARY KEY
├── EmployeeNumber: VARCHAR(50) UNIQUE
├── FirstName: VARCHAR(100)
├── LastName: VARCHAR(100)
├── Email: VARCHAR(200)
├── DepartmentID: INT FK → Departments
├── PositionID: INT FK → Positions
├── HireDate: DATE
├── IsActive: BOOLEAN DEFAULT TRUE
├── Skills: JSONB -- Skill matrix
├── CreatedDate: TIMESTAMP
├── ModifiedDate: TIMESTAMP

-- Empty in Phase 1, populated in Phase 2
```

**EmployeeResourceQualifications** - Who Can Operate What
```
Table: EmployeeResourceQualifications
├── QualificationID: BIGINT PRIMARY KEY
├── EmployeeID: BIGINT FK → Employees
├── ResourceID: BIGINT FK → Resources
├── QualificationLevel: ENUM('Basic', 'Advanced', 'Expert')
├── CertificationDate: DATE
├── ExpiryDate: DATE
├── IsActive: BOOLEAN DEFAULT TRUE

-- Empty in Phase 1, used for skill matching in Phase 2
```

**Teams** - Work Teams for Rotation
```
Table: Teams
├── TeamID: INT PRIMARY KEY
├── TeamName: VARCHAR(50)
├── TeamCode: CHAR(1) -- 'A', 'B', 'C', 'D'
├── ShiftPatternID: INT FK → ShiftPatternTemplates
├── IsActive: BOOLEAN DEFAULT TRUE

-- Empty in Phase 1, supports rotations in Phase 2
```

**EmployeeTeamAssignments** - Team Membership
```
Table: EmployeeTeamAssignments
├── EmployeeID: BIGINT FK → Employees
├── TeamID: INT FK → Teams
├── StartDate: DATE
├── EndDate: DATE
├── IsPrimary: BOOLEAN
├── PRIMARY KEY (EmployeeID, TeamID, StartDate)

-- Empty in Phase 1, enables team scheduling in Phase 2
```

**EmployeeSchedules** - Individual Work Schedules
```
Table: EmployeeSchedules
├── ScheduleID: BIGINT PRIMARY KEY
├── EmployeeID: BIGINT FK → Employees
├── ResourceID: BIGINT FK → Resources
├── ScheduleDate: DATE
├── ShiftStartTime: TIMESTAMP
├── ShiftEndTime: TIMESTAMP
├── ScheduleStatus: ENUM('Scheduled', 'Worked', 'Absent', 'Leave')
├── TeamID: INT FK → Teams
├── GeneratedFrom: BIGINT FK → EquipmentSchedules

-- Empty in Phase 1, links people to equipment in Phase 2
```

### Audit and System Tables

**AuditLog** - Complete Change History
```
Table: AuditLog
├── AuditID: BIGINT PRIMARY KEY
├── TableName: VARCHAR(50)
├── RecordID: BIGINT
├── Action: ENUM('INSERT', 'UPDATE', 'DELETE')
├── ChangedBy: VARCHAR(100)
├── ChangedDate: TIMESTAMP
├── OldValues: JSONB
├── NewValues: JSONB
├── IPAddress: INET
├── UserAgent: TEXT
```

**SystemConfiguration** - Feature Flags and Settings
```
Table: SystemConfiguration
├── ConfigKey: VARCHAR(100) PRIMARY KEY
├── ConfigValue: TEXT
├── ConfigType: ENUM('Feature', 'Setting', 'Integration')
├── IsActive: BOOLEAN DEFAULT TRUE
├── ModifiedDate: TIMESTAMP

-- Examples:
-- ('employee_module_enabled', 'false', 'Feature')
-- ('pattern_complexity_level', 'Simple', 'Setting')
-- ('oee_api_version', 'v1', 'Integration')
```

## 3. API Design

### API Versioning Strategy
- **URL Versioning**: /api/v1/ and /api/v2/
- **Backward Compatibility**: v1 endpoints never break
- **Feature Detection**: Clients can query available features
- **Gradual Migration**: v1 and v2 coexist indefinitely

### Phase 1 Endpoints (v1)

#### Equipment Management
```
GET    /api/v1/equipment                    # List all equipment
GET    /api/v1/equipment/{id}              # Get equipment details
GET    /api/v1/equipment/hierarchy         # Get ISA-95 tree
POST   /api/v1/equipment/import            # Bulk import
PUT    /api/v1/equipment/{id}              # Update equipment
DELETE /api/v1/equipment/{id}              # Remove equipment
```

#### Pattern Management
```
GET    /api/v1/patterns                    # List available patterns
GET    /api/v1/patterns/{id}              # Get pattern details
POST   /api/v1/patterns                    # Create custom pattern
PUT    /api/v1/patterns/{id}              # Update pattern
```

#### Pattern Assignment
```
GET    /api/v1/equipment/{id}/pattern      # Get current pattern
POST   /api/v1/equipment/{id}/pattern      # Assign pattern
DELETE /api/v1/equipment/{id}/pattern      # Remove pattern
POST   /api/v1/equipment/bulk-assign       # Bulk pattern assignment
```

#### Schedule Generation
```
POST   /api/v1/schedules/generate          # Generate schedules
GET    /api/v1/schedules                   # Query schedules
GET    /api/v1/equipment/{id}/availability # Get equipment availability
```

#### OEE Integration
```
GET    /api/v1/oee/availability            # Bulk availability data
GET    /api/v1/oee/changes                 # Recent schedule changes
POST   /api/v1/oee/webhook                 # Register webhook
```

### Phase 2 Endpoints (v2 - Additive)

#### Employee Management (New in v2)
```
GET    /api/v2/employees                   # List employees
GET    /api/v2/employees/{id}              # Get employee details
POST   /api/v2/employees/{id}/skills       # Update skills
GET    /api/v2/employees/{id}/schedule     # Get employee schedule
```

#### Coverage Analysis (New in v2)
```
GET    /api/v2/coverage/gaps               # Find coverage gaps
GET    /api/v2/coverage/overtime           # Overtime analysis
POST   /api/v2/coverage/optimize           # Optimization suggestions
```

#### Enhanced Equipment API (v1 compatible)
```
GET    /api/v2/equipment/{id}/availability # Same as v1
       Response includes optional 'operators' field in v2
       
       v1 Response:
       {
         "equipmentId": "123",
         "availability": [...]
       }
       
       v2 Response:
       {
         "equipmentId": "123",
         "availability": [...],
         "operators": [...]  // New optional field
       }
```

### API Response Standards

#### Success Response
```json
{
  "success": true,
  "data": {
    // Response payload
  },
  "meta": {
    "timestamp": "2025-01-20T10:00:00Z",
    "version": "v1",
    "requestId": "uuid"
  }
}
```

#### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Pattern assignment failed",
    "details": [
      {
        "field": "patternId",
        "issue": "Pattern not found"
      }
    ]
  },
  "meta": {
    "timestamp": "2025-01-20T10:00:00Z",
    "version": "v1",
    "requestId": "uuid"
  }
}
```

#### Pagination
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalPages": 10,
    "totalItems": 487,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## 4. Security Architecture

### Authentication
- **Primary**: SSO via SAML 2.0 / OAuth 2.0
- **Secondary**: API Key for system integration
- **Session**: JWT tokens with 8-hour expiry
- **MFA**: Optional two-factor authentication

### Authorization
```
Roles:
├── SystemAdmin: Full system access
├── OperationsManager: Equipment and schedules
├── Engineer: Patterns and configuration
├── Viewer: Read-only access
├── API: System integration access

Phase 2 adds:
├── HRManager: Employee management
├── TeamLead: Team scheduling
├── Employee: Self-service portal
```

### Data Security
- **Encryption at Rest**: AES-256
- **Encryption in Transit**: TLS 1.3
- **Database**: Row-level security where applicable
- **API**: Rate limiting and DDoS protection
- **Audit**: Complete activity logging

## 5. Integration Architecture

### OEE System Integration
```
Standard Integration Flow:
1. OEE system registers via API
2. Authenticates with API key
3. Queries /api/v1/oee/availability
4. Receives equipment schedules
5. Optional: Registers webhook for changes
6. Processes availability data for OEE calculation
```

### Enterprise System Integration
```
Future Integration Points:
├── ERP: Equipment master data sync
├── MES: Real-time production data
├── CMMS: Maintenance schedule import
├── HR: Employee data sync (Phase 2)
├── Active Directory: SSO authentication
```

## 6. Performance Considerations

### Database Optimization
- **Indexing**: Strategic indexes on frequently queried columns
- **Partitioning**: EquipmentSchedules by month for large datasets
- **Caching**: Redis for frequently accessed patterns and hierarchies
- **Connection Pooling**: Maximum 100 connections

### Application Performance
- **Response Time**: < 200ms for API calls
- **Batch Processing**: Async job queue for large operations
- **Caching Strategy**: 
  - Equipment hierarchy: 1 hour
  - Patterns: 24 hours
  - Schedules: 5 minutes
- **CDN**: Static assets served via CDN

### Scalability Targets
- **Equipment Items**: 10,000+ per installation
- **Schedule Records**: 10M+ total records
- **Concurrent Users**: 500+ simultaneous
- **API Throughput**: 10,000 requests/hour

## 7. Deployment Architecture

### Infrastructure
```
Production Environment:
├── Web Servers: 2× load-balanced instances
├── API Servers: 2× load-balanced instances
├── Database: Primary + Read replica
├── Cache: Redis cluster
├── Queue: RabbitMQ or AWS SQS
├── Storage: S3-compatible for exports
```

### Deployment Strategy
- **Containerization**: Docker containers
- **Orchestration**: Kubernetes or Docker Swarm
- **CI/CD**: GitLab CI or GitHub Actions
- **Monitoring**: Prometheus + Grafana
- **Logging**: ELK stack or CloudWatch

### Environment Strategy
```
Environments:
├── Development: Feature development
├── Staging: Pre-production testing
├── Production: Live system
├── DR: Disaster recovery standby
```

## 8. Phase Activation Mechanism

### Feature Flag Implementation
```
Phase 2 Activation Process:
1. Update SystemConfiguration table
2. Set 'employee_module_enabled' = 'true'
3. Set 'pattern_complexity_level' = 'Advanced'
4. Restart application (or hot reload)
5. UI shows employee modules
6. API v2 endpoints activate
7. Begin populating employee tables
```

### Data Migration Strategy
```
Phase 1 → Phase 2 Migration:
1. No database schema changes required
2. Begin populating employee tables
3. Link employees to existing equipment
4. Generate employee schedules from equipment schedules
5. Enable team rotations on existing patterns
```

This architecture ensures that Phase 1 delivers immediate value while Phase 2 features remain ready for activation without any system refactoring or data migration.