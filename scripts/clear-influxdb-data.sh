#!/bin/bash

# Clear test data from InfluxDB

echo "🗑️  Clearing InfluxDB Test Data..."
echo "=================================="

# InfluxDB connection details
INFLUX_URL="http://localhost:8086"
INFLUX_TOKEN="adam-super-secret-token"
INFLUX_ORG="adam_org" 
INFLUX_BUCKET="adam_counters"

echo "🔍 Checking InfluxDB connection..."
if ! curl -s -f "$INFLUX_URL/health" > /dev/null 2>&1; then
    echo "❌ Error: InfluxDB is not accessible at $INFLUX_URL"
    echo "   Make sure InfluxDB is running: docker-compose -f docker/docker-compose.infrastructure-only.yml up -d"
    exit 1
fi

echo "✅ InfluxDB is accessible"

# Function to execute InfluxDB command
execute_influx_cmd() {
    local query="$1"
    local description="$2"
    
    echo -n "$description: "
    
    response=$(curl -s -X POST "$INFLUX_URL/api/v2/query?org=$INFLUX_ORG" \
        -H "Authorization: Token $INFLUX_TOKEN" \
        -H "Content-Type: application/vnd.flux" \
        -d "$query")
    
    if [ $? -eq 0 ]; then
        echo "✅ Success"
    else
        echo "❌ Failed"
        echo "Response: $response"
    fi
}

# Show current data count before deletion
echo
echo "📊 Current data in InfluxDB:"
count_query='from(bucket: "adam_counters") |> range(start: -30d) |> filter(fn: (r) => r._measurement == "counter_data") |> count()'
execute_influx_cmd "$count_query" "Getting current record count"

echo
echo "🗑️  Deleting all test data..."

# Delete all data in the bucket (keep bucket structure)
delete_query='from(bucket: "adam_counters") |> range(start: 1970-01-01T00:00:00Z) |> filter(fn: (r) => r._measurement == "counter_data") |> drop()'

# Alternative approach: Delete by time range (safer)
delete_all_query="
import \"influxdata/influxdb/v1\"
v1.tagValues(bucket: \"adam_counters\", tag: \"device_id\")
    |> filter(fn: (r) => r._value =~ /SIM.*/)
    |> map(fn: (r) => ({
        _start: 1970-01-01T00:00:00Z,
        _stop: now(),
        _predicate: device_id == r._value
    }))
"

# Use curl to delete data by time range (more reliable)
echo -n "Deleting all counter data: "

# Delete using the delete API (more reliable for clearing data)
delete_response=$(curl -s -X POST "$INFLUX_URL/api/v2/delete?org=$INFLUX_ORG&bucket=$INFLUX_BUCKET" \
    -H "Authorization: Token $INFLUX_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
        "start": "1970-01-01T00:00:00Z",
        "stop": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
        "predicate": "_measurement=\"counter_data\""
    }')

if [ $? -eq 0 ]; then
    echo "✅ Delete request sent"
else
    echo "❌ Delete request failed"
    echo "Response: $delete_response"
fi

# Wait for deletion to process
echo "⏳ Waiting for deletion to process..."
sleep 5

# Verify deletion
echo
echo "🔍 Verifying data deletion..."
execute_influx_cmd "$count_query" "Checking remaining record count"

echo
echo "✅ InfluxDB data cleanup complete!"
echo
echo "📝 Notes:"
echo "========"
echo "- All counter_data measurements have been deleted"
echo "- Bucket structure and configuration remain intact"
echo "- You can now start fresh data collection"
echo
echo "🚀 Next steps:"
echo "============="
echo "1. Start simulators: ./scripts/start-simulators.sh"
echo "2. Start logger: dotnet run --project src/Industrial.Adam.Logger.Console"
echo "3. Check Grafana: http://localhost:3002"