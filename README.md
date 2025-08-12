# Industrial ADAM Logger

A robust, maintainable service for logging data from ADAM-6051 industrial counter devices to InfluxDB. Built for 24/7 industrial operation with comprehensive error handling and automatic recovery.

## Overview

The Industrial ADAM Logger connects to ADAM-6051 devices via Modbus TCP, collects counter values from configured channels, and stores them in InfluxDB for monitoring and analysis. Designed specifically for industrial environments where reliability is critical.

### Key Features

- **Reliable ADAM Device Communication**: Robust Modbus TCP connection with automatic retry
- **Concurrent Device Polling**: Efficiently polls multiple devices simultaneously
- **InfluxDB Integration**: Optimized time-series data storage
- **Industrial-Grade Error Handling**: Comprehensive error recovery for 24/7 operation
- **Docker Deployment**: Production-ready containerized deployment
- **Simple Configuration**: JSON-based configuration for easy management
- **Standard .NET Logging**: Uses Microsoft.Extensions.Logging (no Serilog dependency)
- **Clean Architecture**: Maintainable codebase following SOLID principles
- **Multi-Language Support**: Both C# (.NET 9) and Python implementations available

## Repository Structure

```
adam-6000-counter/
├── README.md                     # This file - overview and getting started
├── CLAUDE.md                     # AI assistant development guidelines  
├── Industrial.Adam.Logger.sln    # Main C# solution
├── src/                          # Source code
│   ├── Industrial.Adam.Logger.Core/          # Core library (V2)
│   ├── Industrial.Adam.Logger.Core.Tests/    # Unit tests
│   ├── Industrial.Adam.Logger.Console/       # Console application (Docker entry point)
│   ├── Industrial.Adam.Logger.WebApi/        # REST API
│   ├── Industrial.Adam.Logger.Simulator/     # ADAM device simulator
│   └── Industrial.Adam.Logger.IntegrationTests/
├── docker/                       # Docker deployment
│   ├── docker-compose.yml        # Main stack (InfluxDB + Grafana + Logger)
│   ├── docker-compose.simulator.yml  # Simulator for testing
│   ├── config/                   # Configuration files
│   └── csharp/                   # Dockerfile and scripts
├── python/                       # Python implementation
│   ├── adam_counter_logger.py    # Python logger
│   └── adam_config_json.json     # Configuration example
├── scripts/                      # Development and testing scripts
│   ├── setup-dev-environment.sh  # One-click development setup
│   ├── start-simulators.sh       # Start ADAM device simulators
│   ├── test-simulators.sh        # Test simulator connectivity
│   └── install-dotnet9.sh        # Install .NET 9 SDK
└── docs/                         # Documentation
```

## 🚀 New Features in V2

### Windowed Rate Calculation
- **Configurable Time Windows**: Set per-channel rate calculation windows (e.g., 60 seconds for production, 180 seconds for rejects)
- **Smooth Rate Metrics**: Eliminates spikes from brief stoppages or single-point calculations
- **Counter Overflow Detection**: Automatic handling of 16-bit and 32-bit counter wraparounds
- **Circular Buffer Storage**: Efficient memory usage with automatic cleanup of old readings

### Data Reliability
- **Dead Letter Queue**: Failed database writes are queued and retried automatically
- **Persistent Storage**: Failed batches saved to disk to survive application restarts
- **Automatic Recovery**: Processes queued data when database connection is restored
- **No Data Loss**: Ensures critical production data is never lost

### Development Tools
- **One-Click Setup**: Run `./scripts/setup-dev-environment.sh` for complete environment
- **Simulator Testing**: Built-in ADAM device simulators for development without hardware
- **Test Coverage**: Comprehensive unit tests with isolated test mode for components
- **.NET 9 Performance**: Latest runtime optimizations for industrial workloads

## Quick Start

### 🎯 Automated Development Setup (New!)

