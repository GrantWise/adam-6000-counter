#!/bin/bash
# Test script for V2 Docker setup

echo "Testing V2 Docker configuration..."
echo "================================="

# Build the Docker image
echo "Building Docker image with V2..."
docker-compose -f docker-compose.yml build adam-logger

# Check if build succeeded
if [ $? -eq 0 ]; then
    echo "✅ Docker build successful"
else
    echo "❌ Docker build failed"
    exit 1
fi

# Test configuration loading
echo ""
echo "Testing configuration loading..."
docker run --rm \
    -v $(pwd)/config:/app/config \
    -e DEMO_MODE=true \
    adam-counter-logger \
    bash -c "cat /app/appsettings.json | head -20"

echo ""
echo "V2 Docker setup test complete!"
echo ""
echo "To run the full stack with simulators:"
echo "  docker-compose -f docker-compose.yml -f docker-compose.simulator.yml up"