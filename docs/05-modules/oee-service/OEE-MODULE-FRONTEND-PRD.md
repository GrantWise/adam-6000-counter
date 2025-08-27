# OEE Module Frontend PRD
**Industrial ADAM Overall Equipment Effectiveness - Frontend Product Requirements Document**

**Module**: OEE Service Frontend Integration  
**Priority**: High - Core Business Module  
**Target Timeline**: Phase 2 (Week 6) of Frontend Implementation Roadmap  
**Dependencies**: Phase 1 Infrastructure and Logger Module Integration Complete  
**Last Updated**: August 26, 2025

---

## 🎯 Executive Summary

The OEE Module Frontend provides comprehensive interfaces for monitoring and analyzing Overall Equipment Effectiveness (OEE) metrics, including Availability, Performance, and Quality calculations. This module serves production supervisors and managers with real-time OEE monitoring, work order management, and stoppage tracking.

**Current State**: UI components exist but operate on mock OEE calculations  
**Target State**: Fully integrated with OEE API (port 5140) displaying real OEE metrics with Equipment Scheduling integration

### Key Integration Points
- **OEE API**: Real OEE calculations and metrics (port 5140)
- **SignalR Stoppage Hub**: Real-time stoppage notifications and updates
- **Equipment Scheduling Integration**: Planned availability from scheduling module
- **Work Order Management**: Production job tracking and completion monitoring

---

## 🏗️ System Architecture

### Frontend Architecture
```
OEE Module Frontend
├── OEE Dashboard Interface
│   ├── Real-time OEE Metrics Display
│   ├── Availability, Performance, Quality Breakdown
│   ├── Historical Trends and Analysis
│   └── Multi-Equipment OEE Comparison
├── Work Order Management
│   ├── Work Order Creation and Assignment
│   ├── Production Progress Tracking
│   ├── Work Order Completion and Validation
│   └── Work Order Performance Analytics
├── Stoppage Management
│   ├── Real-time Stoppage Detection
│   ├── Stoppage Categorization and Reason Codes
│   ├── Stoppage Duration and Impact Analysis
│   └── Stoppage Prevention Analytics
└── Integration Layer
    ├── OEE API Client (port 5140)
    ├── SignalR Stoppage Hub Connection
    ├── Equipment Scheduling Integration
    └── Logger Module Counter Data Integration
```

### Backend Integration
- **OEE API Endpoints**: OEE calculations, work orders, stoppage tracking
- **SignalR Stoppage Hub**: Real-time stoppage notifications and updates
- **Equipment Scheduling API**: Planned availability integration
- **Logger Counter Data**: Production count data for performance calculations

---

## 📋 Functional Requirements

### FR1: OEE Metrics Dashboard
**Priority**: Critical  
**Current State**: Mock OEE calculations and displays  
**Target State**: Real OEE calculations from backend with live updates

#### FR1.1: Real-time OEE Display
- **Live OEE percentage** with color-coded status indicators
- **Availability, Performance, Quality** component breakdown
- **Target vs. actual** comparison with variance indicators
- **Time period selection** (shift, day, week, month)
- **Equipment selection** for individual or group OEE views

#### FR1.2: OEE Component Analysis
- **Availability breakdown**: Planned vs. unplanned downtime
- **Performance breakdown**: Speed losses and minor stoppages
- **Quality breakdown**: Defect rates and rework impact
- **Loss categorization** with Pareto analysis
- **Improvement opportunity identification** and prioritization

#### FR1.3: Historical Trends and Analytics
- **OEE trend charts** over selectable time periods
- **Component trend analysis** showing improvement patterns
- **Benchmark comparisons** against targets and historical performance
- **Statistical analysis** including averages, medians, and variability
- **Export capabilities** for further analysis and reporting

