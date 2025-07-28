#!/bin/bash

# Group 3B: Health & Monitoring - Individual health check classes
# Classes: SystemResourceHealthCheck, InfluxDbHealthCheck, ApplicationHealthCheck

set -e

echo "=================================================="
echo "Group 3B: Health & Monitoring - Individual Health Checks"
echo "=================================================="
echo ""

# Change to the test project directory
cd "$(dirname "$0")/../src/Industrial.Adam.Logger.Tests"

# Run tests for individual health check classes
echo "Running SystemResourceHealthCheck tests..."
dotnet test --no-build --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Health.Checks.SystemResourceHealthCheckTests" --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

echo ""
echo "Running InfluxDbHealthCheck tests..."
dotnet test --no-build --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Health.Checks.InfluxDbHealthCheckTests" --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

echo ""
echo "Running ApplicationHealthCheck tests..."
dotnet test --no-build --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Health.Checks.ApplicationHealthCheckTests" --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

echo ""
echo "=================================================="
echo "Group 3B Tests Summary"
echo "=================================================="
echo ""

# Run all Group 3B tests together for a summary
dotnet test --no-build --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Health.Checks" --logger "console;verbosity=minimal" --collect:"XPlat Code Coverage"

echo ""
echo "Group 3B testing completed!"