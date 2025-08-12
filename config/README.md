# Configuration Templates

This directory contains configuration templates for different deployment scenarios with TimescaleDB integration and advanced features.

## Available Templates

### 1. `appsettings.template.json`
**Purpose**: General production template with windowed rate calculation
**Usage**: Copy to `src/Industrial.Adam.Logger.Console/appsettings.json` and customize

**Key Features**:
- Production-ready polling intervals (5 seconds)
- Two-channel ADAM-6051 configuration
- TimescaleDB settings with dead letter queue
- Windowed rate calculation configuration
- Placeholder values that need customization

**Required Customizations**:
- Update TimescaleDB connection settings
- Replace device IP addresses with actual device IPs
- Modify device IDs and names
- Adjust rate window settings per channel

### 2. `appsettings.local.json`
**Purpose**: Local development with simulator
**Usage**: Perfect for testing with the included ADAM simulator

**Key Features**:
- Connects to localhost simulators on ports 5502-5504
- Debug logging enabled
- Lower batch sizes for immediate feedback
- Pre-configured TimescaleDB settings for local development
- Windowed rate calculation enabled

**Usage**:
```bash
# Copy to console app directory
cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json

# Start local TimescaleDB (if not already running)
docker run -d --name adam-timescaledb -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=adam_user \
  -e POSTGRES_PASSWORD=adam_password \
  timescale/timescaledb:2.17.2-pg17

# Start simulators
./scripts/start-simulators.sh

# Run logger
cd src/Industrial.Adam.Logger.Console && dotnet run
```

### 3. `appsettings.docker.json`  
**Purpose**: Docker containerized deployment
**Usage**: Use with Docker Compose setups

**Key Features**:
- Uses Docker service names (e.g., `timescaledb:5432`)
- Environment variable placeholders
- Optimized for container networking
- Production logging levels
- Dead letter queue enabled for reliability

**Environment Variables Required**:
```bash
export TIMESCALE_HOST="timescaledb"
export TIMESCALE_USER="adam_user"
export TIMESCALE_PASSWORD="your-secure-password"
export TIMESCALE_DATABASE="adam_counters"
```

## 🚀 Quick Setup Guide

### For Automated Development Setup:
```bash
# One-command setup
./scripts/setup-dev-environment.sh
```

### For Local Development:
1. Copy local template: `cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json`
2. Start TimescaleDB and simulator (see local template section above)
3. Run the application

### For Production:
1. Copy production template: `cp config/appsettings.template.json src/Industrial.Adam.Logger.Console/appsettings.json`
2. Edit the copied file:
   - Update TimescaleDB connection settings
   - Replace device IP addresses and IDs
   - Configure rate windows per channel
   - Enable/configure dead letter queue
3. Deploy and run

### For Docker:
1. Use `appsettings.docker.json` in your Docker image
2. Set required environment variables
3. Deploy with Docker Compose

## 📋 Configuration Field Reference

See [../docs/configuration-guide.md](../docs/configuration-guide.md) for detailed information about all configuration options.

## 🎯 New Features Configuration

### Windowed Rate Calculation:
```json
{
  "Channels": [
    {
      "ChannelNumber": 0,
      "Name": "ProductionCounter",
      "RateWindowSeconds": 60,    // 1-minute window
      "MaxChangeRate": 1000        // Alert threshold
    },
    {
      "ChannelNumber": 1,
      "Name": "RejectCounter",
      "RateWindowSeconds": 180,   // 3-minute window
      "MaxChangeRate": 100
    }
  ]
}
```

### Dead Letter Queue:
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
    "GlobalPollIntervalMs": 5000,  // Global default
    "Devices": [
      {
        "DeviceId": "ADAM001",
        "PollIntervalMs": 2000,     // Override for this device
        // ...
      }
    ]
  }
}
```

### Production TimescaleDB Settings:
```json
{
  "TimescaleDb": {
    "Host": "timescale.company.com",
    "Port": 5432,
    "Database": "production_counters",
    "Username": "prod_user",
    "Password": "${TIMESCALE_PASSWORD}",  // From environment
    "EnableSsl": true,
    "BatchSize": 100,
    "FlushIntervalMs": 10000,
    "EnableDeadLetterQueue": true,
    "MaxPoolSize": 50,
    "Tags": {
      "environment": "production",
      "plant": "plant_01"
    }
  }
}
```

## Performance Optimization

### For High-Frequency Data:
```json
{
  "AdamLogger": {
    "GlobalPollIntervalMs": 1000,    // 1 second polling
    "Devices": [{
      "PollIntervalMs": 500,         // 500ms for critical device
      "Channels": [{
        "RateWindowSeconds": 30,      // Shorter window for responsiveness
        "MaxChangeRate": 5000
      }]
    }]
  },
  "TimescaleDb": {
    "BatchSize": 200,                // Larger batches
    "BatchTimeoutMs": 2000,           // Shorter timeout
    "MaxPoolSize": 50                 // More connections
  }
}
```

### For Stable, Low-Frequency Data:
```json
{
  "AdamLogger": {
    "GlobalPollIntervalMs": 10000,   // 10 second polling
    "Devices": [{
      "Channels": [{
        "RateWindowSeconds": 300,     // 5-minute window
        "MaxChangeRate": 100
      }]
    }]
  },
  "TimescaleDb": {
    "BatchSize": 50,                 // Smaller batches
    "BatchTimeoutMs": 30000,          // Longer timeout
    "MaxPoolSize": 10                 // Fewer connections
  }
}
```

## Validation

All templates are validated to ensure:
- ✅ Correct JSON syntax
- ✅ Required fields present
- ✅ Valid configuration structure
- ✅ Reasonable default values
- ✅ Valid rate window settings
- ✅ Proper TimescaleDB connection parameters

The application will validate your configuration at startup and provide helpful error messages if any issues are found.