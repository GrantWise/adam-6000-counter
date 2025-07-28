# Industrial Counter Data Acquisition Platform

A comprehensive, extensible platform for industrial counter data acquisition and monitoring. Built for manufacturing, production lines, and industrial IoT applications. This repository provides enterprise-grade implementations supporting ADAM-6051 devices with extensibility for additional counter device types.

## Overview

The Industrial Counter Data Acquisition Platform connects to industrial counter devices via Modbus TCP, collects counter values from configured channels, and provides comprehensive data processing with industrial-grade reliability. Currently supporting ADAM-6051 devices with an extensible architecture for additional counter device types.

### Key Features

- **Industrial Counter Focus**: Specialized for production counters, quality gates, and manufacturing metrics
- **Extensible Architecture**: Plugin-based design supporting multiple counter device types
- **Enterprise-Grade Reliability**: Designed for 24/7 industrial operation with comprehensive error handling
- **Real-time Processing**: Live counter data with rate calculations and trend analysis
- **Production Monitoring**: Built-in OEE calculations, production line monitoring, and quality tracking
- **Modbus TCP Communication**: Robust connection handling with automatic retry and recovery
- **Data Validation**: Counter-specific validation including overflow detection and rate limiting
- **Time-Series Storage**: Optimized InfluxDB integration with configurable batching and retention
- **Health Monitoring**: Comprehensive system health checks and production metrics
- **Multi-Language Support**: Complete implementations in both Python and C#
- **Testing Framework**: Production test mode with `--test` flag for validation
- **Configuration Management**: JSON-based configuration for devices, channels, and industrial parameters

## Repository Structure

```
industrial-counter-platform/
├── README.md                    # This file - platform overview and getting started
├── CLAUDE.md                    # AI assistant development guidelines
├── EXAMPLES.md                  # Comprehensive C# usage examples and counter scenarios
├── Industrial.Adam.Logger.sln   # Main C# solution
├── src/                         # C# implementation (primary platform)
│   ├── Industrial.Adam.Logger/           # Core counter platform library
│   ├── Industrial.Adam.Logger.Tests/     # Unit tests (183 tests)
│   ├── Industrial.Adam.Logger.IntegrationTests/  # Integration tests
│   └── Industrial.Adam.Logger.Examples/  # Usage examples and counter applications
├── docs/                        # Comprehensive documentation
│   ├── Industrial Data Acquisition Platform Architecture.md  # Platform architecture
│   ├── Industrial-Software-Development-Standards.md         # Development standards
│   ├── counter-device-integration-guide.md  # Device integration guide
│   ├── counter-application-patterns.md      # Application patterns
│   └── TESTING_PLAN.md                      # Testing strategy
├── docker/                      # Docker deployment (InfluxDB + Grafana monitoring)
└── python/                      # Python implementation (lightweight alternative)
    ├── adam_counter_logger.py   # Python counter logger
    └── adam_config_json.json    # Configuration example
```

## Quick Start

### 🐳 Docker Deployment (Recommended)

**Complete industrial monitoring stack with InfluxDB + Grafana:**

```bash
# 1. Clone the repository
git clone https://github.com/GrantWise/industrial-counter-platform.git
cd industrial-counter-platform

# 2. Start the monitoring infrastructure
cd docker
docker-compose up -d

# 3. Verify services are running
docker-compose ps

# 4. Access the dashboards
# - Grafana: http://localhost:3000 (admin/admin)
# - InfluxDB: http://localhost:8086 (admin/admin123)
```

**The industrial counter platform runs automatically in Docker! 🎉**

### 🎮 **Demo Mode (No Hardware Required)**
```bash
# Start with mock production counter data for testing/demo
echo "DEMO_MODE=true" > docker/.env
docker-compose up -d

# View logs to see mock production line counter data being generated
docker-compose logs -f adam-logger
```

### 🏭 **Production Mode (Real Counter Devices)**
```bash
# Configure for your actual production line counters
echo "DEMO_MODE=false" > docker/.env
echo "ADAM_HOST=192.168.1.100" >> docker/.env
echo "ADAM_UNIT_ID=1" >> docker/.env
echo "POLL_INTERVAL=2000" >> docker/.env

# Restart with production counter configuration
docker-compose restart adam-logger

# View logs to see real production counter data
docker-compose logs -f adam-logger
```

### 💻 Local Development

#### Python Implementation (Lightweight)

```bash
cd python/
pip install pymodbus influxdb-client
python adam_counter_logger.py
```

#### C# Implementation (Enterprise Platform)

