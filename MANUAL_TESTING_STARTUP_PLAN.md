# Industrial ADAM Platform - Manual Testing Startup Plan (No Docker)

**For comprehensive manual testing and debugging - all services run directly on host**

## Prerequisites

- ✅ .NET 9 SDK installed
- ✅ Node.js 18+ installed  
- ✅ PostgreSQL 15+ with TimescaleDB extension installed locally
- ✅ Git repository cloned
- ✅ Multiple terminal windows/tabs available

## Overview of Services

| Service | Purpose | Port | Directory |
|---------|---------|------|-----------|
| PostgreSQL + TimescaleDB | Database | 5432 | Local installation |
| Logger API | Device communication & data acquisition | 5000 | `src/Industrial.Adam.Logger.WebApi` |
| OEE API | Overall Equipment Effectiveness | 5001 | `src/Industrial.Adam.Oee/WebApi` |
| Equipment Scheduling API | Production scheduling | 5141 | `src/Industrial.Adam.EquipmentScheduling/WebApi` |
| Security API | Authentication & user management | 5139 | `src/Industrial.Adam.Security` |
| Admin Dashboard API | System administration | 5002 | `src/Industrial.Adam.AdminDashboard/WebApi` |
| Platform Frontend | React web interface | 3001 | `platform-frontend` |

## Step 1: Database Setup (Local PostgreSQL)

### 1.1 Start Local PostgreSQL
```bash
# Start PostgreSQL service (varies by OS)
# Ubuntu/Debian:
sudo systemctl start postgresql

# macOS (Homebrew):
brew services start postgresql

# Windows:
# Start PostgreSQL service from Services panel
```

### 1.2 Create Database and User
```bash
# Connect as postgres superuser
sudo -u postgres psql

# In PostgreSQL shell:
CREATE DATABASE adam_counters;
CREATE USER industrial_system WITH PASSWORD 'IndustrialCounter2024!@#$';
GRANT ALL PRIVILEGES ON DATABASE adam_counters TO industrial_system;
\q
```

### 1.3 Install TimescaleDB Extension
```bash
# Connect to the adam_counters database
psql -U industrial_system -d adam_counters -h localhost

# In database shell:
CREATE EXTENSION IF NOT EXISTS timescaledb;
\q
```

### 1.4 Verify Database Connection
```bash
# Test connection with exact credentials used by services
psql postgresql://industrial_system:IndustrialCounter2024!@#$@localhost:5432/adam_counters -c "SELECT 1;"
```

**Expected:** Should return `1` without errors.

---

## Step 2: Backend Services Startup

**🚨 IMPORTANT:** Start each service in a separate terminal and keep them running. Watch the logs for any errors.

### 2.1 Logger API (Terminal 1)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Logger.WebApi

# Set environment variables and start
ASPNETCORE_URLS=http://localhost:5000 \
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5432 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

**Success Indicators:**
- ✅ "Starting Industrial ADAM Logger API..."
- ✅ "Database connection validated successfully"
- ✅ "Now listening on: http://localhost:5000"
- ✅ No connection or authentication errors

**Test:** `curl http://localhost:5000/health` (should return HTTP 200)

### 2.2 OEE API (Terminal 2)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Oee/WebApi

ASPNETCORE_URLS=http://localhost:5001 \
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5432 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

**Success Indicators:**
- ✅ "Starting Industrial ADAM OEE API..."
- ✅ "Database connection validated successfully"
- ✅ "Now listening on: http://localhost:5001"

**Test:** `curl http://localhost:5001/health`

### 2.3 Equipment Scheduling API (Terminal 3)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.EquipmentScheduling/WebApi

ASPNETCORE_URLS=http://localhost:5141 \
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5432 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

**Test:** `curl http://localhost:5141/health`

### 2.4 Security API (Terminal 4)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Security

ASPNETCORE_URLS=http://localhost:5139 \
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5432 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

**Test:** `curl http://localhost:5139/health`

### 2.5 Admin Dashboard API (Terminal 5)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.AdminDashboard/WebApi

ASPNETCORE_URLS=http://localhost:5002 \
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5432 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

**Test:** `curl http://localhost:5002/health`

---

## Step 3: Frontend Application

### 3.1 Platform Frontend (Terminal 6)
```bash
cd /home/grant/adam-6000-counter/platform-frontend

# Install dependencies if needed
npm install

# Start development server
PORT=3001 npm run dev
```

**Success Indicators:**
- ✅ "Local: http://localhost:3001/"
- ✅ "ready in X ms"
- ✅ No CORS or connection errors

