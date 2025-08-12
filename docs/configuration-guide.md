# Configuration Guide

This guide provides comprehensive information about configuring the Industrial ADAM Logger application with TimescaleDB integration and advanced features like windowed rate calculation and dead letter queue.

## Quick Start Configuration

The minimal configuration required to get started:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Industrial.Adam.Logger.Core": "Debug"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 5000,
    "Devices": [
      {
        "DeviceId": "ADAM001",
        "Name": "Production Line Counter",
        "IpAddress": "192.168.1.100",
        "Port": 502,
        "Enabled": true,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "Product Counter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "Enabled": true
          }
        ]
      }
    ],
    "TimescaleDb": {
      "Host": "localhost",
      "Port": 5433,
      "Database": "adam_counters",
      "Username": "adam_user",
      "Password": "adam_password",
      "TableName": "counter_data"
    }
  }
}
```

## Configuration Structure

### Important: Configuration Nesting

⚠️ **Critical**: TimescaleDB settings must be nested under `AdamLogger:TimescaleDb`, not at the root level.

**✅ Correct Structure:**
```json
{
  "AdamLogger": {
    "Devices": [...],
    "TimescaleDb": {
      "Host": "...",
      "Database": "..."
    }
  }
}
```

**❌ Incorrect Structure:**
```json
{
  "AdamLogger": {
    "Devices": [...]
  },
  "TimescaleDb": {
    "Host": "...",
    "Database": "..."
  }
}
```

## Device Configuration

### Device Settings

| Property | Required | Type | Default | Description |
|----------|----------|------|---------|-------------|
| `DeviceId` | ✅ | string | - | Unique identifier for the device |
| `Name` | ❌ | string | "" | Human-readable device name |
| `IpAddress` | ✅ | string | - | IP address or hostname (supports `localhost`, `127.0.0.1`, or hostnames like `adam-device-01`) |
| `Port` | ❌ | int | 502 | Modbus TCP port |
| `UnitId` | ❌ | byte | 1 | Modbus unit/slave ID |
| `Enabled` | ❌ | bool | true | Whether to poll this device |
| `PollIntervalMs` | ❌ | int | 5000 | Device-specific polling interval |
| `TimeoutMs` | ❌ | int | 3000 | Communication timeout |
| `MaxRetries` | ❌ | int | 3 | Maximum retry attempts |
| `KeepAlive` | ❌ | bool | true | Enable TCP keep-alive |

### IP Address / Hostname Support

The application supports multiple address formats:

- **IP Addresses**: `192.168.1.100`, `127.0.0.1`
- **Localhost**: `localhost` (automatically resolved)
- **Hostnames**: `adam-device-01`, `plc.factory.com`

### Channel Configuration

| Property | Required | Type | Default | Description |
|----------|----------|------|---------|-------------|
| `ChannelNumber` | ✅ | int | - | Physical channel number (0-15) |
| `Name` | ❌ | string | "" | Human-readable channel name |
| `StartRegister` | ✅ | int | - | Starting Modbus register address |
| `RegisterCount` | ❌ | int | 2 | Number of registers (2 for 32-bit counter) |
| `Enabled` | ❌ | bool | true | Whether to poll this channel |
| `ScaleFactor` | ❌ | double | 1.0 | Multiply raw value by this factor |
| `Unit` | ❌ | string | "items" | Unit of measurement |
| `MinValue` | ❌ | long | 0 | Minimum expected value |
| `MaxValue` | ❌ | long | 4294967295 | Maximum expected value |
| `MaxChangeRate` | ❌ | double | 1000 | Maximum rate change per second |
| `RateWindowSeconds` | ❌ | int | 60 | Time window for rate calculation (NEW) |

### Channel Register Mapping

ADAM-6051 devices use 32-bit counters that span 2 consecutive 16-bit Modbus registers:

```json
{
  "Channels": [
    {
      "ChannelNumber": 0,
      "StartRegister": 0,  // Uses registers 0-1
      "RegisterCount": 2
    },
    {
      "ChannelNumber": 1, 
      "StartRegister": 2,  // Uses registers 2-3
      "RegisterCount": 2
    }
  ]
}
```

## TimescaleDB Configuration

### Required Settings

| Property | Required | Description | Example |
|----------|----------|-------------|---------|
| `Host` | ✅ | TimescaleDB server hostname | `localhost` or `timescaledb` |
| `Port` | ✅ | PostgreSQL port | `5433` for Docker, `5432` for standard |
| `Database` | ✅ | Database name | `adam_counters` |
| `Username` | ✅ | Database username | `adam_user` |
| `Password` | ✅ | Database password | Use environment variable in production |
| `TableName` | ❌ | Table name for counter data | Default: `counter_data` |

### Optional Settings

| Property | Default | Description |
|----------|---------|-------------|
| `BatchSize` | 50 | Number of readings per batch |
| `BatchTimeoutMs` | 5000 | Maximum time before sending partial batch |
| `FlushIntervalMs` | 5000 | Force flush interval |
| `MaxRetryAttempts` | 3 | Retry attempts for failed writes |
| `RetryDelayMs` | 1000 | Initial retry delay (exponential backoff) |
| `MaxRetryDelayMs` | 30000 | Maximum retry delay |
| `EnableDeadLetterQueue` | true | Save failed batches for retry |
| `DeadLetterQueuePath` | null | Custom path for dead letter storage |
| `ShutdownTimeoutSeconds` | 30 | Graceful shutdown timeout |
| `EnableSsl` | false | Enable SSL connection |
| `TimeoutSeconds` | 30 | Connection timeout |
| `MaxPoolSize` | 20 | Maximum database connections |
| `MinPoolSize` | 2 | Minimum database connections |
| `Tags` | {} | Additional tags for all measurements |

### Example TimescaleDB Configuration

```json
{
  "TimescaleDb": {
    "Host": "localhost",
    "Port": 5433,
    "Database": "adam_counters",
    "Username": "adam_user",
    "Password": "${TIMESCALE_PASSWORD}",
    "TableName": "counter_data",
    "BatchSize": 50,
    "FlushIntervalMs": 5000,
    "EnableDeadLetterQueue": true,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "Tags": {
      "location": "factory_floor",
      "environment": "production",
      "plant": "plant_01"
    }
  }
}
```

## Windowed Rate Calculation (NEW)

The application now uses windowed rate calculation for more stable and reliable rate metrics:

### How It Works

1. **Circular Buffer**: Stores recent readings for each channel (up to 200 readings)
2. **Time Window**: Calculates rate over a configurable time period (e.g., 60 seconds)
3. **Smooth Metrics**: Eliminates spikes from brief stoppages or single-point calculations
4. **Overflow Detection**: Automatically handles 16-bit and 32-bit counter wraparounds

### Configuration

Configure the rate window per channel:

```json
{
  "Channels": [
    {
      "ChannelNumber": 0,
      "Name": "ProductionCounter",
      "RateWindowSeconds": 60,  // 1-minute window for production
      "MaxChangeRate": 1000      // Alert if rate exceeds 1000/sec
    },
    {
      "ChannelNumber": 1,
      "Name": "RejectCounter",
      "RateWindowSeconds": 180, // 3-minute window for rejects
      "MaxChangeRate": 100       // Alert if reject rate exceeds 100/sec
    }
  ]
}
```

### Benefits

- **Stability**: Rate doesn't drop to zero during brief stoppages
- **Accuracy**: Better represents actual production throughput
- **Flexibility**: Different windows for different metrics
- **Reliability**: Handles counter overflows seamlessly

## Dead Letter Queue (NEW)

Ensures no data loss during database outages:

### Features

- **Automatic Retry**: Failed writes are queued and retried
- **Persistent Storage**: Survives application restarts
- **Exponential Backoff**: Intelligent retry timing
- **Recovery Mode**: Processes queued data when connection restored

### Configuration

```json
{
  "TimescaleDb": {
    "EnableDeadLetterQueue": true,
    "DeadLetterQueuePath": "/app/data/dead-letter",
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "MaxRetryDelayMs": 30000
  }
}
```

## Complete Configuration Examples

### Local Development Setup

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Industrial.Adam.Logger.Core": "Debug"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 5000,
    "HealthCheckIntervalMs": 10000,
    "DemoMode": false,
    "Devices": [
      {
        "DeviceId": "DEV-001",
        "Name": "Test Device",
        "IpAddress": "localhost",
        "Port": 5502,
        "UnitId": 1,
        "Enabled": true,
        "PollIntervalMs": 2000,
        "TimeoutMs": 3000,
        "MaxRetries": 2,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "Test Counter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "Unit": "items"
          }
        ]
      }
    ],
    "InfluxDb": {
      "Url": "http://localhost:8086",
      "Token": "dev-token-12345",
      "Organization": "dev_org",
      "Bucket": "dev_counters",
      "MeasurementName": "counter_data",
      "BatchSize": 10,
      "BatchTimeoutMs": 5000,
      "FlushIntervalMs": 5000,
      "EnableGzip": false,
      "Tags": {
        "environment": "development"
      }
    }
  }
}
```

