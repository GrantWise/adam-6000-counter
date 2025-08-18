# Quick Start Guide

Get the Industrial ADAM Counter Ecosystem running in 10 minutes! This repository contains **three separate applications** that work together.

## 🏭 Four Applications Overview

| Application | Purpose | Location | Port |
|-------------|---------|----------|------|
| **ADAM Logger** | C# backend data service | `src/Industrial.Adam.Logger.Console` | Console app |
| **OEE API Service** | .NET 9 OEE backend API | `src/Industrial.Adam.Oee` | 5001 |
| **Counter Frontend** | React device dashboard | `adam-counter-frontend/` | 3000 |
| **OEE Application** | Manufacturing analytics | `oee-app/oee-interface/` | 3001 |

## Option 1: Automated Setup (Recommended) 🚀

### Prerequisites
- Git, Docker, Docker Compose
- .NET 9 SDK (auto-installed if missing)

### One-Command Setup
```bash
# Clone repository
git clone [repository-url]
cd adam-6000-counter

# Complete automated setup - installs everything!
./scripts/setup-dev-environment.sh
```

This script automatically:
- ✅ Installs .NET 9 SDK if needed
- ✅ Starts TimescaleDB with proper schema
- ✅ Launches Grafana with dashboards
- ✅ Starts 3 ADAM device simulators
- ✅ Builds and starts the logger service
- ✅ Builds and starts the OEE API service
- ✅ Sets up frontend development environment

## Option 2: Docker Infrastructure + Manual Apps 🐳

### Step 1: Start Infrastructure
```bash
cd docker
docker-compose up -d  # TimescaleDB, Grafana, Prometheus
```

### Step 2: Start Backend Services
```bash
# Terminal 1: Build and run ADAM Logger
dotnet build
dotnet run --project src/Industrial.Adam.Logger.Console

# Terminal 2: Build and run OEE API Service
dotnet run --project src/Industrial.Adam.Oee/WebApi
# → http://localhost:5001
```

### Step 3: Start Frontend Applications
```bash
# Terminal 3: ADAM Counter Frontend
cd adam-counter-frontend
npm install && npm run dev
# → http://localhost:3000

# Terminal 4: OEE Application  
cd oee-app/oee-interface
npm install && npm run dev  
# → http://localhost:3001
```

## Access Points

### 📊 Monitoring & Data
- **Grafana Dashboards**: http://localhost:3002 (admin/admin)
- **TimescaleDB**: postgresql://localhost:5433 (adam_user/adam_password)
- **Prometheus**: http://localhost:9090

### 💻 Application Interfaces
- **ADAM Counter Frontend**: http://localhost:3000 (device management)
- **OEE API Service**: http://localhost:5001 (manufacturing data API)
- **OEE Dashboard**: http://localhost:3001 (production analytics)

### 🔧 API Health Checks
- **Logger Health**: Check console output for "Connected to device" messages
- **OEE API Health**: http://localhost:5001/health
- **OEE API Detailed**: http://localhost:5001/api/health/detailed

### 🔍 Verification
```bash
# Check all services
docker-compose ps

# View live data streaming
docker-compose logs -f adam-logger

# Test API connectivity
curl http://localhost:5001/health
curl http://localhost:5001/api/health/detailed

# Test frontend connectivity
curl http://localhost:3000/api/health
curl http://localhost:3001/api/health
```

## Using Real ADAM Devices

### For Docker Deployment
1. **Edit Docker configuration**
   ```bash
   nano docker/config/adam_config_v2.json
   ```

2. **Update device IP address**
   ```json
   {
     "AdamLogger": {
       "Devices": [{
         "DeviceId": "ADAM-6051-01",
         "IpAddress": "192.168.1.100",  // Your ADAM device IP
         "Channels": [
           {
             "ChannelNumber": 0,
             "Name": "ProductionCounter",
             "RateWindowSeconds": 60
           }
         ]
       }]
     }
   }
   ```

3. **Restart the logger**
   ```bash
   cd docker
   docker-compose restart adam-logger
   ```

### For Local Development
1. **Edit console configuration**
   ```bash
   nano src/Industrial.Adam.Logger.Console/appsettings.json
   ```

2. **Update configuration with your device details**
3. **Restart the console application**

## What You'll See When Running

### ✅ Successful Backend Output

**ADAM Logger**:
```
adam-logger | [10:30:45 INF] Starting Industrial ADAM Logger...
adam-logger | [10:30:45 INF] Connected to SIM-6051-01 at 127.0.0.1:5502
adam-logger | [10:30:46 INF] Channel 0: ProductionCounter = 12345 (rate: 150.2/min)
adam-logger | [10:30:46 INF] Channel 1: RejectCounter = 67 (rate: 2.1/min)
adam-logger | [10:30:46 INF] Data written to TimescaleDB successfully
```

