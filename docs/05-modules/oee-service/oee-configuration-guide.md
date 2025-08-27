# OEE Configuration Guide

This guide provides comprehensive configuration instructions for the Industrial ADAM OEE service. The OEE service uses a structured configuration approach with clear separation of concerns across database, caching, resilience, and performance settings.

## Table of Contents

- [Overview](#overview)
- [Configuration Structure](#configuration-structure)
- [Database Configuration](#database-configuration)
- [Equipment Line Configuration](#equipment-line-configuration)
- [Reason Code Configuration](#reason-code-configuration)
- [Cache Configuration](#cache-configuration)
- [Resilience Configuration](#resilience-configuration)
- [Performance Configuration](#performance-configuration)
- [Environment Variables](#environment-variables)
- [Docker Configuration](#docker-configuration)
- [Production Examples](#production-examples)
- [Troubleshooting](#troubleshooting)

## Overview

The OEE service configuration is organized under the `Oee` section in your `appsettings.json`. This section contains four main subsections:

- **Database**: TimescaleDB connection and pooling settings (including Phase 1 tables)
- **Cache**: In-memory caching configuration for different data types
- **Resilience**: Retry policies and circuit breaker settings
- **Performance**: Performance monitoring and logging configuration

### Phase 1 Implementation Note

Phase 1 has been completed and includes the following new database tables that require configuration:
- `equipment_lines` - Production line to ADAM device mapping
- `stoppage_reason_categories` - Level 1 reason codes (3x3 matrix)
- `stoppage_reason_subcodes` - Level 2 reason codes
- `equipment_stoppages` - Enhanced stoppage tracking with classification
- `job_completion_issues` - Job completion problem tracking

## Configuration Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=adam_counters;Username=adam_user;Password=adam_password"
  },
  "Oee": {
    "Database": { /* Database-specific settings */ },
    "Cache": { /* Caching configuration */ },
    "Resilience": { /* Retry and circuit breaker policies */ },
    "Performance": { /* Performance monitoring settings */ }
  }
}
```

## Database Configuration

The database configuration controls how the OEE service connects to TimescaleDB. With Phase 1 implementation, the database now includes equipment line management, reason code systems, and enhanced stoppage tracking.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | string | `""` | Database connection string (overrides DefaultConnection if set) |
| `ConnectionTimeoutSeconds` | int | `30` | Maximum time to wait for connection establishment |
| `CommandTimeoutSeconds` | int | `60` | Maximum time to wait for command execution |
| `EnableConnectionPooling` | bool | `true` | Whether to use connection pooling |
| `MaxPoolSize` | int | `100` | Maximum number of connections in the pool |

### Example

```json
{
  "Oee": {
    "Database": {
      "ConnectionString": "",
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 60,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 100
    }
  }
}
```

### Phase 1 Database Requirements

The database must include the following Phase 1 tables:
- `equipment_lines` - For ADAM device to production line mapping
- `stoppage_reason_categories` - For the 2-level reason code system
- `stoppage_reason_subcodes` - For detailed reason classifications
- `equipment_stoppages` - For enhanced stoppage tracking
- `job_completion_issues` - For job completion problem tracking

### Notes

- If `ConnectionString` is empty, the service will use `ConnectionStrings:DefaultConnection`
- Connection pooling is recommended for production environments
- Adjust `MaxPoolSize` based on your expected concurrent load
- Phase 1 tables must exist before starting the service

## Equipment Line Configuration

### Overview

Equipment lines map ADAM devices to production lines and are fundamental to Phase 1 functionality. Each equipment line must have a unique combination of ADAM device ID and channel.

### Configuration Requirements

1. **Database Setup**: Equipment lines are stored in the `equipment_lines` table
2. **ADAM Device Mapping**: Each line maps to exactly one ADAM device + channel combination
3. **Unique Constraints**: No two lines can share the same ADAM device/channel
4. **Active Status**: Lines can be activated/deactivated without deletion

### Initial Setup Example

```sql
-- Example equipment line setup
INSERT INTO equipment_lines (line_id, line_name, adam_device_id, adam_channel, is_active) VALUES
('LINE-001', 'Production Line A', 'ADAM-6051-01', 0, true),
('LINE-002', 'Production Line B', 'ADAM-6051-01', 1, true),
('LINE-003', 'Quality Control Line', 'ADAM-6051-02', 0, true);
```

### API Configuration

Equipment lines can be managed through the API:
- `GET /api/equipment-lines` - List all equipment lines
- `POST /api/equipment-lines` - Create new equipment line
- `PUT /api/equipment-lines/{lineId}` - Update equipment line
- `GET /api/equipment-lines/adam-mappings` - Get ADAM device mappings

## Reason Code Configuration

### Overview

Phase 1 implements a 2-level reason code system using a 3x3 matrix structure for classifying stoppages and job completion issues.

### Matrix Structure

**Level 1 Categories (3x3 Matrix)**:
- A1, A2, A3 (Row 1)
- B1, B2, B3 (Row 2)
- C1, C2, C3 (Row 3)

**Level 2 Subcodes**: 1-9 for each category

### Initial Setup Example

```sql
-- Example reason code setup
INSERT INTO stoppage_reason_categories (category_code, category_name, category_description, matrix_row, matrix_col, is_active) VALUES
('A1', 'Equipment Failure', 'Mechanical or electrical equipment failures', 1, 1, true),
('A2', 'Material Issues', 'Raw material or supply problems', 1, 2, true),
('A3', 'Quality Issues', 'Product quality problems', 1, 3, true),
('B1', 'Planned Maintenance', 'Scheduled maintenance activities', 2, 1, true),
('B2', 'Changeover', 'Product or setup changeovers', 2, 2, true),
('B3', 'Training', 'Operator training activities', 2, 3, true),
('C1', 'External Factors', 'Utility or external disruptions', 3, 1, true),
('C2', 'Operator Issues', 'Staffing or operator-related delays', 3, 2, true),
('C3', 'Other', 'Miscellaneous unclassified issues', 3, 3, true);

INSERT INTO stoppage_reason_subcodes (category_id, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active) VALUES
(1, '1', 'Motor Failure', 'Electric motor malfunction', 1, 1, true),
(1, '2', 'Sensor Malfunction', 'Sensor reading errors', 1, 2, true),
(1, '3', 'Hydraulic Issues', 'Hydraulic system problems', 1, 3, true);
-- Add more subcodes as needed
```

### API Access

Reason codes are accessible through:
- `GET /api/reason-codes/categories` - Get all categories
- `GET /api/reason-codes/subcodes/{categoryCode}` - Get subcodes for category
- `GET /api/reason-codes/matrix` - Get complete matrix

## Cache Configuration

The cache configuration controls in-memory caching for different types of OEE data to improve performance, including Phase 1 equipment and reason code data.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultExpirationMinutes` | int | `5` | Default cache expiration for unspecified cache entries |
| `OeeMetricsExpirationMinutes` | int | `2` | Cache expiration for OEE calculation results |
| `WorkOrderExpirationMinutes` | int | `10` | Cache expiration for work order information |
| `DeviceStatusExpirationMinutes` | int | `1` | Cache expiration for device status information |
| `EquipmentLineExpirationMinutes` | int | `15` | Cache expiration for equipment line configuration |
| `ReasonCodeExpirationMinutes` | int | `60` | Cache expiration for reason code data |

### Example

```json
{
  "Oee": {
    "Cache": {
      "DefaultExpirationMinutes": 5,
      "OeeMetricsExpirationMinutes": 2,
      "WorkOrderExpirationMinutes": 10,
      "DeviceStatusExpirationMinutes": 1,
      "EquipmentLineExpirationMinutes": 15,
      "ReasonCodeExpirationMinutes": 60
    }
  }
}
```

### Guidelines

- **OEE Metrics**: Short cache time (2 minutes) for near real-time calculations
- **Work Orders**: Longer cache time (10 minutes) as they change less frequently
- **Device Status**: Very short cache time (1 minute) for current status information
- **Equipment Lines**: Medium cache time (15 minutes) as configuration changes infrequently
- **Reason Codes**: Long cache time (60 minutes) as these are relatively static
- **Default**: Fallback for any unconfigured cache entries

## Resilience Configuration

The resilience configuration defines retry policies and circuit breaker patterns to handle transient failures.

### Database Retry Policy

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxRetryAttempts` | int | `3` | Maximum number of retry attempts for database operations |
| `BaseDelayMs` | int | `1000` | Base delay between retries in milliseconds |
| `UseExponentialBackoff` | bool | `true` | Whether to use exponential backoff for retry delays |
| `MaxDelayMs` | int | `30000` | Maximum delay between retries in milliseconds |

### Circuit Breaker Policy

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ExceptionsAllowedBeforeBreaking` | int | `5` | Number of consecutive exceptions before opening circuit |
| `DurationOfBreakSeconds` | int | `30` | How long circuit stays open before attempting to close |
| `SamplingDurationSeconds` | int | `60` | Time window for measuring failure rate |
| `MinimumThroughput` | int | `10` | Minimum requests before circuit breaking is considered |

### Example

```json
{
  "Oee": {
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
    }
  }
}
```

## Performance Configuration

The performance configuration controls monitoring and logging of performance metrics.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `true` | Whether performance monitoring is enabled |
| `EnableDetailedMetrics` | bool | `false` | Whether to collect detailed query-level metrics |
| `SlowQueryThresholdMs` | int | `1000` | Threshold for considering a query "slow" |
| `LogSlowQueries` | bool | `true` | Whether to log queries that exceed the slow threshold |

### Example

```json
{
  "Oee": {
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": false,
      "SlowQueryThresholdMs": 1000,
      "LogSlowQueries": true
    }
  }
}
```

### Guidelines

- Enable `DetailedMetrics` only in development or when debugging performance issues
- Adjust `SlowQueryThresholdMs` based on your performance requirements
- Slow query logging helps identify optimization opportunities

## Environment Variables

You can override any configuration setting using environment variables with the pattern `Section__Subsection__Property`.

### Common Environment Variables

```bash
# Database connection
ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=adam_counters;Username=adam_user;Password=adam_password"

# OEE-specific overrides
Oee__Database__ConnectionTimeoutSeconds=45
Oee__Cache__OeeMetricsExpirationMinutes=1
Oee__Cache__EquipmentLineExpirationMinutes=15
Oee__Cache__ReasonCodeExpirationMinutes=60
Oee__Resilience__DatabaseRetry__MaxRetryAttempts=5
Oee__Performance__SlowQueryThresholdMs=500

# Common deployment settings
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
```

## Docker Configuration

### docker-compose.yml Example

```yaml
version: '3.8'
services:
  oee-api:
    image: industrial-adam-oee:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=timescaledb;Port=5432;Database=adam_counters;Username=adam_user;Password=adam_password
      - Oee__Database__ConnectionTimeoutSeconds=30
      - Oee__Cache__OeeMetricsExpirationMinutes=2
      - Oee__Cache__EquipmentLineExpirationMinutes=15
      - Oee__Cache__ReasonCodeExpirationMinutes=60
      - Oee__Performance__SlowQueryThresholdMs=1000
    ports:
      - "5001:8080"
    depends_on:
      - timescaledb
    restart: unless-stopped

  timescaledb:
    image: timescale/timescaledb:latest-pg15
    environment:
      - POSTGRES_DB=adam_counters
      - POSTGRES_USER=adam_user
      - POSTGRES_PASSWORD=adam_password
    ports:
      - "5433:5432"
    volumes:
      - timescale_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  timescale_data:
```

## Production Examples

### High-Volume Production

Configuration optimized for high-throughput environments:

```json
{
  "Oee": {
    "Database": {
      "ConnectionTimeoutSeconds": 45,
      "CommandTimeoutSeconds": 120,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 200
    },
    "Cache": {
      "DefaultExpirationMinutes": 3,
      "OeeMetricsExpirationMinutes": 1,
      "WorkOrderExpirationMinutes": 15,
      "DeviceStatusExpirationMinutes": 1,
      "EquipmentLineExpirationMinutes": 15,
      "ReasonCodeExpirationMinutes": 60
    },
    "Resilience": {
      "DatabaseRetry": {
        "MaxRetryAttempts": 5,
        "BaseDelayMs": 500,
        "UseExponentialBackoff": true,
        "MaxDelayMs": 15000
      },
      "CircuitBreaker": {
        "ExceptionsAllowedBeforeBreaking": 10,
        "DurationOfBreakSeconds": 60,
        "SamplingDurationSeconds": 120,
        "MinimumThroughput": 20
      }
    },
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": true,
      "SlowQueryThresholdMs": 500,
      "LogSlowQueries": true
    }
  }
}
```

### Development Environment

Configuration optimized for development and debugging:

```json
{
  "Oee": {
    "Database": {
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 60,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 50
    },
    "Cache": {
      "DefaultExpirationMinutes": 1,
      "OeeMetricsExpirationMinutes": 1,
      "WorkOrderExpirationMinutes": 5,
      "DeviceStatusExpirationMinutes": 1,
      "EquipmentLineExpirationMinutes": 5,
      "ReasonCodeExpirationMinutes": 10
    },
    "Resilience": {
      "DatabaseRetry": {
        "MaxRetryAttempts": 2,
        "BaseDelayMs": 1000,
        "UseExponentialBackoff": false,
        "MaxDelayMs": 5000
      },
      "CircuitBreaker": {
        "ExceptionsAllowedBeforeBreaking": 3,
        "DurationOfBreakSeconds": 10,
        "SamplingDurationSeconds": 30,
        "MinimumThroughput": 5
      }
    },
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": true,
      "SlowQueryThresholdMs": 100,
      "LogSlowQueries": true
    }
  }
}
```

## Troubleshooting

### Common Configuration Issues

#### Database Connection Problems

**Symptom**: Service fails to start with database connection errors

**Solutions**:
1. Verify `ConnectionStrings:DefaultConnection` is correct
2. Check database server is running and accessible
3. Increase `ConnectionTimeoutSeconds` for slow networks
4. Verify database credentials and permissions

#### High Memory Usage

**Symptom**: Service consuming excessive memory

**Solutions**:
1. Reduce cache expiration times
2. Decrease `MaxPoolSize` in database settings
3. Disable `EnableDetailedMetrics` in production
4. Monitor cache hit rates and adjust accordingly

#### Performance Issues

**Symptom**: Slow API responses

**Solutions**:
1. Enable performance monitoring to identify bottlenecks
2. Adjust cache expiration times (longer for stable data)
3. Increase `MaxPoolSize` for high concurrency
4. Review slow query logs for optimization opportunities

#### Circuit Breaker Triggering

**Symptom**: Service returning 503 errors during load

**Solutions**:
1. Increase `ExceptionsAllowedBeforeBreaking`
2. Reduce `DurationOfBreakSeconds` for faster recovery
3. Check database health and performance
4. Review retry policy settings

### Validation

You can validate your configuration by checking the service health endpoint:

```bash
# Basic health check
curl http://localhost:5001/health

# Detailed health check with configuration validation
curl http://localhost:5001/api/health/detailed
```

### Logging Configuration Issues

The service logs configuration validation errors at startup. Check the logs for messages like:

```
[ERROR] Configuration validation failed: Oee.Database.ConnectionTimeoutSeconds must be greater than 0
[WARN] Oee.Cache.OeeMetricsExpirationMinutes is set to 0, using default value of 5 minutes
[INFO] Successfully validated OEE configuration
```

## Best Practices

1. **Start with defaults** and adjust based on monitoring
2. **Test configuration changes** in development first
3. **Monitor performance metrics** to validate settings
4. **Use environment variables** for deployment-specific overrides
5. **Keep cache times appropriate** for data freshness requirements
6. **Set reasonable timeouts** based on network and database performance
7. **Enable performance monitoring** in production for optimization insights