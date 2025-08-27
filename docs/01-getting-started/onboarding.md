# Developer Onboarding Guide

Welcome to the Industrial ADAM Counter Ecosystem! This guide will help you understand and contribute to our three separate but complementary industrial applications.

## Project Overview

This repository contains three **standalone industrial applications** that work together to provide a complete manufacturing data platform:

### 🏭 1. ADAM Logger (Backend Service)
**Purpose**: High-performance C# service for collecting data from ADAM-6051 industrial counter devices
- **Location**: `src/` directory
- **Tech Stack**: .NET 9, C#, TimescaleDB, Modbus TCP
- **Role**: Core data acquisition engine for production lines

### 📊 2. ADAM Counter Frontend (React Dashboard)
**Purpose**: Real-time monitoring and management interface for ADAM devices
- **Location**: `adam-counter-frontend/` directory  
- **Tech Stack**: Next.js 15, React 19, TypeScript, Tailwind CSS, shadcn/ui
- **Role**: Device configuration and real-time data visualization

### 📈 3. OEE Application (Manufacturing Analytics)
**Purpose**: Overall Equipment Effectiveness (OEE) calculation and reporting interface
- **Location**: `oee-app/oee-interface/` directory
- **Tech Stack**: Next.js 15, React 19, TypeScript, PostgreSQL
- **Role**: Production analytics and efficiency reporting

### Architecture Philosophy
- **Loosely Coupled**: Each application is standalone and independently deployable
- **Data-Driven Integration**: Applications communicate through shared TimescaleDB/PostgreSQL databases
- **Never Combined**: These applications should remain separate for maintainability and scalability
- **Pragmatic Over Dogmatic**: Clean, readable code that solves real industrial problems

## Tech Stack Summary

| Component | Backend | Frontend | Database | Communication |
|-----------|---------|----------|----------|---------------|
| **ADAM Logger** | .NET 9 C# | - | TimescaleDB | Modbus TCP |
| **Counter Frontend** | - | Next.js 15 + React 19 | TimescaleDB | REST API + WebSocket |
| **OEE Application** | - | Next.js 15 + React 19 | PostgreSQL | REST API |

### Key Technologies
- **Backend**: .NET 9, C#, Clean Architecture, CQRS, DDD patterns
- **Frontend**: React 19, Next.js 15, TypeScript, Tailwind CSS, shadcn/ui components
- **Databases**: TimescaleDB (time-series), PostgreSQL (relational), Redis (caching)
- **Infrastructure**: Docker, Docker Compose, Grafana, Prometheus
- **Communication**: Modbus TCP, WebSocket, REST APIs, SignalR

## Repository Structure

```
adam-6000-counter/
├── README.md                          # Main project overview
├── CLAUDE.md                          # AI development guidelines
├── ONBOARDING.md                      # This file
├── QUICKSTART.md                      # Fast setup guide
├── Industrial.Adam.Logger.sln         # Main C# solution

# 🏭 ADAM Logger Service (C# Backend)
├── src/
│   ├── Industrial.Adam.Logger.Core/          # Core business logic
│   │   ├── Services/AdamLoggerService.cs     # Main orchestration service
│   │   ├── Processing/DataProcessor.cs       # Data processing pipeline
│   │   ├── Storage/TimescaleStorage.cs       # Database operations
│   │   └── Devices/ModbusDevicePool.cs       # Device communication
│   ├── Industrial.Adam.Logger.Console/       # Console application entry point
│   ├── Industrial.Adam.Logger.WebApi/        # REST API service
│   ├── Industrial.Adam.Logger.Simulator/     # ADAM device simulators
│   └── Industrial.Adam.Logger.*.Tests/       # Comprehensive test suites

# 📊 ADAM Counter Frontend (React)
├── adam-counter-frontend/
│   ├── app/                                  # Next.js 15 app router
│   │   ├── layout.tsx                        # Root layout
│   │   └── page.tsx                          # Main dashboard
│   ├── components/                           # React components
│   │   ├── real-time-monitoring.tsx          # Live data display
│   │   ├── device-management.tsx             # Device configuration
│   │   └── ui/                               # shadcn/ui components
│   ├── lib/api/                              # API client utilities
│   └── package.json                          # Dependencies

# 📈 OEE Application (Manufacturing Analytics)
├── oee-app/oee-interface/
│   ├── app/                                  # Next.js 15 app router
│   ├── components/                           # OEE-specific components
│   ├── lib/database-queries.ts               # PostgreSQL queries
│   ├── scripts/                              # Database schema
│   └── package.json                          # Dependencies

# 🐳 Infrastructure & Configuration
├── docker/                                   # Docker deployment
│   ├── docker-compose.yml                   # Main infrastructure stack
│   ├── config/                               # Application configurations
│   └── grafana/dashboards/                   # Monitoring dashboards
├── scripts/                                  # Development automation
│   ├── setup-dev-environment.sh             # One-click setup
│   ├── start-simulators.sh                  # Device simulators
│   └── test-*.sh                             # Testing scripts
└── docs/                                     # Technical documentation
    ├── architecture_guide.md                # System architecture
    ├── development_standards.md             # Coding standards
    └── configuration-guide.md               # Setup instructions
```