```bash
# One-command setup for complete development environment
./scripts/setup-dev-environment.sh

# This script will:
# ✅ Install .NET 9 SDK if needed
# ✅ Start TimescaleDB and create database/tables
# ✅ Start Grafana with pre-configured dashboards
# ✅ Start Prometheus for metrics monitoring
# ✅ Launch 3 ADAM device simulators
# ✅ Build and start the logger application
# ✅ Verify everything is working
```

### 🐳 Docker Deployment (Production)

**Complete monitoring stack with TimescaleDB + Grafana + Prometheus:**

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/adam-6000-counter.git
cd adam-6000-counter

# 2. Start the monitoring infrastructure
cd docker
docker-compose up -d

# 3. Verify services are running
docker-compose ps

# 4. Access the dashboards
# - Grafana: http://localhost:3002 (admin/admin)
# - TimescaleDB: postgresql://localhost:5433 (adam_user/adam_password)
# - Prometheus: http://localhost:9090
```

### 🎮 **Demo Mode with Simulator (No Hardware Required)**
```bash
# Start with simulated ADAM devices for testing
cd docker
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up -d

# View logs to see simulated data being processed
docker-compose logs -f adam-logger
```

### 🏭 **Production Mode (Real Devices)**
```bash
# Edit docker/config/adam_config_v2.json with your device settings
# Then start the stack
docker-compose up -d

# View logs to see real device data
docker-compose logs -f adam-logger
```

### 💻 Local Development

#### Python Implementation (Lightweight)

```bash
cd python/
pip install pymodbus influxdb-client
python adam_counter_logger.py
```

#### C# Implementation

```bash
# Build the solution
dotnet build

# Run the console application
dotnet run --project src/Industrial.Adam.Logger.Console

# Run with custom config
dotnet run --project src/Industrial.Adam.Logger.Console -- --config myconfig.json
```

## Implementation Comparison

### Python Implementation
- **Lightweight**: Single-file implementation for quick deployment
- **Simple Setup**: Minimal dependencies and configuration
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Dependencies**: PyModbus, InfluxDB Client
- **Use Cases**: Development, testing, simple installations

### C# Implementation
- **Production-Ready**: Designed for 24/7 industrial operation
- **Clean Architecture**: Maintainable codebase with SOLID principles
- **Comprehensive Testing**: Unit and integration test coverage
- **Docker Support**: Production-ready containerized deployment
- **Concurrent Polling**: Efficient multi-device support
- **Standard Logging**: Microsoft.Extensions.Logging integration
- **Use Cases**: Production environments, multi-device deployments

## Configuration

📖 **For detailed configuration information, see [Configuration Guide](docs/configuration-guide.md)**

📋 **Ready-to-use templates available in [`config/`](config/) directory**

Configuration uses standard .NET JSON format with new windowed rate calculation settings:

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
        "DeviceId": "Device001",
        "IpAddress": "192.168.1.100",
        "Port": 502,
        "UnitId": 1,
        "Enabled": true,
        "PollIntervalMs": 5000,
        "TimeoutMs": 3000,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "ProductionCounter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "MinValue": 0,
            "MaxValue": 4294967295,
            "MaxChangeRate": 1000,
            "RateWindowSeconds": 60  // NEW: Windowed rate calculation
          },
          {
            "ChannelNumber": 1,
            "Name": "RejectCounter",
            "StartRegister": 2,
            "RegisterCount": 2,
            "Enabled": true,
            "ScaleFactor": 1.0,
            "MaxChangeRate": 100,
            "RateWindowSeconds": 180  // NEW: Longer window for reject analysis
          }
        ]
      }
    ],
    "TimescaleDb": {  // NEW: Replaced InfluxDB with TimescaleDB
      "Host": "localhost",
      "Port": 5433,
      "Database": "adam_counters",
      "Username": "adam_user",
      "Password": "adam_password",
      "TableName": "counter_data",
      "BatchSize": 50,
      "FlushIntervalMs": 5000,
      "EnableDeadLetterQueue": true,  // NEW: Automatic retry for failed writes
      "MaxRetryAttempts": 3,
      "RetryDelayMs": 1000
    }
  }
}
```

