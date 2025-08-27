# API Endpoint Analysis & Remediation Plan

## Executive Summary

After comprehensive analysis of the frontend services versus actual backend API endpoints, significant mismatches have been identified. The frontend is calling many **non-existent endpoints**, causing it to fall back to mock/placeholder data instead of displaying real system information.

## Current API Infrastructure

### Running APIs & Ports
- **Logger API**: `http://localhost:5139` - Device monitoring and data collection
- **OEE API**: `http://localhost:5001` - Overall Equipment Effectiveness metrics  
- **Equipment Scheduling API**: `http://localhost:5141` - Resource scheduling and management
- **Frontend**: `http://localhost:3001` - React application

## Critical Issues Found

### 1. Dashboard Service - **ALL ENDPOINTS MISSING**

**Frontend Calls (dashboardService.ts)**:
- ❌ `GET /api/dashboard/overview` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/modules` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/alerts` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/user-activity` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/storage` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/security` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/performance` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/realtime` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/maintenance` - **DOES NOT EXIST**
- ❌ `GET /api/dashboard/all` - **DOES NOT EXIST**

**Result**: Dashboard shows **hardcoded mock data** (24 devices instead of real 3 devices)

### 2. Device Service - **PARTIALLY WORKING**

**Frontend Calls (deviceService.ts)**:
- ❌ `GET /api/devices/summary` - **DOES NOT EXIST**
- ❌ `GET /api/devices/realtime` - **DOES NOT EXIST**
- ❌ `GET /api/devices/{id}/health` - **DOES NOT EXIST**
- ❌ `GET /api/devices/{id}/configuration` - **DOES NOT EXIST**
- ❌ `POST /api/devices/{id}/test` - **DOES NOT EXIST**

**Actual Logger API Endpoints**:
- ✅ `GET /devices` - Get all devices status
- ✅ `GET /devices/{deviceId}` - Get specific device status
- ✅ `GET /data/latest` - Get latest readings from all devices
- ✅ `GET /data/latest/{deviceId}` - Get latest readings for device
- ✅ `GET /data/stats` - Get data collection statistics
- ✅ `POST /devices/{deviceId}/restart` - Restart device connection

### 3. OEE Service - **PARTIALLY WORKING**

**Frontend Calls (oeeService.ts)**:
- ❌ `GET /api/oee/overview` - **DOES NOT EXIST**
- ? `GET /api/oee/current` - **Format mismatch**
- ? `GET /api/oee/equipment/{id}` - **Format mismatch**

**Actual OEE API Endpoints**:
- ✅ `GET /api/oee/current?deviceId={id}` - Get current OEE (requires deviceId parameter)
- ✅ `GET /api/oee/historical?deviceId={id}` - Get historical OEE data
- ✅ `GET /api/jobs` - Get work orders/jobs
- ✅ `GET /api/stoppages` - Get stoppage events

### 4. Authentication Service - **WORKING BUT INCORRECT ENDPOINT**

**Frontend Calls (authService.ts)**:
- ❌ `POST /api/auth/verify` - **DOES NOT EXIST**

**Actual Logger API Endpoints**:
- ✅ `POST /auth/login` - User login
- ✅ `POST /auth/refresh` - Token refresh
- ✅ `POST /auth/logout` - Logout and revoke token

### 5. User & Hierarchy Services - **NO BACKEND ENDPOINTS**

**Frontend expects but backend missing**:
- ❌ User management CRUD operations
- ❌ ISA-95 hierarchy management endpoints  
- ❌ Role assignment functionality

## Root Cause Analysis

1. **Architectural Mismatch**: Frontend designed for comprehensive REST APIs, but backend uses minimal APIs for specific functions
2. **Missing Aggregation Layer**: No backend service aggregates data from multiple sources for dashboard consumption
3. **Endpoint Naming Inconsistency**: Frontend expects `/api/*` but Logger API uses root paths like `/devices`, `/data`
4. **Parameter Format Differences**: OEE endpoints expect query parameters but frontend sends path parameters
5. **Missing Business Logic APIs**: No user management, hierarchy, or dashboard aggregation services

## Detailed Remediation Plan

### Phase 1: Immediate Fixes (Dashboard Mock Data)
**Priority**: Critical
**Timeline**: 1-2 days

#### Fix Dashboard Device Count
- ✅ **COMPLETED**: Updated Dashboard.tsx to call `deviceService.getDeviceSummary()`
- ✅ **COMPLETED**: Replaced hardcoded "24 devices" with real device count (3)

#### Map Frontend Device Service to Logger API
```typescript
// CURRENT (Wrong)
GET /api/devices/summary

// FIX TO (Logger API Port 5139)
GET http://localhost:5139/data/stats
```

### Phase 2: API Endpoint Mapping (1-2 weeks)
**Priority**: High

#### 2.1 Update API Client Base URLs
```typescript
// Current: All services use same base URL
// Fix: Configure service-specific base URLs
const LOGGER_API = 'http://localhost:5139'
const OEE_API = 'http://localhost:5001'
const SCHEDULING_API = 'http://localhost:5141'
```