### Production Setup

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Industrial.Adam.Logger.Core": "Information"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 1000,
    "HealthCheckIntervalMs": 30000,
    "DemoMode": false,
    "Devices": [
      {
        "DeviceId": "ADAM001",
        "Name": "Production Line 1 - Station A",
        "IpAddress": "192.168.10.101",
        "Port": 502,
        "UnitId": 1,
        "Enabled": true,
        "PollIntervalMs": 1000,
        "TimeoutMs": 3000,
        "MaxRetries": 3,
        "KeepAlive": true,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "Product Counter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "Unit": "items",
            "MinValue": 0,
            "MaxValue": 4294967295,
            "MaxChangeRate": 1000
          },
          {
            "ChannelNumber": 1,
            "Name": "Reject Counter", 
            "StartRegister": 2,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "Unit": "items",
            "MinValue": 0,
            "MaxValue": 4294967295,
            "MaxChangeRate": 100
          }
        ]
      },
      {
        "DeviceId": "ADAM002", 
        "Name": "Production Line 1 - Station B",
        "IpAddress": "192.168.10.102",
        "Port": 502,
        "UnitId": 1,
        "Enabled": true,
        "PollIntervalMs": 1000,
        "TimeoutMs": 3000,
        "MaxRetries": 3,
        "KeepAlive": true,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "Assembly Counter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "Unit": "assemblies",
            "MaxChangeRate": 500
          }
        ]
      }
    ],
    "InfluxDb": {
      "Url": "http://influxdb.factory.local:8086",
      "Token": "production-token-supersecret",
      "Organization": "factory_operations", 
      "Bucket": "production_counters",
      "MeasurementName": "adam_counters",
      "BatchSize": 100,
      "BatchTimeoutMs": 5000,
      "FlushIntervalMs": 5000,
      "EnableGzip": true,
      "TimeoutSeconds": 30,
      "Tags": {
        "location": "factory_floor",
        "environment": "production",
        "plant": "main_facility",
        "line": "line_01"
      }
    }
  }
}
```

## Environment Variables

You can override configuration using environment variables:

```bash
# InfluxDB settings
export AdamLogger__InfluxDb__Url="http://influxdb:8086"
export AdamLogger__InfluxDb__Token="my-secret-token"
export AdamLogger__InfluxDb__Organization="my-org"
export AdamLogger__InfluxDb__Bucket="my-bucket"

