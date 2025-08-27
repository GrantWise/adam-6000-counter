# Setup Troubleshooting Guide

This guide documents common issues encountered during setup and their solutions based on real-world experience with the Industrial ADAM Counter system.

## Working Configuration Summary

### Database Credentials (TimescaleDB)
- **Host**: `localhost`
- **Port**: `5433`
- **Database**: `adam_counters`
- **Username**: `industrial_system`
- **Password**: `IndustrialCounter2024!@#$`

### Service Ports
- **Logger API**: `5000`
- **OEE API**: `5001`
- **TimescaleDB**: `5433`
- **Counter Frontend**: `3000`
- **OEE Frontend**: `3001`
- **Grafana**: `3002`

### Required Environment Variables
```bash
TIMESCALEDB_USERNAME=industrial_system
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
JWT_ISSUER=Industrial.Adam.System
JWT_AUDIENCE=Industrial.Adam.APIs
```

## Common Setup Issues and Solutions

### 1. Environment Variable Substitution Not Working

**Problem**: Configuration files contain `${VARIABLE}` placeholders that aren't being replaced.

**Symptoms**:
- Database connection failures with literal `${TIMESCALEDB_USERNAME}` as username
- Security validation rejecting `${JWT_SECRET_KEY}` as invalid

**Solution**: Export environment variables before running services:

```bash
# Export all required environment variables
export TIMESCALEDB_USERNAME=industrial_system
export TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
export JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
export JWT_ISSUER=Industrial.Adam.System
export JWT_AUDIENCE=Industrial.Adam.APIs

# Then start the service
cd src/Industrial.Adam.Logger.WebApi
dotnet run
```

**Alternative**: Set environment variables inline:

```bash
cd src/Industrial.Adam.Logger.WebApi
TIMESCALEDB_USERNAME=industrial_system TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d JWT_ISSUER=Industrial.Adam.System JWT_AUDIENCE=Industrial.Adam.APIs dotnet run
```

### 2. Security Validation Failures

**Problem**: Security validator is very strict and rejects default/simple values.

**Symptoms**:
- Error: "Username 'adam_user' is too simple"
- Error: "Password does not meet complexity requirements"
- Error: "JWT secret key is too short or weak"

**Solution**: Use complex credentials that meet security requirements:

- **Username**: Must not be default values like `adam_user`, `admin`, etc.
- **Password**: Must contain uppercase, lowercase, numbers, and special characters
- **JWT Secret**: Must be at least 256 bits (64 hex characters or equivalent)

**Working Example**:
```bash
Username: industrial_system
Password: IndustrialCounter2024!@#$
JWT_Secret: 8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
```

### 3. Database Credential Mismatches

**Problem**: Different services using different database credentials.

**Symptoms**:
- One service connects but another fails
- Inconsistent authentication errors

**Solution**: Ensure ALL services use the same database credentials:

**appsettings.json files should contain**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=adam_counters;Username=industrial_system;Password=IndustrialCounter2024!@#$"
  }
}
```

**Environment variables should match**:
```bash
TIMESCALEDB_USERNAME=industrial_system
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
```

### 4. Port Conflicts

**Problem**: Multiple APIs trying to use the same port.

**Symptoms**:
- Error: "Address already in use"
- Second API fails to start

**Solution**: Assign different ports to each API:

```bash
# Logger API - Port 5000
cd src/Industrial.Adam.Logger.WebApi
ASPNETCORE_URLS=http://localhost:5000 dotnet run

# OEE API - Port 5001  
cd src/Industrial.Adam.Oee/WebApi
ASPNETCORE_URLS=http://localhost:5001 dotnet run
```

### 5. TimescaleDB Docker Container Issues

**Problem**: TimescaleDB container fails to initialize or accept connections.

**Symptoms**:
- Connection refused errors
- Database not found errors
- Authentication failures

**Solution**: Start TimescaleDB with correct credentials:

```bash
docker run -d --name adam-timescaledb \
  -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=industrial_system \
  -e POSTGRES_PASSWORD=IndustrialCounter2024!@#$ \
  timescale/timescaledb:2.17.2-pg17

# Wait for startup
sleep 10

# Verify connection
docker exec adam-timescaledb pg_isready -U industrial_system -d adam_counters
```

### 6. Modbus Simulator Connectivity Issues

**Problem**: Services can't connect to Modbus simulators.

**Symptoms**:
- Connection timeout errors
- "No route to host" errors

**Solution**: Start simulators first and verify connectivity:

```bash
# Start simulators (if available)
./scripts/start-simulators.sh

