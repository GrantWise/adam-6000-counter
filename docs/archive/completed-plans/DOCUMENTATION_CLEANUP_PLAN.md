# Documentation Cleanup and Reorganization Plan

## Current Issues
1. **Root folder clutter**: 12+ documentation files mixed with code files
2. **Duplicate content**: Multiple PRDs, implementation plans, and setup guides
3. **Outdated documents**: Legacy plans and completed migration guides
4. **Poor organization**: No clear categorization or hierarchy
5. **Redundant folders**: Both /docs and root containing similar content

## Proposed New Structure

```
adam-6000-counter/
├── README.md (Main project overview - keep minimal)
├── QUICKSTART.md (Quick setup guide)
├── CLAUDE.md (AI assistant guide - required for development)
├── docs/
│   ├── README.md (Documentation index with links)
│   ├── 01-getting-started/
│   │   ├── installation.md
│   │   ├── development-setup.md
│   │   ├── quickstart.md
│   │   └── troubleshooting.md
│   ├── 02-architecture/
│   │   ├── system-overview.md
│   │   ├── clean-architecture.md
│   │   ├── microservices.md
│   │   └── data-flow.md
│   ├── 03-development/
│   │   ├── coding-standards.md
│   │   ├── testing-guide.md
│   │   ├── api-reference.md
│   │   └── cfr-compliance.md
│   ├── 04-deployment/
│   │   ├── docker-setup.md
│   │   ├── production-guide.md
│   │   ├── configuration.md
│   │   └── monitoring.md
│   ├── 05-modules/
│   │   ├── logger-service/
│   │   ├── oee-service/
│   │   ├── scheduling-service/
│   │   ├── security-module/
│   │   └── frontend/
│   ├── 06-hardware/
│   │   ├── adam-6051-specs.md
│   │   ├── modbus-integration.md
│   │   └── simulator-guide.md
│   └── archive/
│       ├── completed-plans/
│       ├── legacy-docs/
│       └── old-reports/
```

## Files to Move/Archive

### Keep in Root (3 files only)
- `README.md` - Simplified overview
- `QUICKSTART.md` - Quick start guide
- `CLAUDE.md` - AI assistant guide

### Move to docs/01-getting-started/
- `DEVELOPMENT_SETUP.md` → `development-setup.md`
- `ONBOARDING.md` → Merge into `installation.md`
- `docs/SETUP_TROUBLESHOOTING.md` → `troubleshooting.md`

### Move to docs/02-architecture/
- `docs/Industrial Data Acquisition Platform Architecture.md` → `system-overview.md`
- Parts of `docs/2-canonical-object-catalog-v2.md` → `clean-architecture.md`

### Move to docs/03-development/
- `docs/Industrial-Software-Development-Standards.md` → `coding-standards.md`
- `CFR_PART_11_COMPLIANCE_SUMMARY.md` → `cfr-compliance.md`
- `DEV-CREDENTIALS.md` → `development-credentials.md`

### Move to docs/04-deployment/
- `docs/configuration-guide.md` → `configuration.md`
- `docs/SERVICE_STARTUP_GUIDE.md` → `production-guide.md`

### Move to docs/05-modules/
- `docs/OEE_*` files → `oee-service/`
- `docs/PLATFORM-FRONTEND-*` files → `frontend/`
- `docs/counter-*` files → `logger-service/`

### Archive (outdated/completed)
- `API-ENDPOINT-ANALYSIS.md` → `archive/completed-plans/`
- `REMEDIATION-COMPLETION-REPORT.md` → `archive/completed-plans/`
- `OEE_TECHNOLOGY_STACK_MIGRATION_PLAN.md` → `archive/completed-plans/`
- `docs/MIGRATION_GUIDE_INFLUXDB_TO_TIMESCALEDB.md` → `archive/completed-plans/`
- `docs/IMPLEMENTATION-STATUS.md` → `archive/completed-plans/`
- `docs/plans/archive/` → Keep as is

## Implementation Steps

1. **Create new folder structure**
2. **Move and rename files according to plan**
3. **Update internal links in documents**
4. **Create comprehensive README.md in docs/**
5. **Simplify root README.md**
6. **Delete empty folders**
7. **Update any references in code**

## Expected Benefits

1. **Clear hierarchy**: Easy to find documentation by category
2. **Reduced clutter**: Only 3 essential files in root
3. **Better discoverability**: Logical organization by topic
4. **Clean separation**: Active docs vs archived content
5. **Module-specific docs**: Each service has its own folder
6. **Consistent naming**: Lowercase with hyphens

## Files to Delete (truly redundant)
- Multiple frontend PRDs that say the same thing
- Test result files in root
- Duplicate implementation plans

Would you like me to proceed with this reorganization?