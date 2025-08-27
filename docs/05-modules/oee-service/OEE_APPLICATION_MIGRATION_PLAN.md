# OEE Application Migration Plan

## Executive Summary

This document outlines the strategic plan to convert the sample OEE interface provided by frontend designers into a production-ready OEE operator application. The migration leverages the existing Industrial Counter Platform architecture, TimescaleDB integration, and established counter application patterns while addressing critical gaps in the sample implementation.

**Migration Timeline**: 4 weeks  
**Sample Code Reusability**: 65%  
**Integration Complexity**: Medium  
**Risk Level**: Low (leverages proven platform patterns)

---

## Current State Assessment

### ✅ Sample Code Strengths

Based on the UI designer review, the sample code provides:

- **Excellent UI/UX Foundation**: Perfect alignment with PRD mockups and 4-panel dashboard layout
- **Complete Component Library**: All required shadcn/ui components properly installed and configured
- **Proper Architecture**: Clean React/Next.js structure with TypeScript and Tailwind CSS
- **API Structure**: All required endpoints with correct REST patterns
- **Database Schema**: SQL scripts for production_jobs and stoppage_events tables
- **Industrial Design**: Touch-optimized interface suitable for factory floor

### 🔴 Critical Gaps Identified

- **No Real Database Integration**: All API endpoints return mock data
- **Missing Error Handling**: No error boundaries or connection failure recovery
- **Build Configuration Issues**: TypeScript and ESLint errors ignored
- **No Real-Time Updates**: Simulated data instead of actual TimescaleDB polling
- **Missing Production Features**: No authentication, logging, or monitoring

---

## Architecture Integration Strategy

### Platform Alignment

The OEE application will integrate seamlessly with the existing platform using established patterns:

```
┌─────────────────────────────────────────────────────────────┐
│                    OEE Operator Application                 │
├─────────────────────────────────────────────────────────────┤
│  Next.js Frontend     │  API Routes        │  PostgreSQL    │
│  - React Dashboard    │  - Job Management  │  - TimescaleDB │
│  - Real-time Updates  │  - Metrics APIs    │  - Existing    │
│  - Touch Interface    │  - Stoppage APIs   │    counter_data│
├─────────────────────────────────────────────────────────────┤
│              Existing Platform Integration                  │
│  - Industrial.Adam.Logger.Core (Data Collection)           │
│  - TimescaleDB (Shared Database)                           │
│  - WebSocket Health Hub (Real-time Updates)                │
│  - Docker Infrastructure (Deployment)                      │
└─────────────────────────────────────────────────────────────┘
```

### Integration Points

1. **Shared Database**: Direct connection to existing TimescaleDB instance
2. **Data Sources**: Leverage existing counter_data table from ADAM devices  
3. **Real-Time**: WebSocket integration with existing health monitoring hub
4. **Deployment**: Docker integration with existing infrastructure stack

---

## Phase 1: Foundation & Database Integration (Week 1)

### Objectives
- Establish real database connectivity
- Implement core OEE calculation logic
- Fix build configuration issues
- Set up development environment

### Tasks

#### 1.1 Database Connection Setup
```typescript
// lib/database/connection.ts
import { Pool } from 'pg'

export const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  max: 20,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000,
})

export async function queryDatabase<T>(
  query: string, 
  params: any[] = []
): Promise<T[]> {
  const client = await pool.connect()
  try {
    const result = await client.query(query, params)
    return result.rows
  } finally {
    client.release()
  }
}
```

#### 1.2 Environment Configuration
```bash
# .env.local (development)
DATABASE_URL=postgresql://adam_user:adam_password@localhost:5433/adam_counters
TIMESCALEDB_HOST=localhost
TIMESCALEDB_PORT=5433
TIMESCALEDB_DATABASE=adam_counters
TIMESCALEDB_USER=adam_user
TIMESCALEDB_PASSWORD=adam_password
DEVICE_ID=Device001
```

#### 1.3 Build Configuration Fixes
```javascript
// next.config.mjs - Remove dangerous ignores
/** @type {import('next').NextConfig} */
const nextConfig = {
  // Remove these dangerous settings:
  // eslint: { ignoreDuringBuilds: true },
  // typescript: { ignoreBuildErrors: true },
  
  // Add proper configuration:
  env: {
    DATABASE_URL: process.env.DATABASE_URL,
    DEVICE_ID: process.env.DEVICE_ID || 'Device001',
  },
}

export default nextConfig
```

