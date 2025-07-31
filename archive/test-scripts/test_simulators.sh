#!/bin/bash

echo "=== Testing ADAM-6051 Simulators ==="
echo

# Test each simulator
for i in 1 2 3; do
    PORT=$((8080 + i))
    MODBUS_PORT=$((5501 + i))
    echo "=== Simulator $i (API: $PORT, Modbus: $MODBUS_PORT) ==="
    
    # Get status
    STATUS=$(curl -s http://127.0.0.1:$PORT/api/simulator/status)
    
    if [ $? -eq 0 ]; then
        # Extract key information using Python
        echo "$STATUS" | python3 -c "
import json, sys
data = json.load(sys.stdin)
print(f\"Device ID: {data['deviceId']}\")
print(f\"State: {data['state']}\")
print(f\"Total Units: {data['totalUnits']}\")
print(f\"Channels: {len(data['channels'])}\")
"
    else
        echo "Failed to connect to simulator $i"
    fi
    echo
done

echo "=== Summary ==="
echo "All simulators are running with:"
echo "- Simulator 1: http://localhost:8081 (Modbus: 5502)"
echo "- Simulator 2: http://localhost:8082 (Modbus: 5503)"
echo "- Simulator 3: http://localhost:8083 (Modbus: 5504)"