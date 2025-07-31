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
└── docs/                         # Documentation
```

## Quick Start

### 🐳 Docker Deployment (Recommended)

**Complete monitoring stack with InfluxDB + Grafana:**

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
# - InfluxDB: http://localhost:8086 (admin/admin123)
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

Configuration uses standard .NET JSON format:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Industrial.Adam.Logger.Core": "Information"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 2000,
    "Devices": [
      {
        "DeviceId": "Device001",
        "IpAddress": "192.168.1.100",
        "Port": 502,
        "UnitId": 1,
        "Enabled": true,
        "PollIntervalMs": 2000,
        "TimeoutMs": 3000,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "ProductionCounter",
            "RegisterAddress": 0,
            "RegisterCount": 2,
            "Enabled": true
          },
          {
            "ChannelNumber": 1,
            "Name": "QualityCounter",
            "RegisterAddress": 2,
            "RegisterCount": 2,
            "Enabled": true
          }
        ]
      }
    ]
  },
  "InfluxDb": {
    "Url": "http://localhost:8086",
    "Token": "your-token",
    "Organization": "adam_org",
    "Bucket": "adam_counters",
    "Measurement": "counter_data"
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

- **InfluxDB 2.7**: Time-series database for counter data storage
- **Grafana 12.0**: Real-time dashboard and visualization  
- **ADAM Logger**: C# .NET 9 application using Core library (V2)
- **Prometheus**: Metrics collection and monitoring
- **ADAM Simulator**: Device simulator for testing without hardware

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

**Services won't start:**
```bash
# Check port conflicts  
netstat -tulpn | grep -E ':(3002|8086|9090)'

# View detailed logs
docker-compose logs
```

**Can't connect to ADAM device:**
```bash
# Check configuration
docker-compose exec adam-logger cat /app/appsettings.json

# Test with simulator
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up
```

**Dashboard shows no data:**
1. Check InfluxDB: http://localhost:8086
2. Verify logger is running: `docker-compose logs adam-logger`
3. Check Grafana datasource is configured

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
- **[docker/README.md](docker/README.md)**: Docker deployment guide
- **[src/Industrial.Adam.Logger.Simulator/README.md](src/Industrial.Adam.Logger.Simulator/README.md)**: Simulator documentation
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