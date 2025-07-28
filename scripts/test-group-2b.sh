#!/bin/bash
# Test Group 2B: Infrastructure Services
# Tests: NullInfluxDbWriter

set -e

echo "🧪 Testing Group 2B: Infrastructure Services"
echo "============================================="

# Test individual infrastructure services
echo "📋 Testing NullInfluxDbWriter..."
dotnet test --filter "FullyQualifiedName~NullInfluxDbWriterTests" --verbosity normal --logger "console;verbosity=detailed"

echo "✅ Group 2B: Infrastructure Services - All tests passed!"
echo "📈 Running coverage analysis for Group 2B..."

# Run coverage for this group
dotnet test --filter "FullyQualifiedName~NullInfluxDbWriterTests" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Group2B \
    --logger "trx;LogFileName=group2b-results.trx"

echo "📊 Group 2B coverage analysis complete!"
echo "📁 Results saved to: ./TestResults/Group2B/"