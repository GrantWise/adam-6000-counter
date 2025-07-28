#!/bin/bash
# Test Group 1B: Configuration Classes
# Tests: InfluxDbConfig, AdamLoggerConfig, AdamDeviceConfig, ChannelConfig

set -e

echo "🧪 Testing Group 1B: Configuration Classes"
echo "==========================================="

# Test individual configuration classes
echo "📋 Testing InfluxDbConfig..."
dotnet test --filter "FullyQualifiedName~InfluxDbConfigTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing AdamLoggerConfig..."
dotnet test --filter "FullyQualifiedName~AdamLoggerConfigTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing AdamDeviceConfig..."
dotnet test --filter "FullyQualifiedName~AdamDeviceConfigTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing ChannelConfig..."
dotnet test --filter "FullyQualifiedName~ChannelConfigTests" --verbosity normal --logger "console;verbosity=detailed"

echo "✅ Group 1B: Configuration Classes - All tests passed!"
echo "📈 Running coverage analysis for Group 1B..."

# Run coverage for this group
dotnet test --filter "FullyQualifiedName~(InfluxDbConfigTests|AdamLoggerConfigTests|AdamDeviceConfigTests|ChannelConfigTests)" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Group1B \
    --logger "trx;LogFileName=group1b-results.trx"

echo "📊 Group 1B coverage analysis complete!"
echo "📁 Results saved to: ./TestResults/Group1B/"