#### 2.2 Map Device Service Endpoints
| Frontend Call | Current Endpoint | Fix to Logger API | Status |
|---------------|------------------|-------------------|---------|
| `getDevices()` | `/api/devices` | `GET /devices` | ❌ Map |
| `getDeviceSummary()` | `/api/devices/summary` | `GET /data/stats` | ❌ Map |
| `getRealTimeReadings()` | `/api/devices/realtime` | `GET /data/latest` | ❌ Map |
| `getDeviceHealth()` | `/api/devices/{id}/health` | `GET /devices/{id}` | ❌ Map |

#### 2.3 Map OEE Service Endpoints  
| Frontend Call | Current Endpoint | Fix to OEE API | Status |
|---------------|------------------|----------------|---------|
| `getCurrentOEE()` | `GET /api/oee/current` | `GET /api/oee/current?deviceId={id}` | ❌ Fix params |
| `getOEEOverview()` | `GET /api/oee/overview` | Custom aggregation needed | ❌ Build |

#### 2.4 Fix Authentication Endpoints
| Frontend Call | Current Endpoint | Fix to Logger API | Status |
|---------------|------------------|------------------|---------|
| `login()` | `POST /api/auth/login` | `POST /auth/login` | ❌ Map |
| `refresh()` | `POST /api/auth/refresh` | `POST /auth/refresh` | ❌ Map |
| `verify()` | `POST /api/auth/verify` | Remove (not needed) | ❌ Remove |

### Phase 3: Missing Backend Services (2-3 weeks)
**Priority**: Medium-High

#### 3.1 Dashboard Aggregation Service
**Create new controller**: `DashboardController.cs` in appropriate API

```csharp
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    // Aggregate data from Logger, OEE, and other services
    [HttpGet("overview")]
    public async Task<ActionResult> GetSystemOverview()
    {
        // Call Logger API for device data
        // Call OEE API for production metrics  
        // Aggregate into dashboard format
    }
}
```

#### 3.2 User Management Service  
**Options**:
1. Extend existing Industrial.Adam.Security module
2. Create new UserManagement API service  
3. Add endpoints to existing APIs

**Required Endpoints**:
```
GET /api/users - List users
POST /api/users - Create user
PUT /api/users/{id} - Update user  
DELETE /api/users/{id} - Delete user
PUT /api/users/{id}/roles - Assign roles
```

#### 3.3 ISA-95 Hierarchy Service
**Create new controller** or extend Equipment Scheduling API:

```
GET /api/hierarchy - Get hierarchy tree
POST /api/hierarchy/nodes - Create hierarchy node
PUT /api/hierarchy/nodes/{id} - Update node
DELETE /api/hierarchy/nodes/{id} - Delete node
```

### Phase 4: Advanced Integration (3-4 weeks)  
**Priority**: Medium

#### 4.1 Real-time Data Hub
- Implement SignalR hubs for live device data updates
- Connect frontend WebSocket clients to real device readings

#### 4.2 Cross-API Data Aggregation
- Build service layer that combines data from multiple APIs
- Implement caching for dashboard performance
- Add proper error handling and fallbacks

#### 4.3 Frontend Service Architecture Refactor
- Split services by API domain (Logger, OEE, Scheduling)
- Implement proper TypeScript interfaces for all responses
- Add comprehensive error handling

## Implementation Strategy

### Quick Wins (This Week)
1. ✅ **DONE**: Fix dashboard device count mock data
2. Update deviceService to call Logger API endpoints directly
3. Fix authentication endpoint paths
4. Map basic OEE service calls

### Medium Term (Next 2 Weeks)  
1. Create dashboard aggregation endpoints
2. Map all device monitoring functionality
3. Implement missing OEE overview endpoints
4. Add proper error handling throughout

### Long Term (Next Month)
1. Build complete user management system
2. Implement ISA-95 hierarchy management
3. Add real-time SignalR integration
4. Performance optimization and caching

## Testing Strategy

### API Testing
1. Test each mapped endpoint with Postman/curl
2. Verify response formats match frontend expectations
3. Test authentication flows
4. Load test aggregation endpoints

### Frontend Integration Testing  
1. Test all dashboard widgets with real data
2. Verify user management workflows
3. Test device monitoring real-time updates
4. End-to-end authentication testing

### Data Accuracy Verification
1. Compare frontend displays with direct API responses
2. Verify device counts match Logger API reality
3. Test OEE calculations against expected values
4. Validate hierarchy displays match backend structure

## Risk Mitigation

### Fallback Strategies
1. Keep mock data as fallback for API failures
2. Implement timeout handling for slow API responses  
3. Add circuit breaker pattern for unstable services
4. Cache frequently accessed data

### Monitoring & Alerting
1. Add API endpoint monitoring
2. Track frontend error rates
3. Monitor dashboard load performance  
4. Alert on API integration failures

---

**Next Steps**: Begin Phase 1 immediate fixes, starting with deviceService endpoint mapping to Logger API.