**API Integration Requirements**:
```typescript
// OEE Metrics API Calls
GET /api/oee/current/{equipmentId}     // Current OEE metrics
GET /api/oee/history/{equipmentId}     // Historical OEE data
GET /api/oee/components/{equipmentId}  // OEE component breakdown
GET /api/oee/targets/{equipmentId}     // OEE targets and benchmarks
GET /api/oee/comparison               // Multi-equipment comparison
POST /api/oee/targets/{equipmentId}    // Set OEE targets
GET /api/oee/analytics/{equipmentId}   // OEE analytics and insights
```

### FR2: Work Order Management
**Priority**: Critical  
**Current State**: Mock work order data and workflows  
**Target State**: Real work order management with OEE integration

#### FR2.1: Work Order Creation and Assignment
- **Work order wizard** for creating production jobs
- **Equipment assignment** with capacity validation
- **Resource allocation** and skill matching
- **Schedule integration** with planned availability
- **Priority management** and job sequencing

#### FR2.2: Production Progress Tracking
- **Real-time progress monitoring** against work order targets
- **Production rate tracking** with performance indicators
- **Milestone management** and completion tracking
- **Quality checkpoints** and inspection results
- **Resource utilization** monitoring during production

#### FR2.3: Work Order Completion and Analytics
- **Work order completion** with final metrics capture
- **Performance analysis** against targets and standards
- **Cost tracking** and resource consumption analysis
- **Quality metrics** and defect rate calculation
- **Lessons learned** capture and knowledge management

**Work Order API Integration**:
```typescript
// Work Order Management API Calls
GET /api/workorders                    // List all work orders
POST /api/workorders                   // Create new work order
GET /api/workorders/{id}              // Get work order details
PUT /api/workorders/{id}              // Update work order
DELETE /api/workorders/{id}           // Delete work order
POST /api/workorders/{id}/start       // Start work order
POST /api/workorders/{id}/complete    // Complete work order
GET /api/workorders/{id}/progress     // Get production progress
POST /api/workorders/{id}/quality     // Record quality data
```

### FR3: Stoppage Management
**Priority**: High  
**Current State**: Mock stoppage data and notifications  
**Target State**: Real-time stoppage tracking with SignalR integration

#### FR3.1: Real-time Stoppage Detection and Notification
- **Automatic stoppage detection** from production data
- **Real-time notifications** via SignalR stoppage hub
- **Stoppage alerts** with escalation procedures
- **Stoppage acknowledgment** and response tracking
- **Mobile notifications** for critical stoppages

#### FR3.2: Stoppage Categorization and Analysis
- **Standardized reason codes** for stoppage categorization
- **Root cause analysis** tools and workflows
- **Stoppage duration tracking** with automatic timing
- **Impact assessment** on OEE and production targets
- **Corrective action tracking** and follow-up

#### FR3.3: Stoppage Prevention and Analytics
- **Stoppage pattern analysis** and trend identification
- **Predictive analytics** for stoppage prevention
- **Maintenance correlation** with stoppage frequency
- **Best practices** sharing and implementation tracking
- **Continuous improvement** metrics and reporting

**Stoppage Tracking Implementation**:
```typescript
interface StoppageEvent {
  id: string
  equipmentId: string
  startTime: Date
  endTime?: Date
  duration?: number
  reasonCode: string
  category: 'Planned' | 'Unplanned'
  impact: {
    availabilityLoss: number
    performanceLoss: number
    qualityLoss: number
    productionLoss: number
  }
  status: 'Active' | 'Acknowledged' | 'Resolved'
  assignedTo?: string
  notes?: string
}

// SignalR Stoppage Hub Integration
hubConnection.on('StoppageStarted', (stoppage: StoppageEvent))
hubConnection.on('StoppageEnded', (stoppage: StoppageEvent))
hubConnection.on('StoppageUpdated', (stoppage: StoppageEvent))
hubConnection.on('StoppageAcknowledged', (stoppageId: string, userId: string))
```

---

## 🎨 User Interface Requirements

### UI1: OEE Dashboard
**Layout**: Multi-panel dashboard with real-time metrics and charts  
**Responsive**: Optimized for both desktop and factory tablets

