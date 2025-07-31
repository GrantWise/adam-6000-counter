# Python Implementation

This directory contains a lightweight Python implementation of the ADAM-6051 counter logger for development, testing, and simple deployments.

## Overview

The Python implementation (`adam_counter_logger.py`) provides a simple, single-file solution for logging ADAM-6051 counter data to InfluxDB.

**Features:**
- Modbus TCP communication with comprehensive error handling
- Rate calculation and overflow detection
- InfluxDB time-series data storage
- 24/7 continuous operation capability
- Configurable retry logic and connection management

**Usage:**
```bash
# Run with default configuration
python adam_counter_logger.py

# Use custom configuration file
python adam_counter_logger.py --config custom_config.json

# Test connectivity and single reading
python adam_counter_logger.py --test
```

## Installation

1. **Install Python 3.8+ and pip**

2. **Install dependencies:**
```bash
pip install pymodbus influxdb-client
```

## Configuration

### ADAM-6051 Counter Configuration (`adam_config_json.json`)
```json
{
  "modbus": {
    "host": "192.168.1.100",
    "port": 502,
    "unit_id": 1,
    "timeout": 3,
    "retries": 3
  },
  "influxdb": {
    "url": "http://localhost:8086",
    "token": "your-token",
    "org": "your-org",
    "bucket": "adam_counters"
  },
  "counters": {
    "channels": [0, 1],
    "calculate_rate": true,
    "rate_window": 60
  }
}
```

## Configuration Parameters

### Modbus Settings
- `host`: IP address of the ADAM-6051 device
- `port`: Modbus TCP port (default: 502)
- `unit_id`: Modbus unit identifier (usually 1)
- `timeout`: Connection timeout in seconds
- `retries`: Number of retry attempts on failure

### InfluxDB Settings
- `url`: InfluxDB server URL
- `token`: Authentication token
- `org`: Organization name
- `bucket`: Data bucket name

### Counter Settings
- `channels`: List of counter channels to read (0-15)
- `calculate_rate`: Enable rate calculation
- `rate_window`: Window size for rate calculation (seconds)

## Docker Usage

The Python logger can be run in Docker:

```bash
# Build image
docker build -t adam-logger-python .

# Run with custom config
docker run -v $(pwd)/config:/app/config adam-logger-python
```

## Comparison with C# Implementation

| Feature | Python | C# |
|---------|--------|-----|
| Lines of Code | ~300 | ~2000 |
| Dependencies | 2 | 6+ |
| Startup Time | <1s | 2-3s |
| Memory Usage | ~30MB | ~100MB |
| Concurrent Devices | No | Yes |
| Production Ready | Simple cases | Yes |
| Docker Support | Basic | Full |

## Use Cases

The Python implementation is ideal for:
- Quick testing and verification
- Development and prototyping
- Single device deployments
- Resource-constrained environments
- Learning the system

For production deployments with multiple devices, use the C# implementation.

## Troubleshooting

### Connection Issues
1. **Verify network connectivity** to ADAM device
   ```bash
   ping 192.168.1.100
   ```
2. **Check Modbus TCP port** is accessible
   ```bash
   telnet 192.168.1.100 502
   ```
3. **Verify device configuration** matches config file

### Data Issues
1. **No data in InfluxDB**: Check token and bucket configuration
2. **Rate calculation incorrect**: Adjust rate_window parameter
3. **Missing channels**: Verify channel numbers in configuration

### Performance Issues
1. **High CPU usage**: Increase polling interval
2. **Connection timeouts**: Increase timeout value
3. **Memory growth**: Check for Python version compatibility

## Development Notes

The Python implementation follows a simple, procedural design:
- Single file for easy deployment
- Clear function separation
- Comprehensive error handling
- Standard logging output

## Migration to C#

When migrating to the C# implementation:
1. Configuration format is different (see C# documentation)
2. Multi-device support is built-in
3. Docker deployment is production-ready
4. Real-time data streaming via SignalR

## Support

For issues and questions:
1. Check application logs
2. Verify ADAM device is accessible
3. Test with known working configuration
4. Compare with C# implementation behavior