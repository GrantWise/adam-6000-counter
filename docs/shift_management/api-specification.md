# API Specification - Equipment Scheduling System

## 1. API Overview

### Purpose
Provide RESTful API endpoints for equipment availability scheduling, enabling integration with OEE systems and supporting both Phase 1 (equipment-only) and Phase 2 (employee scheduling) functionality.

### Design Principles
- **RESTful Architecture**: Standard HTTP methods and status codes
- **Version Stability**: v1 endpoints never break, v2 extends functionality
- **Backward Compatibility**: New fields are optional/nullable
- **Consistent Structure**: Uniform request/response patterns
- **Performance First**: Sub-200ms response time target

### Base URLs
```
Production:  https://api.equipmentscheduler.com
Staging:     https://staging-api.equipmentscheduler.com
Development: https://dev-api.equipmentscheduler.com
```

### Versioning Strategy
```
URL Path Versioning:
/api/v1/  - Phase 1 endpoints (stable forever)
/api/v2/  - Phase 2 endpoints (additive only)

Version Selection:
- Default: Latest stable version
- Explicit: /api/v1/ for version lock
- Header: Accept-Version: v1 (optional)
```

## 2. Authentication & Authorization

### Authentication Methods

#### Method 1: API Key (System Integration)
```http
GET /api/v1/equipment
Headers:
  X-API-Key: sk_live_a1b2c3d4e5f6g7h8i9j0
```

#### Method 2: OAuth 2.0 (User Access)
```http
POST /api/v1/auth/token
Content-Type: application/json

{
  "grant_type": "client_credentials",
  "client_id": "your_client_id",
  "client_secret": "your_client_secret"
}

Response:
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

#### Method 3: JWT Bearer Token
```http
GET /api/v1/equipment
Headers:
  Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Authorization Roles

| Role | Equipment Read | Equipment Write | Pattern Manage | Schedule Generate | API Access |
|------|---------------|-----------------|----------------|-------------------|------------|
| Viewer | ✓ | ✗ | ✗ | ✗ | ✗ |
| Operator | ✓ | ✓ | ✗ | ✓ | ✗ |
| Engineer | ✓ | ✓ | ✓ | ✓ | ✗ |
| Admin | ✓ | ✓ | ✓ | ✓ | ✓ |
| System | ✓ | ✗ | ✗ | ✗ | ✓ |

## 3. Common Standards

### Request Headers
```http
Content-Type: application/json
Accept: application/json
X-Request-ID: uuid-v4 (optional, for tracing)
X-API-Key: your_api_key (for API auth)
Authorization: Bearer token (for OAuth)
```

### Response Structure

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
    "request_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

#### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid pattern assignment",
    "details": [
      {
        "field": "pattern_id",
        "issue": "Pattern not found",
        "value": "999"
      }
    ]
  },
  "meta": {
    "timestamp": "2025-01-20T10:00:00Z",
    "version": "v1",
    "request_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

#### Paginated Response
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "page_size": 50,
    "total_pages": 10,
    "total_items": 487,
    "has_next": true,
    "has_previous": false,
    "links": {
      "first": "/api/v1/equipment?page=1",
      "last": "/api/v1/equipment?page=10",
      "next": "/api/v1/equipment?page=2",
      "previous": null
    }
  },
  "meta": {...}
}
```

### Standard HTTP Status Codes
```
200 OK              - Successful GET, PUT
201 Created         - Successful POST
204 No Content      - Successful DELETE
400 Bad Request     - Invalid request format
401 Unauthorized    - Missing/invalid authentication
403 Forbidden       - Valid auth, insufficient permissions
404 Not Found       - Resource doesn't exist
409 Conflict        - Resource conflict (duplicate, etc.)
422 Unprocessable   - Validation errors
429 Too Many        - Rate limit exceeded
500 Server Error    - Internal server error
503 Unavailable     - Service temporarily unavailable
```

### Error Codes
```
AUTHENTICATION_REQUIRED    - No auth credentials provided
AUTHENTICATION_INVALID     - Invalid credentials
AUTHORIZATION_FAILED       - Insufficient permissions
VALIDATION_ERROR          - Request validation failed
RESOURCE_NOT_FOUND        - Requested resource doesn't exist
RESOURCE_CONFLICT         - Duplicate or conflicting resource
RATE_LIMIT_EXCEEDED       - Too many requests
SERVER_ERROR              - Internal processing error
SERVICE_UNAVAILABLE       - Temporary outage
```

## 4. Phase 1 API Endpoints

### Equipment Management

#### List Equipment
```http
GET /api/v1/equipment

