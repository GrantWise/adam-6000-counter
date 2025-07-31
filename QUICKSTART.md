# Quick Start Guide

Get the Industrial ADAM Logger running in 5 minutes!

## Option 1: Docker (Recommended) 🐳

### Prerequisites
- Docker and Docker Compose installed
- Network access to your ADAM-6051 device (or use simulator)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/adam-6000-counter.git
   cd adam-6000-counter/docker
   ```

2. **Start with simulator (no hardware needed)**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up -d
   ```

3. **Access the dashboards**
   - Grafana: http://localhost:3002 (admin/admin)
   - InfluxDB: http://localhost:8086 (admin/admin123)

4. **View real-time data**
   ```bash
   docker-compose logs -f adam-logger
   ```

That's it! You're now logging simulated ADAM device data.

### Using Real Devices

1. **Edit configuration**
   ```bash
   nano config/adam_config_v2.json
   ```

2. **Update device IP address**
   ```json
   {
     "AdamLogger": {
       "Devices": [{
         "IpAddress": "192.168.1.100"  // Your device IP
       }]
     }
   }
   ```

3. **Restart the logger**
   ```bash
   docker-compose restart adam-logger
   ```

## Option 2: Local Development 💻

### C# Implementation

1. **Prerequisites**
   - .NET 8 SDK
   - InfluxDB running locally

2. **Build and run**
   ```bash
   cd src/Industrial.Adam.Logger.Console
   dotnet run
   ```

### Python Implementation

1. **Prerequisites**
   - Python 3.8+
   - pip

2. **Install and run**
   ```bash
   cd python
   pip install pymodbus influxdb-client
   python adam_counter_logger.py
   ```

## What's Next?

- 📊 **Configure Grafana dashboards** for your specific counters
- 🔧 **Add more devices** to the configuration
- 📡 **Enable the REST API** for external integrations
- 📚 **Read the full documentation** for advanced features

## Common Issues

### Can't connect to device?
- Check device IP is correct
- Verify network connectivity: `ping <device-ip>`
- Ensure Modbus TCP is enabled on port 502

### No data in Grafana?
- Wait 30 seconds for initial data
- Check InfluxDB has data at http://localhost:8086
- Verify logger is running: `docker-compose logs adam-logger`

### Need help?
- Check the [main README](README.md)
- Review [Docker guide](docker/README.md)
- See [troubleshooting section](README.md#troubleshooting)

## Example Output

When running correctly, you'll see:
```
adam-logger | [10:30:45 INF] Starting Industrial ADAM Logger...
adam-logger | [10:30:45 INF] Connected to Device001 at 192.168.1.100
adam-logger | [10:30:46 INF] Channel 0: 12345 (Good)
adam-logger | [10:30:46 INF] Channel 1: 67890 (Good)
adam-logger | [10:30:46 INF] Data written to InfluxDB
```

Happy logging! 🎉