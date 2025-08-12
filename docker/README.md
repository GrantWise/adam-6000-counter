# Docker Deployment Guide

This guide covers deploying the Industrial ADAM Logger stack using Docker Compose with TimescaleDB.

## Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/adam-6000-counter.git
cd adam-6000-counter/docker

# Start the stack
docker-compose up -d

# View logs
docker-compose logs -f
```

## Services Overview

| Service | Port | Purpose | Default Credentials |
|---------|------|---------|---------------------|
| TimescaleDB | 5433 | PostgreSQL time-series database | adam_user/adam_password |
| Grafana | 3002 | Visualization dashboard | admin/admin |
| Prometheus | 9090 | Metrics collection | None |
| Node Exporter | 9100 | System metrics | None |
| ADAM Logger | - | C# logging service | None |

## Configuration

### 1. Device Configuration

Edit `config/adam_config_docker.json` to configure your ADAM devices:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Industrial.Adam.Logger.Core": "Debug"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 5000,
    "Devices": [
      {
        "DeviceId": "Device001",
        "IpAddress": "192.168.1.100",  // Your ADAM device IP
        "Port": 502,
        "UnitId": 1,
        "Enabled": true,
        "Channels": [
          {
            "ChannelNumber": 0,
            "Name": "ProductionCounter",
            "StartRegister": 0,
            "RegisterCount": 2,
            "RateWindowSeconds": 60,     // NEW: Windowed rate calculation
            "MaxChangeRate": 1000,       // NEW: Quality monitoring
            "Enabled": true
          }
        ]
      }
    ],
    "TimescaleDb": {                   // NEW: TimescaleDB configuration
      "Host": "timescaledb",
      "Port": 5432,
      "Database": "adam_counters",
      "Username": "adam_user",
      "Password": "adam_password",
      "EnableDeadLetterQueue": true    // NEW: Data reliability
    }
  }
}
```

### 2. Environment Variables

The following environment variables are available:

- `DEMO_MODE`: Set to `true` to use demo configuration
- `LOG_LEVEL`: Logging level (Debug, Information, Warning, Error)

## Deployment Modes

### Production Mode (Real Devices)

```bash
# Edit your device configuration
nano config/adam_config_docker.json

# Start the stack
docker-compose up -d
```

### Demo Mode with Simulator

```bash
# Start with simulated ADAM devices
./scripts/setup-dev-environment.sh

# Or manually start simulators
./scripts/start-simulators.sh
docker-compose up -d
```

## Data Storage

### TimescaleDB Configuration
- **Database**: `adam_counters`
- **Table**: `counter_data` (hypertable with 1-hour chunks)
- **Username**: `adam_user` / **Password**: `adam_password`

### Data Schema
```sql
CREATE TABLE counter_data (
    timestamp TIMESTAMPTZ NOT NULL,
    device_id TEXT NOT NULL,
    channel INTEGER NOT NULL,
    raw_value BIGINT NOT NULL,
    processed_value DOUBLE PRECISION,
    rate DOUBLE PRECISION,           -- NEW: Windowed rate calculation
    quality TEXT,
    unit TEXT DEFAULT 'counts',
    PRIMARY KEY (timestamp, device_id, channel)
);
```

## New Features

### Windowed Rate Calculation
- Configurable time windows per channel (e.g., 60 seconds for production)
- Smooth rate metrics that eliminate brief stoppage spikes
- Automatic counter overflow detection for 16-bit and 32-bit counters

### Dead Letter Queue
- Automatic retry for failed database writes
- Persistent storage survives application restarts
- No data loss during database outages

## Monitoring

### Grafana Dashboards

Access Grafana at http://localhost:3002 (admin/admin)

Pre-configured dashboards include:
- Counter values and windowed rates
- Device status and health
- Data quality indicators
- TimescaleDB performance metrics

### Prometheus Metrics

Access Prometheus at http://localhost:9090

Available metrics:
- Container resource usage
- Application performance metrics
- System health indicators
- Dead letter queue status

## Troubleshooting

### Check Service Status
```bash
docker-compose ps
docker-compose logs adam-logger
docker-compose logs timescaledb
```

### Common Issues

**Logger not connecting to device:**
1. Check device IP in configuration
2. Verify network connectivity: `docker-compose exec adam-logger ping <device-ip>`
3. Ensure Modbus TCP port 502 is accessible

**No data in TimescaleDB:**
1. Check logger logs for errors
2. Verify TimescaleDB is running: `docker exec adam-timescaledb pg_isready`
3. Check configuration file syntax
4. Verify hypertable is created

**Grafana shows no data:**
1. Verify TimescaleDB datasource is configured
2. Check if data exists: `psql postgresql://adam_user:adam_password@localhost:5433/adam_counters`
3. Review Grafana datasource settings