#### Main OEE Display Panel
- **Large OEE gauge** with color-coded performance zones (0-65% Red, 65-85% Yellow, 85%+ Green)
- **Component breakdown** with availability, performance, quality percentages
- **Target comparison** with variance indicators and improvement arrows
- **Time period selector** with preset options (current shift, today, week, month)
- **Equipment selector** with multi-select capability for comparison views

#### Component Analysis Panel
- **Availability chart** showing planned vs. unplanned downtime
- **Performance chart** displaying speed efficiency and minor stops
- **Quality chart** with defect rates and first-pass yield
- **Loss waterfall chart** showing OEE loss categories
- **Pareto analysis** of loss categories for improvement focus

#### Historical Trends Panel
- **OEE trend line chart** with selectable time ranges
- **Component trend charts** showing individual A, P, Q trends
- **Statistical summary** with mean, median, standard deviation
- **Benchmark comparison** against industry standards
- **Trend analysis** with improvement/decline indicators

### UI2: Work Order Management Interface
**Layout**: Split-screen with work order list and detail view  
**Context**: Production planning and execution interface

#### Work Order List View
- **Active work orders** with status indicators and progress bars
- **Priority sorting** with color-coded urgency levels
- **Equipment grouping** showing work orders by production line
- **Search and filtering** by status, equipment, date range
- **Bulk operations** for work order management

#### Work Order Detail View
- **Work order information**: Job number, product, quantity, due date
- **Equipment assignment** with resource allocation details
- **Production progress**: Current vs. target production rates
- **Quality metrics**: Defect rates, inspection results, compliance status
- **Real-time updates**: Progress tracking and milestone completion

#### Work Order Creation Wizard
- **Step-by-step wizard** for new work order creation
- **Product selection** from catalog with specifications
- **Equipment assignment** with capacity and capability validation
- **Resource planning** with skill matching and availability
- **Schedule integration** with automatic conflict detection

### UI3: Stoppage Management Dashboard
**Layout**: Real-time stoppage monitoring with detailed analysis panels  
**Context**: Production monitoring and incident response

#### Active Stoppages Panel
- **Real-time stoppage alerts** with severity indicators
- **Stoppage timeline** showing current and recent events
- **Impact assessment** with OEE loss calculations
- **Response tracking** with assigned personnel and status
- **Escalation indicators** for unresolved stoppages

#### Stoppage Analysis Panel
- **Reason code breakdown** with frequency analysis
- **Duration analysis** with average and trend indicators
- **Root cause analysis** tools and templates
- **Corrective action tracking** with completion status
- **Prevention metrics** showing improvement progress

#### Stoppage History and Reporting
- **Historical stoppage data** with trend analysis
- **Pattern identification** and recurring issue tracking
- **Benchmark comparisons** against performance targets
- **Improvement tracking** with before/after analysis
- **Report generation** for management and continuous improvement

---

## 🔧 Technical Requirements

### TR1: API Integration Architecture
**Technology**: TypeScript with Axios HTTP client  
**Error Handling**: OEE-specific error recovery and data validation

#### OEE API Client Implementation
```typescript
interface OEEApiClient {
  // OEE Metrics
  getCurrentOEE(equipmentId: string): Promise<OEEMetrics>
  getHistoricalOEE(equipmentId: string, timeRange: TimeRange): Promise<OEEHistory[]>
  getOEEComponents(equipmentId: string, timeRange: TimeRange): Promise<OEEComponents>
  getOEETargets(equipmentId: string): Promise<OEETargets>
  setOEETargets(equipmentId: string, targets: OEETargets): Promise<void>
  
  // Work Order Management
  getWorkOrders(filters?: WorkOrderFilter): Promise<WorkOrder[]>
  getWorkOrder(workOrderId: string): Promise<WorkOrder>
  createWorkOrder(workOrder: CreateWorkOrderRequest): Promise<WorkOrder>
  updateWorkOrder(workOrderId: string, updates: UpdateWorkOrderRequest): Promise<WorkOrder>
  startWorkOrder(workOrderId: string): Promise<WorkOrder>
  completeWorkOrder(workOrderId: string, completion: WorkOrderCompletion): Promise<WorkOrder>
  
  // Stoppage Management
  getStoppages(equipmentId: string, timeRange: TimeRange): Promise<Stoppage[]>
  acknowledgeStoppage(stoppageId: string, userId: string): Promise<void>
  updateStoppage(stoppageId: string, updates: StoppageUpdate): Promise<Stoppage>
  getStoppageReasons(): Promise<StoppageReason[]>
  getStoppageAnalytics(equipmentId: string, timeRange: TimeRange): Promise<StoppageAnalytics>
}
```

