# OEE API Reference

This document provides comprehensive API reference documentation for the Industrial ADAM OEE service. The API follows RESTful principles and returns JSON responses with standardized error handling.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Base URL and Versioning](#base-url-and-versioning)
- [Response Format](#response-format)
- [Error Handling](#error-handling)
- [Health Endpoints](#health-endpoints)
- [OEE Endpoints](#oee-endpoints)
- [Work Order Management](#work-order-management)
- [Equipment Line Management](#equipment-line-management)
- [Stoppage Management](#stoppage-management)
- [Reason Code Management](#reason-code-management)
- [Data Models](#data-models)
- [Examples](#examples)

## Overview

The OEE API provides endpoints for:
- **OEE Calculations**: Real-time and historical Overall Equipment Effectiveness metrics
- **Work Order Management**: Production job lifecycle management with enhanced validation
- **Equipment Line Management**: ADAM device to production line mapping and configuration
- **Stoppage Detection**: Equipment downtime monitoring and analysis
- **Reason Code Management**: 2-level classification system for stoppages and issues
- **Health Monitoring**: Service and dependency status checks

### API Characteristics

- **Protocol**: HTTP/HTTPS
- **Data Format**: JSON
- **Architecture**: CQRS with MediatR
- **Validation**: FluentValidation with detailed error responses
- **Documentation**: OpenAPI/Swagger (development environments only)

## Authentication

Currently, the API does not implement authentication. This will be added in future versions for production deployments.

## Base URL and Versioning

```
Base URL: http://localhost:5001
API Base: http://localhost:5001/api
```

The API currently does not use versioning. Future versions will implement API versioning through URL paths (e.g., `/api/v1/`).

## Response Format

### Success Responses

All successful responses return JSON with appropriate HTTP status codes:

```json
{
  "data": { /* response payload */ },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error Responses

Error responses follow RFC 7807 Problem Details format:

```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "string",
  "instance": "string"
}
```

## Error Handling

### HTTP Status Codes

| Code | Description | Usage |
|------|-------------|-------|
| 200 | OK | Successful GET requests |
| 201 | Created | Successful POST requests |
| 204 | No Content | Successful PUT/DELETE requests or empty results |
| 400 | Bad Request | Invalid request parameters or validation errors |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Business rule violation or resource conflict |
| 500 | Internal Server Error | Unexpected server errors |
| 501 | Not Implemented | Feature not yet implemented |

### Common Error Examples

#### Validation Error (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The DeviceId field is required.",
  "instance": "/api/oee/current",
  "errors": {
    "DeviceId": ["The DeviceId field is required."]
  }
}
```

#### Business Logic Error (409)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Work Order Conflict",
  "status": 409,
  "detail": "Device ADAM-6051-01 already has an active work order",
  "instance": "/api/jobs"
}
```

## Health Endpoints

### Basic Health Check

```http
GET /health
```

**Description**: Basic health status of the service.

**Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "OEE API",
  "version": "1.0.0"
}
```

### Detailed Health Check

```http
GET /api/health/detailed
```

**Description**: Comprehensive health status including dependencies.

**Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "OEE API",
  "version": "1.0.0",
  "environment": "Development",
  "dependencies": {
    "database": "Healthy",
    "timescaleDB": "Healthy"
  },
  "uptime": 3600
}
```

## OEE Endpoints

### Get Current OEE Metrics

```http
GET /api/oee/current?deviceId={deviceId}&startTime={startTime}&endTime={endTime}
```

**Description**: Calculate current OEE metrics for a specific device.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `deviceId` | string | Yes | Device/resource identifier |
| `startTime` | datetime | No | Start time for calculation period (ISO 8601) |
| `endTime` | datetime | No | End time for calculation period (ISO 8601) |

**Response** (200):
```json
{
  "oeeId": "oee_ADAM-6051-01_20240115_103000",
  "resourceReference": "ADAM-6051-01",
  "calculationPeriodStart": "2024-01-15T09:30:00Z",
  "calculationPeriodEnd": "2024-01-15T10:30:00Z",
  "availabilityPercentage": 95.5,
  "performancePercentage": 87.2,
  "qualityPercentage": 98.1,
  "oeePercentage": 81.7,
  "periodHours": 1.0,
  "worstFactor": "Performance",
  "classification": "Good",
  "requiresAttention": false,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses**:
- `400`: Invalid deviceId or time parameters
- `404`: Device not found or no active work order
- `500`: Internal server error

### Get OEE History

```http
GET /api/oee/history?deviceId={deviceId}&period={period}&startTime={startTime}&endTime={endTime}&intervalMinutes={intervalMinutes}
```

**Description**: Retrieve historical OEE data for a device over a time period.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `deviceId` | string | Yes | - | Device/resource identifier |
| `period` | int | No | 24 | Number of hours to look back (1-8760) |
| `startTime` | datetime | No | - | Custom start time (overrides period) |
| `endTime` | datetime | No | - | Custom end time (overrides period) |
| `intervalMinutes` | int | No | 60 | Data aggregation interval (1-1440) |

**Response** (200):
```json
[
  {
    "oeeId": "oee_ADAM-6051-01_20240115_093000",
    "resourceReference": "ADAM-6051-01",
    "calculationPeriodStart": "2024-01-15T08:30:00Z",
    "calculationPeriodEnd": "2024-01-15T09:30:00Z",
    "availabilityPercentage": 92.3,
    "performancePercentage": 89.1,
    "qualityPercentage": 97.8,
    "oeePercentage": 80.7,
    "periodHours": 1.0,
    "worstFactor": "Availability",
    "classification": "Good",
    "requiresAttention": false,
    "createdAt": "2024-01-15T09:30:00Z"
  }
]
```

### Get OEE Breakdown

```http
GET /api/oee/breakdown?deviceId={deviceId}&startTime={startTime}&endTime={endTime}
```

**Description**: Get detailed OEE breakdown showing availability, performance, and quality factors.

**Parameters**: Same as current OEE endpoint.

**Response** (200): Same format as current OEE metrics with detailed factor analysis.

## Work Order Management

### Get Active Work Order

```http
GET /api/jobs/active?deviceId={deviceId}
```

**Description**: Retrieve the active work order for a specific device.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `deviceId` | string | Yes | Device/resource identifier |

**Response** (200):
```json
{
  "workOrderId": "WO-2024-001",
  "workOrderDescription": "Production run for Widget A",
  "productId": "WIDGET-A-001",
  "productDescription": "Standard Widget Type A",
  "plannedQuantity": 1000.0,
  "unitOfMeasure": "pieces",
  "scheduledStartTime": "2024-01-15T08:00:00Z",
  "scheduledEndTime": "2024-01-15T16:00:00Z",
  "resourceReference": "ADAM-6051-01",
  "status": "InProgress",
  "actualQuantityGood": 450.0,
  "actualQuantityScrap": 15.0,
  "totalQuantityProduced": 465.0,
  "actualStartTime": "2024-01-15T08:15:00Z",
  "actualEndTime": null,
  "completionPercentage": 46.5,
  "yieldPercentage": 96.8,
  "productionRate": 77.5,
  "isBehindSchedule": false,
  "requiresAttention": false,
  "estimatedCompletionTime": "2024-01-15T15:45:00Z",
  "createdAt": "2024-01-15T07:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses**:
- `400`: Invalid deviceId
- `404`: No active work order found
- `500`: Internal server error

### Get Work Order Details

```http
GET /api/jobs/{id}
```

**Description**: Retrieve specific work order details by ID.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Work order identifier |

**Response** (200): Same format as active work order response.

### Get Work Order Progress

```http
GET /api/jobs/{id}/progress
```

**Description**: Get progress information for a specific work order.

**Response** (200):
```json
{
  "workOrderId": "WO-2024-001",
  "productDescription": "Standard Widget Type A",
  "status": "InProgress",
  "completionPercentage": 46.5,
  "yieldPercentage": 96.8,
  "productionRate": 77.5,
  "plannedQuantity": 1000.0,
  "actualQuantityGood": 450.0,
  "actualQuantityScrap": 15.0,
  "totalQuantityProduced": 465.0,
  "isBehindSchedule": false,
  "requiresAttention": false,
  "estimatedCompletionTime": "2024-01-15T15:45:00Z",
  "actualStartTime": "2024-01-15T08:15:00Z",
  "scheduledEndTime": "2024-01-15T16:00:00Z",
  "lastUpdate": "2024-01-15T10:30:00Z"
}
```

### Start Work Order

```http
POST /api/jobs
Content-Type: application/json
```

**Description**: Start a new work order.

**Request Body**:
```json
{
  "workOrderId": "WO-2024-002",
  "workOrderDescription": "Production run for Widget B",
  "productId": "WIDGET-B-001",
  "productDescription": "Premium Widget Type B",
  "plannedQuantity": 500.0,
  "unitOfMeasure": "pieces",
  "scheduledStartTime": "2024-01-15T16:00:00Z",
  "scheduledEndTime": "2024-01-16T00:00:00Z",
  "deviceId": "ADAM-6051-01",
  "operatorId": "OP-001"
}
```

**Validation Rules**:
- `workOrderId`: Required, 1-50 characters
- `workOrderDescription`: Required, 1-200 characters
- `productId`: Required, 1-50 characters
- `productDescription`: Required, 1-200 characters
- `plannedQuantity`: Required, 0.1-999999.99
- `unitOfMeasure`: Optional, max 20 characters, defaults to "pieces"
- `scheduledStartTime`: Required, valid datetime
- `scheduledEndTime`: Required, valid datetime, must be after start time
- `deviceId`: Required, 1-20 characters
- `operatorId`: Optional, max 50 characters

**Response** (201):
```json
"WO-2024-002"
```

**Error Responses**:
- `400`: Validation errors or invalid data
- `409`: Work order already exists, device has active work order, or equipment line unavailable
- `500`: Internal server error

### Complete Work Order

```http
PUT /api/jobs/{id}/complete
Content-Type: application/json
```

**Description**: Complete an active work order.

**Request Body**:
```json
{
  "actualQuantityGood": 480.0,
  "actualQuantityScrap": 20.0,
  "completedByOperatorId": "OP-001",
  "completionNotes": "Production completed successfully",
  "actualEndTime": "2024-01-15T15:30:00Z"
}
```

**Validation Rules**:
- `actualQuantityGood`: Optional, 0-999999.99
- `actualQuantityScrap`: Optional, 0-999999.99
- `completedByOperatorId`: Optional, max 50 characters
- `completionNotes`: Optional, max 500 characters
- `actualEndTime`: Optional, defaults to current time

**Response** (204): No content

**Error Responses**:
- `400`: Validation errors
- `404`: Work order not found
- `409`: Work order already completed or cannot be completed
- `500`: Internal server error

## Equipment Line Management

### Get Equipment Lines

```http
GET /api/equipment-lines
```

**Description**: Retrieve all equipment lines with their ADAM device mappings.

**Response** (200):
```json
[
  {
    "id": 1,
    "lineId": "LINE-001",
    "lineName": "Production Line A",
    "adamDeviceId": "ADAM-6051-01",
    "adamChannel": 0,
    "isActive": true,
    "createdAt": "2024-01-15T08:00:00Z",
    "updatedAt": "2024-01-15T08:00:00Z"
  }
]
```

### Get Equipment Line by Line ID

```http
GET /api/equipment-lines/{lineId}
```

**Description**: Retrieve specific equipment line details.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lineId` | string | Yes | Equipment line identifier |

**Response** (200): Same format as equipment line array item above.

**Error Responses**:
- `404`: Equipment line not found

### Create Equipment Line

```http
POST /api/equipment-lines
Content-Type: application/json
```

**Description**: Create a new equipment line with ADAM device mapping.

**Request Body**:
```json
{
  "lineId": "LINE-002",
  "lineName": "Production Line B",
  "adamDeviceId": "ADAM-6051-02",
  "adamChannel": 1,
  "isActive": true
}
```

**Validation Rules**:
- `lineId`: Required, 1-50 characters, must be unique
- `lineName`: Required, 1-100 characters
- `adamDeviceId`: Required, 1-50 characters
- `adamChannel`: Required, 0-15
- `isActive`: Optional, defaults to true
- ADAM device/channel combination must be unique

**Response** (201):
```json
{
  "id": 2,
  "lineId": "LINE-002",
  "lineName": "Production Line B",
  "adamDeviceId": "ADAM-6051-02",
  "adamChannel": 1,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses**:
- `400`: Validation errors
- `409`: Equipment line ID or ADAM device/channel already exists

### Update Equipment Line

```http
PUT /api/equipment-lines/{lineId}
Content-Type: application/json
```

**Description**: Update equipment line configuration.

**Request Body**:
```json
{
  "lineName": "Updated Production Line B",
  "adamDeviceId": "ADAM-6051-03",
  "adamChannel": 2,
  "isActive": false
}
```

**Response** (204): No content

**Error Responses**:
- `400`: Validation errors
- `404`: Equipment line not found
- `409`: ADAM device/channel already in use by another line

### Get ADAM Device Mappings

```http
GET /api/equipment-lines/adam-mappings
```

**Description**: Get all ADAM device to equipment line mappings.

**Response** (200):
```json
[
  {
    "adamDeviceId": "ADAM-6051-01",
    "adamChannel": 0,
    "lineId": "LINE-001",
    "lineName": "Production Line A"
  }
]
```

## Stoppage Management

### Get Current Stoppage

```http
GET /api/stoppages/current?deviceId={deviceId}&minimumMinutes={minimumMinutes}
```

**Description**: Check for current stoppage on a specific device.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `deviceId` | string | Yes | - | Device/resource identifier |
| `minimumMinutes` | int | No | 5 | Minimum stoppage duration to consider (1-60) |

**Response** (200) - If stoppage detected:
```json
{
  "startTime": "2024-01-15T10:15:00Z",
  "durationMinutes": 15.5,
  "isActive": true,
  "deviceId": "ADAM-6051-01",
  "estimatedImpact": {
    "lostProductionUnits": 19.3,
    "lostRevenue": null,
    "availabilityImpact": 2.6
  },
  "classification": null,
  "endTime": null,
  "calculatedEndTime": "2024-01-15T10:30:30Z"
}
```

**Response** (204): No content - Device is running normally

### Get Stoppage History

```http
GET /api/stoppages?deviceId={deviceId}&period={period}&startTime={startTime}&endTime={endTime}&minimumMinutes={minimumMinutes}
```

**Description**: Retrieve historical stoppage data for a device.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `deviceId` | string | Yes | - | Device/resource identifier |
| `period` | int | No | 24 | Hours to look back (1-8760) |
| `startTime` | datetime | No | - | Custom start time |
| `endTime` | datetime | No | - | Custom end time |
| `minimumMinutes` | int | No | 5 | Minimum stoppage duration (1-60) |

**Response** (200):
```json
[
  {
    "startTime": "2024-01-15T09:45:00Z",
    "durationMinutes": 8.2,
    "isActive": false,
    "deviceId": "ADAM-6051-01",
    "estimatedImpact": {
      "lostProductionUnits": 10.5,
      "lostRevenue": null,
      "availabilityImpact": 1.4
    },
    "classification": null,
    "endTime": "2024-01-15T09:53:12Z",
    "calculatedEndTime": "2024-01-15T09:53:12Z"
  }
]
```

### Get Unclassified Stoppages

```http
GET /api/stoppages/unclassified?lineId={lineId}
```

**Description**: Get stoppages that require classification.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lineId` | string | No | Filter by equipment line ID |

**Response** (200):
```json
[
  {
    "id": 1,
    "lineId": "LINE-001",
    "workOrderId": "WO-2024-001",
    "startTime": "2024-01-15T10:15:00Z",
    "endTime": "2024-01-15T10:23:00Z",
    "durationMinutes": 8.0,
    "isClassified": false,
    "autoDetected": true,
    "minimumThresholdMinutes": 5,
    "createdAt": "2024-01-15T10:23:00Z"
  }
]
```

### Classify Stoppage

```http
PUT /api/stoppages/{id}/classify
Content-Type: application/json
```

**Description**: Classify a stoppage with a reason code.

**Request Body**:
```json
{
  "categoryCode": "A1",
  "subcode": "3",
  "operatorComments": "Changeover delay due to setup complexity",
  "classifiedBy": "OP-001"
}
```

**Validation Rules**:
- `categoryCode`: Required, must exist in stoppage reason categories
- `subcode`: Required, must exist for the specified category
- `operatorComments`: Optional, max 500 characters
- `classifiedBy`: Required, max 100 characters

**Response** (204): No content

**Error Responses**:
- `400`: Validation errors or invalid reason codes
- `404`: Stoppage not found
- `409`: Stoppage already classified

### Get Active Stoppages by Line

```http
GET /api/stoppages/active/{lineId}
```

**Description**: Get active stoppages for a specific equipment line.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lineId` | string | Yes | Equipment line identifier |

**Response** (200): Array of stoppage objects (same format as unclassified stoppages)

## Reason Code Management

### Get Reason Categories

```http
GET /api/reason-codes/categories
```

**Description**: Get all stoppage reason categories (3x3 matrix).

**Response** (200):
```json
[
  {
    "id": 1,
    "categoryCode": "A1",
    "categoryName": "Equipment Failure",
    "categoryDescription": "Mechanical or electrical equipment failures",
    "matrixRow": 1,
    "matrixCol": 1,
    "isActive": true,
    "createdAt": "2024-01-15T08:00:00Z"
  }
]
```

### Get Reason Subcodes

```http
GET /api/reason-codes/subcodes/{categoryCode}
```

**Description**: Get subcodes for a specific reason category.

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `categoryCode` | string | Yes | Category code (e.g., "A1") |

**Response** (200):
```json
[
  {
    "id": 1,
    "categoryId": 1,
    "subcode": "1",
    "subcodeName": "Motor Failure",
    "subcodeDescription": "Electric motor malfunction or failure",
    "matrixRow": 1,
    "matrixCol": 1,
    "isActive": true,
    "createdAt": "2024-01-15T08:00:00Z"
  }
]
```

### Get Complete Reason Code Matrix

```http
GET /api/reason-codes/matrix
```

**Description**: Get the complete 3x3 reason code matrix with categories and subcodes.

**Response** (200):
```json
{
  "categories": [
    {
      "categoryCode": "A1",
      "categoryName": "Equipment Failure",
      "matrixRow": 1,
      "matrixCol": 1,
      "subcodes": [
        {
          "subcode": "1",
          "subcodeName": "Motor Failure",
          "matrixRow": 1,
          "matrixCol": 1
        }
      ]
    }
  ]
}
```

## Data Models

### OeeCalculationDto

| Property | Type | Description |
|----------|------|-------------|
| `oeeId` | string | OEE calculation identifier |
| `resourceReference` | string | Device/machine identifier |
| `calculationPeriodStart` | datetime | Start of calculation period |
| `calculationPeriodEnd` | datetime | End of calculation period |
| `availabilityPercentage` | decimal | Availability percentage (0-100) |
| `performancePercentage` | decimal | Performance percentage (0-100) |
| `qualityPercentage` | decimal | Quality percentage (0-100) |
| `oeePercentage` | decimal | Overall OEE percentage (0-100) |
| `periodHours` | decimal | Period duration in hours |
| `worstFactor` | string | Factor with worst performance |
| `classification` | string | OEE classification (World Class, Good, Fair, Poor) |
| `requiresAttention` | boolean | Whether OEE requires attention |
| `createdAt` | datetime | When calculation was created |

### WorkOrderDto

| Property | Type | Description |
|----------|------|-------------|
| `workOrderId` | string | Work order identifier |
| `workOrderDescription` | string | Work order description |
| `productId` | string | Product identifier |
| `productDescription` | string | Product description |
| `plannedQuantity` | decimal | Planned quantity to produce |
| `unitOfMeasure` | string | Unit of measure for quantities |
| `scheduledStartTime` | datetime | Scheduled start time |
| `scheduledEndTime` | datetime | Scheduled end time |
| `resourceReference` | string | Device/machine identifier |
| `status` | string | Current work order status |
| `actualQuantityGood` | decimal | Actual good pieces produced |
| `actualQuantityScrap` | decimal | Actual scrap pieces |
| `totalQuantityProduced` | decimal | Total quantity produced |
| `actualStartTime` | datetime? | Actual start time |
| `actualEndTime` | datetime? | Actual end time |
| `completionPercentage` | decimal | Completion percentage |
| `yieldPercentage` | decimal | Yield/quality percentage |
| `productionRate` | decimal | Production rate (pieces/min) |
| `isBehindSchedule` | boolean | Whether behind schedule |
| `requiresAttention` | boolean | Whether requires attention |
| `estimatedCompletionTime` | datetime? | Estimated completion time |
| `createdAt` | datetime | When work order was created |
| `updatedAt` | datetime | Last updated timestamp |

### EquipmentLineDto

| Property | Type | Description |
|----------|------|-------------|
| `id` | int | Database identifier |
| `lineId` | string | Equipment line identifier |
| `lineName` | string | Human-readable line name |
| `adamDeviceId` | string | ADAM device identifier |
| `adamChannel` | int | ADAM channel number (0-15) |
| `isActive` | boolean | Whether line is active |
| `createdAt` | datetime | When line was created |
| `updatedAt` | datetime | Last updated timestamp |

### EquipmentStoppageDto

| Property | Type | Description |
|----------|------|-------------|
| `id` | int | Database identifier |
| `lineId` | string | Equipment line identifier |
| `workOrderId` | string? | Associated work order |
| `startTime` | datetime | Stoppage start time |
| `endTime` | datetime? | Stoppage end time |
| `durationMinutes` | decimal? | Duration in minutes |
| `isClassified` | boolean | Whether classified with reason |
| `categoryCode` | string? | Reason category code |
| `subcode` | string? | Reason subcode |
| `operatorComments` | string? | Operator comments |
| `classifiedBy` | string? | Who classified the stoppage |
| `classifiedAt` | datetime? | When classified |
| `autoDetected` | boolean | Whether auto-detected |
| `minimumThresholdMinutes` | int | Detection threshold |
| `createdAt` | datetime | When created |
| `updatedAt` | datetime | Last updated |

### StoppageReasonCategoryDto

| Property | Type | Description |
|----------|------|-------------|
| `id` | int | Database identifier |
| `categoryCode` | string | Category code (A1, A2, etc.) |
| `categoryName` | string | Category name |
| `categoryDescription` | string? | Category description |
| `matrixRow` | int | Matrix row position (1-3) |
| `matrixCol` | int | Matrix column position (1-3) |
| `isActive` | boolean | Whether active |
| `createdAt` | datetime | When created |

### StoppageReasonSubcodeDto

| Property | Type | Description |
|----------|------|-------------|
| `id` | int | Database identifier |
| `categoryId` | int | Parent category ID |
| `subcode` | string | Subcode (1, 2, etc.) |
| `subcodeName` | string | Subcode name |
| `subcodeDescription` | string? | Subcode description |
| `matrixRow` | int | Matrix row position (1-3) |
| `matrixCol` | int | Matrix column position (1-3) |
| `isActive` | boolean | Whether active |
| `createdAt` | datetime | When created |

### JobCompletionIssueDto

| Property | Type | Description |
|----------|------|-------------|
| `id` | int | Database identifier |
| `workOrderId` | string | Associated work order |
| `issueType` | string | Issue type (UNDER_COMPLETION, etc.) |
| `completionPercentage` | decimal? | Completion percentage |
| `targetQuantity` | decimal? | Target quantity |
| `actualQuantity` | decimal? | Actual quantity |
| `categoryCode` | string? | Reason category code |
| `subcode` | string? | Reason subcode |
| `operatorComments` | string? | Operator comments |
| `resolvedBy` | string? | Who resolved the issue |
| `resolvedAt` | datetime? | When resolved |
| `createdAt` | datetime | When created |

### StoppageInfoDto

| Property | Type | Description |
|----------|------|-------------|
| `startTime` | datetime | When stoppage started |
| `durationMinutes` | decimal | Duration in minutes |
| `isActive` | boolean | Whether currently active |
| `deviceId` | string | Device identifier |
| `estimatedImpact` | StoppageImpactDto? | Estimated production impact |
| `classification` | string? | Stoppage classification |
| `endTime` | datetime? | When stoppage ended |
| `calculatedEndTime` | datetime | Calculated end time |

### StoppageImpactDto

| Property | Type | Description |
|----------|------|-------------|
| `lostProductionUnits` | decimal | Estimated units lost |
| `lostRevenue` | decimal? | Estimated revenue impact |
| `availabilityImpact` | decimal | Impact on availability % |

## Examples

### Complete OEE Workflow

1. **Setup equipment line** (if not already configured):
```bash
curl -X POST "http://localhost:5001/api/equipment-lines" \
  -H "Content-Type: application/json" \
  -d '{
    "lineId": "LINE-001",
    "lineName": "Production Line A",
    "adamDeviceId": "ADAM-6051-01",
    "adamChannel": 0,
    "isActive": true
  }'
```

2. **Check equipment line status**:
```bash
curl "http://localhost:5001/api/equipment-lines/LINE-001"
```

3. **Check device status**:
```bash
curl "http://localhost:5001/api/jobs/active?deviceId=ADAM-6051-01"
```

4. **Start work order if none active**:
```bash
curl -X POST "http://localhost:5001/api/jobs" \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId": "WO-2024-001",
    "workOrderDescription": "Widget A Production",
    "productId": "WIDGET-A",
    "productDescription": "Standard Widget",
    "plannedQuantity": 1000,
    "scheduledStartTime": "2024-01-15T08:00:00Z",
    "scheduledEndTime": "2024-01-15T16:00:00Z",
    "deviceId": "ADAM-6051-01"
  }'
```

5. **Monitor OEE during production**:
```bash
curl "http://localhost:5001/api/oee/current?deviceId=ADAM-6051-01"
```

6. **Check for stoppages**:
```bash
curl "http://localhost:5001/api/stoppages/current?deviceId=ADAM-6051-01"
```

7. **Classify any unclassified stoppages**:
```bash
# Get unclassified stoppages
curl "http://localhost:5001/api/stoppages/unclassified?lineId=LINE-001"

# Classify a stoppage
curl -X PUT "http://localhost:5001/api/stoppages/1/classify" \
  -H "Content-Type: application/json" \
  -d '{
    "categoryCode": "A1",
    "subcode": "3",
    "operatorComments": "Equipment changeover",
    "classifiedBy": "OP-001"
  }'
```

8. **Complete work order**:
```bash
curl -X PUT "http://localhost:5001/api/jobs/WO-2024-001/complete" \
  -H "Content-Type: application/json" \
  -d '{
    "actualQuantityGood": 980,
    "actualQuantityScrap": 20,
    "completionNotes": "Production completed successfully"
  }'
```

### Query Historical Data

```bash
# Get last 8 hours of OEE data
curl "http://localhost:5001/api/oee/history?deviceId=ADAM-6051-01&period=8&intervalMinutes=30"

# Get yesterday's stoppages
curl "http://localhost:5001/api/stoppages?deviceId=ADAM-6051-01&startTime=2024-01-14T00:00:00Z&endTime=2024-01-14T23:59:59Z"

# Get equipment line configuration
curl "http://localhost:5001/api/equipment-lines"

# Get ADAM device mappings
curl "http://localhost:5001/api/equipment-lines/adam-mappings"
```

### Reason Code Management

```bash
# Get reason code categories (3x3 matrix)
curl "http://localhost:5001/api/reason-codes/categories"

# Get subcodes for a specific category
curl "http://localhost:5001/api/reason-codes/subcodes/A1"

# Get complete reason code matrix
curl "http://localhost:5001/api/reason-codes/matrix"
```

### Health Monitoring

```bash
# Basic health check for monitoring systems
curl "http://localhost:5001/health"

# Detailed health for troubleshooting
curl "http://localhost:5001/api/health/detailed"
```

## Rate Limiting and Performance

- No rate limiting is currently implemented
- API responses are typically under 100ms for single-device queries
- Historical queries may take longer depending on time range and data volume
- Concurrent requests are supported with connection pooling

## Troubleshooting

### Common Issues

1. **404 Device Not Found**: Ensure deviceId exists in the system
2. **409 Work Order Conflict**: Check for existing active work orders
3. **400 Validation Error**: Review request format and required fields
4. **500 Internal Error**: Check service logs and database connectivity

### Debug Headers

In development environments, additional debug information may be included in response headers:
- `X-Request-Id`: Unique request identifier for tracing
- `X-Processing-Time`: Request processing time in milliseconds

For production debugging, correlate requests using structured logs with the request path and timestamp.