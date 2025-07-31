#!/bin/bash

# Test the ADAM Logger with local simulators

echo "🔍 Testing Industrial ADAM Logger..."
echo "===================================="

PROJECT_ROOT="/home/grant/adam-6000-counter"
LOGGER_PROJECT="$PROJECT_ROOT/src/Industrial.Adam.Logger.Console"
CONFIG_FILE="$PROJECT_ROOT/docker/config/adam_config_local_simulators.json"

# Check prerequisites
echo "📋 Checking prerequisites..."

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo "❌ Config file not found: $CONFIG_FILE"
    exit 1
fi
echo "✅ Configuration file found"

# Check if logger project exists
if [ ! -d "$LOGGER_PROJECT" ]; then
    echo "❌ Logger project not found: $LOGGER_PROJECT"
    exit 1
fi
echo "✅ Logger project found"

# Check if InfluxDB is running
if ! curl -s -f "http://localhost:8086/health" > /dev/null 2>&1; then
    echo "❌ InfluxDB is not accessible"
    echo "   Start it with: docker-compose -f docker/docker-compose.infrastructure-only.yml up -d"
    exit 1
fi
echo "✅ InfluxDB is accessible"

# Check if simulators are running
echo
echo "🤖 Checking simulators..."
sim_count=0
for port in 8081 8082 8083; do
    if curl -s -f "http://localhost:$port/api/simulator/health" > /dev/null 2>&1; then
        sim_count=$((sim_count + 1))
        echo "✅ Simulator on port $port is running"
    else
        echo "❌ Simulator on port $port is not running"
    fi
done

if [ $sim_count -eq 0 ]; then
    echo "❌ No simulators are running"
    echo "   Start them with: ./scripts/start-simulators.sh"
    exit 1
elif [ $sim_count -lt 3 ]; then
    echo "⚠️  Only $sim_count out of 3 simulators are running"
    echo "   This is OK for testing, but full test requires all 3"
else
    echo "✅ All 3 simulators are running"
fi

echo
echo "🚀 Starting ADAM Logger..."
echo "=========================="

cd "$LOGGER_PROJECT"

# Copy the local simulator config to the project directory
cp "$CONFIG_FILE" "./appsettings.json"
echo "✅ Configuration file copied to logger project"

echo
echo "🏃 Running logger (Press Ctrl+C to stop)..."
echo "============================================"
echo "Logger output:"
echo

# Set environment variables for better logging
export DOTNET_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Development

# Run the logger with the local simulator configuration
dotnet run --configuration Release