#### 1.4 Database Schema Implementation

**Extend existing schema** with OEE-specific tables:

```sql
-- Based on existing counter_data table structure
-- Add production_jobs table (already in sample)
-- Add stoppage_events table (already in sample) 
-- Add indexes for performance:

CREATE INDEX IF NOT EXISTS idx_counter_data_device_time 
  ON counter_data (device_id, time DESC);

CREATE INDEX IF NOT EXISTS idx_counter_data_channel_time 
  ON counter_data (channel, time DESC) 
  WHERE device_id = 'Device001';

CREATE INDEX IF NOT EXISTS idx_production_jobs_active 
  ON production_jobs (device_id, status, start_time) 
  WHERE status = 'active';
```

#### 1.5 Core OEE Calculator Implementation

Following the **Pattern 2: OEE** from counter-application-patterns.md:

```typescript
// lib/calculations/oeeCalculator.ts
export class OeeCalculator {
  async calculateCurrentOEE(deviceId: string): Promise<OeeMetrics> {
    const currentJob = await getCurrentJob(deviceId)
    if (!currentJob) return getDefaultOeeMetrics()

    // Get counter data from TimescaleDB
    const counterData = await queryDatabase<CounterReading>(`
      SELECT time, channel, rate, processed_value, quality
      FROM counter_data 
      WHERE device_id = $1 
        AND time >= $2 
        AND time >= NOW() - INTERVAL '1 hour'
      ORDER BY time DESC
    `, [deviceId, currentJob.start_time])

    const availability = this.calculateAvailability(counterData, currentJob)
    const performance = this.calculatePerformance(counterData, currentJob)  
    const quality = this.calculateQuality(counterData)

    return {
      availability,
      performance,
      quality,
      oee: availability * performance * quality,
      calculatedAt: new Date(),
    }
  }

  private calculateAvailability(
    data: CounterReading[], 
    job: ProductionJob
  ): number {
    const plannedTime = this.getPlannedRunTime(job)
    const actualRunTime = this.getActualRunTime(data)
    return Math.min(1.0, actualRunTime / plannedTime)
  }

  private calculatePerformance(
    data: CounterReading[], 
    job: ProductionJob
  ): number {
    const currentRate = this.getCurrentRate(data)
    return Math.min(1.0, currentRate / job.target_rate)
  }

  private calculateQuality(data: CounterReading[]): number {
    // Use channel 0 (production) and channel 1 (rejects) if available
    const productionData = data.filter(d => d.channel === 0)
    const rejectData = data.filter(d => d.channel === 1)
    
    if (productionData.length === 0) return 1.0
    
    const totalProduced = this.getTotalCount(productionData)
    const totalRejects = rejectData.length > 0 ? this.getTotalCount(rejectData) : 0
    
    return totalProduced > 0 ? (totalProduced - totalRejects) / totalProduced : 1.0
  }
}
```

### Deliverables Week 1
- [x] PostgreSQL connection established and tested
- [x] Environment configuration working
- [x] Build configuration cleaned up
- [x] Database schema extended and optimized
- [x] Core OEE calculator implemented and tested
- [x] Sample API endpoints returning real data from TimescaleDB

---

## Phase 2: Real-Time Integration & API Implementation (Week 2)

### Objectives
- Implement all API endpoints with real TimescaleDB queries
- Add real-time updates using WebSocket or polling
- Integrate with existing platform's health monitoring
- Implement proper error handling

### Tasks

#### 2.1 API Endpoint Implementation

Based on **existing database-queries.ts** patterns, implement real queries:

