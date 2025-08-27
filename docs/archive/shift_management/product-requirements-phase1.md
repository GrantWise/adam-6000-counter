# Product Requirements Document - Phase 1: Equipment Scheduling System

## 1. Product Overview

### Purpose
Provide manufacturing facilities with a simple, reliable system to schedule equipment availability, feeding accurate planned operating hours to OEE (Overall Equipment Effectiveness) systems.

### Scope - Phase 1 (MVP)
**IN SCOPE:**
- ISA-95 equipment hierarchy management
- Simple operational pattern assignment
- Automated schedule generation
- OEE system API integration
- Basic exception handling

**OUT OF SCOPE (Phase 2):**
- Employee/operator scheduling
- Complex rotation patterns
- Team management
- Shift swapping
- Labor coverage analysis

### Success Metrics
- Setup time: < 30 minutes from login to first schedule
- Schedule generation: < 5 seconds for 1000 equipment items
- API response time: < 200ms for availability queries
- User adoption: 80% prefer over Excel within 30 days

## 2. User Personas

### Primary: Operations Manager (Daily User)
- **Goal**: Maintain accurate equipment schedules for OEE reporting
- **Pain Points**: Manual Excel updates, no inheritance, error-prone
- **Needs**: Quick exception handling, visual status dashboard
- **Technical Level**: Comfortable with web applications

### Secondary: Industrial Engineer (Weekly User)
- **Goal**: Design and optimize equipment operating patterns
- **Pain Points**: No standard pattern library, changes require manual cascading
- **Needs**: Pattern templates, bulk assignment, inheritance rules
- **Technical Level**: Advanced, understands manufacturing systems

### Tertiary: System Administrator (Monthly User)
- **Goal**: Maintain equipment hierarchy and integrations
- **Pain Points**: No standard structure, manual API configurations
- **Needs**: Bulk import, ISA-95 compliance, integration monitoring
- **Technical Level**: IT proficient

## 3. Functional Requirements

### REQ-1: Equipment Hierarchy Management

#### REQ-1.1: ISA-95 Structure
**User Story**: As a System Administrator, I want to organize equipment using ISA-95 standards so that our structure aligns with industry practices.

**Acceptance Criteria:**
- System enforces 5-level hierarchy: Enterprise → Site → Area → Work Center → Work Unit
- Each equipment item has unique identifier and hierarchical path
- Equipment can be marked as "Requires Scheduling" (boolean flag)
- Bulk import supports 1000+ items via CSV/Excel template
- Visual tree representation with expand/collapse navigation

**Priority**: P0 - Must Have

#### REQ-1.2: Bulk Import
**User Story**: As a System Administrator, I want to import equipment from Excel so that I can quickly populate the system.

**Acceptance Criteria:**
- Downloadable Excel template with ISA-95 structure
- Validation preview before import with error highlighting
- Import processes 1000+ items in under 60 seconds
- Duplicate detection and resolution options
- Success/failure report with row-level details

**Priority**: P0 - Must Have

### REQ-2: Pattern Management

#### REQ-2.1: Simple Pattern Library
**User Story**: As an Industrial Engineer, I want to select from standard patterns so that I can quickly assign operating schedules.

**Acceptance Criteria:**
- Five pre-built patterns available:
  - 24/7 Continuous (always running)
  - Two-Shift (06:00-22:00 Mon-Fri)
  - Day-Only (08:00-17:00 Mon-Fri)
  - Extended (06:00-02:00 Mon-Sat)
  - Custom (user-defined)
- Each pattern shows weekly coverage hours
- Visual timeline preview (24-hour × 7-day grid)
- Pattern names are customizable

**Priority**: P0 - Must Have

#### REQ-2.2: Custom Pattern Creation
**User Story**: As an Industrial Engineer, I want to create custom patterns so that I can handle unique operational requirements.