Query Parameters:
  page           - Page number (default: 1)
  page_size      - Items per page (default: 50, max: 200)
  search         - Search term for name/code
  parent_id      - Filter by parent resource
  level          - Filter by ISA-95 level (E,S,A,WC,WU)
  has_schedule   - Filter by scheduling flag (true/false)
  sort           - Sort field (name, code, created_date)
  order          - Sort order (asc, desc)

Response:
{
  "success": true,
  "data": [
    {
      "id": 123,
      "name": "Production Line A",
      "code": "PL-A-001",
      "type": "WorkCenter",
      "parent_id": 45,
      "hierarchy_path": "Plant.Area1.LineA",
      "requires_scheduling": true,
      "has_pattern": true,
      "pattern_name": "Two-Shift",
      "is_active": true,
      "created_date": "2025-01-15T10:00:00Z",
      "modified_date": "2025-01-15T10:00:00Z"
    }
  ],
  "pagination": {...}
}
```

#### Get Equipment Details
```http
GET /api/v1/equipment/{id}

Response:
{
  "success": true,
  "data": {
    "id": 123,
    "name": "Production Line A",
    "code": "PL-A-001",
    "type": "WorkCenter",
    "parent_id": 45,
    "parent_name": "Production Area 1",
    "hierarchy_path": "Plant.Area1.LineA",
    "requires_scheduling": true,
    "pattern": {
      "id": 2,
      "name": "Two-Shift",
      "type": "Simple",
      "inherited_from": 45,
      "is_override": false
    },
    "children_count": 5,
    "metadata": {
      "location": "Building A",
      "capacity": "100 units/hour"
    },
    "is_active": true,
    "created_date": "2025-01-15T10:00:00Z",
    "modified_date": "2025-01-15T10:00:00Z"
  }
}
```

#### Create Equipment
```http
POST /api/v1/equipment
Content-Type: application/json

{
  "name": "New Equipment",
  "code": "EQ-NEW-001",
  "type": "WorkUnit",
  "parent_id": 123,
  "requires_scheduling": true,
  "metadata": {
    "location": "Building B"
  }
}

Response: 201 Created
{
  "success": true,
  "data": {
    "id": 456,
    // Full equipment object
  }
}
```

#### Update Equipment
```http
PUT /api/v1/equipment/{id}
Content-Type: application/json

{
  "name": "Updated Name",
  "requires_scheduling": false,
  "metadata": {
    "capacity": "150 units/hour"
  }
}

Response: 200 OK
```

#### Delete Equipment
```http
DELETE /api/v1/equipment/{id}

Response: 204 No Content
```

#### Bulk Import Equipment
```http
POST /api/v1/equipment/import
Content-Type: multipart/form-data

file: equipment.csv
validate_only: false (optional, true for preview)

Response:
{
  "success": true,
  "data": {
    "total_rows": 847,
    "imported": 843,
    "failed": 4,
    "errors": [
      {
        "row": 45,
        "error": "Duplicate code: PL-A-001"
      }
    ]
  }
}
```

### Pattern Management

#### List Patterns
```http
GET /api/v1/patterns

Query Parameters:
  complexity     - Filter by complexity (Simple, Advanced, Expert)
  type          - Filter by type (Continuous, Shift, DayOnly)
  visible       - Include hidden patterns (true/false)

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "24/7 Continuous",
      "code": "24_7",
      "complexity": "Simple",
      "type": "Continuous",
      "cycle_days": 7,
      "weekly_hours": 168,
      "description": "Equipment runs continuously",
      "configuration": {
        "monday": {"start": "00:00", "end": "24:00"},
        "tuesday": {"start": "00:00", "end": "24:00"},
        // ... all days
      },
      "is_visible": true,
      "usage_count": 234
    }
  ]
}
```

#### Get Pattern Details
```http
GET /api/v1/patterns/{id}