## Getting Started

### Prerequisites
- **Git**: Version control
- **Docker & Docker Compose**: Infrastructure services
- **.NET 9 SDK**: C# development
- **Node.js 18+**: Frontend development
- **VS Code/Rider**: Recommended IDEs

### 🚀 Automated Setup (Recommended)
```bash
# Clone repository
git clone [repository-url]
cd adam-6000-counter

# One-command setup - installs everything!
./scripts/setup-dev-environment.sh
```

This script automatically:
- ✅ Installs .NET 9 SDK if needed
- ✅ Starts TimescaleDB with proper schema
- ✅ Launches Grafana with dashboards
- ✅ Starts Prometheus monitoring
- ✅ Launches 3 ADAM device simulators
- ✅ Builds and starts the logger service
- ✅ Verifies everything works

### Manual Setup

#### 1. Start Infrastructure Services
```bash
cd docker
docker-compose up -d  # TimescaleDB, Grafana, Prometheus
```

#### 2. Build C# Backend
```bash
dotnet build
dotnet test
dotnet run --project src/Industrial.Adam.Logger.Console
```

#### 3. Start Frontend Applications
```bash
# ADAM Counter Frontend
cd adam-counter-frontend
npm install
npm run dev  # Runs on http://localhost:3000

# OEE Application  
cd ../oee-app/oee-interface
npm install
npm run dev  # Runs on http://localhost:3001
```

## Key Components & Entry Points

### 🏭 ADAM Logger Service
- **Entry Point**: `src/Industrial.Adam.Logger.Console/Program.cs`
- **Main Service**: `src/Industrial.Adam.Logger.Core/Services/AdamLoggerService.cs:16`
- **Configuration**: `src/Industrial.Adam.Logger.Console/appsettings.json`
- **Key Features**:
  - Modbus TCP communication with ADAM devices
  - TimescaleDB time-series storage
  - Windowed rate calculation with circular buffers
  - Dead letter queue for data reliability
  - Comprehensive error handling and retry logic

### 📊 ADAM Counter Frontend
- **Entry Point**: `adam-counter-frontend/app/layout.tsx:13`
- **Main Dashboard**: `adam-counter-frontend/app/page.tsx`
- **Device API**: `adam-counter-frontend/lib/api/devices.ts`
- **Key Features**:
  - Real-time counter monitoring with WebSocket
  - Device configuration management
  - Data visualization with Recharts
  - Modern UI with shadcn/ui components

### 📈 OEE Application
- **Entry Point**: `oee-app/oee-interface/app/layout.tsx:14`
- **Database Queries**: `oee-app/oee-interface/lib/database-queries.ts`
- **Key Features**:
  - OEE calculation and reporting
  - Production analytics and trends
  - Job performance monitoring
  - PostgreSQL integration for structured data

## Development Workflow

### 1. Creating New Features