**Acceptance Criteria:**
- Visual week designer with day/night indicators
- Click-and-drag to set operating hours
- Support for different weekday vs weekend schedules
- Save custom patterns with descriptive names
- Validation ensures at least one operating hour per week

**Priority**: P1 - Should Have

### REQ-3: Pattern Assignment

#### REQ-3.1: Hierarchical Assignment
**User Story**: As an Operations Manager, I want to assign patterns using inheritance so that I can efficiently configure large equipment groups.

**Acceptance Criteria:**
- Drag-and-drop patterns onto hierarchy nodes
- Children automatically inherit parent patterns
- Visual indicators show inherited vs override status
- Bulk assignment to multiple selected items
- Assignment summary shows coverage statistics

**Priority**: P0 - Must Have

#### REQ-3.2: Override Management
**User Story**: As an Operations Manager, I want to override inherited patterns so that I can handle equipment-specific exceptions.

**Acceptance Criteria:**
- Right-click to override inherited pattern
- Visual distinction between inherited and override
- "Reset to inherited" option available
- Override reasons can be documented
- Report showing all overrides in system

**Priority**: P1 - Should Have

### REQ-4: Schedule Generation

#### REQ-4.1: Automated Generation
**User Story**: As an Operations Manager, I want to generate schedules automatically so that OEE systems have availability data.

**Acceptance Criteria:**
- Generate schedules for specified date range (up to 12 months)
- Generation includes all equipment marked for scheduling
- Schedules reflect assigned patterns accurately
- Holiday calendars automatically applied
- Generation completes in < 5 seconds for 1000 items

**Priority**: P0 - Must Have

#### REQ-4.2: Schedule Data Structure
**User Story**: As an OEE System, I want to receive structured availability data so that I can calculate performance metrics.

**Acceptance Criteria:**
- Each schedule record includes:
  - Equipment ID and hierarchy path
  - Date and shift identifier
  - Planned start and end times
  - Planned operating hours
  - Status (Operating/Maintenance/Holiday/Shutdown)
- Data available in JSON and CSV formats
- Timezone handling (UTC storage, local display)

**Priority**: P0 - Must Have

### REQ-5: Exception Handling

#### REQ-5.1: Planned Maintenance
**User Story**: As an Operations Manager, I want to schedule maintenance windows so that OEE systems know when equipment is unavailable.

**Acceptance Criteria:**
- Create one-time or recurring maintenance events
- Maintenance affects schedule generation
- Visual indicators on affected dates
- Notification when schedules need regeneration
- Maintenance history tracked for reporting

**Priority**: P1 - Should Have

#### REQ-5.2: Unplanned Downtime
**User Story**: As an Operations Manager, I want to record breakdowns so that schedules reflect reality.

**Acceptance Criteria:**
- Quick-entry form for breakdown events
- Affects current day's schedule immediately
- API notifies connected OEE systems within 5 minutes
- Option to extend breakdown to multiple days
- Breakdown history maintained

**Priority**: P2 - Nice to Have

### REQ-6: OEE Integration

#### REQ-6.1: REST API
**User Story**: As an OEE System, I want to query equipment availability via API so that I can calculate OEE metrics.

**Acceptance Criteria:**
- RESTful API with OpenAPI 3.0 specification
- Endpoints for:
  - GET /equipment/{id}/availability
  - GET /equipment/availability (bulk)
  - GET /schedules/changes (webhook)
- Response time < 200ms for typical queries
- API key authentication
- Rate limiting (1000 requests/hour)

**Priority**: P0 - Must Have

#### REQ-6.2: Push Notifications
**User Story**: As an OEE System, I want to receive schedule changes immediately so that my calculations stay current.

**Acceptance Criteria:**
- Webhook registration for schedule changes
- Changes pushed within 30 seconds
- Retry logic for failed deliveries
- Change types: Created, Updated, Deleted
- Bulk change notifications supported

**Priority**: P2 - Nice to Have

### REQ-7: Calendar Management

