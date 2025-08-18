# Industrial.Adam.Oee.WebApi

## Overview

The Industrial Adam OEE (Overall Equipment Effectiveness) Web API is a .NET 9 service that provides the business logic layer for OEE calculations and monitoring. This service replaces the TypeScript-based OEE implementation and follows Clean Architecture principles with CQRS patterns.

## Architecture

The OEE API follows Clean Architecture with clear separation of concerns:

```
├── Industrial.Adam.Oee.Domain/          # Domain models and business rules
├── Industrial.Adam.Oee.Application/     # CQRS commands/queries and use cases  
├── Industrial.Adam.Oee.Infrastructure/  # Data access and external services
├── Industrial.Adam.Oee.WebApi/         # REST API controllers and configuration
└── Industrial.Adam.Oee.Tests/          # Unit and integration tests
```

## Features

- **Health Monitoring**: Built-in health checks for service and database connectivity
- **CQRS Pattern**: MediatR implementation for command/query separation
- **Validation**: FluentValidation for request validation
- **Logging**: Structured logging with Serilog
- **Docker Support**: Multi-stage Docker build with health checks
- **OpenAPI/Swagger**: API documentation (development only)
- **CORS**: Configured for React frontend integration

## Quick Start

### Prerequisites

- .NET 9 SDK
- Docker (optional)
- TimescaleDB (provided via Docker Compose)

### Development

1. **Start Dependencies**:
   ```bash
   cd docker
   docker-compose up -d timescaledb
   ```

2. **Run the API**:
   ```bash
   cd src/Industrial.Adam.Oee.WebApi
   dotnet run
   ```

3. **Access Health Check**:
   ```bash
   curl http://localhost:5001/health
   curl http://localhost:5001/api/health/detailed
   ```

### Docker Development

1. **Build and Start All Services**:
   ```bash
   cd docker
   docker-compose up -d
   ```

2. **Access OEE API**:
   - API: http://localhost:5001
   - Health: http://localhost:5001/health
   - Detailed Health: http://localhost:5001/api/health/detailed

### Testing

```bash
# Run all OEE tests
dotnet test src/Industrial.Adam.Oee.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Development |
| `ConnectionStrings__DefaultConnection` | TimescaleDB connection string | localhost:5433 |
| `LOG_LEVEL` | Logging level | Information |
| `Oee__Database__ConnectionString` | TimescaleDB connection string | See ConnectionStrings section |
| `Oee__Cache__DefaultExpirationMinutes` | Default cache expiration | 5 |
| `Oee__Resilience__DatabaseRetry__MaxRetryAttempts` | Database retry attempts | 3 |
| `Oee__Performance__Enabled` | Enable performance monitoring | true |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=adam_counters;Username=adam_user;Password=adam_password"
  },
  "Oee": {
    "Database": {
      "ConnectionString": "",
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 60,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 100
    },
    "Cache": {
      "DefaultExpirationMinutes": 5,
      "OeeMetricsExpirationMinutes": 2,
      "WorkOrderExpirationMinutes": 10,
      "DeviceStatusExpirationMinutes": 1
    },
    "Resilience": {
      "DatabaseRetry": {
        "MaxRetryAttempts": 3,
        "BaseDelayMs": 1000,
        "UseExponentialBackoff": true,
        "MaxDelayMs": 30000
      },
      "CircuitBreaker": {
        "ExceptionsAllowedBeforeBreaking": 5,
        "DurationOfBreakSeconds": 30,
        "SamplingDurationSeconds": 60,
        "MinimumThroughput": 10
      }
    },
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": false,
      "SlowQueryThresholdMs": 1000,
      "LogSlowQueries": true
    }
  }
}
```

## API Endpoints

### Health Checks
- `GET /health` - Basic health status
- `GET /api/health/detailed` - Comprehensive health report with dependencies

#### Health Check Response Formats

**Basic Health (`GET /health`)**:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "OEE API",
  "version": "1.0.0"
}
```

**Detailed Health (`GET /api/health/detailed`)**:
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

### OEE Operations
- `GET /api/oee/current?deviceId={id}` - Current OEE metrics for device
- `GET /api/oee/history?deviceId={id}` - Historical OEE data for device
- `GET /api/oee/breakdown?deviceId={id}` - Detailed OEE breakdown with factors

### Work Order Management
- `GET /api/jobs/active?deviceId={id}` - Get active work order for device
- `GET /api/jobs/{id}` - Get specific work order details
- `GET /api/jobs/{id}/progress` - Get work order progress information
- `POST /api/jobs` - Start new work order
- `PUT /api/jobs/{id}/complete` - Complete work order

### Stoppage Management
- `GET /api/stoppages/current?deviceId={id}` - Current stoppage information
- `GET /api/stoppages?deviceId={id}` - Historical stoppage data
- `PUT /api/stoppages/{id}/classify` - Classify stoppage (future feature)

## Database Integration

The OEE API integrates with the existing TimescaleDB instance used by the Industrial.Adam.Logger system:

- **Connection**: Uses existing `adam_counters` database
- **Tables**: Reads from `counter_data` table for OEE calculations
- **Health Checks**: Monitors TimescaleDB connectivity

## Development Workflow

1. **Phase 0** (Complete): Project structure and Docker integration
2. **Phase 1** (Next): Domain model implementation and basic CQRS
3. **Phase 2**: API endpoints and React frontend integration
4. **Phase 3**: Advanced OEE features and optimization

## Docker Integration

### Multi-stage Build
The Dockerfile uses multi-stage builds for optimization:
- Build stage: .NET SDK for compilation
- Runtime stage: .NET Runtime for minimal production image

### Health Checks
- Container health check every 30 seconds
- Monitors both service availability and database connectivity

### Resource Limits
- Memory: 512MB limit, 256MB reservation
- CPU: 0.5 cores limit, 0.25 cores reservation

## Logging

Structured logging with Serilog:
- Console output for development
- File logging with daily rotation
- Contextual information for debugging
- Performance metrics for all requests

## Next Steps

See the [OEE Technology Stack Migration Plan](../../OEE_TECHNOLOGY_STACK_MIGRATION_PLAN.md) for:
- Domain model migration from TypeScript
- CQRS implementation details
- React frontend integration
- Production deployment strategy