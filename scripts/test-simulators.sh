#!/bin/bash

# Test ADAM simulators connectivity and data

echo "🔍 Testing Industrial ADAM Logger Simulators..."
echo "==============================================="

PROJECT_ROOT="/home/grant/adam-6000-counter"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to test a simulator
test_simulator() {
    local sim_number=$1
    local api_port=$2
    local modbus_port=$3
    
    echo
    echo "🤖 Testing Simulator $sim_number"
    echo "================================"
    
    # Test API health endpoint
    echo -n "API Health Check (port $api_port): "
    if curl -s -f "http://localhost:$api_port/api/simulator/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API Healthy${NC}"
        
        # Get status information
        echo "📊 Simulator Status:"
        curl -s "http://localhost:$api_port/api/simulator/status" | python3 -m json.tool 2>/dev/null || echo "   ⚠️  Could not parse status JSON"
        
    else
        echo -e "${RED}❌ API Not Responding${NC}"
        return 1
    fi
    
    # Test Modbus connectivity
    echo
    echo -n "Modbus TCP Test (port $modbus_port): "
    
    # Create a simple Python script to test Modbus
    cat > /tmp/test_modbus_$sim_number.py << EOF
import socket
import sys

try:
    # Simple TCP connection test
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(5)
    result = sock.connect_ex(('localhost', $modbus_port))
    sock.close()
    
    if result == 0:
        print("✅ Modbus TCP Port Open")
        sys.exit(0)
    else:
        print("❌ Modbus TCP Port Closed")
        sys.exit(1)
except Exception as e:
    print(f"❌ Modbus Test Failed: {e}")
    sys.exit(1)
EOF
    
    if python3 /tmp/test_modbus_$sim_number.py 2>/dev/null; then
        echo -e "${GREEN}✅ Modbus TCP Accessible${NC}"
    else
        echo -e "${RED}❌ Modbus TCP Not Accessible${NC}"
    fi
    
    # Cleanup
    rm -f /tmp/test_modbus_$sim_number.py
    
    echo "🌐 Simulator $sim_number URLs:"
    echo "   - API Status: http://localhost:$api_port/api/simulator/status"
    echo "   - API Health: http://localhost:$api_port/api/simulator/health"
    echo "   - Modbus TCP: localhost:$modbus_port"
}

# Test each simulator
test_simulator 1 8081 5502
test_simulator 2 8082 5503  
test_simulator 3 8083 5504

echo
echo "📋 Overall System Status"
echo "========================"

# Check if InfluxDB is accessible
echo -n "InfluxDB (port 8086): "
if curl -s -f "http://localhost:8086/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Available${NC}"
else
    echo -e "${RED}❌ Not Available${NC}"
fi

# Check if Grafana is accessible
echo -n "Grafana (port 3002): "
if curl -s -f "http://localhost:3002/api/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Available${NC}"
else
    echo -e "${RED}❌ Not Available${NC}"
fi

# Check if Prometheus is accessible
echo -n "Prometheus (port 9090): "
if curl -s -f "http://localhost:9090/-/healthy" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Available${NC}"
else
    echo -e "${RED}❌ Not Available${NC}"
fi

echo
echo "🏁 Simulator testing complete!"
echo
echo "📊 Quick Access URLs:"
echo "===================="
echo "Grafana Dashboard:  http://localhost:3002 (admin/admin)"
echo "InfluxDB UI:        http://localhost:8086 (admin/admin123)"
echo "Prometheus:         http://localhost:9090"
echo "Simulator 1 API:    http://localhost:8081/api/simulator/status"
echo "Simulator 2 API:    http://localhost:8082/api/simulator/status"
echo "Simulator 3 API:    http://localhost:8083/api/simulator/status"