#### REQ-7.1: Holiday Calendars
**User Story**: As an Operations Manager, I want to define holidays so that schedules account for non-working days.

**Acceptance Criteria:**
- Pre-loaded calendars for South Africa, USA, UK, Canada
- Custom holiday creation
- Holidays can be company-wide or site-specific
- Holiday impact visible in schedule preview
- Different equipment can use different calendars

**Priority**: P1 - Should Have

### REQ-8: User Interface

#### REQ-8.1: Dashboard
**User Story**: As an Operations Manager, I want to see system status at a glance so that I know everything is running correctly.

**Acceptance Criteria:**
- Summary cards showing:
  - Equipment scheduled vs total
  - Patterns in use
  - Schedule currency
  - API connection status
- Quick actions for common tasks
- Visual alerts for issues needing attention
- Refresh without page reload

**Priority**: P0 - Must Have

#### REQ-8.2: Responsive Design
**User Story**: As a user, I want to access the system from any device so that I can work from anywhere.

**Acceptance Criteria:**
- Desktop optimized (1024px+)
- Tablet readable (768px+)
- Mobile view-only (320px+)
- Print-friendly schedule reports
- Consistent experience across Chrome, Firefox, Safari, Edge

**Priority**: P1 - Should Have

## 4. Non-Functional Requirements

### Performance
- Page load time: < 2 seconds
- Schedule generation: < 5 seconds for 1000 items
- API response: < 200ms average, < 500ms 95th percentile
- Concurrent users: Support 100+ simultaneous users

### Scalability
- Equipment items: Support 10,000+ per installation
- Schedule records: Handle 1M+ records
- API calls: 10,000+ per day
- Data retention: 2 years of history

### Reliability
- System availability: 99.9% uptime during business hours
- Data durability: Zero data loss, daily backups
- Recovery time: < 1 hour from backup
- Graceful degradation: System remains usable if API fails

### Security
- Authentication: SSO support (SAML 2.0)
- Authorization: Role-based access control
- Encryption: TLS 1.3 in transit, AES-256 at rest
- Audit: Complete activity logging
- Compliance: GDPR ready

### Usability
- Setup time: < 30 minutes for average factory
- Training required: < 2 hours for new users
- Task completion: Common tasks < 3 clicks
- Error recovery: Clear messages with resolution steps

## 5. Implementation Priorities

### Phase 1A - Core (Weeks 1-4)
1. Equipment hierarchy with ISA-95 structure
2. Five simple patterns
3. Pattern assignment with inheritance
4. Basic schedule generation

### Phase 1B - Integration (Weeks 5-6)
5. REST API for OEE systems
6. Holiday calendar support
7. Exception handling
8. Dashboard and monitoring

### Phase 1C - Polish (Weeks 7-8)
9. Bulk import refinement
10. Custom pattern creation
11. Reporting and exports
12. Performance optimization

## 6. Success Criteria

### Launch Readiness
- [ ] 1000 equipment items load in < 2 seconds
- [ ] Schedule generation accurate to 99.9%
- [ ] API documentation complete
- [ ] 5 patterns cover 80% of use cases
- [ ] Setup achievable in 30 minutes

### Post-Launch Success (30 days)
- [ ] 5+ customers successfully integrated
- [ ] Zero critical bugs in production
- [ ] 80% users prefer over Excel
- [ ] OEE systems receiving accurate data
- [ ] No performance degradation

## 7. Future Considerations (Phase 2 Ready)

### Database Structure
- Employee tables exist but unused
- Complex pattern algorithms implemented but hidden
- Team rotation logic ready but disabled
- Skill matching structure defined

### API Design
- Endpoints designed to extend without breaking
- Version 2 additions won't affect Version 1
- Additional fields nullable for backward compatibility

### UI Architecture
- Employee modules built but feature-flagged
- Navigation structure supports additional sections
- Component library handles increased complexity

This ensures Phase 2 activation requires no refactoring, only configuration changes and feature flag updates.