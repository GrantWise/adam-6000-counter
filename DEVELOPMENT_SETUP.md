# Development Setup Guide

This guide walks you through setting up a complete development environment for the Industrial ADAM Logger.

## 🚀 Quick Setup (Automated)

The fastest way to get started:

```bash
./scripts/setup-dev-environment.sh
```

This script automatically:
- ✅ Installs .NET 9 SDK if needed
- ✅ Starts TimescaleDB and creates database schema
- ✅ Launches Grafana with pre-configured dashboards
- ✅ Starts Prometheus for metrics monitoring
- ✅ Launches 3 ADAM device simulators
- ✅ Builds and starts the logger application
- ✅ Verifies everything is working

## 📋 Prerequisites

### Required Software
- **Git**: For cloning the repository
- **Docker & Docker Compose**: For running infrastructure services
- **.NET 9 SDK**: For building and running the application
  - Install with: `./scripts/install-dotnet9.sh`
- **PostgreSQL Client** (optional): For database inspection
  - Ubuntu/Debian: `sudo apt-get install postgresql-client`
  - macOS: `brew install postgresql`

### System Requirements
- **OS**: Linux, macOS, or Windows with WSL2
- **RAM**: Minimum 4GB (8GB recommended)
- **Disk**: 2GB free space
- **Network**: Ports 3002, 5433, 9090, 5502-5504 available

## 🔧 Manual Setup

### Step 1: Clone Repository
```bash
git clone https://github.com/yourusername/adam-6000-counter.git
cd adam-6000-counter
```

### Step 2: Install .NET 9
```bash
# Check if .NET 9 is installed
dotnet --version

# If not installed or version < 9.0
./scripts/install-dotnet9.sh
```

### Step 3: Start Infrastructure Services

#### TimescaleDB
```bash
docker run -d --name adam-timescaledb \
  -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=adam_user \
  -e POSTGRES_PASSWORD=adam_password \
  timescale/timescaledb:2.17.2-pg17

# Create hypertables (wait 10 seconds for startup first)
sleep 10
docker exec adam-timescaledb psql -U adam_user -d adam_counters -c "
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE TABLE IF NOT EXISTS counter_data (
    time TIMESTAMPTZ NOT NULL,
    device_id TEXT NOT NULL,
    channel INT NOT NULL,
    raw_value BIGINT,
    processed_value DOUBLE PRECISION,
    rate DOUBLE PRECISION,
    quality INT,
    tags JSONB,
    PRIMARY KEY (time, device_id, channel)
);
SELECT create_hypertable('counter_data', 'time', if_not_exists => TRUE);
"
```

#### Grafana
```bash
docker run -d --name adam-grafana \
  -p 3002:3000 \
  -e GF_SECURITY_ADMIN_USER=admin \
  -e GF_SECURITY_ADMIN_PASSWORD=admin \
  grafana/grafana:12.0.2
```

#### Prometheus
```bash
docker run -d --name adam-prometheus \
  -p 9090:9090 \
  prom/prometheus:v2.47.0
```

### Step 4: Start Simulators
```bash
# Start 3 ADAM device simulators on ports 5502-5504
./scripts/start-simulators.sh

# Verify simulators are running
./scripts/test-simulators.sh
```

### Step 5: Configure Application

Edit `src/Industrial.Adam.Logger.Console/appsettings.json`:

```json
{
  "AdamLogger": {
    "Devices": [
      {
        "DeviceId": "SIM-6051-01",
        "IpAddress": "127.0.0.1",
        "Port": 5502,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "ProductionCounter",
            "RateWindowSeconds": 60  // NEW: Windowed calculation
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
      "EnableDeadLetterQueue": true  // NEW: Data reliability
    }
  }
}
```

### Step 6: Build and Run
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Start the application
dotnet run --project src/Industrial.Adam.Logger.Console
```

## 🧪 Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run with coverage
./scripts/run-coverage.sh

# Run specific test groups
./scripts/test-group-1a.sh  # Core functionality
./scripts/test-group-2a.sh  # Data processing
./scripts/test-group-3a.sh  # Storage layer
```

