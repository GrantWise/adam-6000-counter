# OEE Operator Interface - Product Requirements Document

## Project Overview

Create an MVP operator interface for real-time OEE (Overall Equipment Effectiveness) monitoring and stoppage classification. This interface will read data from the existing TimescaleDB and provide operators with immediate feedback on production performance.

## Tech Stack
- **Frontend**: React with TypeScript
- **Styling**: Tailwind CSS + Shadcn/ui components
- **Database**: TimescaleDB (PostgreSQL) - existing counter_data table
- **API**: REST API (to be built) or direct database connection

## 1. Core User Stories

### As an Operator, I want to:
1. **Start/End Jobs**: Easily start new production jobs and end current ones
2. **See Real-time Performance**: View current performance vs target in real-time
3. **Classify Stoppages**: Quickly classify machine stoppages when they occur
4. **Monitor OEE**: See overall equipment effectiveness metrics at a glance

## 2. User Interface Requirements

### 2.1 Main Dashboard Screen

**Single page layout with 4 main sections:**

#### Section A: Job Control (Top Left)
```
┌─── Job Control ───────────────────┐
│ Current Job: #12345 - Widget A    │
│ Started: 08:30 AM (3h 45m ago)    │
│                                   │
│ [START NEW JOB]  [END JOB]       │
└───────────────────────────────────┘
```

#### Section B: Performance Metrics (Top Right)
```
┌─── Performance ───────────────────┐
│ Target: 120/min  Actual: 118/min  │
│ Performance: 98% 🟢               │
│ Quality: 95% 🟡                   │
│ Availability: 85% 🔴              │
│                                   │
│ Overall OEE: 79%                  │
└───────────────────────────────────┘
```

#### Section C: Real-time Rate Chart (Bottom Left)
```
┌─── Production Rate ───────────────┐
│     Rate (units/min)              │
│ 140 ┌─────────────────────────────┐│
│ 120 │  ╭─╮ ╭──╮     ╭──╮         ││
│ 100 │ ╱   ╲╱    ╲╱╲╱    ╲       ││
│  80 │╱             ╲      ╲____  ││
│  60 │                           ╲││
│   0 └─────────────────────────────┘│
│     8AM    10AM    12PM    2PM     │
└───────────────────────────────────┘
```

#### Section D: Stoppage Management (Bottom Right)
```
┌─── Stoppages ─────────────────────┐
│ Status: RUNNING ✅                │
│ Running: 3h 22m                   │
│ Stopped: 23m total                │
│                                   │
│ Last Stop: 8 minutes              │
│ Reason: UNCLASSIFIED 🔴           │
│                                   │
│ [CLASSIFY STOPPAGE]               │
└───────────────────────────────────┘
```

### 2.2 Start New Job Modal

**Modal triggered by "START NEW JOB" button:**

```
┌─── Start New Job ─────────────────┐
│                                   │
│ Job Number: [____________]        │
│ Part Number: [____________]       │
│ Target Rate: [____] units/min     │
│                                   │
│ [CANCEL]           [START JOB]    │
└───────────────────────────────────┘
```

### 2.3 Stoppage Classification Interface

**Two-level 3x3 grid system:**

#### Level 1 - Main Categories
```
┌─── Classify Stoppage ─────────────┐
│                                   │
│ Duration: 8 minutes               │
│ Select category:                  │
│                                   │
│ [Mechanical] [Electrical] [Material] │
│ [Quality]    [Operator]   [Planned]  │
│ [External]   [Setup]      [Other]    │
│                                   │
│ [CANCEL]                          │
└───────────────────────────────────┘
```

#### Level 2 - Sub-categories (Example: Mechanical selected)
```
┌─── Mechanical Issue ──────────────┐
│                                   │
│ Select specific reason:           │
│                                   │
│ [Jam]        [Wear]       [Breakage]    │
│ [Alignment]  [Lubrication][Hydraulic]   │
│ [Pneumatic]  [Belt/Chain] [Other]       │
│                                   │
│ Comments (optional):              │
│ [________________________]       │
│                                   │
│ [BACK]              [CONFIRM]     │
└───────────────────────────────────┘
```

