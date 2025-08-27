# OEE Monitoring System - Functional Specification

## System Overview

The OEE (Overall Equipment Effectiveness) monitoring system tracks manufacturing equipment performance in real-time by collecting immutable count data from ADAM devices and overlaying contextual information through operator interactions. The system calculates and displays the three key OEE metrics: Availability, Performance, and Quality.

## Core Architecture Principles

### Two-Layer Data Model
1. **Layer 1: Immutable Count Data**
   - Raw counts from ADAM devices stored in time-series database
   - Never modified or deleted
   - Serves as the single source of truth

2. **Layer 2: Contextual Annotations**
   - Job assignments, stoppage classifications, and other metadata
   - Overlaid on top of count data
   - Can be added retrospectively with full audit trail

## Equipment Configuration

- Each ADAM unit is permanently connected to a specific production line or equipment
- When an operator logs into a machine, the system automatically knows which ADAM unit data stream to monitor
- Creates a fixed relationship: Machine → ADAM unit → Data stream

## Job Management

### Job Queue Population
- External ERP/scheduling systems populate the work order queue via API
- Jobs contain:
  - Item code and description
  - Target quantity
  - Planned running speed
  - Equipment ID (links to specific machine/line)
  - Planned start time
  - Job status (scheduled, in progress, completed, cancelled)

### Job Selection and Start
1. **Primary Workflow - Pre-scheduled Jobs**
   - Operator logs onto specific machine/line
   - Views list of jobs scheduled for that equipment
   - Selects appropriate job from queue
   - All parameters auto-populate
   - Starts production

2. **Alternative Workflow - Manual Job Creation**
   - For unscheduled or ad-hoc production runs
   - Operator manually inputs all required parameters

### Job Sequencing Rules
- Only one job can run on a line at any given time
- No overlap between jobs is permitted
- Clear state progression: Job Running → Job Ended → Changeover → Next Job Started

### Job Completion Validation
When starting a new job, the system validates the previous job:
- If previous job completed ≥X% of target quantity: Allow immediate transition
- If previous job completed <X% of target quantity:
  - Display warning: "Previous job only completed 75% (750/1000 units). Confirm job removal?"
  - Operator must provide reason using 2-level reason code structure:
    - Level 1: 3x3 matrix = 9 high-level categories
    - Level 2: Each Level 1 category has its own 3x3 matrix = 9 specific reasons
    - Total possible combinations: 81 reason codes (not all may be used)
  - Only after confirmation can the new job be started

## Stoppage Management

### Automatic Stoppage Detection
- System detects when counts stop incrementing
- Stoppage timer begins automatically
- Initial state: "Unclassified stoppage"

### Stoppage Classification Rules
- **Short stops** (<X minutes): 
  - Recorded automatically
  - No operator classification required
  - Visible in detailed reports but not in main dashboard
  
- **Long stops** (≥X minutes):
  - Requires operator classification
  - System alerts operator to classify
  - Classification uses 2-level reason code structure:
    - Level 1: 3x3 matrix = 9 high-level categories
    - Level 2: Each Level 1 category has its own 3x3 matrix = 9 specific reasons
    - Total possible combinations: 81 reason codes (not all may be used)

### Changeover Tracking
- When job is selected but line hasn't started running
- System automatically creates a stoppage
- Operator should classify as "Changeover"
- Differentiates planned downtime from unplanned issues

## Real-Time Operation

### Normal Operation Flow
1. Operator selects/starts job
2. ADAM units continuously send count data
3. System calculates OEE metrics every few minutes
4. Dashboard displays:
   - Current OEE score (Availability × Performance × Quality)
   - Production count vs target
   - Current run rate
   - Active stoppages requiring classification

### Alerts
- Long stoppage requiring classification
- Job nearing completion
- Quality issues (reject rate exceeding threshold)
- Performance below target rate

## Retrospective Data Assignment

### The "Forgotten Start" Problem
When operators forget to start a job in the system but production has begun:

1. **Orphan Count Detection**
   - System identifies periods with count data but no assigned job
   - Highlights these gaps in supervisor dashboard

2. **Overproduction Detection**
   - System monitors when active job exceeds target quantity
   - Identifies pattern: Long stop followed by continued counting on old job
   - This typically indicates:
     - Operator didn't end the previous job
     - New job started physically but not in the system
     - Counts accumulating against wrong job
   - System highlights as "Potential job transition error"
   - Only operator/supervisor can confirm what actually happened

3. **Retrospective Assignment Process**
   - Supervisor/operator accesses timeline view
   - Visual graph shows count data with gaps and overproduction highlighted
   - User determines what actually happened and can:
     - End the previous job at the correct point
     - Start the new job (system automatically assigns subsequent counts)
     - Record startup waste/scrap if applicable
     - Add or classify stoppages
     - Leave as-is if the overproduction was intentional
   - System behavior:
     - Once a job is ended, any counts after that point automatically flow to the next started job
     - No manual count reassignment needed
   - System creates backdated annotations for all changes

3. **Validation Rules**
   - Assigned time range must not overlap with existing jobs
   - Count rate must be reasonable for the selected job type
   - When splitting overproduced jobs:
     - Original job end time is adjusted
     - New job created with reassigned counts
     - Gap between jobs can be classified (typically changeover)
   - All retrospective assignments logged with:
     - Who made the assignment
     - When it was made
     - Original vs assigned timestamps

### Data Integrity
- Original count data never modified
- Annotations can be corrected but never deleted
- Most recent annotation takes precedence
- Complete audit trail maintained
- Corrections improve accuracy without corrupting raw data

## User Interfaces

### Operator Dashboard
- Current job information
- Real-time OEE metrics
- Production count progress bar
- Stoppage classification prompts
- Quick job selection/start controls

### Supervisor Dashboard
- Multi-line overview
- Historical trends
- Orphan count alerts
- Retrospective assignment tools
- Detailed reports and analytics

### Timeline View (for retrospective work)
- Visual count data graph
- Job assignment overlay
- Drag-to-select interface
- Stoppage markers
- Annotation history

## API Integration

### Inbound APIs
- Job queue population from ERP/scheduling systems
- Product master data synchronization

### Outbound APIs
- Real-time OEE metrics
- Production counts
- Stoppage events
- Job completion notifications

## Scrap/Waste Management

### Automatic Scrap Detection
- If equipment has a dedicated scrap counter (separate ADAM channel), scrap is tracked automatically
- Reduces good count to calculate actual yield

### Manual Scrap Recording
- API endpoint for manual scrap entry when:
  - Scrap doesn't pass through the counter (e.g., startup waste, setup pieces)
  - Quality inspection reveals defects after counting
  - Material is scrapped before reaching the counter
- Manual entries include:
  - Quantity
  - Reason code (using 2-level structure)
  - Timestamp
  - Job assignment

### Quality Calculation
- Quality % = (Good Count - Scrap) / Total Count × 100
- Both automatic and manual scrap entries affect OEE quality metric

### Automated Stoppage Classification
- System learns patterns to auto-classify certain stoppages
- Planned downtime automatically labeled based on schedule
- Operator can override automatic classifications

### Predictive Analytics
- Identify patterns leading to unplanned downtime
- Predict when maintenance is needed
- Optimize changeover sequences

## Success Metrics
- Reduction in unclassified stoppage time
- Improvement in data accuracy through retrospective assignments
- Decrease in time to classify stoppages
- Overall OEE improvement through better visibility