# Migration Guide: InfluxDB to TimescaleDB

This guide helps you migrate your Industrial ADAM Logger from InfluxDB to TimescaleDB, taking advantage of new features like windowed rate calculation and dead letter queue.

## 🎯 Why Migrate to TimescaleDB?

### Benefits
- **PostgreSQL Foundation**: Full SQL support with familiar tools and ecosystem
- **Better Performance**: Optimized for time-series data with automatic partitioning
- **Advanced Analytics**: Continuous aggregates, compression, and data retention policies
- **Reliability**: Dead letter queue ensures no data loss during outages
- **Windowed Rate Calculation**: More stable and accurate production metrics
- **Cost Effective**: Open source with no vendor lock-in

### New Features in V2
- Windowed rate calculation with configurable time windows
- Dead letter queue for automatic retry of failed writes
- Circular buffer for efficient memory management
- Counter overflow detection (16-bit and 32-bit)
- .NET 9 performance optimizations

## 📊 Data Migration Strategy

### Option 1: Fresh Start (Recommended for Development)
Start fresh with TimescaleDB if historical data isn't critical:

```bash
# 1. Stop the InfluxDB-based logger
docker-compose down

# 2. Start TimescaleDB stack
cd docker
docker-compose up -d timescaledb

# 3. Update configuration (see Configuration Changes below)

# 4. Start the updated logger
docker-compose up -d adam-logger
```

### Option 2: Parallel Run (Recommended for Production)
Run both databases temporarily to ensure smooth transition:

```bash
# 1. Keep existing InfluxDB logger running

# 2. Deploy TimescaleDB instance
docker run -d --name adam-timescaledb \
  -p 5433:5432 \
  -e POSTGRES_DB=adam_counters \
  -e POSTGRES_USER=adam_user \
  -e POSTGRES_PASSWORD=secure_password \
  timescale/timescaledb:2.17.2-pg17

# 3. Deploy a second logger instance with TimescaleDB config
# 4. Verify data collection in TimescaleDB
# 5. Gradually transition dashboards and consumers
# 6. Decommission InfluxDB after validation period
```

### Option 3: Historical Data Migration
Export from InfluxDB and import to TimescaleDB:

```bash
# 1. Export data from InfluxDB
influx export \
  --bucket adam_counters \
  --start 2024-01-01T00:00:00Z \
  --output-path influx_export.lp

# 2. Convert to CSV for TimescaleDB import
# (Use custom script or tool to convert line protocol to CSV)

# 3. Import to TimescaleDB
psql postgresql://adam_user:password@localhost:5433/adam_counters << EOF
\COPY counter_data FROM 'converted_data.csv' WITH CSV HEADER;
EOF
```

## 🔧 Configuration Changes

### 1. Update appsettings.json

#### Remove InfluxDB Configuration:
```json
// OLD - Remove this section
"InfluxDb": {
  "Url": "http://localhost:8086",
  "Token": "your-token",
  "Organization": "adam_org",
  "Bucket": "adam_counters",
  "Measurement": "counter_data"
}
```

#### Add TimescaleDB Configuration:
```json
// NEW - Add this section
"TimescaleDb": {
  "Host": "localhost",
  "Port": 5433,
  "Database": "adam_counters",
  "Username": "adam_user",
  "Password": "adam_password",
  "TableName": "counter_data",
  "BatchSize": 50,
  "FlushIntervalMs": 5000,
  "EnableDeadLetterQueue": true,
  "MaxRetryAttempts": 3,
  "RetryDelayMs": 1000,
  "Tags": {
    "environment": "production"
  }
}
```

### 2. Add Windowed Rate Configuration

Update channel configurations with rate window settings:

```json
"Channels": [
  {
    "ChannelNumber": 0,
    "Name": "ProductionCounter",
    "StartRegister": 0,
    "RegisterCount": 2,
    "RateWindowSeconds": 60,    // NEW: 1-minute window
    "MaxChangeRate": 1000        // NEW: Alert threshold
  },
  {
    "ChannelNumber": 1,
    "Name": "RejectCounter",
    "StartRegister": 2,
    "RegisterCount": 2,
    "RateWindowSeconds": 180,   // NEW: 3-minute window
    "MaxChangeRate": 100
  }
]
```

### 3. Update Docker Compose

Replace InfluxDB service with TimescaleDB:

```yaml
# OLD - Remove
influxdb:
  image: influxdb:2.7
  # ...

# NEW - Add
timescaledb:
  image: timescale/timescaledb:2.17.2-pg17
  container_name: adam-timescaledb
  ports:
    - "5433:5432"
  environment:
    - POSTGRES_DB=adam_counters
    - POSTGRES_USER=adam_user
    - POSTGRES_PASSWORD=adam_password
  volumes:
    - timescaledb_data:/var/lib/postgresql/data
```

## 📈 Database Schema