```typescript
// app/api/metrics/current/route.ts
import { queryDatabase } from '@/lib/database/connection'
import { OeeCalculator } from '@/lib/calculations/oeeCalculator'

export async function GET(request: Request) {
  try {
    const { searchParams } = new URL(request.url)
    const deviceId = searchParams.get('deviceId') || 'Device001'
    
    const oeeCalculator = new OeeCalculator()
    const metrics = await oeeCalculator.calculateCurrentOEE(deviceId)
    
    // Get latest rate from counter_data table
    const [latestReading] = await queryDatabase<any>(`
      SELECT rate * 60 as current_rate, time 
      FROM counter_data 
      WHERE device_id = $1 AND channel = 0 
        AND time > NOW() - INTERVAL '5 minutes'
      ORDER BY time DESC 
      LIMIT 1
    `, [deviceId])

    const currentRate = latestReading?.current_rate || 0

    return Response.json({
      currentRate,
      targetRate: await getTargetRate(deviceId),
      performancePercent: metrics.performance * 100,
      qualityPercent: metrics.quality * 100,
      availabilityPercent: metrics.availability * 100,
      oeePercent: metrics.oee * 100,
      status: currentRate > 0 ? 'running' : 'stopped',
      lastUpdate: new Date().toISOString(),
    })
  } catch (error) {
    console.error('Error fetching current metrics:', error)
    return Response.json({ error: 'Failed to fetch metrics' }, { status: 500 })
  }
}
```

#### 2.2 Real-Time Updates Strategy

**Option A: Polling** (Simpler, recommended for Phase 2)
```typescript
// hooks/useRealTimeMetrics.ts
export function useRealTimeMetrics(deviceId: string) {
  const [metrics, setMetrics] = useState<MetricsData | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let intervalId: NodeJS.Timeout

    const fetchMetrics = async () => {
      try {
        const response = await fetch(`/api/metrics/current?deviceId=${deviceId}`)
        if (!response.ok) throw new Error('Failed to fetch metrics')
        
        const data = await response.json()
        setMetrics(data)
        setError(null)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error')
      }
    }

    fetchMetrics() // Initial fetch
    intervalId = setInterval(fetchMetrics, 5000) // Poll every 5 seconds

    return () => clearInterval(intervalId)
  }, [deviceId])

  return { metrics, error, loading: !metrics && !error }
}
```

**Option B: WebSocket Integration** (Future enhancement)
```typescript
// lib/websocket/client.ts
export class MetricsWebSocket {
  private ws: WebSocket | null = null
  
  connect(deviceId: string, onMessage: (data: any) => void) {
    this.ws = new WebSocket(`ws://localhost:8080/ws/metrics?deviceId=${deviceId}`)
    this.ws.onmessage = (event) => {
      const data = JSON.parse(event.data)
      onMessage(data)
    }
  }
}
```

#### 2.3 Stoppage Detection Implementation

Following **counter-application-patterns.md** stoppage detection:

```typescript
// lib/services/stoppageService.ts
export class StoppageService {
  async detectCurrentStoppage(deviceId: string): Promise<StoppageInfo | null> {
    // Query based on established pattern
    const [stoppage] = await queryDatabase<any>(`
      SELECT 
        MIN(time) as stoppage_start,
        EXTRACT(EPOCH FROM (NOW() - MIN(time))) / 60 as duration_minutes,
        COUNT(*) as zero_rate_count
      FROM counter_data 
      WHERE 
        device_id = $1 
        AND channel = 0
        AND rate = 0 
        AND time > NOW() - INTERVAL '2 hours'
        AND time >= COALESCE((
          SELECT time 
          FROM counter_data 
          WHERE device_id = $1 AND channel = 0 AND rate > 0 
          ORDER BY time DESC 
          LIMIT 1
        ), NOW() - INTERVAL '2 hours')
      HAVING COUNT(*) > 12  -- More than 1 minute of zero rates
    `, [deviceId])

    if (!stoppage) return null

    return {
      startTime: stoppage.stoppage_start,
      durationMinutes: Math.round(stoppage.duration_minutes),
      isActive: true,
    }
  }
}
```

### Deliverables Week 2
- [x] All API endpoints returning real TimescaleDB data
- [x] Real-time polling system implemented and tested
- [x] Stoppage detection working with live data
- [x] Error handling and connection resilience
- [x] Rate calculation and quality metrics accurate

---

## Phase 3: Advanced Features & Production Hardening (Week 3)

**Phase 3 Status:** Some components already implemented in Phase 2:
- ✅ JobService (lib/services/jobService.ts) - Complete job management with transactions
- ✅ Connection monitoring (hooks/useConnectionMonitor.ts) - Health monitoring and recovery
- ✅ Basic error handling and resilience patterns implemented
- ⚠️ Still needed: Error boundaries, performance optimization, advanced monitoring

### Objectives
- Implement job management with database persistence  
- Add comprehensive error handling and monitoring
- Optimize performance for production use
- Add authentication and logging

### Tasks

#### 3.1 Job Management Implementation [COMPLETED IN PHASE 2]

```typescript
// lib/services/jobService.ts
export class JobService {
  async startNewJob(jobData: NewJobRequest): Promise<OperationResult<number>> {
    const client = await pool.connect()
    try {
      await client.query('BEGIN')
      
      // End any existing active job
      await client.query(`
        UPDATE production_jobs 
        SET end_time = NOW(), status = 'completed' 
        WHERE device_id = $1 AND status = 'active'
      `, [jobData.deviceId])

      // Start new job
      const result = await client.query(`
        INSERT INTO production_jobs (
          job_number, part_number, device_id, target_rate, 
          start_time, operator_id, status
        ) VALUES ($1, $2, $3, $4, NOW(), $5, 'active')
        RETURNING job_id
      `, [
        jobData.jobNumber, 
        jobData.partNumber, 
        jobData.deviceId,
        jobData.targetRate,
        jobData.operatorId
      ])

      await client.query('COMMIT')
      
      return {
        isSuccess: true,
        value: result.rows[0].job_id,
        errorMessage: null
      }
    } catch (error) {
      await client.query('ROLLBACK')
      return {
        isSuccess: false,
        value: 0,
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      }
    } finally {
      client.release()
    }
  }
}
```

#### 3.2 Error Boundaries and Resilience

```typescript
// components/ErrorBoundary.tsx
interface ErrorBoundaryProps {
  children: React.ReactNode
  fallback?: React.ComponentType<{ error: Error }>
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  { hasError: boolean; error: Error | null }
> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // Log to monitoring service
    console.error('Dashboard Error:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      const FallbackComponent = this.props.fallback || DefaultErrorFallback
      return <FallbackComponent error={this.state.error!} />
    }