Response:
{
  "success": true,
  "data": {
    "id": 2,
    "name": "Two-Shift",
    "code": "TWO_SHIFT",
    "complexity": "Simple",
    "type": "Shift",
    "cycle_days": 7,
    "weekly_hours": 80,
    "description": "Two 8-hour shifts, Monday-Friday",
    "configuration": {
      "monday": {
        "shifts": [
          {"start": "06:00", "end": "14:00", "code": "D"},
          {"start": "14:00", "end": "22:00", "code": "A"}
        ]
      },
      // ... other days
    },
    "is_visible": true,
    "is_custom": false,
    "created_by": "System",
    "created_date": "2025-01-01T00:00:00Z"
  }
}
```

#### Create Custom Pattern
```http
POST /api/v1/patterns
Content-Type: application/json

{
  "name": "Weekend Only",
  "code": "WEEKEND",
  "type": "Custom",
  "description": "Operations on weekends only",
  "configuration": {
    "saturday": {"start": "08:00", "end": "16:00"},
    "sunday": {"start": "08:00", "end": "16:00"}
  }
}

Response: 201 Created
```

### Pattern Assignment

#### Get Equipment Pattern
```http
GET /api/v1/equipment/{id}/pattern

Response:
{
  "success": true,
  "data": {
    "equipment_id": 123,
    "pattern": {
      "id": 2,
      "name": "Two-Shift",
      "inherited_from": 45,
      "inherited_from_name": "Production Area",
      "is_override": false,
      "effective_date": "2025-01-01",
      "assigned_by": "john.doe",
      "assigned_date": "2025-01-01T10:00:00Z"
    }
  }
}
```

#### Assign Pattern to Equipment
```http
POST /api/v1/equipment/{id}/pattern
Content-Type: application/json

{
  "pattern_id": 2,
  "effective_date": "2025-02-01",
  "apply_to_children": true,
  "notes": "Switching to two-shift operation"
}

Response:
{
  "success": true,
  "data": {
    "assigned_to": 123,
    "pattern_id": 2,
    "affected_equipment": 47,
    "effective_date": "2025-02-01"
  }
}
```

#### Remove Pattern Assignment
```http
DELETE /api/v1/equipment/{id}/pattern

Response: 204 No Content
```

#### Bulk Pattern Assignment
```http
POST /api/v1/patterns/bulk-assign
Content-Type: application/json

{
  "pattern_id": 2,
  "equipment_ids": [123, 124, 125],
  "effective_date": "2025-02-01",
  "override_existing": false
}

Response:
{
  "success": true,
  "data": {
    "pattern_id": 2,
    "assigned_count": 3,
    "skipped_count": 0,
    "affected_total": 15  // Including children
  }
}
```

### Schedule Generation

#### Generate Schedules
```http
POST /api/v1/schedules/generate
Content-Type: application/json

{
  "start_date": "2025-02-01",
  "end_date": "2025-12-31",
  "equipment_ids": null,  // null for all, or array of IDs
  "regenerate": false,    // true to overwrite existing
  "apply_holidays": true
}

Response:
{
  "success": true,
  "data": {
    "generated_count": 254320,
    "equipment_count": 847,
    "date_range": {
      "start": "2025-02-01",
      "end": "2025-12-31"
    },
    "generation_time_ms": 4523
  }
}
```

#### Query Schedules
```http
GET /api/v1/schedules

Query Parameters:
  equipment_id   - Filter by equipment
  start_date     - Start of date range
  end_date       - End of date range
  status         - Filter by status (Operating, Maintenance, Holiday)
  has_exception  - Filter exceptions (true/false)

