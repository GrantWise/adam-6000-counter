# Docker Deployment Guide

This guide covers deploying the Industrial ADAM Logger stack using Docker Compose.

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
| InfluxDB | 8086 | Time-series database | admin/admin123 |
| Grafana | 3002 | Visualization dashboard | admin/admin |
| Prometheus | 9090 | Metrics collection | None |
| ADAM Logger | - | C# logging service | None |

## Configuration

### 1. Device Configuration

Edit `config/adam_config_v2.json` to configure your ADAM devices:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AdamLogger": {
    "GlobalPollIntervalMs": 2000,
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
            "RegisterAddress": 0,
            "RegisterCount": 2,
            "Enabled": true
          }
        ]
      }
    ]
  },
  "InfluxDb": {
    "Url": "http://influxdb:8086",
    "Token": "adam-super-secret-token",
    "Organization": "adam_org",
    "Bucket": "adam_counters"
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
nano config/adam_config_v2.json

# Start the stack
docker-compose up -d
```

### Demo Mode with Simulator

```bash
# Start with simulated ADAM devices
docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up -d

# The simulator creates 3 virtual ADAM devices on ports 5020-5022
```

## Data Storage

### InfluxDB Configuration
- **Organization**: `adam_org`
- **Bucket**: `adam_counters` (365-day retention)
- **Token**: `adam-super-secret-token` (change in production!)

### Data Schema
```
counter_data,device=Device001,channel=0 value=12345,quality="Good" 1640995200000000000
```

## Monitoring

### Grafana Dashboards

Access Grafana at http://localhost:3002 (admin/admin)

Pre-configured dashboards include:
- Counter values and trends
- Device status and health
- Data quality indicators

### Prometheus Metrics

Access Prometheus at http://localhost:9090

Available metrics:
- Container resource usage
- Application performance metrics
- System health indicators

## Troubleshooting

### Check Service Status
```bash
docker-compose ps
docker-compose logs adam-logger
```

### Common Issues

**Logger not connecting to device:**
1. Check device IP in configuration
2. Verify network connectivity: `docker-compose exec adam-logger ping <device-ip>`
3. Ensure Modbus TCP port 502 is accessible

**No data in InfluxDB:**
1. Check logger logs for errors
2. Verify InfluxDB is running: http://localhost:8086
3. Check configuration file syntax

**Grafana shows no data:**
1. Verify InfluxDB datasource is configured
2. Check if data exists in InfluxDB
3. Review Grafana datasource settings

## Advanced Usage

### Multiple Devices

Add multiple devices to the configuration:

```json
{
  "AdamLogger": {
    "Devices": [
      {
        "DeviceId": "Device001",
        "IpAddress": "192.168.1.100"
      },
      {
        "DeviceId": "Device002",
        "IpAddress": "192.168.1.101"
      }
    ]
  }
}
```

### Custom Configuration

Use environment variables to override configuration:

```bash
# Use a different configuration file
docker run -v $(pwd)/myconfig.json:/app/appsettings.json adam-logger
```

### Data Export

Export data from InfluxDB:

```bash
# Export last 24 hours as CSV
docker exec adam-influxdb influx query \
  'from(bucket:"adam_counters") |> range(start: -24h)' \
  --org adam_org \
  --token adam-super-secret-token \
  --format csv > export.csv
```

## Security

For production deployments:

1. **Change default passwords** in `docker-compose.yml`
2. **Update InfluxDB token** to a secure value
3. **Use Docker secrets** for sensitive data
4. **Enable HTTPS** for external access
5. **Restrict network access** using firewall rules

## Maintenance

### Backup

```bash
# Backup InfluxDB data
docker exec adam-influxdb influx backup /backup
docker cp adam-influxdb:/backup ./influx-backup

# Backup Grafana dashboards
docker cp adam-grafana:/var/lib/grafana ./grafana-backup
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

- Adjust `GlobalPollIntervalMs` in configuration for polling frequency
- Configure InfluxDB retention policies for data management
- Use Grafana query caching for better dashboard performance

## Support

For issues or questions:
1. Check service logs: `docker-compose logs`
2. Review configuration syntax
3. Verify network connectivity
4. Consult main project documentation