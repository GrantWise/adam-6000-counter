#!/bin/bash
# Test Group 1A: Core Utilities & Models
# Tests: OperationResult, RetryPolicyService, IndustrialErrorService, AdamDataReading, Constants

set -e

echo "🧪 Testing Group 1A: Core Utilities & Models"
echo "============================================"

# Test individual components
echo "📋 Testing OperationResult utilities..."
dotnet test --filter "FullyQualifiedName~OperationResultTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing RetryPolicyService..."
dotnet test --filter "FullyQualifiedName~RetryPolicyServiceTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing IndustrialErrorService..."
dotnet test --filter "FullyQualifiedName~IndustrialErrorServiceTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing AdamDataReading model..."
dotnet test --filter "FullyQualifiedName~AdamDataReadingTests" --verbosity normal --logger "console;verbosity=detailed"

echo "📋 Testing Constants..."
dotnet test --filter "FullyQualifiedName~ConstantsTests" --verbosity normal --logger "console;verbosity=detailed"

echo "✅ Group 1A: Core Utilities & Models - All tests passed!"
echo "📈 Running coverage analysis for Group 1A..."

# Run coverage for this group
dotnet test --filter "FullyQualifiedName~(OperationResultTests|RetryPolicyServiceTests|IndustrialErrorServiceTests|AdamDataReadingTests|ConstantsTests)" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Group1A \
    --logger "trx;LogFileName=group1a-results.trx"

echo "📊 Group 1A coverage analysis complete!"
echo "📁 Results saved to: ./TestResults/Group1A/"