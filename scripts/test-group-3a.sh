#!/bin/bash
# Test Group 3A: Service & Health Monitoring
# Tests: CounterMetricsCollector, WebSocketHealthHub, TestRunner, ServiceCollectionExtensions

set -e

echo "🧪 Testing Group 3A: Service & Health Monitoring"
echo "================================================="

# Test individual service and monitoring classes
echo "📋 Testing CounterMetricsCollector..."
dotnet test --filter "FullyQualifiedName~CounterMetricsCollectorTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing WebSocketHealthHub..."
dotnet test --filter "FullyQualifiedName~WebSocketHealthHubTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing TestRunner..."
dotnet test --filter "FullyQualifiedName~TestRunnerTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing existing ServiceCollectionExtensions..."
dotnet test --filter "FullyQualifiedName~ServiceCollectionExtensionsTests" --verbosity normal --logger "console;verbosity=detailed"

echo "✅ Group 3A: Service & Health Monitoring - All tests passed!"
echo "📈 Running coverage analysis for Group 3A..."

# Run coverage for this group
dotnet test --filter "FullyQualifiedName~(CounterMetricsCollectorTests|WebSocketHealthHubTests|TestRunnerTests|ServiceCollectionExtensionsTests)" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Group3A \
    --logger "trx;LogFileName=group3a-results.trx"

echo "📊 Group 3A coverage analysis complete!"
echo "📁 Results saved to: ./TestResults/Group3A/"

echo ""
echo "🚧 Note: Additional Group 3A classes status:"
echo "   - CounterMetricsCollector: ✅ Complete (23 tests)"
echo "   - WebSocketHealthHub: ✅ Complete (27 tests)"
echo "   - TestRunner: ✅ Complete (21 tests)"
echo "   - ServiceCollectionExtensions: ✅ Complete (existing tests)"
echo "   - HealthCheckService: ❌ Exists but has compilation issues"