**Test:** Open http://localhost:3001 in browser

---

## Step 4: Optional - Device Simulators

### 4.1 Start ADAM Device Simulators (Terminal 7)
```bash
cd /home/grant/adam-6000-counter

# Start simulators on ports 5502-5504
./scripts/start-simulators.sh

# Verify they're running
./scripts/test-simulators.sh
```

**Expected:** 3 simulators running on ports 5502, 5503, 5504

---

## Credentials & Authentication

### Database Connection
```
Host: localhost
Port: 5432 (standard PostgreSQL port)
Database: adam_counters
Username: industrial_system
Password: IndustrialCounter2024!@#$
```

### JWT Configuration
```
Secret Key: 8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
Issuer: Industrial.Adam.System
Audience: Industrial.Adam.APIs
```

### Default User Accounts
```
Admin User:
- Username: admin@industrial-adam.local
- Password: AdminPass123!
- Role: SystemAdmin

Operator User:
- Username: operator@industrial-adam.local  
- Password: OperatorPass123!
- Role: Operator
```

---

## Complete Service Health Check

Run these commands to verify all services are operational:

```bash
# Database connectivity
psql postgresql://industrial_system:IndustrialCounter2024!@#$@localhost:5432/adam_counters -c "SELECT 1;"

# API Health Checks
curl http://localhost:5000/health  # Logger API
curl http://localhost:5001/health  # OEE API  
curl http://localhost:5141/health  # Equipment Scheduling API
curl http://localhost:5139/health  # Security API
curl http://localhost:5002/health  # Admin Dashboard API

# Frontend Accessibility
curl http://localhost:3001  # Platform Frontend

# Check running processes
ps aux | grep dotnet  # Should show 5 dotnet processes
ss -tlnp | grep -E ':(5000|5001|5141|5139|5002|3001|5432)'  # Check ports
```

---

## Troubleshooting Guide

### Database Issues

**"Connection refused to localhost:5432"**
```bash
# Check PostgreSQL service status
sudo systemctl status postgresql

# Check if port 5432 is open
ss -tlnp | grep :5432

# Check PostgreSQL logs
sudo journalctl -u postgresql -f
```

**"Authentication failed for user industrial_system"**
```bash
# Verify user exists and has permissions
sudo -u postgres psql -c "\du industrial_system"
sudo -u postgres psql -c "\l adam_counters"
```

### API Startup Issues

**"Port already in use"**
```bash
# Find what's using the port
ss -tlnp | grep :5000  # (replace with failing port)

# Kill conflicting process if needed
pkill -f "dotnet.*WebApi"
```

**"Database connection failed"**
- Ensure PostgreSQL is running and TimescaleDB extension is installed
- Verify credentials match exactly (case-sensitive)
- Check database exists and user has permissions

**"JWT key validation failed"**  
- Ensure JWT_SECRET_KEY is exactly 64 characters
- Verify all services use identical JWT configuration

### Frontend Issues

**"Cannot connect to API"**
- Verify all backend APIs are running and healthy
- Check browser console for CORS errors
- Ensure API URLs in frontend configuration are correct

---

## Manual Testing Scenarios

### 1. Authentication Testing
```bash
# Test login endpoint
curl -X POST http://localhost:5139/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin@industrial-adam.local",
    "password": "AdminPass123!"
  }'
```

### 2. Device Data Testing
```bash
# Query latest counter readings
curl http://localhost:5000/api/counters/latest

# View device status
curl http://localhost:5000/api/devices
```

### 3. OEE Calculations Testing
```bash
# Get current OEE metrics
curl "http://localhost:5001/api/oee/current?deviceId=SIM-6051-01"

# View active work orders
curl http://localhost:5001/api/workorders/active
```

### 4. Frontend Integration Testing
- Open http://localhost:3001 in browser
- Test user login with provided credentials
- Navigate through different modules
- Verify real-time data updates
- Test device configuration features

---

## Success Criteria Checklist

- [ ] PostgreSQL with TimescaleDB running locally
- [ ] All 5 backend APIs started and healthy (ports 5000, 5001, 5141, 5139, 5002)
- [ ] Frontend application accessible at http://localhost:3001
- [ ] Database connection test passes
- [ ] All health endpoints return HTTP 200
- [ ] Authentication endpoints respond correctly
- [ ] No CORS errors in browser console
- [ ] Real-time data flows between services
- [ ] Device simulators operational (if using)

**When all items are checked, the system is ready for comprehensive manual testing!**

---

*This plan eliminates Docker complexity while maintaining full system functionality for thorough debugging and testing.*