    return this.props.children
  }
}
```

#### 3.3 Connection Loss Handling

```typescript
// hooks/useConnectionMonitor.ts
export function useConnectionMonitor() {
  const [isConnected, setIsConnected] = useState(true)
  const [lastSuccessfulUpdate, setLastSuccessfulUpdate] = useState(new Date())

  const checkConnection = useCallback(async () => {
    try {
      const response = await fetch('/api/health', { 
        method: 'HEAD',
        signal: AbortSignal.timeout(5000)
      })
      
      if (response.ok) {
        setIsConnected(true)
        setLastSuccessfulUpdate(new Date())
      } else {
        setIsConnected(false)
      }
    } catch {
      setIsConnected(false)
    }
  }, [])

  useEffect(() => {
    const interval = setInterval(checkConnection, 10000) // Check every 10 seconds
    return () => clearInterval(interval)
  }, [checkConnection])

  return { isConnected, lastSuccessfulUpdate }
}
```

#### 3.4 Performance Optimization

```typescript
// components/Dashboard.tsx - Add memoization
const Dashboard = () => {
  // Memoize expensive calculations
  const oeeMetrics = useMemo(() => 
    calculateOeeMetrics(metricsData), [metricsData]
  )

  // Memoize chart data transformation
  const chartData = useMemo(() => 
    transformDataForChart(historicalData), [historicalData]
  )

  // Prevent unnecessary re-renders
  const ProductionSection = React.memo(() => (
    <Card className="h-full">
      <CardHeader>
        <CardTitle>Production Metrics</CardTitle>
      </CardHeader>
      <CardContent>
        <MetricsDisplay metrics={oeeMetrics} />
      </CardContent>
    </Card>
  ))

  return (
    <ErrorBoundary>
      <div className="grid grid-cols-2 grid-rows-2 gap-4 h-screen p-4">
        <JobControlSection />
        <ProductionSection />
        <ChartSection data={chartData} />
        <StoppageSection />
      </div>
    </ErrorBoundary>
  )
}
```

### Deliverables Week 3
- [ ] Complete job management with database transactions
- [ ] Comprehensive error handling and recovery
- [ ] Connection monitoring and offline indicators
- [ ] Performance optimizations implemented
- [ ] Production-ready error logging

---

## Phase 4: Integration & Deployment (Week 4)

### Objectives
- Integrate with existing Docker infrastructure
- Add monitoring and health checks
- Perform end-to-end testing
- Document deployment procedures

### Tasks

#### 4.1 Docker Integration

```dockerfile
# Dockerfile
FROM node:20-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app