## 3. Data Requirements

### 3.1 Existing TimescaleDB Schema

**Table: `counter_data`**
```sql
CREATE TABLE counter_data (
    time TIMESTAMPTZ NOT NULL,
    device_id TEXT NOT NULL,
    channel INT NOT NULL,
    raw_value BIGINT,
    processed_value DOUBLE PRECISION,
    rate DOUBLE PRECISION,        -- Windowed rate (units/sec)
    quality INT,                  -- 0=Good, 1=Degraded, 2=Bad
    tags JSONB,
    PRIMARY KEY (time, device_id, channel)
);
```

### 3.2 New Tables Needed

**Table: `production_jobs`**
```sql
CREATE TABLE production_jobs (
    job_id SERIAL PRIMARY KEY,
    job_number TEXT NOT NULL,
    part_number TEXT NOT NULL,
    device_id TEXT NOT NULL,
    target_rate DOUBLE PRECISION,  -- units/minute
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    operator_id TEXT,
    status TEXT DEFAULT 'active'   -- active, completed, cancelled
);
```

**Table: `stoppage_events`**
```sql
CREATE TABLE stoppage_events (
    event_id SERIAL PRIMARY KEY,
    device_id TEXT NOT NULL,
    job_id INTEGER REFERENCES production_jobs(job_id),
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    duration_minutes INTEGER,
    category TEXT,                 -- Mechanical, Electrical, etc.
    sub_category TEXT,             -- Jam, Wear, etc.
    comments TEXT,
    classified_at TIMESTAMPTZ,
    operator_id TEXT,
    status TEXT DEFAULT 'unclassified'  -- unclassified, classified
);
```

## 4. API Endpoints Needed

### 4.1 Job Management
```typescript
// Start new job
POST /api/jobs
{
  jobNumber: string;
  partNumber: string;
  targetRate: number;  // units/minute
  deviceId: string;
  operatorId: string;
}

// End current job
PUT /api/jobs/{jobId}/end

// Get current job
GET /api/jobs/current?deviceId={deviceId}
```

### 4.2 Real-time Data
```typescript
// Get current performance metrics
GET /api/metrics/current?deviceId={deviceId}
Response: {
  currentRate: number;        // units/minute
  targetRate: number;
  performancePercent: number;
  qualityPercent: number;
  availabilityPercent: number;
  oeePercent: number;
  status: 'running' | 'stopped';
  lastStoppageMinutes?: number;
}

// Get rate history for chart
GET /api/metrics/history?deviceId={deviceId}&hours={hours}
Response: {
  timestamps: string[];
  rates: number[];
  targetRate: number;
}
```

### 4.3 Stoppage Management
```typescript
// Get unclassified stoppages
GET /api/stoppages/unclassified?deviceId={deviceId}

// Classify stoppage
PUT /api/stoppages/{eventId}/classify
{
  category: string;
  subCategory: string;
  comments?: string;
  operatorId: string;
}
```

## 5. Key Database Queries

### 5.1 Current Rate Calculation
```sql
-- Get latest rate (last 2 minutes of data)
SELECT 
  rate * 60 as units_per_minute,  -- Convert from units/sec to units/min
  time
FROM counter_data 
WHERE 
  device_id = $1 
  AND channel = 0  -- Production counter
  AND time > NOW() - INTERVAL '2 minutes'
ORDER BY time DESC 
LIMIT 1;
```

### 5.2 Quality Calculation
```sql
-- Calculate quality for current job
SELECT 
  production.total_production,
  COALESCE(rejects.total_rejects, 0) as total_rejects,
  (production.total_production - COALESCE(rejects.total_rejects, 0)) / 
  production.total_production * 100 as quality_percent
FROM (
  -- Production counter (channel 0)
  SELECT MAX(processed_value) - MIN(processed_value) as total_production
  FROM counter_data cd
  JOIN production_jobs pj ON cd.device_id = pj.device_id
  WHERE pj.status = 'active' 
    AND cd.channel = 0
    AND cd.time >= pj.start_time
) production
LEFT JOIN (
  -- Reject counter (channel 1)
  SELECT MAX(processed_value) - MIN(processed_value) as total_rejects
  FROM counter_data cd
  JOIN production_jobs pj ON cd.device_id = pj.device_id
  WHERE pj.status = 'active' 
    AND cd.channel = 1
    AND cd.time >= pj.start_time
) rejects ON true;
```