**OEE API Service**:
```
oee-api | [10:30:45 INF] Starting Industrial ADAM OEE API...
oee-api | [10:30:45 INF] Database connection validated successfully
oee-api | [10:30:45 INF] OEE API listening on http://localhost:5001
oee-api | [10:30:46 INF] Health check: Healthy - All dependencies OK
```

### 📊 Applications
- **Counter Frontend**: Real-time device monitoring with live charts
- **OEE API Service**: RESTful API for OEE calculations, work orders, and stoppage detection
- **OEE Dashboard**: Production efficiency analytics and job tracking

### 🔧 Simulators (Development)
The setup includes 3 ADAM device simulators:
- **Simulator 1**: Port 5502 (Production line)
- **Simulator 2**: Port 5503 (Quality station)  
- **Simulator 3**: Port 5504 (Packaging line)

## What's Next?

### 🎯 Immediate Next Steps
1. **Explore the Applications**:
   - ADAM Counter Frontend: Device configuration and real-time monitoring
   - OEE API Service: Test API endpoints and explore manufacturing data
   - OEE Dashboard: Production analytics and efficiency tracking
   - Grafana: Advanced visualization and alerting

### 🔧 OEE API Quick Tests
```bash
# Test OEE API endpoints
curl "http://localhost:5001/api/oee/current?deviceId=SIM-6051-01"
curl "http://localhost:5001/api/jobs/active?deviceId=SIM-6051-01"
curl "http://localhost:5001/api/stoppages/current?deviceId=SIM-6051-01"

# Create a test work order
curl -X POST http://localhost:5001/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId": "TEST-001",
    "workOrderDescription": "Test Production Run",
    "productId": "WIDGET-A",
    "productDescription": "Test Widget",
    "plannedQuantity": 1000,
    "scheduledStartTime": "2024-01-15T08:00:00Z",
    "scheduledEndTime": "2024-01-15T16:00:00Z",
    "deviceId": "SIM-6051-01"
  }'
```

2. **Customize for Your Environment**:
   - Add your real ADAM devices to configuration
   - Configure production profiles for simulators
   - Set up custom Grafana dashboards

3. **Development Tasks**:
   - Follow [ONBOARDING.md](ONBOARDING.md) for detailed developer guide
   - Review architecture documentation in `/docs`
   - Explore test suites and development scripts

## 🚨 Common Issues & Solutions

### ❌ Backend Issues
**"Can't connect to ADAM device"**
```bash
# Check device connectivity
ping <device-ip>
nc -zv <device-ip> 502

# Verify configuration
cat src/Industrial.Adam.Logger.Console/appsettings.json
```

**"TimescaleDB connection failed"**
```bash
# Check database status
docker ps | grep timescale
docker logs adam-timescaledb

# Test connection
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters
```

### ❌ Frontend Issues
**"Frontend won't start"**
```bash
# Check Node.js version (need 18+)
node --version

# Clear and reinstall
rm -rf node_modules package-lock.json
npm install
```

**"No real-time data updates"**
- Check WebSocket connection in browser dev tools
- Verify backend services are running and healthy
- Ensure no firewall blocking ports 3000/3001/5001
- Test OEE API health: `curl http://localhost:5001/health`

### ❌ Docker Issues
**"Services won't start"**
```bash
# Check for port conflicts
netstat -tulpn | grep -E ':(3000|3001|3002|5001|5433)'

# Reset Docker environment
docker-compose down
docker system prune -f
docker-compose up -d

# Check OEE API specific issues
dotnet build src/Industrial.Adam.Oee/
curl http://localhost:5001/health
```

## 📚 Additional Resources

- **[ONBOARDING.md](ONBOARDING.md)**: Complete developer guide
- **[README.md](README.md)**: Detailed project overview
- **[DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)**: Advanced setup options
- **[docs/](docs/)**: Technical architecture and standards

## 🎉 Success Indicators

When everything is working correctly:
- ✅ All Docker services running (`docker-compose ps`)
- ✅ ADAM Logger processing device data (check console logs)
- ✅ OEE API responding to health checks (`curl http://localhost:5001/health`)
- ✅ Frontend applications accessible on ports 3000/3001
- ✅ OEE API accessible on port 5001
- ✅ Grafana showing live data visualizations
- ✅ TimescaleDB receiving time-series data

### 📋 Complete Service Checklist
```bash
# Verify all services are healthy
curl http://localhost:5001/health                    # OEE API
curl http://localhost:3000                          # Counter Frontend
curl http://localhost:3001                          # OEE Dashboard  
curl http://localhost:3002                          # Grafana
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters -c "SELECT 1;"  # Database
```

**You now have a complete industrial data acquisition ecosystem running! 🏭**