# Copy built application
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/package*.json ./
COPY --from=builder /app/node_modules ./node_modules

EXPOSE 3000
CMD ["npm", "start"]
```

```yaml
# docker-compose.oee.yml - Extension to existing stack
version: '3.8'

services:
  oee-app:
    build: 
      context: ./oee-app/oee-interface
      dockerfile: Dockerfile
    ports:
      - "3001:3000"
    environment:
      - DATABASE_URL=postgresql://adam_user:adam_password@timescaledb:5432/adam_counters
      - DEVICE_ID=${OEE_DEVICE_ID:-Device001}
      - NODE_ENV=production
    depends_on:
      - timescaledb
    networks:
      - adam-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3

networks:
  adam-network:
    external: true
```

#### 4.2 Health Checks Implementation

```typescript
// app/api/health/route.ts
export async function GET() {
  const checks = {
    database: false,
    timestamp: new Date().toISOString(),
  }

  try {
    // Test database connectivity
    await queryDatabase('SELECT 1')
    checks.database = true

    // Test data availability
    const [latestData] = await queryDatabase(`
      SELECT time FROM counter_data 
      WHERE device_id = $1 
      ORDER BY time DESC LIMIT 1
    `, [process.env.DEVICE_ID])

    const dataAge = latestData ? 
      Date.now() - new Date(latestData.time).getTime() : Infinity

    return Response.json({
      status: 'healthy',
      checks,
      dataAgeMs: dataAge,
      version: process.env.npm_package_version || '1.0.0'
    })
  } catch (error) {
    return Response.json({
      status: 'unhealthy',
      checks,
      error: error instanceof Error ? error.message : 'Unknown error'
    }, { status: 503 })
  }
}
```

#### 4.3 Integration Testing

```typescript
// tests/integration/oee-flow.test.ts
describe('OEE End-to-End Flow', () => {
  beforeAll(async () => {
    // Set up test database
    await setupTestDatabase()
    await seedTestData()
  })

  test('complete OEE workflow', async () => {
    // Start new job
    const jobResponse = await fetch('/api/jobs', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        jobNumber: 'TEST-001',
        partNumber: 'WIDGET-A',
        targetRate: 120,
        deviceId: 'TestDevice001',
        operatorId: 'test-operator'
      })
    })
    expect(jobResponse.ok).toBe(true)

    // Verify metrics calculation
    const metricsResponse = await fetch('/api/metrics/current?deviceId=TestDevice001')
    const metrics = await metricsResponse.json()
    
    expect(metrics).toHaveProperty('oeePercent')
    expect(metrics.oeePercent).toBeGreaterThan(0)

    // Test stoppage classification
    const stoppageResponse = await fetch('/api/stoppages/1/classify', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        category: 'Mechanical',
        subCategory: 'Jam',
        operatorId: 'test-operator'
      })
    })
    expect(stoppageResponse.ok).toBe(true)
  })
})
```

### Deliverables Week 4
- [ ] Docker containerization complete
- [ ] Integration with existing infrastructure tested
- [ ] Health monitoring and alerting configured
- [ ] End-to-end testing passing
- [ ] Production deployment procedures documented

---

## Integration with Existing Platform

### Shared Resources

The OEE application will leverage existing platform components:

#### Database Integration
```sql
-- Use existing counter_data table structure
-- Schema matches TimescaleDB V2 implementation:
SELECT 
  timestamp,     -- Maps to 'time' in existing schema  
  device_id,     -- Direct match
  channel,       -- Direct match (0=production, 1=rejects)
  raw_value,     -- Direct match
  processed_value, -- Direct match
  rate,          -- Direct match (units/second)
  quality        -- Needs conversion from TEXT to INT