### 5.3 Stoppage Detection
```sql
-- Detect current stoppage (rate = 0 for > 1 minute)
SELECT 
  MIN(time) as stoppage_start,
  EXTRACT(EPOCH FROM (NOW() - MIN(time))) / 60 as duration_minutes
FROM counter_data 
WHERE 
  device_id = $1 
  AND channel = 0
  AND rate = 0 
  AND time > NOW() - INTERVAL '1 hour'
  AND time >= ALL(
    SELECT time 
    FROM counter_data 
    WHERE device_id = $1 AND channel = 0 AND rate > 0 
    ORDER BY time DESC 
    LIMIT 1
  )
HAVING COUNT(*) > 12;  -- More than 1 minute of zero rates (5sec intervals)
```

## 6. UI Component Specifications

### 6.1 Required Shadcn Components
- `Button` - For all action buttons
- `Card` - For main dashboard sections
- `Dialog` - For modals (job start, stoppage classification)
- `Input` - For form fields
- `Badge` - For status indicators
- `Alert` - For warnings/notifications
- `Textarea` - For comments field

### 6.2 Color Coding
- **Green (🟢)**: Performance > 95%
- **Yellow (🟡)**: Performance 85-95%
- **Red (🔴)**: Performance < 85% or unclassified stoppages
- **Gray**: No data/inactive

### 6.3 Responsive Design
- **Desktop First**: Optimized for industrial touch screens (1920x1080)
- **Minimum Touch Target**: 44px for all interactive elements
- **High Contrast**: Suitable for factory floor lighting conditions

## 7. Real-time Updates

### 7.1 Polling Strategy
- **Metrics Update**: Every 5 seconds
- **Rate Chart**: Every 30 seconds
- **Stoppage Detection**: Every 10 seconds

### 7.2 WebSocket Alternative
Consider WebSocket for real-time updates if polling creates performance issues:
```typescript
// WebSocket endpoint
ws://api/metrics/live?deviceId={deviceId}

// Message format
{
  type: 'metrics_update' | 'stoppage_detected' | 'job_changed';
  data: { ... };
  timestamp: string;
}
```

## 8. Error Handling

### 8.1 Connection Issues
- Show "Connection Lost" banner when API calls fail
- Continue showing last known data with timestamp
- Retry connection every 10 seconds

### 8.2 Data Validation
- Validate target rate > 0 and < 10000 units/minute
- Require job number and part number
- Prevent starting job if one is already active

## 9. Performance Requirements

### 9.1 Response Times
- **Page Load**: < 2 seconds
- **Metric Updates**: < 500ms
- **Chart Updates**: < 1 second

### 9.2 Data Retention
- **Chart History**: Last 8 hours visible
- **Stoppage List**: Last 10 events
- **Performance Calculation**: Current job only

## 10. Future Enhancements (Out of Scope for MVP)

- Shift management integration
- Historical OEE reporting
- Multi-device dashboard
- Mobile app for supervisors
- Automatic planned downtime detection
- Integration with ERP systems

## 11. Success Metrics

### 11.1 Operator Adoption
- Job start/end compliance > 90%
- Stoppage classification rate > 80%
- Average classification time < 2 minutes

### 11.2 Data Quality
- Real-time metric accuracy within 2%
- Zero unclassified stoppages > 30 minutes
- Complete job data for OEE calculation

## 12. Development Phases

### Phase 1: Core Dashboard (Week 1-2)
- Job control interface
- Real-time metrics display
- Basic rate chart

### Phase 2: Stoppage Management (Week 3)
- Stoppage detection logic
- Classification interface
- Database integration

### Phase 3: Polish & Testing (Week 4)
- Error handling
- Performance optimization
- User acceptance testing

---

**Total Estimated Development Time**: 3-4 weeks for MVP
**Priority**: High - Critical for production visibility