### Integration Testing
```bash
# Full system test with simulators
./scripts/full-system-test.sh

# Test logger with real data flow
./scripts/test-logger.sh
```

## 📊 Monitoring

### Grafana Dashboards
Access at http://localhost:3002 (admin/admin)

Available dashboards:
- **Counter Overview**: Real-time counter values and rates
- **Production Analytics**: Throughput, efficiency, and trends
- **System Health**: Application and infrastructure metrics

### TimescaleDB Queries
```sql
-- Connect to database
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters

-- View recent data
SELECT time, device_id, channel, processed_value, rate 
FROM counter_data 
ORDER BY time DESC 
LIMIT 10;

-- Check data volume
SELECT 
  time_bucket('1 hour', time) AS hour,
  device_id,
  COUNT(*) as readings,
  AVG(rate) as avg_rate
FROM counter_data
WHERE time > NOW() - INTERVAL '24 hours'
GROUP BY hour, device_id
ORDER BY hour DESC;
```

### Prometheus Metrics
Access at http://localhost:9090

Key metrics:
- `adam_logger_readings_total`: Total readings processed
- `adam_logger_errors_total`: Error count by type
- `adam_logger_database_writes`: Database write performance
- `adam_logger_device_status`: Device connection status

## 🔍 Troubleshooting

### Common Issues

#### TimescaleDB Connection Failed
```bash
# Check if TimescaleDB is running
docker ps | grep timescale

# Test connection
docker exec adam-timescaledb pg_isready

# View logs
docker logs adam-timescaledb
```

#### Simulator Not Responding
```bash
# Check simulator status
ps aux | grep Simulator

# Restart simulators
./scripts/stop-simulators.sh
./scripts/start-simulators.sh

# Test connectivity
nc -zv localhost 5502
```

#### Build Errors
```bash
# Clear build artifacts
dotnet clean

# Restore packages
dotnet restore

# Update packages
./scripts/update-packages.sh
```

## 📁 Project Structure

```
adam-6000-counter/
├── src/
│   ├── Industrial.Adam.Logger.Core/        # Core business logic
│   │   ├── Processing/                     # Data processing
│   │   │   ├── DataProcessor.cs           # Main processor
│   │   │   ├── WindowedRateCalculator.cs  # NEW: Rate calculation
│   │   │   └── CircularBuffer.cs          # NEW: Efficient storage
│   │   └── Storage/
│   │       ├── TimescaleStorage.cs        # NEW: TimescaleDB integration
│   │       └── DeadLetterQueue.cs         # NEW: Reliability layer
│   ├── Industrial.Adam.Logger.Console/     # Console application
│   └── Industrial.Adam.Logger.Simulator/   # Device simulators
├── scripts/                                # Development scripts
│   ├── setup-dev-environment.sh           # One-click setup
│   ├── start-simulators.sh                # Launch simulators
│   └── test-simulators.sh                 # Test connectivity
└── docker/                                 # Docker configuration
    ├── docker-compose.yml                  # Main stack
    └── config/                             # Configuration files
```

## 🎯 Next Steps

1. **Explore the Simulators**: Modify simulator behavior in `src/Industrial.Adam.Logger.Simulator/config/`
2. **Customize Dashboards**: Import custom Grafana dashboards from `docker/grafana/dashboards/`
3. **Add Real Devices**: Update configuration with your ADAM device IP addresses
4. **Enable Production Features**: Configure dead letter queue, adjust rate windows, set up alerting

## 📚 Additional Resources

- [Configuration Guide](docs/configuration-guide.md)
- [Simulator Configuration Guide](docs/simulator-configuration-guide.md)
- [Architecture Documentation](docs/Industrial%20Data%20Acquisition%20Platform%20Architecture.md)
- [Development Standards](docs/Industrial-Software-Development-Standards.md)