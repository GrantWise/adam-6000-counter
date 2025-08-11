# Configuration Guide

This guide provides comprehensive information about configuring the Industrial ADAM Logger application.

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
    "InfluxDb": {
      "Url": "http://localhost:8086",
      "Token": "your-influxdb-token",
      "Organization": "your-org",
      "Bucket": "adam_counters"
    }
  }
}
```

## Configuration Structure

### Important: Configuration Nesting

⚠️ **Critical**: InfluxDB settings must be nested under `AdamLogger:InfluxDb`, not at the root level.

**✅ Correct Structure:**
```json
{
  "AdamLogger": {
    "Devices": [...],
    "InfluxDb": {
      "Url": "...",
      "Token": "..."
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
  "InfluxDb": {
    "Url": "...",
    "Token": "..."
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

## InfluxDB Configuration

### Required Settings

| Property | Required | Description | How to Get |
|----------|----------|-------------|------------|
| `Url` | ✅ | InfluxDB server URL | Usually `http://localhost:8086` for local setup |
| `Token` | ✅ | Authentication token | InfluxDB UI → Data → Tokens → Generate Token |
| `Organization` | ✅ | Organization name | InfluxDB UI → User menu → About |
| `Bucket` | ✅ | Data bucket name | InfluxDB UI → Data → Buckets → Create Bucket |

### Optional Settings

| Property | Default | Description |
|----------|---------|-------------|
| `MeasurementName` | "counter_data" | InfluxDB measurement name |
| `BatchSize` | 100 | Number of points per batch |
| `BatchTimeoutMs` | 5000 | Maximum time to wait before sending batch |
| `FlushIntervalMs` | 5000 | Force flush interval |
| `EnableGzip` | true | Enable compression |
| `TimeoutSeconds` | 30 | Connection timeout |
| `Tags` | {} | Additional tags for all measurements |

### Example InfluxDB Configuration

```json
{
  "InfluxDb": {
    "Url": "http://localhost:8086",
    "Token": "your-super-secret-admin-token",
    "Organization": "my_company",
    "Bucket": "production_counters",
    "MeasurementName": "adam_counters",
    "BatchSize": 50,
    "BatchTimeoutMs": 10000,
    "FlushIntervalMs": 10000,
    "EnableGzip": true,
    "TimeoutSeconds": 30,
    "Tags": {
      "location": "factory_floor",
      "environment": "production",
      "plant": "plant_01"
    }
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