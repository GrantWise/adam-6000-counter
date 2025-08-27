# Industrial ADAM Platform - Manual Testing Startup Plan (No Docker)

**For comprehensive manual testing and debugging - all services run directly on host**  
**🖥️ WSL Setup: Using Windows PostgreSQL from WSL**

## Prerequisites

- ✅ .NET 9 SDK installed in WSL
- ✅ Node.js 18+ installed in WSL
- ✅ PostgreSQL 15+ with TimescaleDB extension installed on Windows host
- ✅ Git repository cloned in WSL
- ✅ Multiple terminal windows/tabs available
- ✅ Network connectivity from WSL to Windows PostgreSQL

## Overview of Services

| Service | Purpose | Port | Location |
|---------|---------|------|----------|
| PostgreSQL + TimescaleDB | Database | 5432 | Windows host (172.31.47.17) |
| Logger API | Device communication & data acquisition | 5000 | `src/Industrial.Adam.Logger.WebApi` (WSL) |
| OEE API | Overall Equipment Effectiveness | 5001 | `src/Industrial.Adam.Oee/WebApi` (WSL) |
| Equipment Scheduling API | Production scheduling | 5141 | `src/Industrial.Adam.EquipmentScheduling/WebApi` (WSL) |
| Security API | Authentication & user management | 5139 | `src/Industrial.Adam.Security` (WSL) |
| Admin Dashboard API | System administration | 5002 | `src/Industrial.Adam.AdminDashboard/WebApi` (WSL) |
| Platform Frontend | React web interface | 3001 | `platform-frontend` (WSL) |

## Step 1: Database Setup (Windows PostgreSQL from WSL)

### 1.1 Verify PostgreSQL is Running on Windows
```powershell
# On Windows PowerShell/Command Prompt - verify PostgreSQL service is running
Get-Service postgresql*
# OR
sc query postgresql*
```

### 1.2 Test WSL to Windows PostgreSQL Connectivity
```bash
# From WSL, test connection to your Windows PostgreSQL
# Replace with your actual Windows username if different
psql -h 172.31.47.17 -U postgres -d postgres -c "SELECT version();"
```

### 1.3 Create Database and User
```bash
# Connect to Windows PostgreSQL from WSL as postgres superuser
psql -h 172.31.47.17 -U postgres

# In PostgreSQL shell:
CREATE DATABASE adam_counters;
CREATE USER industrial_system WITH PASSWORD 'IndustrialCounter2024!@#$';
GRANT ALL PRIVILEGES ON DATABASE adam_counters TO industrial_system;
\q
```

### 1.4 Install TimescaleDB Extension (if not already installed)
```bash
# Connect to the adam_counters database on Windows from WSL
psql -h 172.31.47.17 -U industrial_system -d adam_counters

# In database shell:
CREATE EXTENSION IF NOT EXISTS timescaledb;
\q
```

### 1.5 Verify Database Connection with Service Credentials
```bash
# Test connection with exact credentials used by services
psql postgresql://industrial_system:IndustrialCounter2024!@#$@172.31.47.17:5432/adam_counters -c "SELECT 1;"
```

**Expected Result:** Should return `1` without errors.

**Expected:** Should return `1` without errors.

---

## Step 2: Backend Services Startup

**🚨 IMPORTANT:** Start each service in a separate terminal and keep them running. Watch the logs for any errors.

### 2.1 Logger API (Terminal 1)
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Logger.WebApi

# Set environment variables and start (using Windows PostgreSQL IP)
ASPNETCORE_URLS=http://localhost:5000 \
TIMESCALE_HOST=172.31.47.17 \
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
TIMESCALE_HOST=172.31.47.17 \
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
TIMESCALE_HOST=172.31.47.17 \
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
TIMESCALE_HOST=172.31.47.17 \
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
TIMESCALE_HOST=172.31.47.17 \
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
Host: 172.31.47.17 (Windows PostgreSQL from WSL)
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
# Database connectivity (Windows PostgreSQL from WSL)
psql postgresql://industrial_system:IndustrialCounter2024!@#$@172.31.47.17:5432/adam_counters -c "SELECT 1;"

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

### WSL to Windows PostgreSQL Issues

**"Connection refused to 172.31.47.17:5432"**
```bash
# 1. Check if PostgreSQL is running on Windows
# From Windows PowerShell:
# Get-Service postgresql*

# 2. Test network connectivity from WSL
ping 172.31.47.17
telnet 172.31.47.17 5432

# 3. Check Windows Firewall (may need to allow PostgreSQL port 5432)
# 4. Verify PostgreSQL is configured to accept external connections
```

**"PostgreSQL not accepting connections from WSL"**
```bash
# Check PostgreSQL configuration files on Windows:
# - postgresql.conf: should have listen_addresses = '*' or '172.31.47.17'
# - pg_hba.conf: should allow connections from WSL subnet

# Example pg_hba.conf entry:
# host    all             all             172.31.0.0/16           md5
```

**"Authentication failed for user industrial_system"**
```bash
# Connect from WSL and verify user exists
psql -h 172.31.47.17 -U postgres -c "\du industrial_system"
psql -h 172.31.47.17 -U postgres -c "\l adam_counters"

# If user doesn't exist, recreate:
psql -h 172.31.47.17 -U postgres -c "CREATE USER industrial_system WITH PASSWORD 'IndustrialCounter2024!@#$';"
```

### Database Issues

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