#### Data Quality and Validation
```typescript
interface OEEDataValidation {
  validateOEECalculation(oeeData: OEEMetrics): ValidationResult
  validateWorkOrderData(workOrder: WorkOrder): ValidationResult
  validateStoppageData(stoppage: Stoppage): ValidationResult
  
  // CFR Part 11 compliance
  auditDataAccess(userId: string, dataType: string, equipmentId: string): void
  validateDataIntegrity(data: any): IntegrityResult
  requireElectronicSignature(action: string, data: any): boolean
}
```

### TR2: SignalR Integration for Real-time Updates
**Technology**: Microsoft SignalR Client for JavaScript  
**Connection Management**: Automatic reconnection with production data priority

#### SignalR Stoppage Hub Integration
```typescript
interface SignalRStoppageHub {
  connect(): Promise<void>
  disconnect(): Promise<void>
  getConnectionState(): ConnectionState
  
  // Stoppage event handlers
  onStoppageStarted(callback: (stoppage: StoppageEvent) => void): void
  onStoppageEnded(callback: (stoppage: StoppageEvent) => void): void
  onStoppageUpdated(callback: (stoppage: StoppageEvent) => void): void
  
  // OEE update handlers
  onOEEUpdated(callback: (equipmentId: string, oee: OEEMetrics) => void): void
  onProductionUpdate(callback: (equipmentId: string, production: ProductionData) => void): void
  
  // Work order handlers
  onWorkOrderStarted(callback: (workOrder: WorkOrder) => void): void
  onWorkOrderCompleted(callback: (workOrder: WorkOrder) => void): void
  onProductionProgress(callback: (workOrderId: string, progress: ProductionProgress) => void): void
}
```

### TR3: Integration with Equipment Scheduling Module
**Integration Point**: Planned availability calculations from scheduling data  
**Data Synchronization**: Real-time sync of planned vs. actual availability

#### Scheduling Integration Implementation
```typescript
interface SchedulingIntegration {
  // Planned availability from scheduling
  getPlannedAvailability(equipmentId: string, timeRange: TimeRange): Promise<number>
  
  // Schedule impact on OEE
  calculateScheduleImpact(equipmentId: string, schedule: Schedule): Promise<OEEImpact>
  
  // Downtime planning
  planDowntime(equipmentId: string, downtime: PlannedDowntime): Promise<void>
  
  // Schedule variance analysis
  getScheduleVariance(equipmentId: string, timeRange: TimeRange): Promise<ScheduleVariance>
}

interface OEEImpact {
  plannedAvailability: number
  actualAvailability: number
  availabilityVariance: number
  performanceImpact: number
  qualityImpact: number
}
```

---

## 🎯 Performance Requirements

### PR1: Real-time Display Performance
- **OEE dashboard loading**: < 2 seconds for complete dashboard with 10 equipment items
- **Real-time updates**: < 1 second latency from production event to UI update
- **Chart rendering**: < 1 second for historical charts with 1 month of data
- **Work order operations**: < 2 seconds for work order creation and updates
- **Stoppage notifications**: < 500ms from event detection to UI alert

### PR2: Data Processing Performance
- **OEE calculations**: Client-side validation < 100ms for real-time metrics
- **Historical data queries**: < 2 seconds for 6 months of OEE history
- **Analytics processing**: < 3 seconds for complex trend analysis
- **Export operations**: < 10 seconds for large data set exports
- **Search and filtering**: < 500ms for work order and stoppage searches