## 🐳 Docker Deployment

### Prerequisites

- Docker and Docker Compose installed
- ADAM-6051 device accessible on your network
- Ports 3000 (Grafana) and 8086 (InfluxDB) available

### Infrastructure Components

The Docker stack includes:

- **TimescaleDB 2.17**: PostgreSQL-based time-series database optimized for industrial data
  - Hypertables for automatic data partitioning
  - Compression for efficient storage
  - Continuous aggregates for real-time analytics
- **Grafana 12.0**: Real-time dashboard and visualization
  - Pre-configured dashboards for counter metrics
  - Rate calculations and production analytics
- **ADAM Logger**: C# .NET 9 application with advanced features
  - Windowed rate calculation with configurable windows
  - Dead letter queue for data reliability
  - Circular buffer for efficient memory usage
- **Prometheus 2.47**: Metrics collection and monitoring
  - Application health metrics
  - System resource monitoring
- **ADAM Simulator**: Full-featured device simulator
  - Realistic production patterns
  - Configurable production profiles
  - Multiple simulator support

### Setup Instructions

1. **Clone and Navigate:**
   ```bash
   git clone https://github.com/yourusername/adam-6000-counter.git
   cd adam-6000-counter/docker
   ```

2. **Configure Your Devices:**
   ```bash
   # Edit the configuration file
   nano config/adam_config_v2.json
   
   # Or use the demo configuration
   cp config/adam_config_demo.json config/adam_config_v2.json
   ```

3. **Start the Stack:**
   ```bash
   # Production mode (with real devices)
   docker-compose up -d
   
   # Or demo mode with simulator
   docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up -d
   
   # View logs
   docker-compose logs -f
   ```

4. **Access Services:**
   - **Grafana Dashboard**: http://localhost:3002
     - Username: `admin`
     - Password: `admin`
   - **InfluxDB Console**: http://localhost:8086
     - Username: `admin`
     - Password: `admin123`
   - **Prometheus**: http://localhost:9090

### Data Flow

```
ADAM Devices → Logger Service → InfluxDB → Grafana Dashboard
  (Modbus)      (Polling)     (Storage)    (Visualization)
```

### Docker Commands

```bash
# Start services
docker-compose up -d

# Stop services  
docker-compose down

# View logs
docker-compose logs grafana
docker-compose logs influxdb

# Restart a service
docker-compose restart adam-logger

# Update and rebuild
docker-compose pull && docker-compose up -d
```

### Troubleshooting

#### Configuration Issues

**❌ "Configuration validation failed"**

This error indicates problems with your `appsettings.json` structure:

```bash
# Symptoms
❌ Configuration Error
═══════════════════════
Configuration validation failed:
• Missing 'AdamLogger:InfluxDb' configuration section
```

**Solutions:**
- ✅ Use configuration templates: `cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json`
- ✅ Ensure InfluxDB settings are nested: `AdamLogger > InfluxDb`
- ✅ Verify all required fields are present (Token, Organization, Bucket)

**❌ "Invalid IP address for device"**

The application rejected your device IP address:

```bash
# Symptoms  
Invalid IP address or hostname for device ADAM001: 'localhost'
```

**Solutions:**
- ✅ Use `localhost` (now supported)
- ✅ Use IP addresses: `192.168.1.100`  
- ✅ Use hostnames: `adam-device-01`
- ❌ Don't use invalid formats

**❌ "InfluxDB connection failed"**

The logger can't connect to InfluxDB:

```bash
# Check InfluxDB status
docker ps | grep influx

# Verify InfluxDB is accessible
curl http://localhost:8086/health

# Check logs for connection details
docker logs adam-influxdb
```

**Solutions:**
- ✅ Ensure InfluxDB is running: `docker start adam-influxdb`
- ✅ Check token permissions in InfluxDB UI
- ✅ Verify organization and bucket exist

#### Service Issues

