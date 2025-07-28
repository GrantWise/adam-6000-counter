#!/bin/bash

echo "=================================================="
echo "Group 4A: Performance & Monitoring Tests"
echo "=================================================="

# Navigate to the test project directory
cd /home/grant/adam-6000-counter

echo ""
echo "Running CounterMetricsCollector tests..."
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Monitoring.CounterMetricsCollectorTests" \
    --no-build \
    --verbosity normal

echo ""
echo "Running MemoryManager tests..."
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Performance.MemoryManagerTests" \
    --no-build \
    --verbosity normal

echo ""
echo "Running WebSocketHealthHub tests..."
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Monitoring.WebSocketHealthHubTests" \
    --no-build \
    --verbosity normal

echo ""
echo "=================================================="
echo "Group 4A Tests Summary"
echo "=================================================="

# Run all Group 4A tests together for summary
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Monitoring.CounterMetricsCollectorTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Performance.MemoryManagerTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Monitoring.WebSocketHealthHubTests" \
    --no-build \
    --verbosity minimal

echo ""
echo "Group 4A testing completed!"