# Service Startup Sequence Guide

This guide provides the tested and verified startup sequence for the Industrial ADAM Counter system. Follow these steps in order to avoid common setup issues.

## Prerequisites

- .NET 9 SDK installed
- Docker and Docker Compose installed
- Git repository cloned
- Terminal or PowerShell access

## Complete Startup Sequence

### Step 1: Prepare Environment

```bash
# Navigate to project root
cd adam-6000-counter

# Ensure project builds
dotnet build

# Optional: Set environment variables globally (alternative to inline)
export TIMESCALEDB_USERNAME=industrial_system
export TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
export JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
export JWT_ISSUER=Industrial.Adam.System
export JWT_AUDIENCE=Industrial.Adam.APIs
```

### Step 2: Start TimescaleDB (Infrastructure)

**Terminal 1: Database**
```bash
# Start TimescaleDB with working credentials
docker run -d --name adam-timescaledb \
  -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=industrial_system \
  -e POSTGRES_PASSWORD=IndustrialCounter2024!@#$ \
  timescale/timescaledb:2.17.2-pg17

# Wait for startup (CRITICAL - don't skip this)
echo "Waiting for TimescaleDB to start..."
sleep 15

# Verify database is ready
docker exec adam-timescaledb pg_isready -U industrial_system -d adam_counters

# Expected output: "adam_counters:5432 - accepting connections"
```

**If verification fails:**
```bash
# Check container logs
docker logs adam-timescaledb

# Check if container is running
docker ps | grep timescale

# If container failed, remove and retry
docker rm -f adam-timescaledb
```

### Step 3: Start Logger API (Backend Service 1)

**Terminal 2: Logger API**
```bash
cd src/Industrial.Adam.Logger.WebApi

# Start with environment variables (Port 5000)
ASPNETCORE_URLS=http://localhost:5000 \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run

# Expected output:
# [timestamp INF] Starting Industrial ADAM Logger API...
# [timestamp INF] Database connection validated successfully
# [timestamp INF] API listening on http://localhost:5000
```

**Wait for successful startup before proceeding**

### Step 4: Verify Logger API

**Terminal 3: Verification**
```bash
# Test Logger API health
curl -v http://localhost:5000/health

# Expected: HTTP 200 OK with health status
# If this fails, check Terminal 2 for errors
```

### Step 5: Start OEE API (Backend Service 2)

**Terminal 4: OEE API**
```bash
cd src/Industrial.Adam.Oee/WebApi

# Start with environment variables (Port 5001)
ASPNETCORE_URLS=http://localhost:5001 \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run

# Expected output:
# [timestamp INF] Starting Industrial ADAM OEE API...
# [timestamp INF] Database connection validated successfully
# [timestamp INF] OEE API listening on http://localhost:5001
```

### Step 6: Verify OEE API

**Terminal 3: Verification (reuse)**
```bash
# Test OEE API health
curl -v http://localhost:5001/health

# Expected: HTTP 200 OK with health status
# If this fails, check Terminal 4 for errors
```

### Step 7: Start Frontend Applications (Optional)

**Terminal 5: Counter Frontend**
```bash
cd adam-counter-frontend

# Install dependencies if needed
npm install

# Start development server
npm run dev

# Expected: Development server on http://localhost:3000
```

**Terminal 6: OEE Frontend**
```bash
cd oee-app/oee-interface

# Install dependencies if needed
npm install

# Start development server
npm run dev

# Expected: Development server on http://localhost:3001
```

## Final Verification Checklist

Run these tests to confirm everything is working:

```bash
# 1. Database connectivity
psql postgresql://industrial_system:IndustrialCounter2024!@#$@localhost:5433/adam_counters -c "SELECT 1;"

# 2. API Health Checks
curl http://localhost:5000/health  # Logger API
curl http://localhost:5001/health  # OEE API

# 3. Frontend Access (if started)
curl http://localhost:3000  # Counter Frontend
curl http://localhost:3001  # OEE Frontend

# 4. Docker Status
docker ps | grep timescale
```

## Success Indicators

When everything is working correctly, you should see:

### Database
- ✅ TimescaleDB container running and healthy
- ✅ `pg_isready` returns "accepting connections"
- ✅ Can connect via psql

### Logger API (Terminal 2)
- ✅ "Starting Industrial ADAM Logger API..." 
- ✅ "Database connection validated successfully"
- ✅ "API listening on http://localhost:5000"
- ✅ No connection or authentication errors

### OEE API (Terminal 4)
- ✅ "Starting Industrial ADAM OEE API..."
- ✅ "Database connection validated successfully" 
- ✅ "API listening on http://localhost:5001"
- ✅ No connection or authentication errors

### Health Checks
- ✅ `curl http://localhost:5000/health` returns HTTP 200
- ✅ `curl http://localhost:5001/health` returns HTTP 200

## Common Startup Issues

### "Environment variable substitution not working"
**Symptoms**: Configuration shows `${VARIABLE}` instead of actual values
**Solution**: Use inline environment variables as shown above, not placeholder files

### "Security validation failed"
**Symptoms**: "Username is too simple" or "JWT key is too short"
**Solution**: Use the exact credentials provided in this guide - they pass validation

### "Database connection refused" 
**Symptoms**: Cannot connect to TimescaleDB
**Solutions**:
1. Ensure Step 2 completed successfully
2. Wait longer for database startup (try `sleep 30`)
3. Check container logs: `docker logs adam-timescaledb`
4. Verify port 5433 is not in use: `netstat -tulpn | grep 5433`

### "Port already in use"
**Symptoms**: "Address already in use" errors
**Solutions**:
1. Use exact ports specified: Logger=5000, OEE=5001
2. Check what's using ports: `netstat -tulpn | grep -E ':(5000|5001)'`
3. Kill conflicting processes if necessary

### "Authentication failed for user"
**Symptoms**: Database authentication errors
**Solution**: Ensure ALL services use the same credentials:
- Username: `industrial_system`
- Password: `IndustrialCounter2024!@#$`

## Troubleshooting Commands

```bash
# Check running processes
ps aux | grep dotnet
docker ps

# Check port usage  
netstat -tulpn | grep -E ':(5000|5001|5433)'

# Check application logs
# (Logs appear in the terminal where you started each service)

# Reset environment (if needed)
docker rm -f adam-timescaledb
pkill -f dotnet
```

## Next Steps After Successful Startup

1. **Explore APIs**: Visit http://localhost:5000/health and http://localhost:5001/health
2. **View Frontends**: Navigate to http://localhost:3000 and http://localhost:3001 (if started)
3. **Database Operations**: Connect via psql and explore the counter_data table
4. **Monitor Logs**: Watch the terminal outputs for real-time activity
5. **Test Functionality**: Create test data, configure devices, explore OEE features

For detailed feature usage, refer to:
- [QUICKSTART.md](../QUICKSTART.md) - Quick feature overview
- [README.md](../README.md) - Complete system overview
- [SETUP_TROUBLESHOOTING.md](SETUP_TROUBLESHOOTING.md) - Detailed troubleshooting