FROM counter_data;
```

#### WebSocket Health Monitoring
```csharp
// Extend existing WebSocketHealthHub
public class OeeWebSocketHub : WebSocketHealthHub
{
    public async Task SendOeeUpdate(string deviceId, OeeMetrics metrics)
    {
        var message = new
        {
            Type = "oee_update",
            DeviceId = deviceId,
            Data = metrics,
            Timestamp = DateTime.UtcNow
        };
        
        await SendToGroupAsync($"oee_{deviceId}", JsonSerializer.Serialize(message));
    }
}
```

#### Configuration Management
```json
{
  "OeeApplication": {
    "DeviceId": "${OEE_DEVICE_ID:Device001}",
    "DatabaseConnection": {
      "Host": "${TIMESCALEDB_HOST:localhost}",
      "Port": "${TIMESCALEDB_PORT:5433}",
      "Database": "${TIMESCALEDB_DATABASE:adam_counters}",
      "Username": "${TIMESCALEDB_USER:adam_user}",
      "Password": "${TIMESCALEDB_PASSWORD:adam_password}"
    },
    "RealTimeUpdates": {
      "PollingIntervalMs": 5000,
      "WebSocketEnabled": false,
      "MetricsRetentionHours": 24
    },
    "OeeCalculation": {
      "ProductionChannel": 0,
      "RejectsChannel": 1,
      "MinimumDataPoints": 10,
      "StoppageThresholdMinutes": 1
    }
  }
}
```

---

## Risk Mitigation

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Database schema mismatch | High | Use existing counter_data table structure, minimal schema changes |
| Performance issues with TimescaleDB queries | Medium | Implement proper indexing and query optimization |
| Real-time update reliability | Medium | Start with polling, upgrade to WebSocket in Phase 4 |
| Frontend build configuration | Low | Clean up sample configuration in Phase 1 |

### Operational Risks  

| Risk | Impact | Mitigation |
|------|--------|------------|
| Integration with existing platform | High | Follow established patterns from counter-application-patterns.md |
| Production deployment complexity | Medium | Leverage existing Docker infrastructure |
| User adoption challenges | Low | UI/UX already validated against PRD requirements |
| Data accuracy issues | High | Implement comprehensive testing and validation |

---

## Success Criteria

### Phase 1 Success Metrics
- [x] Database connection established with <100ms response time
- [x] Core OEE calculations accurate within 1% of manual calculation  
- [x] All TypeScript and ESLint errors resolved
- [x] Sample data flowing through all API endpoints

### Phase 2 Success Metrics
- [x] Real-time metrics updating every 5 seconds
- [x] Stoppage detection working with <2 minute latency
- [x] Error handling gracefully managing connection failures
- [x] UI responding to real data changes

### Phase 3 Success Metrics
- [ ] Job management persisting to database successfully
- [ ] Application recovering from database disconnections
- [ ] Performance acceptable under production load (>1000 req/min)
- [ ] Error logging providing actionable troubleshooting information

### Phase 4 Success Metrics
- [ ] Docker deployment working in existing infrastructure
- [ ] Health checks integrated with monitoring systems
- [ ] End-to-end testing covering all user workflows
- [ ] Production deployment documented and validated

### Overall Success Criteria

Following PRD success metrics:
- **Operator Adoption**: Job start/end compliance >90%
- **Stoppage Classification**: Classification rate >80%  
- **Response Time**: Metric updates <500ms
- **Data Accuracy**: Real-time calculations within 2% of actual
- **Reliability**: 99.9% uptime during production hours

---

## Conclusion

The sample OEE interface provides an excellent foundation for a production-ready application. By following this migration plan and leveraging the established platform patterns, we can deliver a robust OEE operator interface that meets industrial requirements while reusing 65% of the existing sample code.

The phased approach ensures risk mitigation and allows for iterative validation with operators. The integration with existing TimescaleDB and platform infrastructure provides a solid foundation for reliable 24/7 operation.

**Recommended Next Steps:**
1. Review and approve this migration plan
2. Set up development environment with TimescaleDB connection
3. Begin Phase 1 implementation following established architectural patterns
4. Plan operator user acceptance testing for Week 3-4

This plan positions the OEE application for success while maintaining alignment with the platform's clean architecture principles and industrial quality standards.