# Global settings
export AdamLogger__GlobalPollIntervalMs=2000
export AdamLogger__DemoMode=false

# Logging
export Logging__LogLevel__Default="Warning"
export Logging__LogLevel__Industrial.Adam.Logger.Core="Information"
```

## Configuration Validation

The application validates configuration at startup and provides helpful error messages:

### Common Configuration Errors

1. **Missing AdamLogger section**
   ```
   Missing 'AdamLogger' configuration section in appsettings.json
   ```

2. **InfluxDB at wrong location**
   ```
   InfluxDB configuration found at root level but should be nested under 'AdamLogger'
   ```

3. **Missing required fields**
   ```
   InfluxDB token cannot be empty. Configure 'AdamLogger:InfluxDb:Token' in appsettings.json
   ```

4. **Invalid IP address format**
   ```
   Invalid IP address or hostname for device ADAM001: 'invalid-ip'
   ```

### Validation Features

- ✅ Configuration structure validation
- ✅ Required field checking
- ✅ IP address/hostname validation
- ✅ Port and range validation
- ✅ Channel conflict detection
- ✅ Helpful error messages with fix suggestions

## Configuration Files

### File Locations

The application looks for configuration files in this order:

1. `appsettings.json` (in application directory)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Command line arguments

### Development vs Production

**Development** (`appsettings.Development.json`):
- Higher log levels (Debug)
- Local InfluxDB
- Shorter intervals
- Test device IDs

**Production** (`appsettings.json`):
- Reduced logging
- Production InfluxDB server
- Optimized intervals
- Real device IDs

## Troubleshooting Configuration

### Problem: "Configuration validation failed"

**Symptoms**: Application fails to start with configuration error

**Solution**: 
1. Check your `appsettings.json` structure
2. Ensure `InfluxDb` is nested under `AdamLogger`
3. Verify all required fields are present

### Problem: "Invalid IP address for device"

**Symptoms**: Device validation fails

**Solutions**:
- Use IP address: `192.168.1.100`
- Use localhost: `localhost` 
- Use hostname: `adam-device-01`
- Don't use: invalid formats

### Problem: "InfluxDB connection failed"

**Symptoms**: Application starts but can't connect to InfluxDB

**Solutions**:
1. Verify InfluxDB is running
2. Check URL is correct
3. Validate token has proper permissions
4. Ensure organization/bucket exist

### Problem: "No data appearing in InfluxDB"

**Symptoms**: Application runs but no data is stored

**Solutions**:
1. Check device connectivity
2. Verify Modbus communication
3. Check channel configuration
4. Review application logs

## Best Practices

### Security
- Store tokens in environment variables for production
- Use read-only InfluxDB tokens when possible
- Restrict network access to necessary ports
- Use HTTPS for InfluxDB in production

### Performance
- Use appropriate polling intervals (1-5 seconds)
- Configure batch sizes based on data volume
- Enable compression for remote InfluxDB
- Monitor application logs for warnings

### Reliability  
- Configure proper timeouts and retries
- Use TCP keep-alive for stable connections
- Monitor device health status
- Set up log rotation for production

### Monitoring
- Add meaningful tags for filtering
- Use descriptive device and channel names
- Configure proper log levels
- Set up alerts for device failures