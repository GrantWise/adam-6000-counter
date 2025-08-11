#!/bin/bash

# Complete system test workflow

echo "🏭 Industrial ADAM Logger - Complete System Test"
echo "================================================"

PROJECT_ROOT="/home/grant/adam-6000-counter"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to wait for user input
wait_for_user() {
    local message="$1"
    echo -e "${YELLOW}⏸️  $message${NC}"
    read -p "Press Enter to continue..."
    echo
}

# Function to check if a service is running
check_service() {
    local service_name="$1"
    local url="$2"
    local expected_status="${3:-200}"
    
    echo -n "Checking $service_name: "
    
    if curl -s -f "$url" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Running${NC}"
        return 0
    else
        echo -e "${RED}❌ Not Available${NC}"
        return 1
    fi
}

echo -e "${BLUE}Step 1: Infrastructure Check${NC}"
echo "============================="

check_service "InfluxDB" "http://localhost:8086/health"
check_service "Grafana" "http://localhost:3002/api/health"
check_service "Prometheus" "http://localhost:9090/-/healthy"

echo
echo -e "${BLUE}If any services are down, start them with:${NC}"
echo "docker-compose -f docker/docker-compose.infrastructure-only.yml up -d"

wait_for_user "Make sure all infrastructure services are running, then continue"

echo -e "${BLUE}Step 2: Clear Old Test Data${NC}"
echo "============================"
echo "This will delete all existing counter data from InfluxDB"

read -p "Clear InfluxDB data? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    ./scripts/clear-influxdb-data.sh
else
    echo "Skipping data cleanup"
fi

wait_for_user "Data cleanup complete, continue to start simulators"

echo -e "${BLUE}Step 3: Start ADAM Simulators${NC}"
echo "=============================="
./scripts/start-simulators.sh

wait_for_user "Check simulator logs if needed, then continue to test connectivity"

echo -e "${BLUE}Step 4: Test Simulator Connectivity${NC}"
echo "==================================="
./scripts/test-simulators.sh

wait_for_user "Verify all simulators are healthy, then continue to start logger"

echo -e "${BLUE}Step 5: Start ADAM Logger${NC}"
echo "=========================="
echo "The logger will start and begin collecting data from simulators"
echo "Watch for successful connections and data flow messages"
echo "Press Ctrl+C to stop the logger when you've verified it's working"

wait_for_user "Ready to start the logger?"

./scripts/test-logger.sh

echo
echo -e "${BLUE}Step 6: Verify Data in InfluxDB${NC}"
echo "==============================="
echo "Open InfluxDB UI: http://localhost:8086"
echo "Login: admin/admin123"
echo "Check bucket 'adam_counters' for data"

wait_for_user "Check InfluxDB for data, then continue"

echo -e "${BLUE}Step 7: Check Grafana Dashboard${NC}"
echo "================================="
echo "Open Grafana: http://localhost:3002"
echo "Login: admin/admin"
echo "Look for ADAM counter dashboards with real-time data"

wait_for_user "Check Grafana dashboards, then continue for cleanup"

echo -e "${BLUE}Step 8: Cleanup (Optional)${NC}"
echo "=========================="

read -p "Stop all simulators? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    ./scripts/stop-simulators.sh
else
    echo "Leaving simulators running"
fi

echo
echo -e "${GREEN}🏁 System Test Complete!${NC}"
echo "========================"
echo
echo "📊 System Status URLs:"
echo "======================"
echo "InfluxDB:     http://localhost:8086 (admin/admin123)"
echo "Grafana:      http://localhost:3002 (admin/admin)"
echo "Prometheus:   http://localhost:9090"
echo
echo "🤖 Simulator APIs:"
echo "=================="
echo "Simulator 1:  http://localhost:8081/api/simulator/status"
echo "Simulator 2:  http://localhost:8082/api/simulator/status"
echo "Simulator 3:  http://localhost:8083/api/simulator/status"
echo
echo "📁 Log Files:"
echo "============="
echo "Simulator 1:  $PROJECT_ROOT/logs/simulator1.log"
echo "Simulator 2:  $PROJECT_ROOT/logs/simulator2.log"
echo "Simulator 3:  $PROJECT_ROOT/logs/simulator3.log"
echo
echo "🎯 Next Steps:"
echo "=============="
echo "- Use the running system for development"
echo "- Build your OEE solution using the InfluxDB data"
echo "- Refer to docs/INFLUXDB_DATA_ACCESS_GUIDE.md for data access patterns"