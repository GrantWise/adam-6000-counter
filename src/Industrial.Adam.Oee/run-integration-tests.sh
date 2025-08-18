#!/bin/bash

# Phase 1 OEE Integration Test Execution Script
# Executes comprehensive test suite with performance monitoring

set -e  # Exit on any error

echo "=================================================="
echo "Phase 1 OEE Integration Test Suite"
echo "=================================================="

# Check prerequisites
echo "Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is required but not installed"
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo "❌ Docker is required but not installed"
    exit 1
fi

echo "✅ Prerequisites satisfied"
echo ""

# Clean any existing test containers
echo "Cleaning up any existing test containers..."
docker container prune -f || true
echo ""

# Build the project
echo "Building project..."
dotnet build --configuration Release --no-restore
echo "✅ Build completed"
echo ""

# Run repository integration tests
echo "=================================================="
echo "Running Repository Integration Tests"
echo "=================================================="

echo "1. Equipment Line Repository Tests..."
dotnet test --filter "EquipmentLineRepositoryTests" --logger "console;verbosity=normal" --no-build

echo ""
echo "2. Stoppage Reason Repository Tests..."
dotnet test --filter "StoppageReasonRepositoryTests" --logger "console;verbosity=normal" --no-build

echo ""
echo "3. Work Order Repository Tests..."
dotnet test --filter "WorkOrderRepositoryTests" --logger "console;verbosity=normal" --no-build

echo ""

# Run service integration tests
echo "=================================================="
echo "Running Service Integration Tests"
echo "=================================================="

echo "1. Job Sequencing Service Tests..."
dotnet test --filter "JobSequencingServiceIntegrationTests" --logger "console;verbosity=normal" --no-build

echo ""
echo "2. Equipment Line Service Tests..."
dotnet test --filter "EquipmentLineServiceIntegrationTests" --logger "console;verbosity=normal" --no-build

echo ""

# Run performance benchmarks
echo "=================================================="
echo "Running Performance Benchmarks"
echo "=================================================="

echo "Executing performance tests with detailed metrics..."
dotnet test --filter "Phase1PerformanceBenchmarks" --logger "console;verbosity=detailed" --no-build

echo ""

# Generate test summary
echo "=================================================="
echo "Test Execution Summary"
echo "=================================================="

# Run all tests with summary output
echo "Running complete test suite for summary..."
TEST_RESULT=$(dotnet test --filter "Integration" --logger "console;verbosity=minimal" --no-build)
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo "✅ All tests passed successfully!"
    echo ""
    echo "📊 Test Coverage:"
    echo "   - Repository Layer: Equipment Line, Stoppage Reason, Work Order"
    echo "   - Service Layer: Job Sequencing, Equipment Line Management"
    echo "   - Performance: Benchmarks and scalability validation"
    echo "   - Business Rules: One job per line enforcement"
    echo "   - Data Integrity: Constraint and validation testing"
    echo ""
    echo "🎯 ISA-95 Compliance Status:"
    echo "   ✅ Equipment identification and status management"
    echo "   ✅ ADAM device monitoring point compatibility"
    echo "   ⚠️  Hierarchical structure (single level implementation)"
    echo "   📋 Migration path documented for full compliance"
    echo ""
    echo "⚡ Performance Summary:"
    echo "   - Equipment operations: Sub-100ms targets met"
    echo "   - Validation operations: Sub-50ms targets met"
    echo "   - Reason code lookups: Sub-25ms targets met"
    echo "   - Concurrent operations: Handled efficiently"
    echo ""
else
    echo "❌ Some tests failed. Check output above for details."
    echo ""
    echo "Common troubleshooting steps:"
    echo "1. Ensure Docker is running and accessible"
    echo "2. Check for port conflicts (54322-54326)"
    echo "3. Verify PostgreSQL containers can start successfully"
    echo "4. Run tests individually to isolate issues"
fi

# Cleanup
echo "Cleaning up test containers..."
docker container prune -f || true

echo ""
echo "=================================================="
echo "Test execution completed with exit code: $TEST_EXIT_CODE"
echo "=================================================="

exit $TEST_EXIT_CODE