### Database Access

Connect to TimescaleDB:
```bash
# Using docker exec
docker exec -it adam-timescaledb psql -U adam_user -d adam_counters

# From host (if psql installed)
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters
```

Common queries:
```sql
-- Check recent data
SELECT * FROM counter_data ORDER BY timestamp DESC LIMIT 10;

-- Check data volume
SELECT device_id, COUNT(*) FROM counter_data GROUP BY device_id;

-- Check windowed rates
SELECT 
  device_id, 
  AVG(rate) as avg_rate,
  MAX(rate) as max_rate
FROM counter_data 
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY device_id;
```

## Advanced Usage

### Multiple Devices

Add multiple devices to the configuration:

```json
{
  "AdamLogger": {
    "Devices": [
      {
        "DeviceId": "Device001",
        "IpAddress": "192.168.1.100",
        "Channels": [
          {
            "ChannelNumber": 0,
            "RateWindowSeconds": 60
          }
        ]
      },
      {
        "DeviceId": "Device002", 
        "IpAddress": "192.168.1.101",
        "Channels": [
          {
            "ChannelNumber": 0,
            "RateWindowSeconds": 180    // Different window for different device
          }
        ]
      }
    ]
  }
}
```

### Data Export

Export data from TimescaleDB:

```bash
# Export last 24 hours as CSV
docker exec adam-timescaledb psql -U adam_user -d adam_counters -c "\COPY (
  SELECT timestamp, device_id, channel, processed_value, rate 
  FROM counter_data 
  WHERE timestamp > NOW() - INTERVAL '24 hours'
) TO STDOUT WITH CSV HEADER" > export.csv
```

## Security

For production deployments:

1. **Change default passwords** in `docker-compose.yml`
2. **Use strong TimescaleDB password**
3. **Use Docker secrets** for sensitive data
4. **Enable HTTPS** for external access
5. **Restrict network access** using firewall rules
6. **Enable SSL** for TimescaleDB connections

Example secure configuration:
```yaml
# docker-compose.override.yml
services:
  timescaledb:
    environment:
      - POSTGRES_PASSWORD=${TIMESCALE_PASSWORD}
  adam-logger:
    environment:
      - AdamLogger__TimescaleDb__Password=${TIMESCALE_PASSWORD}
      - AdamLogger__TimescaleDb__EnableSsl=true
```

## Maintenance

### Backup

```bash
# Backup TimescaleDB data
docker exec adam-timescaledb pg_dump -U adam_user adam_counters > backup.sql

# Backup with compression
docker exec adam-timescaledb pg_dump -U adam_user adam_counters | gzip > backup.sql.gz

# Backup Grafana dashboards
docker cp adam-grafana:/var/lib/grafana ./grafana-backup
```

### Restore

```bash
# Restore from backup
docker exec -i adam-timescaledb psql -U adam_user adam_counters < backup.sql
```

### Data Retention

Configure automatic data retention:
```sql
-- Connect to TimescaleDB
SELECT add_retention_policy('counter_data', INTERVAL '90 days');

-- Enable compression after 7 days
SELECT add_compression_policy('counter_data', INTERVAL '7 days');
```

### Update Services

```bash
# Pull latest images
docker-compose pull

# Restart with updates
docker-compose up -d
```

### Cleanup

```bash
# Stop services (keeps data)
docker-compose down

# Remove everything including volumes (WARNING: deletes data)
docker-compose down -v
```

## Performance Tuning

### Application Settings
- Adjust `GlobalPollIntervalMs` for polling frequency
- Configure `BatchSize` and `FlushIntervalMs` for write performance
- Set appropriate `RateWindowSeconds` per channel

### TimescaleDB Optimization
```sql
-- Check chunk size
SELECT * FROM timescaledb_information.chunks WHERE hypertable_name = 'counter_data';

-- Optimize chunk interval (if needed)
SELECT set_chunk_time_interval('counter_data', INTERVAL '1 hour');

-- Add indexes for common queries
CREATE INDEX idx_device_time ON counter_data(device_id, timestamp DESC);
```

### Container Resources
```yaml
# docker-compose.override.yml
services:
  timescaledb:
    deploy:
      resources:
        limits:
          memory: 4G
          cpus: '2.0'
  adam-logger:
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
```

## Migration from InfluxDB

See [Migration Guide](../docs/MIGRATION_GUIDE_INFLUXDB_TO_TIMESCALEDB.md) for complete instructions on migrating from InfluxDB to TimescaleDB.

## Support

For issues or questions:
1. Check service logs: `docker-compose logs`
2. Review configuration syntax  
3. Verify network connectivity
4. Consult [Development Setup Guide](../DEVELOPMENT_SETUP.md)
5. Check [Configuration Guide](../docs/configuration-guide.md)