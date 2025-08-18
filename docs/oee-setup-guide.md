# OEE Setup Guide

This guide provides step-by-step instructions for setting up and configuring the Industrial ADAM OEE service in your environment. Follow these instructions to get the OEE service running in development, staging, or production environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Development Setup](#development-setup)
- [Docker Setup](#docker-setup)
- [Production Deployment](#production-deployment)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Testing the Installation](#testing-the-installation)
- [Integration with Logger Service](#integration-with-logger-service)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

- **.NET 9 SDK**: Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker & Docker Compose**: For containerized deployment
- **TimescaleDB**: PostgreSQL with TimescaleDB extension
- **Git**: For source code management

### Phase 1 Requirements (Completed)

Phase 1 implementation includes:
- **Equipment Line Management**: ADAM device to production line mapping
- **Reason Code System**: 2-level classification (3x3 matrix)
- **Enhanced Work Order Validation**: Job sequencing and equipment availability
- **Stoppage Classification**: Enhanced stoppage tracking with reason codes
- **Job Completion Issues**: Under-completion and overproduction tracking

### Hardware Requirements

| Environment | CPU | Memory | Storage |
|-------------|-----|--------|---------|
| Development | 2 cores | 4 GB | 10 GB |
| Staging | 4 cores | 8 GB | 50 GB |
| Production | 8+ cores | 16+ GB | 200+ GB |

### Network Requirements

- **Database Connection**: Port 5432 (PostgreSQL/TimescaleDB)
- **API Service**: Port 5001 (configurable)
- **Health Checks**: Port 5001 (same as API)

## Development Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd adam-6000-counter
```

### 2. Install Dependencies

```bash
# Restore .NET packages
dotnet restore src/Industrial.Adam.Oee/

# Verify installation
dotnet --version  # Should show 9.x.x
```

### 3. Setup TimescaleDB

#### Option A: Using Docker (Recommended)

```bash
cd docker
docker-compose up -d timescaledb

# Wait for database to be ready
docker-compose logs -f timescaledb
```

#### Option B: Local PostgreSQL Installation

1. Install PostgreSQL 15+ with TimescaleDB extension
2. Create database and user:

```sql
-- Connect as postgres superuser
CREATE DATABASE adam_counters;
CREATE USER adam_user WITH PASSWORD 'adam_password';
GRANT ALL PRIVILEGES ON DATABASE adam_counters TO adam_user;

-- Connect to adam_counters database
CREATE EXTENSION IF NOT EXISTS timescaledb;
```

### 4. Configure the Service

Create or update `src/Industrial.Adam.Oee/WebApi/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=adam_counters;Username=adam_user;Password=adam_password"
  },
  "Oee": {
    "Database": {
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 60,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 50
    },
    "Cache": {
      "DefaultExpirationMinutes": 1,
      "OeeMetricsExpirationMinutes": 1,
      "WorkOrderExpirationMinutes": 5,
      "DeviceStatusExpirationMinutes": 1,
      "EquipmentLineExpirationMinutes": 5,
      "ReasonCodeExpirationMinutes": 10
    },
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": true,
      "SlowQueryThresholdMs": 100,
      "LogSlowQueries": true
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### 5. Run the Service

```bash
cd src/Industrial.Adam.Oee/WebApi
dotnet run

# Service should start on http://localhost:5001
```

### 6. Verify Installation

```bash
# Test health endpoint
curl http://localhost:5001/health

# Test detailed health
curl http://localhost:5001/api/health/detailed
```

## Docker Setup

### 1. Build the Image

```bash
cd src/Industrial.Adam.Oee
docker build -t industrial-adam-oee:latest .
```

### 2. Run with Docker Compose

Create `docker/docker-compose.oee.yml`:

```yaml
version: '3.8'

services:
  timescaledb:
    image: timescale/timescaledb:latest-pg15
    container_name: adam-timescaledb
    environment:
      - POSTGRES_DB=adam_counters
      - POSTGRES_USER=adam_user
      - POSTGRES_PASSWORD=adam_password
    ports:
      - "5433:5432"
    volumes:
      - timescale_data:/var/lib/postgresql/data
      - ./sql/init.sql:/docker-entrypoint-initdb.d/init.sql
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U adam_user -d adam_counters"]
      interval: 30s
      timeout: 10s
      retries: 3

  oee-api:
    image: industrial-adam-oee:latest
    container_name: adam-oee-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=timescaledb;Port=5432;Database=adam_counters;Username=adam_user;Password=adam_password
      - Oee__Database__ConnectionTimeoutSeconds=30
      - Oee__Cache__OeeMetricsExpirationMinutes=2
      - Oee__Cache__EquipmentLineExpirationMinutes=15
      - Oee__Cache__ReasonCodeExpirationMinutes=60
      - Oee__Performance__SlowQueryThresholdMs=1000
    ports:
      - "5001:8080"
    depends_on:
      timescaledb:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

volumes:
  timescale_data:
```

### 3. Start Services

```bash
cd docker
docker-compose -f docker-compose.oee.yml up -d

# Check status
docker-compose -f docker-compose.oee.yml ps

# View logs
docker-compose -f docker-compose.oee.yml logs -f oee-api
```

## Production Deployment

### 1. Environment Preparation

#### Create Production Configuration

Create `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${CONNECTION_STRING}"
  },
  "Oee": {
    "Database": {
      "ConnectionTimeoutSeconds": 45,
      "CommandTimeoutSeconds": 120,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 200
    },
    "Cache": {
      "DefaultExpirationMinutes": 5,
      "OeeMetricsExpirationMinutes": 2,
      "WorkOrderExpirationMinutes": 15,
      "DeviceStatusExpirationMinutes": 1,
      "EquipmentLineExpirationMinutes": 15,
      "ReasonCodeExpirationMinutes": 60
    },
    "Resilience": {
      "DatabaseRetry": {
        "MaxRetryAttempts": 5,
        "BaseDelayMs": 500,
        "UseExponentialBackoff": true,
        "MaxDelayMs": 15000
      },
      "CircuitBreaker": {
        "ExceptionsAllowedBeforeBreaking": 10,
        "DurationOfBreakSeconds": 60,
        "SamplingDurationSeconds": 120,
        "MinimumThroughput": 20
      }
    },
    "Performance": {
      "Enabled": true,
      "EnableDetailedMetrics": false,
      "SlowQueryThresholdMs": 500,
      "LogSlowQueries": true
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "/app/logs/oee-api-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

#### Environment Variables

Create `.env` file for production:

```bash
# Database
CONNECTION_STRING=Host=prod-timescaledb;Port=5432;Database=adam_counters;Username=adam_user;Password=secure_password

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://*:8080

# Logging
LOG_LEVEL=Information

# OEE Configuration
Oee__Database__MaxPoolSize=200
Oee__Cache__OeeMetricsExpirationMinutes=2
Oee__Performance__SlowQueryThresholdMs=500
```

### 2. Database Preparation

#### Production Database Setup

```sql
-- Connect as superuser to create database
CREATE DATABASE adam_counters;
CREATE USER adam_user WITH PASSWORD 'secure_production_password';
GRANT ALL PRIVILEGES ON DATABASE adam_counters TO adam_user;

-- Connect to adam_counters database
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create necessary schemas and tables
-- (Tables will be created automatically by the application)

-- Set up proper permissions
GRANT USAGE ON SCHEMA public TO adam_user;
GRANT CREATE ON SCHEMA public TO adam_user;
```

#### Database Performance Tuning

Add to PostgreSQL configuration (`postgresql.conf`):

```ini
# Memory
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB

# TimescaleDB
shared_preload_libraries = 'timescaledb'
max_connections = 200

# Logging
log_min_duration_statement = 1000
log_statement = 'mod'
```

### 3. Production Docker Deployment

#### Production docker-compose.yml

```yaml
version: '3.8'

services:
  timescaledb:
    image: timescale/timescaledb:latest-pg15
    container_name: prod-timescaledb
    environment:
      - POSTGRES_DB=adam_counters
      - POSTGRES_USER=adam_user
      - POSTGRES_PASSWORD_FILE=/run/secrets/db_password
    secrets:
      - db_password
    ports:
      - "5432:5432"
    volumes:
      - timescale_data:/var/lib/postgresql/data
      - /opt/adam/backups:/backups
      - /opt/adam/config/postgresql.conf:/etc/postgresql/postgresql.conf
    restart: always
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U adam_user -d adam_counters"]
      interval: 30s
      timeout: 10s
      retries: 5
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '1.0'

  oee-api:
    image: industrial-adam-oee:latest
    container_name: prod-oee-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=timescaledb;Port=5432;Database=adam_counters;Username=adam_user;Password_FILE=/run/secrets/db_password
    secrets:
      - db_password
    ports:
      - "80:8080"
    volumes:
      - /opt/adam/logs:/app/logs
      - /opt/adam/config/appsettings.Production.json:/app/appsettings.Production.json:ro
    depends_on:
      timescaledb:
        condition: service_healthy
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 5
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

secrets:
  db_password:
    file: /opt/adam/secrets/db_password.txt

volumes:
  timescale_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /opt/adam/data
```

### 4. SSL/TLS Configuration (Recommended)

#### Using Reverse Proxy (nginx)

Create `/etc/nginx/sites-available/oee-api`:

```nginx
server {
    listen 443 ssl http2;
    server_name oee-api.yourdomain.com;

    ssl_certificate /etc/ssl/certs/oee-api.crt;
    ssl_certificate_key /etc/ssl/private/oee-api.key;

    location / {
        proxy_pass http://localhost:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /health {
        proxy_pass http://localhost:80/health;
        access_log off;
    }
}
```

## Configuration

### Environment-Specific Settings

#### Development
- Enable detailed metrics and debug logging
- Shorter cache times for immediate feedback
- Lower performance thresholds for optimization

#### Staging
- Production-like configuration with debug logging
- Moderate cache times
- Performance monitoring enabled

#### Production
- Optimized for performance and stability
- Longer cache times for stable data
- Minimal logging for performance

### Security Considerations

1. **Database Security**:
   - Use strong passwords
   - Enable SSL connections
   - Restrict network access
   - Regular security updates

2. **Application Security**:
   - Run as non-root user
   - Use secrets management
   - Enable HTTPS
   - Monitor access logs

3. **Network Security**:
   - Firewall configuration
   - VPN access for management
   - Regular security scans

## Database Setup

### Initial Schema

The application automatically creates necessary tables, but you can prepare them manually. Phase 1 includes additional tables for equipment management and reason codes:

```sql
-- Connect to adam_counters database

-- Work orders table
CREATE TABLE IF NOT EXISTS work_orders (
    id VARCHAR(50) PRIMARY KEY,
    work_order_description VARCHAR(200) NOT NULL,
    product_id VARCHAR(50) NOT NULL,
    product_description VARCHAR(200) NOT NULL,
    planned_quantity DECIMAL(10,2) NOT NULL,
    unit_of_measure VARCHAR(20) DEFAULT 'pieces',
    scheduled_start_time TIMESTAMPTZ NOT NULL,
    scheduled_end_time TIMESTAMPTZ NOT NULL,
    resource_reference VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Scheduled',
    actual_quantity_good DECIMAL(10,2) DEFAULT 0,
    actual_quantity_scrap DECIMAL(10,2) DEFAULT 0,
    actual_start_time TIMESTAMPTZ,
    actual_end_time TIMESTAMPTZ,
    operator_id VARCHAR(50),
    completion_notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- OEE calculations table
CREATE TABLE IF NOT EXISTS oee_calculations (
    id VARCHAR(100) PRIMARY KEY,
    resource_reference VARCHAR(20) NOT NULL,
    calculation_period_start TIMESTAMPTZ NOT NULL,
    calculation_period_end TIMESTAMPTZ NOT NULL,
    availability_percentage DECIMAL(5,2) NOT NULL,
    performance_percentage DECIMAL(5,2) NOT NULL,
    quality_percentage DECIMAL(5,2) NOT NULL,
    oee_percentage DECIMAL(5,2) NOT NULL,
    period_hours DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create hypertables for time-series data
SELECT create_hypertable('oee_calculations', 'calculation_period_start');

-- Create indexes for performance
-- Phase 1 tables (automatically created by application)
CREATE TABLE IF NOT EXISTS equipment_lines (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(50) UNIQUE NOT NULL,
    line_name VARCHAR(100) NOT NULL,
    adam_device_id VARCHAR(50) NOT NULL,
    adam_channel INTEGER NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(adam_device_id, adam_channel)
);

CREATE TABLE IF NOT EXISTS stoppage_reason_categories (
    id SERIAL PRIMARY KEY,
    category_code VARCHAR(10) UNIQUE NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    category_description TEXT,
    matrix_row INTEGER NOT NULL CHECK (matrix_row BETWEEN 1 AND 3),
    matrix_col INTEGER NOT NULL CHECK (matrix_col BETWEEN 1 AND 3),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS stoppage_reason_subcodes (
    id SERIAL PRIMARY KEY,
    category_id INTEGER REFERENCES stoppage_reason_categories(id),
    subcode VARCHAR(10) NOT NULL,
    subcode_name VARCHAR(100) NOT NULL,
    subcode_description TEXT,
    matrix_row INTEGER NOT NULL CHECK (matrix_row BETWEEN 1 AND 3),
    matrix_col INTEGER NOT NULL CHECK (matrix_col BETWEEN 1 AND 3),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(category_id, subcode)
);

CREATE TABLE IF NOT EXISTS equipment_stoppages (
    id SERIAL PRIMARY KEY,
    line_id VARCHAR(50) NOT NULL REFERENCES equipment_lines(line_id),
    work_order_id VARCHAR(100) REFERENCES work_orders(work_order_id),
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP,
    duration_minutes DECIMAL(10,2),
    is_classified BOOLEAN DEFAULT false,
    category_code VARCHAR(10) REFERENCES stoppage_reason_categories(category_code),
    subcode VARCHAR(10),
    operator_comments TEXT,
    classified_by VARCHAR(100),
    classified_at TIMESTAMP,
    auto_detected BOOLEAN DEFAULT true,
    minimum_threshold_minutes INTEGER DEFAULT 5,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS job_completion_issues (
    id SERIAL PRIMARY KEY,
    work_order_id VARCHAR(100) NOT NULL REFERENCES work_orders(work_order_id),
    issue_type VARCHAR(50) NOT NULL,
    completion_percentage DECIMAL(5,2),
    target_quantity DECIMAL(10,2),
    actual_quantity DECIMAL(10,2),
    category_code VARCHAR(10) REFERENCES stoppage_reason_categories(category_code),
    subcode VARCHAR(10),
    operator_comments TEXT,
    resolved_by VARCHAR(100),
    resolved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_work_orders_resource_status ON work_orders(resource_reference, status);
CREATE INDEX IF NOT EXISTS idx_work_orders_scheduled_times ON work_orders(scheduled_start_time, scheduled_end_time);
CREATE INDEX IF NOT EXISTS idx_oee_calculations_resource_time ON oee_calculations(resource_reference, calculation_period_start);
CREATE INDEX IF NOT EXISTS idx_equipment_lines_adam_device ON equipment_lines(adam_device_id, adam_channel);
CREATE INDEX IF NOT EXISTS idx_equipment_stoppages_line_time ON equipment_stoppages(line_id, start_time);
CREATE INDEX IF NOT EXISTS idx_stoppage_reason_categories_matrix ON stoppage_reason_categories(matrix_row, matrix_col);
CREATE INDEX IF NOT EXISTS idx_job_completion_issues_work_order ON job_completion_issues(work_order_id);
```

## Phase 1 Setup

### Equipment Line Configuration

After database setup, configure equipment lines to map ADAM devices to production lines:

```bash
# Create equipment line via API
curl -X POST http://localhost:5001/api/equipment-lines \
  -H "Content-Type: application/json" \
  -d '{
    "lineId": "LINE-001",
    "lineName": "Production Line A",
    "adamDeviceId": "ADAM-6051-01",
    "adamChannel": 0,
    "isActive": true
  }'

# Verify equipment line creation
curl http://localhost:5001/api/equipment-lines/LINE-001
```

### Reason Code Initialization

Initialize the 2-level reason code system:

```sql
-- Insert reason code categories (3x3 matrix)
INSERT INTO stoppage_reason_categories (category_code, category_name, category_description, matrix_row, matrix_col, is_active) VALUES
('A1', 'Equipment Failure', 'Mechanical or electrical equipment failures', 1, 1, true),
('A2', 'Material Issues', 'Raw material or supply problems', 1, 2, true),
('A3', 'Quality Issues', 'Product quality problems', 1, 3, true),
('B1', 'Planned Maintenance', 'Scheduled maintenance activities', 2, 1, true),
('B2', 'Changeover', 'Product or setup changeovers', 2, 2, true),
('B3', 'Training', 'Operator training activities', 2, 3, true),
('C1', 'External Factors', 'Utility or external disruptions', 3, 1, true),
('C2', 'Operator Issues', 'Staffing or operator-related delays', 3, 2, true),
('C3', 'Other', 'Miscellaneous unclassified issues', 3, 3, true);

-- Insert sample subcodes (expand as needed)
INSERT INTO stoppage_reason_subcodes (category_id, subcode, subcode_name, subcode_description, matrix_row, matrix_col, is_active) VALUES
(1, '1', 'Motor Failure', 'Electric motor malfunction', 1, 1, true),
(1, '2', 'Sensor Malfunction', 'Sensor reading errors', 1, 2, true),
(1, '3', 'Hydraulic Issues', 'Hydraulic system problems', 1, 3, true),
(2, '1', 'Material Shortage', 'Raw material not available', 1, 1, true),
(2, '2', 'Material Quality', 'Poor quality raw materials', 1, 2, true),
(2, '3', 'Material Handling', 'Material transport issues', 1, 3, true),
(3, '1', 'Product Defects', 'Manufacturing defects detected', 1, 1, true),
(3, '2', 'Quality Control', 'Quality inspection delays', 1, 2, true),
(3, '3', 'Rework Required', 'Product requires rework', 1, 3, true);
```

### Verify Reason Code API

```bash
# Get reason code categories
curl http://localhost:5001/api/reason-codes/categories

# Get subcodes for category A1
curl http://localhost:5001/api/reason-codes/subcodes/A1

# Get complete reason code matrix
curl http://localhost:5001/api/reason-codes/matrix
```

### Phase 1 Validation Tests

```bash
# Test equipment line creation with validation
curl -X POST http://localhost:5001/api/equipment-lines \
  -H "Content-Type: application/json" \
  -d '{
    "lineId": "LINE-002",
    "lineName": "Production Line B",
    "adamDeviceId": "ADAM-6051-01",
    "adamChannel": 0,
    "isActive": true
  }'
# Should fail with 409 - device/channel already in use

# Test work order with equipment validation
curl -X POST http://localhost:5001/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId": "WO-PHASE1-001",
    "workOrderDescription": "Phase 1 Test Job",
    "productId": "TEST-PRODUCT",
    "productDescription": "Test Product for Phase 1",
    "plannedQuantity": 100,
    "scheduledStartTime": "2024-01-15T08:00:00Z",
    "scheduledEndTime": "2024-01-15T16:00:00Z",
    "deviceId": "ADAM-6051-01"
  }'
# Should succeed with proper equipment line mapping

# Test stoppage classification
curl -X PUT http://localhost:5001/api/stoppages/1/classify \
  -H "Content-Type: application/json" \
  -d '{
    "categoryCode": "A1",
    "subcode": "1",
    "operatorComments": "Motor failure during production",
    "classifiedBy": "OP-001"
  }'
```

### Data Retention Policies

```sql
-- Set up data retention (optional)
SELECT add_retention_policy('oee_calculations', INTERVAL '90 days');
SELECT add_retention_policy('equipment_stoppages', INTERVAL '1 year');
```

## Testing the Installation

### 1. Health Checks

```bash
# Basic health
curl http://localhost:5001/health

# Expected response:
# {
#   "status": "Healthy",
#   "timestamp": "2024-01-15T10:30:00Z",
#   "service": "OEE API",
#   "version": "1.0.0"
# }

# Detailed health
curl http://localhost:5001/api/health/detailed

# Expected response includes database status
```

### 2. API Functionality Tests

```bash
# Test work order creation
curl -X POST http://localhost:5001/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId": "TEST-001",
    "workOrderDescription": "Test Work Order",
    "productId": "TEST-PRODUCT",
    "productDescription": "Test Product",
    "plannedQuantity": 100,
    "scheduledStartTime": "2024-01-15T08:00:00Z",
    "scheduledEndTime": "2024-01-15T16:00:00Z",
    "deviceId": "TEST-DEVICE"
  }'

# Expected: 201 Created with work order ID

# Test OEE calculation (requires active work order and counter data)
curl "http://localhost:5001/api/oee/current?deviceId=TEST-DEVICE"

# Test Phase 1 endpoints
# Get equipment lines
curl http://localhost:5001/api/equipment-lines

# Get reason codes
curl http://localhost:5001/api/reason-codes/categories

# Get unclassified stoppages
curl http://localhost:5001/api/stoppages/unclassified
```

### 3. Performance Tests

```bash
# Load test (requires Apache Bench or similar)
ab -n 100 -c 10 http://localhost:5001/health

# Monitor response times in logs
tail -f logs/oee-api-*.txt | grep "HTTP"
```

### 4. Database Connectivity Test

```bash
# Test database connection
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "SELECT version();"

# Check table creation
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "\\dt"
```

## Integration with Logger Service

### 1. Verify Counter Data Flow

The OEE service depends on counter data from the Industrial.Adam.Logger service:

```bash
# Check counter data in database
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "
  SELECT device_id, channel_name, count_value, timestamp 
  FROM counter_data 
  ORDER BY timestamp DESC 
  LIMIT 10;"
```

### 2. Configure Device Mapping

Ensure device IDs match between logger and OEE services:

```json
// Logger configuration
{
  "AdamLogger": {
    "Devices": [{
      "DeviceId": "ADAM-6051-01",  // Must match OEE API deviceId parameter
      "IpAddress": "192.168.1.100",
      "Channels": [...]
    }]
  }
}
```

### 3. Data Validation

```bash
# Verify data continuity
curl "http://localhost:5001/api/oee/current?deviceId=ADAM-6051-01"

# Check for recent counter data
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "
  SELECT device_id, COUNT(*) as record_count, MAX(timestamp) as latest_data
  FROM counter_data 
  WHERE timestamp > NOW() - INTERVAL '1 hour'
  GROUP BY device_id;"
```

## Troubleshooting

### Common Issues

#### 1. Service Won't Start

**Symptoms**: 
- Service exits immediately
- Database connection errors
- Configuration validation errors

**Solutions**:
```bash
# Check configuration
dotnet run --project src/Industrial.Adam.Oee/WebApi --configuration Development

# Validate database connection
docker exec -it adam-timescaledb pg_isready -U adam_user -d adam_counters

# Check logs
tail -f logs/oee-api-*.txt
```

#### 2. API Returns 500 Errors

**Symptoms**:
- Internal server errors on API calls
- Database timeout errors
- Memory issues

**Solutions**:
```bash
# Check database performance
docker stats adam-timescaledb

# Increase timeouts in configuration
# Review connection pool settings
# Check memory usage
```

#### 3. OEE Calculations Return No Data

**Symptoms**:
- 404 errors for OEE endpoints
- No work orders found
- Missing counter data

**Solutions**:
```bash
# Verify work order exists
curl "http://localhost:5001/api/jobs/active?deviceId=ADAM-6051-01"

# Check counter data availability
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "
  SELECT COUNT(*) FROM counter_data WHERE device_id = 'ADAM-6051-01';"

# Verify device ID matches logger configuration
```

#### 4. Performance Issues

**Symptoms**:
- Slow API responses
- High memory usage
- Database locks

**Solutions**:
1. **Optimize database queries**:
   ```sql
   -- Check slow queries
   SELECT query, mean_time, calls 
   FROM pg_stat_statements 
   ORDER BY mean_time DESC;
   ```

2. **Adjust cache settings**:
   ```json
   {
     "Oee": {
       "Cache": {
         "OeeMetricsExpirationMinutes": 5  // Increase for less frequent calculations
       }
     }
   }
   ```

3. **Monitor resource usage**:
   ```bash
   docker stats
   htop
   ```

### Log Analysis

#### Application Logs
```bash
# View structured logs
tail -f logs/oee-api-*.txt | jq .

# Filter for errors
grep "ERROR" logs/oee-api-*.txt

# Performance monitoring
grep "slow query" logs/oee-api-*.txt
```

#### Database Logs
```bash
# PostgreSQL logs
docker logs adam-timescaledb 2>&1 | grep ERROR

# Long-running queries
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters -c "
  SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
  FROM pg_stat_activity 
  WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes';"
```

### Getting Help

1. **Check Documentation**:
   - [Configuration Guide](oee-configuration-guide.md)
   - [API Reference](oee-api-reference.md)

2. **Review Logs**:
   - Application logs for business logic issues
   - Database logs for data problems
   - Container logs for infrastructure issues

3. **Validate Configuration**:
   - Use health endpoints for quick diagnostics
   - Test database connectivity separately
   - Verify environment variables

4. **Performance Monitoring**:
   - Enable detailed metrics in development
   - Monitor database query performance
   - Check resource utilization

This setup guide should get you running in any environment. For production deployments, ensure you follow security best practices and monitor the service continuously.