```bash
dotnet build
dotnet run --project src/Industrial.Adam.Logger.Examples

# Test production readiness
dotnet run --project src/Industrial.Adam.Logger.Examples -- --test
```

## Implementation Comparison

### Python Implementation
- **Lightweight**: Single-file implementation for smaller counter installations
- **Rapid Deployment**: Quick setup for pilot projects and simple counter monitoring
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Dependencies**: PyModbus, InfluxDB Client
- **Use Cases**: Small-scale production lines, pilot implementations, development

### C# Implementation (Enterprise Platform)
- **Industrial-Grade**: Full platform architecture for enterprise manufacturing
- **Production Testing**: Built-in `--test` mode for production validation
- **Health Monitoring**: Comprehensive system health checks and diagnostics
- **Performance**: Optimized for high-throughput multi-line manufacturing environments
- **Testing**: 183+ unit and integration tests providing comprehensive coverage
- **Architecture**: Clean Architecture with SOLID principles and extensible patterns
- **Scalability**: Designed for large-scale multi-device industrial deployments
- **Monitoring Integration**: Native InfluxDB, Grafana, and Prometheus support
- **Dependency Injection**: Full IoC container support for enterprise integration
- **Observability**: Structured logging, metrics, and industrial error handling
- **Use Cases**: Enterprise manufacturing, multi-line production, OEE systems, MES integration

## Configuration

Industrial counter platform configuration supports comprehensive production line monitoring:

```json
{
  "device_ip": "192.168.1.100",
  "device_port": 502,
  "unit_id": 1,
  "poll_interval": 1000,
  "channels": [
    {
      "channel_number": 0,
      "name": "ProductionLineCounter",
      "description": "Main production line piece count",
      "register_address": 0,
      "register_count": 2,
      "enabled": true,
      "counter_type": "production",
      "oee_calculation": true
    },
    {
      "channel_number": 1,
      "name": "QualityGateCounter", 
      "description": "Quality inspection passed count",
      "register_address": 2,
      "register_count": 2,
      "enabled": true,
      "counter_type": "quality",
      "oee_calculation": true
    },
    {
      "channel_number": 2,
      "name": "RejectCounter",
      "description": "Rejected parts counter",
      "register_address": 4,
      "register_count": 1,
      "enabled": true,
      "counter_type": "reject"
    }
  ],
  "influxdb": {
    "url": "http://localhost:8086",
    "token": "your-token",
    "org": "manufacturing-org", 
    "bucket": "production-data",
    "measurement": "counter_data"
  },
  "oee_settings": {
    "enabled": true,
    "shift_duration_hours": 8,
    "target_rate_per_hour": 1000
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
- **ADAM Logger**: C# .NET 8 application with InfluxDB integration

### Setup Instructions

1. **Clone and Navigate:**
   ```bash
   git clone https://github.com/GrantWise/adam-6051-counter-logger.git
   cd adam-6051-counter-logger/docker
   ```

2. **Configure Environment (Optional):**
   ```bash
   # Create environment file for custom settings
   cp .env.template .env
   
   # Edit with your device settings
   echo "ADAM_HOST=192.168.1.100" >> .env
   echo "ADAM_UNIT_ID=1" >> .env
   echo "POLL_INTERVAL=5.0" >> .env
   ```

3. **Start the Stack:**
   ```bash
   # Start all services
   docker-compose up -d
   
   # View logs
   docker-compose logs -f
   
   # Check service status
   docker-compose ps
   ```

4. **Access Services:**
   - **Grafana Dashboard**: http://localhost:3000
     - Username: `admin`
     - Password: `admin`
   - **InfluxDB Console**: http://localhost:8086
     - Username: `admin`
     - Password: `admin123`
     - Organization: `adam_org`
     - Bucket: `adam_counters`

### Industrial Data Flow

```
Production Line Counters → Industrial Platform (Docker) → InfluxDB → Manufacturing Dashboards
     (ADAM-6051)                      ↓                      ↓
                            Modbus TCP/502              Time-series DB
                         Real-time Processing        production_data
                      
              Enterprise monitoring with:
              • Production rates & OEE calculations
              • Quality metrics & reject rates
              • Multi-line production tracking
              • Device health & connectivity status  
              • System performance & diagnostics
              • Predictive maintenance indicators
              • Shift reports & trend analysis
```

### Configuration

The Docker stack automatically configures the C# logger via environment variables:

```bash
# Device configuration
ADAM_HOST=192.168.1.100        # Your ADAM device IP
ADAM_UNIT_ID=1                 # Modbus unit ID  
POLL_INTERVAL=2000             # Polling interval in ms
LOG_LEVEL=Information          # Logging level

