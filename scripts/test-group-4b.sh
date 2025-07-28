#!/bin/bash

echo "=================================================="
echo "Group 4B: Logging & Extensions Tests"
echo "=================================================="

# Navigate to the test project directory
cd /home/grant/adam-6000-counter

echo ""
echo "Running StructuredLoggingExtensions tests..."
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Logging.StructuredLoggingExtensionsTests" \
    --no-build \
    --verbosity normal

echo ""
echo "Running IndustrialErrorLoggingExtensions tests..."
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.ErrorHandling.IndustrialErrorLoggingExtensionsTests" \
    --no-build \
    --verbosity normal

echo ""
echo "=================================================="
echo "Group 4B Tests Summary"
echo "=================================================="

# Run all Group 4B tests together for summary
dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
    --filter "FullyQualifiedName~Industrial.Adam.Logger.Tests.Logging.StructuredLoggingExtensionsTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.ErrorHandling.IndustrialErrorLoggingExtensionsTests" \
    --no-build \
    --verbosity minimal

echo ""
echo "Group 4B testing completed!"