**Services won't start:**
```bash
# Check port conflicts  
netstat -tulpn | grep -E ':(3002|8086|9090)'

# View detailed logs
docker-compose logs

# Check for configuration errors
cd src/Industrial.Adam.Logger.Console && dotnet run
```

**Can't connect to ADAM device:**
```bash
# Check configuration
docker-compose exec adam-logger cat /app/appsettings.json

# Test network connectivity
ping 192.168.1.100

# Try with simulator first
./scripts/start-simulators.sh
```

**Dashboard shows no data:**
1. ✅ Verify device connectivity: Check application logs
2. ✅ Check InfluxDB: http://localhost:8086 → Data Explorer
3. ✅ Verify logger is running: `docker-compose logs adam-logger`
4. ✅ Check Grafana datasource is configured

#### Quick Fixes

**For Local Development:**
```bash
# Use working local configuration
cp config/appsettings.local.json src/Industrial.Adam.Logger.Console/appsettings.json

# Start InfluxDB with correct settings
docker run -d --name adam-influxdb -p 8086:8086 \
  -e DOCKER_INFLUXDB_INIT_MODE=setup \
  -e DOCKER_INFLUXDB_INIT_USERNAME=admin \
  -e DOCKER_INFLUXDB_INIT_PASSWORD=password123 \
  -e DOCKER_INFLUXDB_INIT_ORG=adam_org \
  -e DOCKER_INFLUXDB_INIT_BUCKET=adam_counters \
  -e DOCKER_INFLUXDB_INIT_ADMIN_TOKEN=adam-super-secret-token \
  influxdb:2.7.12

# Test with simulator
./scripts/start-simulators.sh
```

**For Production Issues:**
```bash  
# Validate configuration without running
cd src/Industrial.Adam.Logger.Console && dotnet run --validate-config

# Check detailed error information  
cd src/Industrial.Adam.Logger.Console && dotnet run --verbosity detailed
```

#### Getting Help

📖 **Detailed Configuration Guide**: [`docs/configuration-guide.md`](docs/configuration-guide.md)
📋 **Configuration Templates**: [`config/`](config/) directory  
🔧 **Example Configurations**: See templates for different scenarios

## Key Capabilities

- **Multi-Device Support**: Poll multiple ADAM devices concurrently
- **Flexible Channel Configuration**: Configure any combination of device channels
- **Industrial Reliability**: Automatic reconnection and error recovery
- **Real-time Data**: Continuous polling with configurable intervals
- **Data Validation**: Counter overflow detection and quality tracking
- **Docker Ready**: Production-ready containerized deployment
- **REST API**: Optional WebAPI for integration with other systems

## Testing

Run the test suite:
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Test with simulator
cd docker
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up
```

## Documentation

- **[README.md](README.md)**: This overview document
- **[CLAUDE.md](CLAUDE.md)**: AI assistant development guidelines
- **[Configuration Guide](docs/configuration-guide.md)**: Detailed system configuration
- **[Simulator Configuration Guide](docs/simulator-configuration-guide.md)**: Complete simulator setup and configuration
- **[docker/README.md](docker/README.md)**: Docker deployment guide
- **[src/Industrial.Adam.Logger.Simulator/README.md](src/Industrial.Adam.Logger.Simulator/README.md)**: Simulator technical documentation
- **[python/README.md](python/README.md)**: Python implementation guide

## Architecture

The project follows Clean Architecture principles with clear separation of concerns:

- **Core Library**: Business logic and domain models
- **Infrastructure**: Device communication and data storage
- **Application Layer**: Console app and WebAPI
- **Testing**: Comprehensive unit and integration tests

## Support

For questions, issues, or contributions:

1. **Issues**: Use the repository issue tracker
2. **Documentation**: Refer to the comprehensive documentation files
3. **Examples**: Check the examples folder for implementation patterns
4. **Tests**: Review the test files for usage examples and edge cases

## License

This project is designed for industrial automation and logging applications. Please ensure compliance with your organization's security and operational requirements.