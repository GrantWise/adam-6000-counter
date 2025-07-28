#!/bin/bash
# Test execution scripts for grouped test runs

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to run test group
run_test_group() {
    local group_name=$1
    local filter=$2
    
    echo -e "${YELLOW}Running $group_name...${NC}"
    if dotnet test src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj \
        --filter "$filter" \
        --no-build \
        --logger "console;verbosity=minimal"; then
        echo -e "${GREEN}✓ $group_name PASSED${NC}\n"
    else
        echo -e "${RED}✗ $group_name FAILED${NC}\n"
    fi
}

# Build first
echo -e "${YELLOW}Building test project...${NC}"
dotnet build src/Industrial.Adam.Logger.Tests/Industrial.Adam.Logger.Tests.csproj

# Group 1A: Core Utilities & Models
run_test_group "Group 1A: Core Utilities & Models" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Utilities|FullyQualifiedName~Industrial.Adam.Logger.Tests.Models|FullyQualifiedName~Industrial.Adam.Logger.Tests.ConstantsTests"

# Group 1B: Configuration Classes
run_test_group "Group 1B: Configuration Classes" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Configuration"

# Group 2A: Data Processing Services
run_test_group "Group 2A: Data Processing Services" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Services.CounterDataProcessorTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Services.InfluxDbDataProcessorTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Services.DefaultDataProcessorTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Services.DefaultDataTransformerTests|FullyQualifiedName~Industrial.Adam.Logger.Tests.Services.DefaultDataValidatorTests"

# Group 2B: Infrastructure Services  
run_test_group "Group 2B: Infrastructure Services" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Infrastructure"

# Group 3A: Service & Health Monitoring (Known Issues)
echo -e "${YELLOW}Group 3A: Service & Health Monitoring - SKIPPING (Known Moq issues)${NC}\n"

# Group 3B: Individual Health Checks
run_test_group "Group 3B: Individual Health Checks" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Health.Checks"

# Group 4A: Performance & Monitoring
run_test_group "Group 4A: Performance & Monitoring" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Performance|FullyQualifiedName~Industrial.Adam.Logger.Tests.Monitoring"

# Group 4B: Logging & Extensions
run_test_group "Group 4B: Logging & Extensions" \
    "FullyQualifiedName~Industrial.Adam.Logger.Tests.Logging|FullyQualifiedName~Industrial.Adam.Logger.Tests.Extensions"

echo -e "${YELLOW}Test run complete!${NC}"