Response:
{
  "success": true,
  "data": [
    {
      "id": 789,
      "equipment_id": 123,
      "equipment_name": "Line A",
      "date": "2025-02-01",
      "shift_code": "D",
      "planned_start": "2025-02-01T06:00:00Z",
      "planned_end": "2025-02-01T14:00:00Z",
      "planned_hours": 8.0,
      "status": "Operating",
      "pattern_id": 2,
      "is_exception": false
    }
  ]
}
```

### OEE Integration

#### Get Equipment Availability
```http
GET /api/v1/oee/availability

Query Parameters:
  equipment_ids  - Comma-separated equipment IDs
  start_date     - Start date (required)
  end_date       - End date (required)
  include_shifts - Include shift details (true/false)
  format         - Response format (json, csv)

Response:
{
  "success": true,
  "data": {
    "period": {
      "start": "2025-02-01",
      "end": "2025-02-28"
    },
    "equipment": [
      {
        "equipment_id": 123,
        "equipment_name": "Line A",
        "total_hours": 352,
        "available_hours": 320,
        "maintenance_hours": 32,
        "availability": [
          {
            "date": "2025-02-01",
            "planned_hours": 16,
            "shifts": ["D", "A"],
            "status": "Operating"
          }
        ]
      }
    ]
  }
}
```

#### Get Recent Changes
```http
GET /api/v1/oee/changes

Query Parameters:
  since          - Timestamp for changes since
  equipment_ids  - Filter by equipment
  change_types   - Filter by change type

Response:
{
  "success": true,
  "data": [
    {
      "change_id": 456,
      "timestamp": "2025-01-20T14:30:00Z",
      "type": "schedule_updated",
      "equipment_id": 123,
      "changes": {
        "date": "2025-02-01",
        "old_status": "Operating",
        "new_status": "Maintenance"
      }
    }
  ]
}
```

#### Register Webhook
```http
POST /api/v1/oee/webhooks
Content-Type: application/json

{
  "url": "https://your-oee-system.com/webhook",
  "events": ["schedule_created", "schedule_updated", "schedule_deleted"],
  "equipment_ids": null,  // null for all
  "secret": "webhook_secret_key"
}

Response:
{
  "success": true,
  "data": {
    "webhook_id": "wh_123",
    "url": "https://your-oee-system.com/webhook",
    "events": ["schedule_created", "schedule_updated", "schedule_deleted"],
    "status": "active",
    "created_date": "2025-01-20T10:00:00Z"
  }
}
```

### Calendar Management

#### List Holiday Calendars
```http
GET /api/v1/calendars

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "South Africa Public Holidays",
      "country": "ZA",
      "region": null,
      "holiday_count": 12,
      "is_default": true
    }
  ]
}
```

#### Get Calendar Holidays
```http
GET /api/v1/calendars/{id}/holidays

Query Parameters:
  year           - Filter by year

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "New Year's Day",
      "date": "2025-01-01",
      "affects_scheduling": true,
      "is_recurring": true
    }
  ]
}
```

### Exception Management

#### Create Exception
```http
POST /api/v1/exceptions
Content-Type: application/json

{
  "equipment_id": 123,
  "type": "Maintenance",
  "start_datetime": "2025-02-01T10:00:00Z",
  "end_datetime": "2025-02-01T14:00:00Z",
  "impact": "NoOperation",
  "description": "Planned maintenance",
  "recurrence": null  // or RRULE string
}

Response: 201 Created
```

#### List Exceptions
```http
GET /api/v1/exceptions

Query Parameters:
  equipment_id   - Filter by equipment
  type          - Filter by type
  active        - Active exceptions only
  date_range    - Date range filter

Response:
{
  "success": true,
  "data": [
    {
      "id": 789,
      "equipment_id": 123,
      "type": "Maintenance",
      "start_datetime": "2025-02-01T10:00:00Z",
      "end_datetime": "2025-02-01T14:00:00Z",
      "impact": "NoOperation",
      "description": "Planned maintenance",
      "is_recurring": false,
      "created_by": "john.doe",
      "created_date": "2025-01-20T10:00:00Z"
    }
  ]
}
```

## 5. Phase 2 API Endpoints (Future)

### Employee Management (v2)

#### List Employees
```http
GET /api/v2/employees