### PR3: Scalability and Concurrency
- **Multi-equipment monitoring**: Support 50+ equipment items simultaneously
- **Concurrent users**: Support 25 users accessing OEE data simultaneously
- **Data volume**: Handle 1M+ production events efficiently
- **Memory usage**: < 100MB growth per 8-hour shift of operation
- **Network efficiency**: < 2MB/hour data transfer for typical OEE monitoring

---

## 🔒 Security Requirements

### SR1: OEE Data Security
- **Production data protection**: Encrypt all production metrics in transit and at rest
- **Access control**: Role-based access to OEE data (Operator read-only, Supervisor edit)
- **Data integrity**: Validate all OEE calculations against source data
- **Audit trails**: Complete logging of all OEE data access and modifications
- **Data retention**: Comply with industrial data retention requirements

### SR2: Work Order Security
- **Work order authorization**: Validate user permissions for work order operations
- **Production data security**: Protect sensitive production targets and costs
- **Change tracking**: Log all work order modifications with user attribution
- **Resource access control**: Validate equipment and resource assignment permissions
- **Completion validation**: Require appropriate authorization for work order completion

### SR3: Compliance and Audit
- **CFR Part 11 compliance**: Electronic signature support for critical OEE reviews
- **Data traceability**: Complete chain of custody for all production data
- **Audit reporting**: Generate compliance reports for regulatory review
- **Data backup**: Secure backup of critical OEE and production data
- **Incident tracking**: Secure logging of all production incidents and responses

---

## 🧪 Testing Requirements

### TR1: OEE Calculation Testing
- **Metric accuracy**: Verify OEE calculations match backend computations exactly
- **Component validation**: Test availability, performance, quality calculations independently
- **Edge case testing**: Test with zero production, 100% efficiency, and error conditions
- **Historical accuracy**: Validate historical OEE trends against source data
- **Target comparison**: Test target vs. actual variance calculations

### TR2: Real-time Integration Testing
- **SignalR connectivity**: Test stoppage hub under various network conditions
- **Event processing**: Validate all stoppage and production event handling
- **Connection recovery**: Test automatic reconnection after network interruptions
- **Performance under load**: Test real-time updates with high event frequency
- **Data synchronization**: Verify real-time data consistency with backend

### TR3: Work Order Workflow Testing
- **Complete workflows**: Test full work order lifecycle from creation to completion
- **Integration testing**: Validate OEE integration with work order progress
- **Resource validation**: Test equipment and resource assignment validation
- **Performance tracking**: Verify production progress accuracy against targets
- **Error handling**: Test all work order error scenarios and recovery

---

## 🚀 Deployment Requirements

### DR1: Configuration Management
- **OEE targets**: Configurable OEE targets per equipment and time period
- **Stoppage reasons**: Customizable stoppage reason codes and categories
- **Alert thresholds**: Configurable thresholds for OEE and stoppage alerts
- **Report templates**: Customizable report templates for different stakeholders
- **Integration settings**: Configure scheduling and logger module integration

### DR2: Data Migration and Setup
- **Historical OEE data**: Import existing OEE data where available
- **Work order templates**: Set up standard work order templates
- **Stoppage categories**: Configure facility-specific stoppage reason codes
- **Equipment configuration**: Set up equipment-specific OEE targets and parameters
- **User training**: Training materials for OEE monitoring and analysis

### DR3: Monitoring and Alerts
- **System performance**: Monitor OEE calculation performance and accuracy
- **Real-time connectivity**: Monitor SignalR hub connection health
- **Data quality**: Monitor data integrity and validation results
- **User adoption**: Track OEE module usage and feature adoption
- **Business metrics**: Monitor OEE improvement trends and target achievement

---

## 📊 Success Criteria

