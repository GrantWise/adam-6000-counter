#!/bin/bash
# ADAM-6051 C# Logger Entrypoint Script (V2)

set -e

echo "Starting ADAM-6051 Counter Logger (Core V2)..."
echo "Configuration:"
echo "  DEMO_MODE: ${DEMO_MODE:-false}"
echo "  LOG_LEVEL: ${LOG_LEVEL:-Information}"

# Select configuration file based on demo mode
if [ "${DEMO_MODE:-false}" = "true" ]; then
    echo "Using demo configuration (3 simulators)"
    cp /app/config/adam_config_v2.json /app/appsettings.json
else
    echo "Using production configuration (real ADAM devices)"
    cp /app/config/adam_config_production_v2.json /app/appsettings.json
fi

# Wait for InfluxDB to be ready
echo "Waiting for InfluxDB to be ready..."
until curl -f http://influxdb:8086/health > /dev/null 2>&1; do
    echo "InfluxDB is not ready yet, waiting..."
    sleep 5
done
echo "InfluxDB is ready!"

# Set minimal logging configuration (let JSON config handle the rest)
export "Logging__LogLevel__Default"=${LOG_LEVEL:-Information}

echo "Starting application..."

# Start the application
echo "Executing: dotnet Industrial.Adam.Logger.Console.dll"
exec dotnet Industrial.Adam.Logger.Console.dll "$@"