Additional fields in response:
- skills: Array of skill certifications
- team_id: Current team assignment
- schedule_pattern: Assigned pattern
```

#### Get Employee Schedule
```http
GET /api/v2/employees/{id}/schedule

Response includes:
- Assigned equipment for each shift
- Team rotation information
- Coverage requirements
```

### Enhanced Equipment API (v2)

#### Get Equipment Availability with Operators
```http
GET /api/v2/equipment/{id}/availability

Response (backward compatible):
{
  "equipment_id": 123,
  "availability": [...],
  "operators": [  // New in v2
    {
      "date": "2025-02-01",
      "shift": "D",
      "assigned": ["John Doe", "Jane Smith"],
      "required": 2,
      "coverage": "Full"
    }
  ]
}
```

### Coverage Analysis (v2)

#### Get Coverage Gaps
```http
GET /api/v2/coverage/gaps

Response:
{
  "gaps": [
    {
      "equipment_id": 123,
      "date": "2025-02-01",
      "shift": "N",
      "required": 2,
      "assigned": 1,
      "gap": 1
    }
  ]
}
```

## 6. Webhook Events

### Event Types
```
schedule.created      - New schedule generated
schedule.updated      - Schedule modified
schedule.deleted      - Schedule removed
exception.created     - Exception added
exception.resolved    - Exception cleared
pattern.assigned      - Pattern assigned to equipment
pattern.removed       - Pattern removed from equipment
```

### Webhook Payload
```json
{
  "event": "schedule.updated",
  "timestamp": "2025-01-20T14:30:00Z",
  "data": {
    "equipment_id": 123,
    "date": "2025-02-01",
    "old_value": {
      "status": "Operating",
      "hours": 8
    },
    "new_value": {
      "status": "Maintenance",
      "hours": 0
    }
  },
  "signature": "sha256=abcdef..."  // HMAC signature
}
```

## 7. Rate Limiting

### Limits by Authentication Type
```
API Key:        1000 requests/hour
OAuth Token:    500 requests/hour
Unauthenticated: 60 requests/hour
```

### Rate Limit Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1611234567
```

### Rate Limit Exceeded Response
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "API rate limit exceeded",
    "retry_after": 3600
  }
}
```

## 8. Testing & Sandbox

### Sandbox Environment
```
Base URL: https://sandbox-api.equipmentscheduler.com
Test API Key: sk_test_sandbox_key
Rate Limits: 10x production limits
Data Reset: Daily at 00:00 UTC
```

### Test Equipment IDs
```
Always Available:
- Equipment ID 1: Always has Two-Shift pattern
- Equipment ID 2: Always has 24/7 pattern
- Equipment ID 3: No pattern assigned
- Equipment ID 999: Always returns 404
```

### Health Check Endpoint
```http
GET /api/v1/health

Response:
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2025-01-20T10:00:00Z",
  "services": {
    "database": "healthy",
    "cache": "healthy",
    "queue": "healthy"
  }
}
```

## 9. SDK Examples

### JavaScript/Node.js
```javascript
const EquipmentScheduler = require('@equipment-scheduler/sdk');

const client = new EquipmentScheduler({
  apiKey: 'sk_live_your_api_key',
  version: 'v1'
});

// Get equipment availability
const availability = await client.oee.getAvailability({
  equipment_ids: [123, 124],
  start_date: '2025-02-01',
  end_date: '2025-02-28'
});
```

### Python
```python
from equipment_scheduler import Client

client = Client(api_key='sk_live_your_api_key')

# Generate schedules
result = client.schedules.generate(
    start_date='2025-02-01',
    end_date='2025-12-31'
)
```

### cURL
```bash
curl -X GET "https://api.equipmentscheduler.com/api/v1/equipment" \
  -H "X-API-Key: sk_live_your_api_key" \
  -H "Accept: application/json"
```

This API specification provides comprehensive documentation for integrating with the Equipment Scheduling System, supporting both current Phase 1 functionality and future Phase 2 extensions.