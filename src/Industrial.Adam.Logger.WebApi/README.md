# Industrial ADAM Logger Web API

RESTful API and real-time SignalR endpoints for the Industrial ADAM Logger system.

## Overview

This Web API provides:
- REST endpoints for device management and data retrieval
- Real-time SignalR hubs for live data streaming
- Health monitoring and diagnostics
- Configuration management

## Getting Started

### Run Locally

```bash
cd src/Industrial.Adam.Logger.WebApi
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Run with Docker

The API is included in the main Docker stack. See the [Docker deployment guide](../../docker/README.md).

## API Endpoints

### Device Management

#### Get All Devices
```http
GET /api/device
```

Response:
```json
[
  {
    "deviceId": "Device001",
    "name": "Production Line 1",
    "ipAddress": "192.168.1.100",
    "status": "Online",
    "lastSeen": "2024-01-15T10:30:00Z"
  }
]
```

#### Get Device Details
```http
GET /api/device/{deviceId}
```

#### Start/Stop Device
```http
POST /api/device/{deviceId}/start
POST /api/device/{deviceId}/stop
```

### Data Retrieval

#### Get Latest Values
```http
GET /api/data/latest?deviceId={deviceId}
```

Response:
```json
{
  "deviceId": "Device001",
  "timestamp": "2024-01-15T10:30:00Z",
  "channels": [
    {
      "channel": 0,
      "name": "ProductionCounter",
      "value": 12345,
      "quality": "Good"
    }
  ]
}
```

#### Get Historical Data
```http
GET /api/data/history?deviceId={deviceId}&start={ISO8601}&end={ISO8601}
```

### Configuration

#### Get Current Configuration
```http
GET /api/configuration
```

#### Update Configuration
```http
PUT /api/configuration
Content-Type: application/json

{
  "globalPollIntervalMs": 2000,
  "devices": [...]
}
```

### Diagnostics

#### Health Check
```http
GET /api/diagnostics/health
```

Response:
```json
{
  "status": "Healthy",
  "services": {
    "influxDb": "Connected",
    "devices": "3 of 3 online"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### Get System Metrics
```http
GET /api/diagnostics/metrics
```

### Monitoring

#### Get Device Status
```http
GET /api/monitoring/status
```

#### Get Alerts
```http
GET /api/monitoring/alerts
```

## SignalR Hubs

### Counter Data Hub

Connect to receive real-time counter updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/counterdata")
    .build();

connection.on("CounterUpdate", (deviceId, channel, value) => {
    console.log(`Device ${deviceId}, Channel ${channel}: ${value}`);
});

connection.start();
```

### Health Status Hub

Connect to receive health status updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/healthstatus")
    .build();

connection.on("HealthUpdate", (status) => {
    console.log("Health status:", status);
});

connection.start();
```

## Authentication & Authorization

Currently, the API does not require authentication. For production deployments, consider implementing:
- JWT authentication
- API key authentication
- Azure AD integration

## Error Handling

All endpoints return standard error responses:

```json
{
  "error": {
    "code": "DEVICE_NOT_FOUND",
    "message": "Device with ID 'Device999' not found",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

HTTP Status Codes:
- 200: Success
- 400: Bad Request
- 404: Not Found
- 500: Internal Server Error

## Configuration

The API uses standard ASP.NET Core configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "InfluxDb": "http://localhost:8086"
  }
}
```

## Development

### API Testing

Use the included HTTP file for testing:

```bash
# Install REST Client extension in VS Code
# Open Industrial.Adam.Logger.WebApi.http
# Click "Send Request" on any endpoint
```

### Swagger/OpenAPI

Swagger UI is available at:
- http://localhost:5000/swagger

## Performance Considerations

- SignalR connections are automatically scaled
- Data is cached for 1 second to reduce database load
- Bulk endpoints are available for large data retrievals

## Deployment

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Development, Staging, Production
- `ASPNETCORE_URLS`: Binding URLs
- `ConnectionStrings__InfluxDb`: InfluxDB connection string

### Health Checks

The API includes health checks for:
- InfluxDB connectivity
- Device manager status
- Memory usage

### CORS

CORS is configured to allow:
- Any origin in Development
- Specific origins in Production (configure in appsettings.json)

## Support

For issues or questions:
1. Check the logs in `logs/` directory
2. Review the main project documentation
3. Check SignalR connection status in browser console