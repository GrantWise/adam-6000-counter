# Implementation Plans Directory

## Active Plans

### 📋 MASTER-PLAN.md
**The primary implementation document** - Start here!
- Complete 4-week execution plan
- OEE cleanup + Equipment Scheduling creation
- GitHub strategy and PR sequence
- All key decisions and commands

### Supporting Documents

#### 🏗️ architectural-separation-plan.md
Technical details on separating OEE from Equipment Scheduling
- Module boundaries and responsibilities  
- Database strategy (shared TimescaleDB)
- API integration patterns

#### ✂️ oee-simplification-plan.md
Detailed analysis of what to remove from OEE
- File-by-file cleanup list
- Entity refactoring specifics
- Service layer simplification

#### 🆕 equipment-scheduling-foundation-plan.md  
Complete technical specification for Equipment Scheduling System
- Domain model design
- Database schema
- API specifications
- Pattern management system

#### ⚡ simplified-execution-plan.md
Streamlined 4-week implementation approach
- No production constraints
- Clean deletion strategy
- Fresh module creation

## Archived Plans

The `/archive/` folder contains earlier, more complex plans that assumed production constraints:
- `master-implementation-timeline.md` - Original 16-week timeline
- `complete-system-architecture.md` - Exhaustive architecture analysis  
- `module-quality-remediation-plan.md` - Earlier cleanup approach

These are kept for reference but are superseded by the current MASTER-PLAN.md.

## Quick Start

1. **Read MASTER-PLAN.md** for the complete picture
2. **Reference supporting documents** for technical details as needed
3. **Execute Week 1** cleanup using `scripts/cleanup-phase3.sh`
4. **Follow GitHub strategy** with three focused PRs

## Status

- ✅ Planning complete
- ⏳ Ready for execution
- 🎯 4-week timeline
- 🚀 No production constraints