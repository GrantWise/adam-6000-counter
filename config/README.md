# Configuration Templates

This directory contains configuration templates for different deployment scenarios.

## Available Templates

### 1. `appsettings.template.json`
**Purpose**: General production template
**Usage**: Copy to `src/Industrial.Adam.Logger.Console/appsettings.json` and customize

**Key Features**:
- Production-ready polling intervals (1 second)
- Two-channel ADAM-6051 configuration
- Standard InfluxDB settings
- Placeholder values that need customization

**Required Customizations**:
- Replace `your-influxdb-token-here` with actual InfluxDB token
- Replace `your-organization-name` with actual organization
- Update device IP addresses
- Modify device IDs and names

### 2. `appsettings.local.json`
**Purpose**: Local development with simulator
**Usage**: Perfect for testing with the included ADAM simulator

**Key Features**:
- Connects to localhost simulator on port 5502
- Debug logging enabled
- Lower batch sizes for immediate feedback
- Pre-configured with working InfluxDB settings for local development

**Usage**:
```bash
# Copy to console app directory
cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json

# Start local InfluxDB (if not already running)
docker run -d --name adam-influxdb -p 8086:8086 \
  -e DOCKER_INFLUXDB_INIT_MODE=setup \
  -e DOCKER_INFLUXDB_INIT_USERNAME=admin \
  -e DOCKER_INFLUXDB_INIT_PASSWORD=password123 \
  -e DOCKER_INFLUXDB_INIT_ORG=adam_org \
  -e DOCKER_INFLUXDB_INIT_BUCKET=adam_counters \
  -e DOCKER_INFLUXDB_INIT_ADMIN_TOKEN=adam-super-secret-token \
  influxdb:2.7.12

# Start simulator
./scripts/start-simulators.sh

# Run logger
cd src/Industrial.Adam.Logger.Console && dotnet run
```

### 3. `appsettings.docker.json`  
**Purpose**: Docker containerized deployment
**Usage**: Use with Docker Compose setups

**Key Features**:
- Uses Docker service names (e.g., `influxdb:8086`)
- Environment variable placeholders
- Optimized for container networking
- Production logging levels

**Environment Variables Required**:
```bash
export INFLUXDB_TOKEN="your-production-token"
export INFLUXDB_ORG="your-organization"
export INFLUXDB_BUCKET="production_counters"
```

## Quick Setup Guide

### For Local Development:
1. Copy local template: `cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json`
2. Start InfluxDB and simulator (see local template section above)
3. Run the application

### For Production:
1. Copy production template: `cp config/appsettings.template.json src/Industrial.Adam.Logger.Console/appsettings.json`
2. Edit the copied file:
   - Replace `your-influxdb-token-here` with actual token
   - Replace `your-organization-name` with actual organization
   - Update device IP addresses and IDs
   - Adjust polling intervals as needed
3. Deploy and run

### For Docker:
1. Use `appsettings.docker.json` in your Docker image
2. Set required environment variables
3. Deploy with Docker Compose

## Configuration Field Reference

See [../docs/configuration-guide.md](../docs/configuration-guide.md) for detailed information about all configuration options.

## Common Customizations

### Adding More Devices:
```json
{
  "AdamLogger": {
    "Devices": [
      {
        "DeviceId": "ADAM001",
        "IpAddress": "192.168.1.100",
        // ... first device config
      },
      {
        "DeviceId": "ADAM002", 
        "IpAddress": "192.168.1.101",
        // ... second device config
      }
    ]
  }
}
```

### Adjusting Polling Rates:
```json
{
  "AdamLogger": {
    "GlobalPollIntervalMs": 2000,  // Global default
    "Devices": [
      {
        "DeviceId": "ADAM001",
        "PollIntervalMs": 1000,    // Override for this device
        // ...
      }
    ]
  }
}
```

### Production InfluxDB Settings:
```json
{
  "InfluxDb": {
    "Url": "https://influxdb.company.com:8086",
    "Token": "production-token-from-vault",
    "Organization": "manufacturing_division",
    "Bucket": "production_line_1_counters",
    "BatchSize": 200,
    "EnableGzip": true
  }
}
```

## Validation

All templates are validated to ensure:
- ✅ Correct JSON syntax
- ✅ Required fields present
- ✅ Valid configuration structure
- ✅ Reasonable default values

The application will validate your configuration at startup and provide helpful error messages if any issues are found.