#!/bin/bash
# Industrial Code Coverage Script for ADAM Logger
# Generates comprehensive test coverage reports with industrial-grade thresholds

set -e

echo "🔍 Industrial ADAM Logger - Code Coverage Analysis"
echo "=================================================="

# Clean previous results
echo "🧹 Cleaning previous coverage results..."
rm -rf TestResults/
rm -rf CoverageReports/

# Restore packages
echo "📦 Restoring packages..."
dotnet restore

# Build solution
echo "🔨 Building solution..."
dotnet build --configuration Release --no-restore

# Run tests with unified coverage collection (MSBuild integration only)
echo "🧪 Running tests with coverage collection..."
dotnet test --configuration Release --no-build \
    --results-directory ./TestResults \
    --logger "trx;LogFileName=test-results.trx" \
    /p:CollectCoverage=true \
    "/p:CoverletOutputFormat=opencover%2Ccobertura%2Cjson%2Clcov" \
    /p:CoverletOutput=./TestResults/coverage \
    "/p:ExcludeByFile=**/bin/**%2C**/obj/**%2C**/*Tests.cs%2C**/Program.cs%2C**/Examples/**" \
    "/p:Exclude=[*Tests]*%2C[*.Examples]*%2C[*.IntegrationTests]*" \
    /p:Threshold=40 \
    "/p:ThresholdType=line%2Cbranch%2Cmethod"

echo "✅ Coverage analysis complete!"
echo ""
echo "📈 Coverage Results:"
echo "   - Threshold: 40% (baseline for Week 1)"
echo "   - Target: 80% (industrial standard - Week 2 goal)"
echo "   - Reports: ./TestResults/"
echo "   - Formats: OpenCover, Cobertura, JSON, LCOV"
echo ""
echo "📝 To view detailed HTML report:"
echo "   Install ReportGenerator: dotnet tool install -g dotnet-reportgenerator-globaltool"
echo "   Generate report: reportgenerator -reports:./TestResults/coverage.opencover.xml -targetdir:./CoverageReports -reporttypes:Html"
echo ""

# Check if coverage threshold was met
if [ $? -eq 0 ]; then
    echo "✅ Coverage threshold (40%) met - Week 1 baseline achieved!"
    echo "🎯 Next goal: 80% for industrial deployment standards"
else
    echo "❌ Coverage threshold (40%) not met - Investigating test execution patterns"
    echo "🔍 This may indicate tests are not executing production code"
    exit 1
fi