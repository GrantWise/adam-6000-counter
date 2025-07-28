#!/bin/bash
# Simple Industrial Code Coverage Script for ADAM Logger
# Generates basic coverage report - continues even with test failures

set -e

echo "🔍 Industrial ADAM Logger - Code Coverage (Simple)"
echo "================================================="

# Clean previous results
rm -rf TestResults/
rm -rf CoverageReports/

# Restore and build
dotnet restore
dotnet build --configuration Release --no-restore

# Run tests with coverage (ignore test failures for coverage purposes)
dotnet test --configuration Release --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    --logger "console;verbosity=minimal" || true

echo "✅ Coverage collection complete!"
echo "📊 Results in ./TestResults/"

# List coverage files
echo "Coverage files generated:"
find ./TestResults -name "*.xml" -o -name "*.json" 2>/dev/null | head -5