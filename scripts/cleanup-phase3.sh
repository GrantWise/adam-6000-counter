#!/bin/bash

# OEE Phase 3 Cleanup Script
# Purpose: Remove all Phase 3 over-implementations from OEE module
# Usage: ./scripts/cleanup-phase3.sh

set -e  # Exit on error

echo "================================================"
echo "OEE Phase 3 Cleanup Script"
echo "This will remove all over-implemented features"
echo "================================================"
echo ""

# Base path
BASE_PATH="/home/grant/adam-6000-counter/src/Industrial.Adam.Oee"

# Confirm before proceeding
read -p "This will DELETE Phase 3 files. Are you sure? (y/N): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    echo "Cleanup cancelled."
    exit 1
fi

echo ""
echo "Starting cleanup..."
echo ""

# 1. Remove Batch Tracking
echo "Removing Batch tracking..."
rm -f "$BASE_PATH/Domain/Entities/Batch.cs"
rm -f "$BASE_PATH/Domain/Services/BatchManagementService.cs"
rm -f "$BASE_PATH/Domain/Interfaces/IBatchRepository.cs"
rm -f "$BASE_PATH/Application/Batches/"*.cs 2>/dev/null || true
rm -f "$BASE_PATH/Tests/Domain/Entities/BatchTests.cs" 2>/dev/null || true
echo "✓ Batch tracking removed"

# 2. Remove Wrong Shift Implementation
echo "Removing Shift management (wrong concept)..."
rm -f "$BASE_PATH/Domain/Entities/Shift.cs"
rm -f "$BASE_PATH/Domain/Services/ShiftService.cs" 2>/dev/null || true
rm -f "$BASE_PATH/Domain/Interfaces/IShiftRepository.cs" 2>/dev/null || true
rm -f "$BASE_PATH/Application/Shifts/"*.cs 2>/dev/null || true
rm -f "$BASE_PATH/Tests/Domain/Entities/ShiftTests.cs" 2>/dev/null || true
echo "✓ Shift management removed"

# 3. Remove Job Scheduling
echo "Removing complex job scheduling..."
rm -f "$BASE_PATH/Domain/Entities/JobSchedule.cs"
rm -f "$BASE_PATH/Domain/Services/AdvancedJobSchedulingService.cs"
rm -f "$BASE_PATH/Domain/Interfaces/IJobScheduleRepository.cs" 2>/dev/null || true
rm -f "$BASE_PATH/Application/JobSchedules/"*.cs 2>/dev/null || true
rm -f "$BASE_PATH/Tests/Domain/Entities/JobScheduleTests.cs" 2>/dev/null || true
echo "✓ Job scheduling removed"

# 4. Remove Quality Inspection (reverting to simple)
echo "Removing complex quality inspection..."
rm -f "$BASE_PATH/Domain/Entities/QualityInspection.cs"
rm -f "$BASE_PATH/Domain/Entities/QualityGate.cs" 2>/dev/null || true
rm -f "$BASE_PATH/Tests/Domain/Entities/QualityInspectionTests.cs" 2>/dev/null || true
echo "✓ Quality inspection removed"

# 5. Remove Canonical Over-Engineering
echo "Removing over-engineered canonical patterns..."
rm -f "$BASE_PATH/Domain/ValueObjects/CanonicalReference.cs"
rm -f "$BASE_PATH/Domain/ValueObjects/TransactionLog.cs"
rm -f "$BASE_PATH/Domain/ValueObjects/StateTransition.cs"
rm -f "$BASE_PATH/Tests/Domain/ValueObjects/CanonicalReferenceTests.cs" 2>/dev/null || true
rm -f "$BASE_PATH/Tests/Domain/ValueObjects/StateTransitionTests.cs" 2>/dev/null || true
echo "✓ Canonical patterns removed"

# 6. Remove Phase 3 Database Migrations
echo "Removing Phase 3 database migrations..."
rm -f "$BASE_PATH/Infrastructure/Data/Migrations/006-create-phase3-batch-tracking.sql" 2>/dev/null || true
rm -f "$BASE_PATH/Infrastructure/Data/Migrations/007-create-phase3-shift-management.sql" 2>/dev/null || true
echo "✓ Phase 3 migrations removed"

# 7. Clean up empty directories
echo "Cleaning up empty directories..."
find "$BASE_PATH" -type d -empty -delete 2>/dev/null || true
echo "✓ Empty directories cleaned"

echo ""
echo "================================================"
echo "Phase 3 Cleanup Complete!"
echo "================================================"
echo ""
echo "Next steps:"
echo "1. Run 'dotnet build' to check for compilation errors"
echo "2. Fix any remaining references to deleted entities"
echo "3. Run tests to ensure Phase 1 & 2 functionality intact"
echo "4. Commit changes with: git commit -m 'refactor: Remove Phase 3 over-implementations'"
echo ""
echo "Files removed successfully. The OEE module is now simplified to its core purpose."