### Functional Success Criteria
- [ ] All OEE calculations match backend computations exactly (100% accuracy)
- [ ] Work order management integrates seamlessly with OEE calculations
- [ ] Real-time stoppage notifications work reliably with < 1 second latency
- [ ] Historical OEE trends display accurately for up to 2 years of data
- [ ] Equipment scheduling integration provides correct planned availability
- [ ] All mock OEE data is replaced with real backend integration

### Performance Success Criteria
- [ ] OEE dashboard loads in < 2 seconds with 10 equipment items
- [ ] Real-time updates maintain < 1 second latency consistently
- [ ] Historical charts render in < 1 second with 6 months of data
- [ ] System handles 50+ equipment items without performance degradation
- [ ] Memory usage remains stable during extended operation (8+ hour shifts)

### Quality Success Criteria
- [ ] OEE data accuracy verified against manual calculations
- [ ] Work order workflows tested and validated by production supervisors
- [ ] Stoppage tracking accuracy verified against actual production events
- [ ] User interface usability tested in factory environment
- [ ] Integration testing passes with Logger and Equipment Scheduling modules

### Business Success Criteria
- [ ] Production supervisors can identify OEE improvement opportunities 75% faster
- [ ] Stoppage response time improved by 50% through real-time notifications
- [ ] Work order completion accuracy improved by 30%
- [ ] OEE visibility drives 15% improvement in overall equipment effectiveness
- [ ] Management reporting efficiency improved by 60%

---

## 📋 Implementation Priority

### Phase 2 Week 6: Core OEE Integration (Critical)
1. Replace mock OEE service with OEE API integration
2. Implement OEE metric calculations with real backend data
3. Add SignalR stoppage hub connection for real-time updates
4. Display real work order data with production progress tracking

### Future Enhancements (Post Phase 4)
1. Advanced OEE analytics with machine learning insights
2. Predictive maintenance integration based on OEE patterns
3. Mobile interface for production floor OEE monitoring
4. Advanced reporting and business intelligence integration
5. Benchmarking against industry OEE standards

---

## 📚 References and Dependencies

### Technical Dependencies
- **OEE API**: Must be stable and performant (port 5140)
- **SignalR Stoppage Hub**: Real-time notifications for production events
- **Equipment Scheduling API**: Planned availability integration
- **Logger Counter Data**: Production count data for performance calculations

### Business Dependencies
- **Production Standards**: OEE targets and benchmark definitions
- **Stoppage Categories**: Standardized reason codes and classifications
- **Work Order Processes**: Integration with existing production workflows
- **User Training**: Training on OEE concepts and system usage

### Standards Compliance
- **CFR Part 11**: Electronic signature and audit trail requirements
- **ISA-95**: Equipment hierarchy and production data modeling
- **OEE Standards**: Industry-standard OEE calculation methodologies
- **Data Integrity**: Industrial data accuracy and traceability requirements

---

## 📝 Acceptance Criteria Summary

The OEE Module Frontend will be considered complete when:

1. **All OEE calculations** work reliably with real backend data and match manual calculations
2. **Work order management** integrates seamlessly with production tracking and OEE metrics
3. **Real-time stoppage tracking** provides immediate notifications and accurate impact analysis
4. **Historical OEE analysis** displays accurate trends and provides actionable insights
5. **Equipment scheduling integration** correctly incorporates planned availability into OEE calculations
6. **Performance requirements** are met under realistic production monitoring loads
7. **CFR Part 11 compliance** is maintained for all production data and calculations
8. **Integration testing** validates all backend API connections and real-time data flow
9. **User acceptance testing** achieves 95% satisfaction rating from production supervisors
10. **Business impact** demonstrates measurable improvement in OEE visibility and response times

This PRD serves as the detailed specification for transforming the existing OEE Module UI scaffold into a fully functional, production-ready interface for Industrial ADAM Overall Equipment Effectiveness monitoring and management.

---

**Document Version**: 1.0  
**Last Updated**: August 26, 2025  
**Next Review**: Start of Phase 2 Week 6 Implementation  
**Owner**: Frontend Development Team  
**Stakeholders**: OEE Module Team, Production Operations, Equipment Scheduling Team