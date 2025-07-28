#!/bin/bash
# Test Group 2A: Data Processing Services
# Tests: InfluxDbDataProcessor, CounterDataProcessor, DefaultDataProcessor

set -e

echo "🧪 Testing Group 2A: Data Processing Services"
echo "=============================================="

# Test individual data processing services
echo "📋 Testing InfluxDbDataProcessor..."
dotnet test --filter "FullyQualifiedName~InfluxDbDataProcessorTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing CounterDataProcessor..."
dotnet test --filter "FullyQualifiedName~CounterDataProcessorTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing DefaultDataProcessor..."
dotnet test --filter "FullyQualifiedName~DefaultDataProcessorTests" --verbosity normal --logger "console;verbosity=detailed"

echo "✅ Group 2A: Data Processing Services - All tests passed!"
echo "📈 Running coverage analysis for Group 2A..."

# Run coverage for this group
dotnet test --filter "FullyQualifiedName~(InfluxDbDataProcessorTests|CounterDataProcessorTests|DefaultDataProcessorTests)" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Group2A \
    --logger "trx;LogFileName=group2a-results.trx"

echo "📊 Group 2A coverage analysis complete!"
echo "📁 Results saved to: ./TestResults/Group2A/"