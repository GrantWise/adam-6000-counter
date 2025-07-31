#!/bin/bash

# Script to run .NET benchmarks and save results

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_DIR="benchmark-results"
BASELINE_DIR="$RESULTS_DIR/baseline-net8"
CURRENT_DIR="$RESULTS_DIR/$TIMESTAMP"

# Create results directories
mkdir -p "$BASELINE_DIR"
mkdir -p "$CURRENT_DIR"

echo "Building benchmark project..."
dotnet build src/Industrial.Adam.Logger.Benchmarks -c Release

echo "Running benchmarks..."
cd src/Industrial.Adam.Logger.Benchmarks

# Run benchmarks and export results
dotnet run -c Release --no-build -- --exporters json markdown --artifacts "../../../$CURRENT_DIR"

cd ../..

# If this is the first run, copy to baseline
if [ -z "$(ls -A $BASELINE_DIR)" ]; then
    echo "Setting baseline results..."
    cp -r "$CURRENT_DIR"/* "$BASELINE_DIR/"
    echo "Baseline saved to: $BASELINE_DIR"
fi

echo "Benchmark results saved to: $CURRENT_DIR"
echo "To compare with baseline, use: BenchmarkDotNet comparison tools"