**For Backend (C# ADAM Logger):**
```bash
# 1. Create feature branch
git checkout -b feature/new-device-support

# 2. Implement feature following Clean Architecture
# - Add models in Models/
# - Add services in Services/
# - Add interfaces in appropriate namespaces
# - Follow SOLID principles from CLAUDE.md

# 3. Add comprehensive tests
dotnet test

# 4. Run quality checks
./scripts/run-coverage.sh
```

**For Frontend Applications:**
```bash
# 1. Create feature branch
git checkout -b feature/new-dashboard

# 2. Add React components following patterns
# - Use TypeScript for type safety
# - Follow shadcn/ui component patterns
# - Implement responsive design with Tailwind

# 3. Test locally
npm run dev
npm run build
npm run lint
```

### 2. Common Development Tasks

#### Adding a New ADAM Device Type
1. **Backend**: Extend `IDeviceProvider` interface in `Core/Devices/`
2. **Configuration**: Update `DeviceConfig.cs` with new device parameters
3. **Frontend**: Add device type to device management components
4. **Testing**: Create integration tests with simulators

#### Adding a New API Endpoint
1. **Backend**: Add controller method in `WebApi/Controllers/`
2. **Frontend**: Update API client in `lib/api/`
3. **Types**: Add TypeScript interfaces in `lib/types/`
4. **Testing**: Add API tests and frontend component tests

#### Adding Database Schema Changes
1. **ADAM Logger**: Update TimescaleDB schema in `Storage/TimescaleStorage.cs`
2. **OEE App**: Add migration scripts in `oee-app/scripts/`
3. **Documentation**: Update configuration guide

### 3. Testing Strategy

#### Backend Testing
```bash
# Unit tests
dotnet test src/Industrial.Adam.Logger.Core.Tests

# Integration tests  
dotnet test src/Industrial.Adam.Logger.IntegrationTests

# Test groups (parallel execution)
./scripts/test-group-1a.sh  # Core functionality
./scripts/test-group-2a.sh  # Data processing
./scripts/test-group-3a.sh  # Storage layer

# Coverage reports
./scripts/run-coverage.sh
```

#### Frontend Testing
```bash
# Each frontend app
cd adam-counter-frontend
npm run test
npm run lint
npm run build

cd ../oee-app/oee-interface  
npm run test
npm run lint
npm run build
```

#### System Testing
```bash
# Full integration test with simulators
./scripts/full-system-test.sh

# Test with real devices
./scripts/test-logger.sh
```

### 4. Code Style & Standards

#### C# Backend Standards
- **Architecture**: Clean Architecture, CQRS, DDD patterns
- **Principles**: SOLID principles, pragmatic over dogmatic
- **Error Handling**: Specific exception types, structured logging
- **Testing**: Arrange-Act-Assert pattern, independent tests
- **Documentation**: XML docs for public APIs

#### Frontend Standards  
- **TypeScript**: Strict type checking enabled
- **Components**: Functional components with hooks
- **Styling**: Tailwind CSS with shadcn/ui patterns
- **State Management**: React Query for server state, Zustand for client state
- **Testing**: React Testing Library patterns

#### Quality Gates
- All tests must pass
- Code coverage > 80% for critical paths
- No compiler warnings
- Lint checks pass
- Documentation updated

## Architecture Decisions

### 1. Clean Architecture (Backend)
```
┌─────────────────────────────────────┐
│           Infrastructure            │
│  (TimescaleDB, Modbus, WebAPI)     │
├─────────────────────────────────────┤
│            Application              │
│     (Services, UseCases)            │
├─────────────────────────────────────┤
│              Domain                 │
│    (Models, Interfaces)             │
└─────────────────────────────────────┘
```

### 2. Loose Coupling Strategy
- **Data Integration**: Applications share databases but not code
- **Independent Deployment**: Each app can be deployed separately
- **Protocol Separation**: Frontend apps use REST/WebSocket, backend uses Modbus
- **Configuration Driven**: Behavior changes through config, not code

### 3. Error Handling Philosophy
- **Defensive Programming**: Every external interaction can fail
- **Graceful Degradation**: Continue operating with reduced functionality
- **Observable Failures**: Every failure must be logged and actionable
- **Industrial Reliability**: 24/7 operation with automatic recovery

### 4. Data Flow Architecture
```
ADAM Devices → Modbus TCP → Logger Service → TimescaleDB → Frontend Apps
                    ↓              ↓              ↓
                Retry Logic → Dead Letter → Real-time Updates
                    ↓         Queue           ↓
                Health Mon. → Recovery → WebSocket Streams
```

## Common Patterns & Anti-Patterns

### ✅ Good Patterns
- **Single Responsibility**: Each class has one clear business purpose
- **Configuration Injection**: Dependencies injected via configuration
- **Windowed Processing**: Rate calculations over time windows
- **Circuit Breaker**: Automatic failure recovery
- **Event-Driven Updates**: WebSocket for real-time UI updates

### ❌ Anti-Patterns to Avoid  
- **Mixing Concerns**: Don't combine UI logic with device communication
- **Hardcoded Values**: Use configuration for all parameters
- **Synchronous Database Calls**: Use async/await patterns
- **Shared State**: Keep applications independent
- **Generic Error Handling**: Use specific exception types

## Performance & Scalability

### 1. Backend Performance
- **Concurrent Device Polling**: Multiple devices polled simultaneously
- **Circular Buffers**: Efficient memory usage for time-series data
- **Connection Pooling**: Reuse database connections
- **Background Processing**: Non-blocking data processing

### 2. Frontend Performance  
- **Server-Side Rendering**: Next.js SSR for fast initial loads
- **WebSocket Optimization**: Efficient real-time updates
- **Component Memoization**: React.memo for expensive renders
- **Lazy Loading**: Code splitting for large dashboards

### 3. Database Optimization
- **TimescaleDB Hypertables**: Automatic time-based partitioning
- **Continuous Aggregates**: Pre-computed rollups for analytics
- **Connection Limits**: Proper connection pool sizing
- **Query Optimization**: Indexed queries for time-series data

## Security Considerations

### 1. Network Security
- **Docker Network Isolation**: Services in private networks
- **Firewall Rules**: Only required ports exposed
- **TLS Encryption**: HTTPS for web interfaces
- **VPN Access**: Production systems behind VPN

### 2. Authentication & Authorization
- **JWT Tokens**: Stateless authentication for APIs
- **Role-Based Access**: Different permissions for operators vs engineers
- **Session Management**: Secure session handling
- **API Rate Limiting**: Prevent abuse of API endpoints

### 3. Data Security
- **Input Validation**: All inputs validated and sanitized
- **SQL Injection Prevention**: Parameterized queries only
- **Secret Management**: No secrets in configuration files
- **Audit Logging**: All configuration changes logged

## Troubleshooting Guide

### Common Issues

#### 1. Backend Issues
**ADAM Device Connection Failed**
```bash
# Check device connectivity
ping [device-ip]
nc -zv [device-ip] 502

# Check configuration
cat src/Industrial.Adam.Logger.Console/appsettings.json

# View detailed logs
dotnet run --project src/Industrial.Adam.Logger.Console
```

**TimescaleDB Connection Issues**
```bash
# Check database status
docker ps | grep timescale
docker logs adam-timescaledb

# Test connection
psql postgresql://adam_user:adam_password@localhost:5433/adam_counters
```

#### 2. Frontend Issues
**Build Failures**
```bash
# Clear caches
rm -rf node_modules package-lock.json
npm install

# Check Node version
node --version  # Should be 18+
```

**API Connection Issues**
```bash
# Check API endpoint
curl http://localhost:3000/api/health

# Check WebSocket connection
# View browser developer tools Network tab
```

#### 3. Docker Issues
**Services Won't Start**
```bash
# Check port conflicts
netstat -tulpn | grep -E ':(3002|5433|9090)'

# View service logs
docker-compose logs [service-name]

# Restart services
docker-compose down && docker-compose up -d
```

### Performance Issues
**High Memory Usage**
- Check circular buffer sizes in configuration
- Monitor TimescaleDB memory usage
- Review connection pool settings

**Slow Database Queries**
- Check TimescaleDB query plans
- Verify hypertables are properly configured
- Monitor continuous aggregate performance

## Monitoring & Observability

### 1. Application Monitoring
- **Grafana Dashboards**: http://localhost:3002 (admin/admin)
  - Counter metrics and production analytics
  - System health and performance
  - Database query performance
- **Prometheus Metrics**: http://localhost:9090
  - Application performance metrics
  - Infrastructure monitoring
  - Custom business metrics

### 2. Logging Strategy
- **Structured Logging**: JSON format for machine processing
- **Log Levels**: DEBUG (dev), INFO (prod), WARN (issues), ERROR (failures)
- **Context**: Include device IDs, timestamps, and operation details
- **Rotation**: Automatic log file rotation to prevent disk filling

### 3. Health Checks
- **Endpoint Monitoring**: `/health` endpoints for all services
- **Database Connectivity**: Automatic connection health checks
- **Device Status**: Real-time device connection monitoring
- **Data Quality**: Validation of incoming sensor data

## Next Steps for New Developers

### Week 1: Environment Setup
1. ✅ Complete automated setup with `./scripts/setup-dev-environment.sh`
2. ✅ Run all test suites successfully
3. ✅ Access all dashboards (Grafana, frontends)
4. ✅ Make a small test change to each application
5. ✅ Understand the main data flow from devices to frontends

### Week 2: Code Exploration
1. ✅ Trace a device reading from ADAM device through to frontend display
2. ✅ Understand Clean Architecture patterns in the C# backend
3. ✅ Explore React component structure in frontend applications
4. ✅ Review configuration management across all applications
5. ✅ Study error handling and retry patterns

### Week 3: Feature Development
1. ✅ Add a new channel to an existing ADAM device
2. ✅ Create a simple dashboard component
3. ✅ Add a new API endpoint with tests
4. ✅ Implement a configuration option
5. ✅ Deploy changes to local Docker environment

### Month 1 Goals
- Comfortable with all three applications
- Understand data flow and architecture decisions
- Can add features following established patterns
- Familiar with testing and deployment procedures
- Contributing to code reviews and technical discussions

## Resources & Documentation

### Technical Documentation
- **[Architecture Guide](docs/Industrial%20Data%20Acquisition%20Platform%20Architecture.md)**: System design and patterns
- **[Development Standards](docs/Industrial-Software-Development-Standards.md)**: Coding practices and quality standards
- **[Configuration Guide](docs/configuration-guide.md)**: Setup and configuration details
- **[CLAUDE.md](CLAUDE.md)**: AI development guidelines and principles

### API Documentation
- **ADAM Logger API**: Available when running WebApi project
- **Frontend API Clients**: See `lib/api/` directories in frontend projects
- **Database Schemas**: TimescaleDB and PostgreSQL schemas in respective documentation

### External Resources
- **[TimescaleDB Documentation](https://docs.timescale.com/)**: Time-series database features
- **[Next.js Documentation](https://nextjs.org/docs)**: Frontend framework guide
- **[shadcn/ui Components](https://ui.shadcn.com/)**: UI component library
- **[ADAM Device Manuals](docs/ADAM_Documentation/)**: Hardware specifications

## Getting Help

### Internal Resources
1. **Code Review**: Submit PRs for feedback on architecture and patterns
2. **Documentation**: All architectural decisions documented in `/docs`
3. **Issue Tracking**: Use repository issues for bugs and feature requests
4. **Development Guidelines**: Follow patterns established in CLAUDE.md

### External Communities
1. **TimescaleDB Community**: For time-series database questions
2. **ASP.NET Core**: For C# backend development issues
3. **React/Next.js**: For frontend development questions
4. **Industrial Automation**: For ADAM device and Modbus questions

---

**Welcome to the team! This ecosystem represents robust, maintainable industrial software that prioritizes reliability and developer experience. Focus on understanding the "why" behind architectural decisions, and always remember: we build software that works reliably in real-world industrial environments.**