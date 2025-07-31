# ADAM-6051 Counter Module Simulator

A realistic simulator for ADAM-6051 16-channel counter modules with production line simulation capabilities.

## Features

- **Accurate ADAM-6051 Emulation**
  - 16 counter channels with 32-bit registers
  - Modbus TCP server on configurable port
  - Digital input status simulation
  - Counter overflow handling

- **Realistic Production Patterns**
  - State machine with Setup, RampUp, Running, Stoppage states
  - Configurable production rates and variations
  - Random minor/major stoppages
  - Scheduled breaks
  - Job-based production cycles

- **Persistence & History**
  - SQLite database for state persistence
  - Production event history
  - Counter values survive restarts

- **REST API Control**
  - Real-time status monitoring
  - Force stoppages for testing
  - Reset counters
  - Start new jobs
  - View production history

## Quick Start

### Run Single Instance

```bash
cd src/Industrial.Adam.Logger.Simulator
dotnet run
```

Default ports:
- Modbus TCP: 502
- REST API: 5000 (or as configured)

### Run Multiple Simulators with Docker

```bash
# From repository root
cd docker
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up -d

# View logs
docker-compose logs -f

# Stop all simulators
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml down
```

This starts:
- Complete monitoring stack (InfluxDB, Grafana, Logger)
- Simulator 1: Modbus on 5020
- Simulator 2: Modbus on 5021
- Simulator 3: Modbus on 5022

## Configuration

### appsettings.json

```json
{
  "SimulatorSettings": {
    "DeviceId": "SIM-6051-01",
    "ModbusPort": 502,
    "DatabasePath": "data/simulator.db"
  },
  "ProductionSettings": {
    "BaseRate": 120,              // Units per minute
    "RateVariation": 0.1,         // ±10% variation
    "JobSizeMin": 1000,
    "JobSizeMax": 5000,
    "MinorStoppageProbability": 0.02,  // 2% per minute
    "MajorStoppageProbability": 0.005   // 0.5% per minute
  },
  "Channels": [
    {
      "Number": 0,
      "Name": "Main Product Counter",
      "Type": "ProductionCounter",
      "Enabled": true
    },
    {
      "Number": 1,
      "Name": "Reject Counter",
      "Type": "RejectCounter",
      "Enabled": true,
      "RejectRate": 0.05        // 5% of production
    }
  ]
}
```

### Environment Variables

Override any setting with environment variables:
```bash
SimulatorSettings__DeviceId=SIM002
SimulatorSettings__ModbusPort=5502
ProductionSettings__BaseRate=200
```

## REST API Endpoints

### Status
```bash
GET http://localhost:8081/api/simulator/status
```

### Reset Counter
```bash
POST http://localhost:8081/api/simulator/channels/0/reset
```

### Force Stoppage
```bash
POST http://localhost:8081/api/simulator/production/force-stoppage
Content-Type: application/json

{
  "type": "minor",
  "reason": "Material jam"
}
```

### Start New Job
```bash
POST http://localhost:8081/api/simulator/production/start-job
Content-Type: application/json

{
  "jobName": "Order #12345"
}
```

### Get History
```bash
GET http://localhost:8081/api/simulator/history?hours=24
```

## Testing with Core Logger

Configure the Core logger to connect to simulators:

```json
{
  "AdamLogger": {
    "Devices": [
      {
        "DeviceId": "SIM001",
        "Name": "Simulator Line 1",
        "IpAddress": "localhost",
        "Port": 5502,
        "UnitId": 1,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "Product Counter",
            "StartRegister": 0,
            "RegisterCount": 2
          }
        ]
      }
    ]
  }
}
```

## Production States

1. **Idle**: No production
2. **Setup**: Preparing for new job (15 min default)
3. **RampUp**: Speed increasing (30 sec default)
4. **Running**: Normal production at target rate
5. **RampDown**: Slowing before stop (10 sec default)
6. **MinorStoppage**: Short pause (30-120 sec)
7. **MajorStoppage**: Long stop (10-30 min)
8. **ScheduledBreak**: Configured break times

## Dashboard

Open http://localhost:8080 to view the simulator dashboard showing:
- Real-time counter values
- Production state
- Job progress
- Control buttons

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
# Test with actual Core logger
dotnet test ../Industrial.Adam.Logger.Core.Tests --filter "Integration"
```

### Docker Build
```bash
docker build -t adam-simulator .
docker run -p 5502:502 -p 8081:80 adam-simulator
```