# InfluxDB connection (auto-configured)
# - URL: http://influxdb:8086
# - Token: adam-super-secret-token
# - Organization: adam_org
# - Bucket: adam_counters
```

**For external C# applications**, use this configuration:

```csharp
config.InfluxDb = new InfluxDbConfig
{
    Url = "http://localhost:8086",
    Token = "adam-super-secret-token",
    Organization = "adam_org",
    Bucket = "adam_counters", 
    Measurement = "counter_data"
};
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
netstat -tulpn | grep -E ':(3000|8086)'

# View detailed logs
docker-compose logs
```

**Can't connect to ADAM device:**
```bash
# Test network connectivity
docker-compose exec adam-logger ping 192.168.1.100

# Check device configuration
docker-compose exec adam-logger cat /app/config/adam_config.json
```

**Dashboard shows no data:**
1. Verify InfluxDB has data: http://localhost:8086/orgs/adam_org/data-explorer
2. Check C# logger is writing to InfluxDB
3. Verify Grafana datasource connection

### Persistent Data

Data is automatically persisted in Docker volumes:
- `influxdb_data`: Time-series data storage
- `grafana_data`: Dashboard configurations and user settings

## Industrial Counter Platform Capabilities

### Production Line Data Acquisition
- **Multi-Counter Support**: Configure production, quality, and reject counters per line
- **Register Flexibility**: Support for 16-bit, 32-bit, and 64-bit industrial counters
- **Real-time Rate Calculation**: Automatic production rate calculation with configurable windows
- **Counter Validation**: Industrial-grade validation including overflow detection and rate limiting
- **OEE Integration**: Built-in Overall Equipment Effectiveness calculations

### Manufacturing Reliability
- **Industrial-Grade Connectivity**: Automatic reconnection with exponential backoff for 24/7 operation
- **Comprehensive Error Handling**: Detailed error handling with troubleshooting guidance
- **Data Integrity**: Validation and quality tracking for all production readings
- **Fault Recovery**: Automatic recovery from communication failures and device issues
- **Production Continuity**: Designed for continuous manufacturing environments

### Enterprise Monitoring
- **Production Health Checks**: Built-in monitoring for production lines and counter devices
- **Manufacturing Metrics**: Production rates, OEE, quality metrics, and trend analysis
- **Predictive Alerting**: Configurable alerting for device failures and production anomalies
- **Industrial Diagnostics**: Detailed diagnostic information for production troubleshooting
- **Shift Reporting**: Automated shift reports and production summaries
- **Integration Ready**: APIs for MES, ERP, and other manufacturing systems

## Testing (C# Implementation)

The C# implementation includes comprehensive testing infrastructure:

- **183 Total Tests**: Complete coverage of all components
- **Unit Tests**: 110 tests covering individual components
- **Integration Tests**: 73 tests covering end-to-end scenarios
- **Test Categories**: Configuration, services, data processing, error handling
- **Continuous Integration**: Automated testing on build

Run tests:
```bash
dotnet test
```

## Documentation

- **[README.md](README.md)**: This overview document
- **[EXAMPLES.md](EXAMPLES.md)**: Comprehensive C# usage examples
- **[docs/adam-6051-influxdb-logger.md](docs/adam-6051-influxdb-logger.md)**: Detailed technical documentation
- **[docs/TESTING_PLAN.md](docs/TESTING_PLAN.md)**: Comprehensive testing strategy
- **[CLAUDE.md](CLAUDE.md)**: Development guidelines and architectural principles

## Architecture Highlights

### Python Implementation
- **Procedural Design**: Straightforward, easy-to-understand flow
- **Single Responsibility**: Each function has a clear, focused purpose
- **Error Handling**: Comprehensive exception handling with logging

### C# Implementation
- **Clean Architecture**: Separation of concerns with clear boundaries
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Design Patterns**: Repository, Service, Factory, Observer patterns
- **Dependency Injection**: Full IoC container support
- **Reactive Streams**: Observable data streams for real-time processing

## Support

For questions, issues, or contributions:

1. **Issues**: Use the repository issue tracker
2. **Documentation**: Refer to the comprehensive documentation files
3. **Examples**: Check the examples folder for implementation patterns
4. **Tests**: Review the test files for usage examples and edge cases

## License

This project is designed for industrial automation and logging applications. Please ensure compliance with your organization's security and operational requirements.