# Test connectivity manually
nc -zv localhost 5502  # Simulator 1
nc -zv localhost 5503  # Simulator 2  
nc -zv localhost 5504  # Simulator 3

# Or use telnet
telnet localhost 5502
```

## Proper Service Startup Sequence

Follow this sequence to avoid startup issues:

### 1. Infrastructure First
```bash
# Start TimescaleDB
docker run -d --name adam-timescaledb \
  -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=industrial_system \
  -e POSTGRES_PASSWORD=IndustrialCounter2024!@#$ \
  timescale/timescaledb:2.17.2-pg17

# Wait for database startup
sleep 15
```

### 2. Verify Database
```bash
# Test database connection
docker exec adam-timescaledb pg_isready -U industrial_system -d adam_counters

# Should return: "adam_counters:5432 - accepting connections"
```

### 3. Start Backend APIs (with Environment Variables)
```bash
# Terminal 1: Logger API
cd src/Industrial.Adam.Logger.WebApi
ASPNETCORE_URLS=http://localhost:5000 \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run

# Terminal 2: OEE API  
cd src/Industrial.Adam.Oee/WebApi
ASPNETCORE_URLS=http://localhost:5001 \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

### 4. Verify Backend APIs
```bash
# Test Logger API
curl http://localhost:5000/health

# Test OEE API
curl http://localhost:5001/health
```

### 5. Start Frontend Applications
```bash
# Terminal 3: Counter Frontend
cd adam-counter-frontend
npm install
npm run dev

# Terminal 4: OEE Frontend
cd oee-app/oee-interface  
npm install
npm run dev
```

## Verification Steps

### Database Verification
```bash
# Connect to database
psql postgresql://industrial_system:IndustrialCounter2024!@#$@localhost:5433/adam_counters

# Check tables exist
\dt

# Check for recent data
SELECT * FROM counter_data ORDER BY time DESC LIMIT 5;
```

### API Health Checks
```bash
# Logger API
curl -v http://localhost:5000/health

# OEE API  
curl -v http://localhost:5001/health

# Expected response: HTTP 200 with health status
```

### Frontend Access
- **Counter Frontend**: http://localhost:3000
- **OEE Frontend**: http://localhost:3001
- **Grafana**: http://localhost:3002 (if running)

## Environment File Template

Create a `.env` file in the project root with these variables:

```bash
# Database Configuration
TIMESCALEDB_USERNAME=industrial_system
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
TIMESCALE_HOST=localhost
TIMESCALE_PORT=5433
TIMESCALE_DATABASE=adam_counters

# JWT Configuration
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
JWT_ISSUER=Industrial.Adam.System
JWT_AUDIENCE=Industrial.Adam.APIs

# API Ports
LOGGER_API_PORT=5000
OEE_API_PORT=5001

# Frontend Ports
COUNTER_FRONTEND_PORT=3000
OEE_FRONTEND_PORT=3001
GRAFANA_PORT=3002
```

## Common Error Messages and Solutions

### "Could not find a part of the path"
**Cause**: Configuration file paths or working directory issues.
**Solution**: Ensure you're running commands from the correct directory.

### "A connection was forcibly closed"
**Cause**: Database connection issues or wrong credentials.
**Solution**: Verify TimescaleDB is running and credentials are correct.

### "The configured user limit (100) on the number of inotify instances has been reached"
**Cause**: Too many file watchers (common in development).
**Solution**: `echo fs.inotify.max_user_instances=524288 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p`

### "CORS policy: No 'Access-Control-Allow-Origin' header"
**Cause**: Frontend can't access backend due to CORS restrictions.
**Solution**: Ensure backend APIs include correct CORS configuration for frontend URLs.

## Getting Help

If you encounter issues not covered here:

1. **Check Logs**: All services provide detailed logging
2. **Verify Configuration**: Double-check all credentials and ports
3. **Test Components**: Use `curl` to test APIs independently
4. **Docker Status**: Use `docker ps` and `docker logs` to check container status
5. **Network**: Use `netstat -tulpn` to check port usage

## Success Indicators

When everything is working correctly, you should see:

- ✅ TimescaleDB accepting connections on port 5433
- ✅ Logger API responding on http://localhost:5000/health
- ✅ OEE API responding on http://localhost:5001/health  
- ✅ Counter Frontend accessible on http://localhost:3000
- ✅ OEE Frontend accessible on http://localhost:3001
- ✅ No authentication or connection errors in logs
- ✅ Real-time data flowing between components

This troubleshooting guide should help you avoid the common pitfalls and get your system running smoothly on the first try.