### TimescaleDB Table Structure

The application automatically creates the necessary schema:

```sql
CREATE TABLE counter_data (
    time TIMESTAMPTZ NOT NULL,
    device_id TEXT NOT NULL,
    channel INT NOT NULL,
    raw_value BIGINT,
    processed_value DOUBLE PRECISION,
    rate DOUBLE PRECISION,        -- NEW: Windowed rate calculation
    quality INT,
    tags JSONB,
    PRIMARY KEY (time, device_id, channel)
);

-- Convert to hypertable for time-series optimization
SELECT create_hypertable('counter_data', 'time');

-- Optional: Add compression policy (after 7 days)
SELECT add_compression_policy('counter_data', INTERVAL '7 days');

-- Optional: Add retention policy (keep 90 days)
SELECT add_retention_policy('counter_data', INTERVAL '90 days');
```

## 🔄 Grafana Dashboard Migration

### 1. Update Data Source

Add TimescaleDB as a PostgreSQL data source:

```yaml
# Grafana datasource configuration
apiVersion: 1
datasources:
  - name: TimescaleDB
    type: postgres
    url: timescaledb:5432
    database: adam_counters
    user: adam_user
    password: adam_password
    sslmode: disable
```

### 2. Convert Queries

#### InfluxDB Query (Flux):
```flux
from(bucket: "adam_counters")
  |> range(start: -1h)
  |> filter(fn: (r) => r._measurement == "counter_data")
  |> filter(fn: (r) => r.device_id == "ADAM001")
```

#### TimescaleDB Query (SQL):
```sql
SELECT 
  time,
  device_id,
  channel,
  processed_value,
  rate
FROM counter_data
WHERE 
  time > NOW() - INTERVAL '1 hour'
  AND device_id = 'ADAM001'
ORDER BY time DESC;
```

### 3. Common Dashboard Queries

#### Production Rate:
```sql
SELECT 
  time_bucket('1 minute', time) AS minute,
  device_id,
  AVG(rate) as avg_rate,
  MAX(rate) as peak_rate
FROM counter_data
WHERE 
  time > NOW() - INTERVAL '24 hours'
  AND channel = 0
GROUP BY minute, device_id
ORDER BY minute DESC;
```

#### Hourly Totals:
```sql
SELECT 
  time_bucket('1 hour', time) AS hour,
  device_id,
  MAX(processed_value) - MIN(processed_value) as hourly_production
FROM counter_data
WHERE 
  time > NOW() - INTERVAL '7 days'
  AND channel = 0
GROUP BY hour, device_id
ORDER BY hour DESC;
```

## 🚨 Troubleshooting

### Connection Issues

#### Error: "Failed to connect to TimescaleDB"
```bash
# Check TimescaleDB is running
docker ps | grep timescale

# Test connection
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters

# Check logs
docker logs adam-timescaledb
```

### Data Not Appearing

#### Check hypertable creation:
```sql
SELECT * FROM timescaledb_information.hypertables;
```

#### Verify data insertion:
```sql
SELECT COUNT(*) FROM counter_data;
SELECT * FROM counter_data ORDER BY time DESC LIMIT 10;
```

### Performance Issues

#### Add indexes for common queries:
```sql
CREATE INDEX idx_device_time ON counter_data(device_id, time DESC);
CREATE INDEX idx_channel_time ON counter_data(channel, time DESC);
```

#### Enable compression:
```sql
ALTER TABLE counter_data SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'device_id,channel'
);
```

## 📝 Validation Checklist

Before decommissioning InfluxDB:

- [ ] TimescaleDB is receiving data from all devices
- [ ] Grafana dashboards are working with TimescaleDB
- [ ] Rate calculations are accurate with windowed calculation
- [ ] Dead letter queue is configured and tested
- [ ] Historical data migration completed (if needed)
- [ ] Backup strategy implemented for TimescaleDB
- [ ] Alert rules updated to use TimescaleDB
- [ ] Documentation updated for operations team

## 🆘 Rollback Plan

If issues arise, you can quickly rollback:

1. **Keep InfluxDB configuration backup**
2. **Run both databases in parallel initially**
3. **Switch back by updating configuration**:
   ```bash
   # Restore InfluxDB configuration
   cp config/appsettings.influxdb.backup.json src/Industrial.Adam.Logger.Console/appsettings.json
   
   # Restart logger
   docker-compose restart adam-logger
   ```

## 📚 Additional Resources

- [TimescaleDB Documentation](https://docs.timescale.com/)
- [Configuration Guide](configuration-guide.md)
- [Development Setup](../DEVELOPMENT_SETUP.md)
- [Windowed Rate Calculation Details](../src/Industrial.Adam.Logger.Core/Processing/WindowedRateCalculator.cs)
- [Dead Letter Queue Implementation](../src/Industrial.Adam.Logger.Core/Storage/DeadLetterQueue.cs)