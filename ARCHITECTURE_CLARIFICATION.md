# Architecture - Industrial Adam Logger

## Current State (As of July 2025)

**UPDATE: V1 has been archived. The project now uses V2 Core exclusively.**

This document describes the current architecture after migrating from V1 to V2.

## Migration Complete

### Industrial.Adam.Logger.Core (v2.0.0) - ACTIVE
- **Status**: Primary implementation
- **Location**: `src/Industrial.Adam.Logger.Core/`
- **Used By**:
  - Docker containers (via Industrial.Adam.Logger.Console)
  - Industrial.Adam.Logger.WebApi
  - Industrial.Adam.Logger.Core.Tests
- **Dependencies**:
  - Microsoft.Extensions.Logging (standard .NET)
  - Polly for retry policies
  - NModbus for device communication
  - InfluxDB.Client for data storage

### Archived Projects (V1)
- Industrial.Adam.Logger - Moved to `archive/v1-projects/`
- Industrial.Adam.Logger.Tests - Moved to `archive/v1-projects/`
- Industrial.Adam.Logger.Examples - Moved to `archive/v1-projects/`

## Key Differences

| Feature | Industrial.Adam.Logger (v1) | Industrial.Adam.Logger.Core (v2) |
|---------|----------------------------|----------------------------------|
| Version | 1.0.0 | 2.0.0 |
| Docker Support | ✅ Active | ❌ Not integrated |
| Logging | Serilog | Microsoft.Extensions.Logging |
| Architecture | Monolithic | Clean Architecture |
| Testing Infrastructure | Built-in TestRunner | Standard unit tests |
| Health Monitoring | Comprehensive | Simplified |
| Performance Features | Full suite | Core features only |

## Current Docker Configuration

The Docker setup now uses V2:
```dockerfile
# From docker/csharp/Dockerfile
RUN dotnet publish ./src/Industrial.Adam.Logger.Console/Industrial.Adam.Logger.Console.csproj
```

And the entrypoint runs:
```bash
# From docker/csharp/entrypoint.sh
exec dotnet Industrial.Adam.Logger.Console.dll
```

Configuration files have been updated to V2 format:
- `adam_config_v2.json` - Demo/simulator configuration
- `adam_config_production_v2.json` - Production configuration

## Migration Complete ✅

The migration from V1 to V2 has been completed:

1. **Docker Updated** - Now uses Console app with V2 Core
2. **Configuration Migrated** - New JSON format without Serilog
3. **V1 Projects Archived** - Moved to `archive/v1-projects/`
4. **Solution Cleaned** - Removed references to archived projects

## Benefits Realized

1. **50% Less Code** - Removed over-engineered features
2. **Simpler Configuration** - Standard .NET logging
3. **Cleaner Architecture** - One library to maintain
4. **Modern Patterns** - Standard Microsoft practices
5. **Smaller Docker Images** - Less code to build and deploy

## Project Structure (After Migration)

```
src/
├── Industrial.Adam.Logger.Core/        # Core library (V2)
├── Industrial.Adam.Logger.Core.Tests/  # Core tests
├── Industrial.Adam.Logger.Console/     # Docker entry point
├── Industrial.Adam.Logger.WebApi/      # REST API
├── Industrial.Adam.Logger.Simulator/   # Device simulator
└── Industrial.Adam.Logger.IntegrationTests/ # Integration tests

archive/v1-projects/
├── Industrial.Adam.Logger/             # V1 library (archived)
├── Industrial.Adam.Logger.Tests/       # V1 tests (archived)
└── Industrial.Adam.Logger.Examples/    # V1 examples (archived)
```

## Important Notes

- V1 has been successfully archived
- Docker now uses V2 Core exclusively
- Configuration format has been simplified
- No Serilog dependency - uses standard .NET logging

## What Was Removed (And Why We Don't Need It)

- **TestRunner Infrastructure** - Over-engineered testing framework
- **Complex Health Monitoring** - Basic health checks are sufficient
- **Performance Optimization Suite** - Premature optimization
- **Industrial Error Service** - Standard logging is enough
- **System.Reactive** - Unnecessary complexity
- **WebSocket Health Hub** - Not used in production

## Result

A clean, maintainable codebase that does exactly what's needed:
- Reliable ADAM device communication
- Concurrent device polling
- InfluxDB data storage
- Industrial-grade error handling
- Simple, effective logging