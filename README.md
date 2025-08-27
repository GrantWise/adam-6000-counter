# Industrial ADAM Counter Ecosystem

A complete industrial data acquisition platform consisting of three robust, standalone applications designed for manufacturing environments. Built for 24/7 industrial operation with comprehensive error handling, real-time monitoring, and production analytics.

## 🏭 Three-Application Ecosystem

This repository contains three **separate but complementary** industrial applications that work together to provide complete manufacturing intelligence:

| Application | Purpose | Technology | Location |
|-------------|---------|------------|----------|
| **🔧 ADAM Logger** | Data acquisition backend | C# .NET 9, TimescaleDB | `src/` |
| **📊 Counter Frontend** | Device monitoring dashboard | React 19, Next.js 15 | `adam-counter-frontend/` |
| **📈 OEE Application** | Manufacturing analytics | React 19, PostgreSQL | `oee-app/oee-interface/` |

### Architecture Philosophy
- **Loosely Coupled**: Each application is independently deployable and maintainable
- **Data-Driven Integration**: Applications communicate through shared databases
- **Never Combined**: Applications remain separate for scalability and maintainability
- **Pragmatic Excellence**: Clean, readable code that solves real industrial problems

## Overview

### 🔧 ADAM Logger Service (Backend)
High-performance C# service that connects to ADAM-6051 devices via Modbus TCP, collects counter values from configured channels, and stores them in TimescaleDB for monitoring and analysis. Features windowed rate calculation, dead letter queues for reliability, and comprehensive error recovery.

### 📊 ADAM Counter Frontend (Dashboard) 
Modern React dashboard for real-time device monitoring and configuration. Provides live counter visualization, device health monitoring, and administrative controls with WebSocket connectivity for instant updates.

### 📈 OEE Application (Analytics)
Manufacturing analytics interface focused on Overall Equipment Effectiveness (OEE) calculations, production reporting, and efficiency tracking. Integrates with production data to provide actionable insights for manufacturing optimization.

## 🚀 Ecosystem Features

### 🔧 Backend Service (ADAM Logger)
- **Reliable Device Communication**: Robust Modbus TCP with automatic retry and reconnection
- **Concurrent Multi-Device Polling**: Efficiently polls multiple ADAM devices simultaneously
- **TimescaleDB Integration**: Optimized time-series database with hypertables and compression
- **Windowed Rate Calculation**: Configurable time windows for smooth production metrics
- **Dead Letter Queue**: Ensures no data loss with automatic recovery from failures
- **Industrial-Grade Error Handling**: Comprehensive error recovery for 24/7 operation
- **Clean Architecture**: CQRS, DDD patterns following SOLID principles

### 📊 Frontend Applications
- **Real-Time Monitoring**: Live counter visualization with WebSocket updates
- **Device Management**: Configuration and health monitoring interfaces
- **Production Analytics**: OEE calculations and manufacturing efficiency tracking
- **Modern UI**: React 19 with shadcn/ui components and Tailwind CSS
- **Responsive Design**: Works on desktop, tablet, and mobile devices
- **Type Safety**: Full TypeScript implementation for reliability

### 🏗️ Infrastructure & DevOps
- **Docker Deployment**: Production-ready containerized deployment with Docker Compose
- **Monitoring Stack**: Grafana dashboards, Prometheus metrics, health checks
- **Development Tools**: One-click setup, device simulators, comprehensive testing
- **Configuration Management**: JSON-based configuration with environment overrides
- **Database Migration**: Automated schema setup and data migration tools

## Repository Structure

```
adam-6000-counter/                                    # Industrial Data Acquisition Ecosystem
├── README.md                                         # This file - ecosystem overview
├── ONBOARDING.md                                     # 📋 Complete developer guide  
├── QUICKSTART.md                                     # ⚡ Fast setup guide
├── CLAUDE.md                                         # 🤖 AI development guidelines
├── Industrial.Adam.Logger.sln                       # Main C# solution

# 🔧 ADAM Logger Service (C# Backend)
├── src/                                              
│   ├── Industrial.Adam.Logger.Core/                 # Core business logic & services
│   │   ├── Services/AdamLoggerService.cs             # Main orchestration service
│   │   ├── Processing/DataProcessor.cs               # Data processing pipeline  
│   │   ├── Storage/TimescaleStorage.cs               # TimescaleDB integration
│   │   └── Devices/ModbusDevicePool.cs               # Device communication
│   ├── Industrial.Adam.Logger.Console/              # Console application entry point
│   ├── Industrial.Adam.Logger.WebApi/               # REST API service
│   ├── Industrial.Adam.Logger.Simulator/            # ADAM device simulators
│   └── Industrial.Adam.Logger.*.Tests/              # Comprehensive test suites

# 📊 ADAM Counter Frontend (React Dashboard)
├── adam-counter-frontend/                            
│   ├── app/                                          # Next.js 15 app router
│   ├── components/                                   # React components
│   │   ├── real-time-monitoring.tsx                 # Live data visualization
│   │   ├── device-management.tsx                    # Device configuration
│   │   └── ui/                                       # shadcn/ui component library
│   ├── lib/api/                                      # API client & WebSocket
│   └── package.json                                  # Dependencies & scripts

# 📈 OEE Application (Manufacturing Analytics)  
├── oee-app/oee-interface/                           
│   ├── app/                                          # Next.js 15 app router
│   ├── components/                                   # OEE-specific components
│   ├── lib/database-queries.ts                      # PostgreSQL integration
│   ├── scripts/                                      # Database schema & migrations
│   └── package.json                                  # Dependencies & scripts

# 🐳 Infrastructure & DevOps
├── docker/                                           # Container deployment
│   ├── docker-compose.yml                           # Complete infrastructure stack
│   ├── config/                                       # Application configurations
│   └── grafana/dashboards/                          # Pre-built monitoring dashboards
├── scripts/                                          # Development automation
│   ├── setup-dev-environment.sh                     # 🚀 One-click complete setup
│   ├── start-simulators.sh                          # Device simulator management
│   └── test-*.sh                                     # Testing & validation scripts
└── docs/                                             # Technical documentation
    ├── architecture_guide.md                        # System design patterns
    ├── development_standards.md                     # Coding practices & quality
    └── configuration-guide.md                       # Setup & deployment guides
```

## 🚀 Platform Capabilities

### 🔧 Advanced Data Processing (ADAM Logger)
- **Windowed Rate Calculation**: Configurable time windows for smooth production metrics
- **Counter Overflow Detection**: Automatic handling of 16-bit and 32-bit counter wraparounds
- **Circular Buffer Storage**: Efficient memory usage with automatic cleanup
- **Dead Letter Queue**: Failed database writes queued and retried automatically
- **No Data Loss**: Critical production data survives application restarts
- **TimescaleDB Integration**: Hypertables with automatic compression and continuous aggregates

### 📊 Real-Time Visualization (Counter Frontend)
- **Live Dashboard**: WebSocket-powered real-time counter monitoring
- **Device Configuration**: Dynamic device setup and channel management
- **Health Monitoring**: Connection status and diagnostic information
- **Responsive Design**: Works across desktop, tablet, and mobile devices
- **Type-Safe API**: Full TypeScript integration with backend services

### 📈 Production Analytics (OEE Application)
- **OEE Calculations**: Overall Equipment Effectiveness tracking and reporting
- **Production Metrics**: Throughput, efficiency, and quality analytics
- **Job Performance**: Production job tracking and analysis
- **Historical Trends**: Time-based performance analysis and reporting
- **Manufacturing Intelligence**: Actionable insights for process optimization

### 🏗️ Development & Operations
- **One-Click Setup**: Complete development environment with `./scripts/setup-dev-environment.sh`
- **Device Simulators**: Built-in ADAM device simulators for hardware-free development
- **Comprehensive Testing**: Unit, integration, and system test suites
- **Monitoring Stack**: Grafana dashboards, Prometheus metrics, health checks
- **.NET 9 Performance**: Latest runtime optimizations for industrial workloads

## Quick Start

For detailed setup instructions, see **[QUICKSTART.md](QUICKSTART.md)** or **[ONBOARDING.md](ONBOARDING.md)** for comprehensive developer guidance.

### 🚀 Complete Ecosystem Setup (Recommended)

```bash
# Clone the repository
git clone [repository-url]
cd adam-6000-counter

# One-command setup for all three applications
./scripts/setup-dev-environment.sh
```

This automatically sets up:
- ✅ ADAM Logger backend service (C# .NET 9)
- ✅ Counter Frontend dashboard (React/Next.js)
- ✅ OEE Analytics application  
- ✅ TimescaleDB with proper schema
- ✅ Grafana with pre-configured dashboards
- ✅ 3 ADAM device simulators for testing
- ✅ All infrastructure services (Prometheus, etc.)

### 📱 Access Your Applications

After setup completes:
- **Counter Frontend Dashboard**: http://localhost:3000
- **OEE Analytics Interface**: http://localhost:3001  
- **Logger API**: http://localhost:5000/health
- **OEE API**: http://localhost:5001/health
- **Grafana Monitoring**: http://localhost:3002 (admin/admin)
- **TimescaleDB**: postgresql://localhost:5433 (industrial_system/IndustrialCounter2024!@#$)

### 🐳 Docker Infrastructure Only

To run just the infrastructure services (databases, monitoring) without the applications:

```bash
cd docker
docker-compose up -d

# Then start applications manually:
# Backend: dotnet run --project src/Industrial.Adam.Logger.Console
# Frontend 1: cd adam-counter-frontend && npm run dev  
# Frontend 2: cd oee-app/oee-interface && npm run dev
```

### 🔍 Verification & Testing

```bash
# Check all services are running
docker-compose ps

# View live data flow  
docker-compose logs -f adam-logger

# Test frontend applications
curl http://localhost:3000/api/health
curl http://localhost:3001/api/health

# Test backend APIs
curl http://localhost:5000/health  # Logger API
curl http://localhost:5001/health  # OEE API
```

## 🏗️ Technology Stack

### Backend (ADAM Logger Service)
- **Runtime**: .NET 9 with C# 13
- **Architecture**: Clean Architecture, CQRS, DDD patterns
- **Database**: TimescaleDB (PostgreSQL with time-series extensions)
- **Communication**: NModbus for Modbus TCP, SignalR for WebSockets
- **Testing**: xUnit, Moq, comprehensive integration tests
- **Monitoring**: Prometheus metrics, structured logging

### Frontend Applications
- **Framework**: Next.js 15 with React 19
- **Language**: TypeScript with strict type checking
- **UI Components**: shadcn/ui with Radix UI primitives
- **Styling**: Tailwind CSS with responsive design
- **State Management**: React Query for server state, Zustand for client state
- **Real-time**: WebSocket integration for live updates

### Infrastructure & DevOps
- **Containerization**: Docker with multi-stage builds
- **Orchestration**: Docker Compose for development
- **Monitoring**: Grafana dashboards, Prometheus metrics
- **Database**: TimescaleDB for time-series, PostgreSQL for relational data
- **Reverse Proxy**: NGINX for production deployments

## Architecture & Design Principles

### System Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                   Frontend Applications                     │
│  [Counter Dashboard] [OEE Analytics] [Grafana Monitoring]  │
├─────────────────────────────────────────────────────────────┤
│                     API Gateway                            │
│        [REST APIs] [WebSocket] [Health Checks]            │
├─────────────────────────────────────────────────────────────┤
│                 ADAM Logger Service                        │
│    [Device Pool] [Data Processor] [Rate Calculator]       │
├─────────────────────────────────────────────────────────────┤
│                   Storage Layer                            │
│     [TimescaleDB] [PostgreSQL] [Dead Letter Queue]        │
├─────────────────────────────────────────────────────────────┤
│                 Communication Layer                        │
│          [Modbus TCP] [Device Simulators]                 │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles
- **Separation of Concerns**: Each application has a clear, focused responsibility
- **Loose Coupling**: Applications communicate through well-defined data contracts
- **High Cohesion**: Related functionality is grouped together logically
- **Fail-Safe Operation**: Graceful degradation and automatic recovery
- **Observable Systems**: Comprehensive logging, metrics, and health monitoring
- **Configuration-Driven**: Behavior changes through config, not code modifications

## Getting Started for Developers

### 📋 New Developer Path
1. **Setup**: Follow [QUICKSTART.md](QUICKSTART.md) for fast setup or [ONBOARDING.md](ONBOARDING.md) for comprehensive guide
2. **Explore**: Run the automated setup and explore all three applications
3. **Understand**: Review architecture documentation in `/docs` directory
4. **Contribute**: Follow patterns established in [CLAUDE.md](CLAUDE.md) development guidelines

### 🎯 First Tasks for New Team Members
1. ✅ Complete automated environment setup
2. ✅ Access all three application interfaces
3. ✅ View live data flowing through the system
4. ✅ Make a small configuration change
5. ✅ Run the test suites successfully

## Configuration Management

📖 **For detailed configuration, see [Setup Troubleshooting Guide](docs/SETUP_TROUBLESHOOTING.md)**

Each application uses JSON configuration with environment variable overrides:

- **ADAM Logger**: `src/Industrial.Adam.Logger.WebApi/appsettings.json`
- **OEE API**: `src/Industrial.Adam.Oee/WebApi/appsettings.json`
- **Counter Frontend**: Environment variables and `next.config.mjs`
- **OEE Application**: Database configuration and environment variables
- **Docker**: Centralized configuration in `docker/config/`

### Required Environment Variables
```bash
TIMESCALEDB_USERNAME=industrial_system
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
JWT_ISSUER=Industrial.Adam.System
JWT_AUDIENCE=Industrial.Adam.APIs
```

### Service Ports
- **Logger API**: Port 5000
- **OEE API**: Port 5001 
- **TimescaleDB**: Port 5433
- **Counter Frontend**: Port 3000
- **OEE Frontend**: Port 3001
- **Grafana**: Port 3002

Key configuration features:
- **Hierarchical Settings**: Default values with environment-specific overrides
- **Hot Reload**: Configuration changes applied without restarts (where supported)
- **Validation**: Startup validation with helpful error messages
- **Templates**: Ready-to-use configuration templates for different scenarios
- **Security Requirements**: Complex passwords and JWT keys required for production

## Testing & Quality Assurance

### 🧪 Comprehensive Testing Strategy
```bash
# Run all backend tests
dotnet test

# Run frontend tests  
cd adam-counter-frontend && npm test
cd oee-app/oee-interface && npm test

# Run with coverage reporting
./scripts/run-coverage.sh

# Integration testing with simulators
./scripts/full-system-test.sh
```

### Test Organization
- **Unit Tests**: Individual component testing
- **Integration Tests**: Cross-component interaction testing  
- **System Tests**: End-to-end workflow validation
- **Performance Tests**: Load testing with multiple devices
- **Simulator Tests**: Hardware-free validation

## Documentation & Resources

### 📚 Complete Documentation Suite
- **[ONBOARDING.md](ONBOARDING.md)**: Comprehensive developer guide with week-by-week learning plan
- **[QUICKSTART.md](QUICKSTART.md)**: Fast setup guide for immediate productivity
- **[DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)**: Advanced development environment configuration
- **[Service Startup Guide](docs/SERVICE_STARTUP_GUIDE.md)**: Step-by-step service startup sequence
- **[Setup Troubleshooting Guide](docs/SETUP_TROUBLESHOOTING.md)**: Common issues and working configurations
- **[CLAUDE.md](CLAUDE.md)**: AI development guidelines and architectural principles

### 🏗️ Technical Documentation  
- **[Architecture Guide](docs/Industrial%20Data%20Acquisition%20Platform%20Architecture.md)**: System design and extensibility patterns
- **[Development Standards](docs/Industrial-Software-Development-Standards.md)**: Coding practices and quality standards
- **[Configuration Guide](docs/configuration-guide.md)**: Detailed setup and configuration instructions
- **[Migration Guide](docs/MIGRATION_GUIDE_INFLUXDB_TO_TIMESCALEDB.md)**: Database migration procedures

### 🎯 Key Concepts
- **Three Separate Applications**: Each serves a distinct purpose and remains independently deployable
- **Data-Driven Integration**: Applications communicate through shared TimescaleDB/PostgreSQL databases
- **Clean Architecture**: SOLID principles, CQRS, and DDD patterns throughout the backend
- **Industrial Reliability**: Designed for 24/7 operation with comprehensive error handling

## Support & Contributing

### 🤝 Getting Help
1. **Documentation**: Start with [ONBOARDING.md](ONBOARDING.md) for comprehensive guidance
2. **Quick Setup**: Use [QUICKSTART.md](QUICKSTART.md) for immediate setup
3. **Issues**: Use the repository issue tracker for bugs and feature requests
4. **Architecture Questions**: Review `/docs` directory for design decisions

### 🔧 Contributing Guidelines
- **Follow CLAUDE.md**: AI development guidelines and architectural principles
- **Clean Architecture**: Maintain separation of concerns and SOLID principles
- **Test Coverage**: Add comprehensive tests for new features
- **Documentation**: Update relevant documentation for changes
- **Code Review**: Submit PRs for architecture and pattern feedback

### 📊 Project Status
- **Backend**: Production-ready C# .NET 9 service with comprehensive testing
- **Frontend**: Modern React applications with real-time capabilities
- **Infrastructure**: Docker-based deployment with monitoring stack
- **Documentation**: Complete developer onboarding and technical guides

## Data Flow Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   ADAM Devices  │───▶│   ADAM Logger    │───▶│   TimescaleDB   │
│  (Modbus TCP)   │    │  (C# Service)    │    │ (Time-series)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                        │
                                ▼                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Device Health  │◀───│   Health Checks  │    │  Counter Frontend│
│   Monitoring    │    │  & Dead Letter   │    │   (React App)   │
└─────────────────┘    │     Queue        │    └─────────────────┘
                       └──────────────────┘            │
                                                       ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │     Grafana      │    │  OEE Analytics  │
                       │   Dashboards     │    │   (React App)   │
                       └──────────────────┘    └─────────────────┘
```

## Success Metrics

When properly deployed, this ecosystem provides:

- **📊 Real-Time Visibility**: Live production counter monitoring across all devices
- **⚡ Sub-Second Response**: WebSocket updates for immediate operator feedback  
- **🔄 99.9% Uptime**: Industrial-grade reliability with automatic recovery
- **📈 Production Intelligence**: OEE calculations and efficiency analytics
- **🚨 Proactive Monitoring**: Health checks and alerting for all system components
- **🏗️ Scalable Architecture**: Easily add new devices, channels, and applications

---

**This Industrial ADAM Counter Ecosystem represents a complete manufacturing intelligence platform built with modern, maintainable technology. Each application serves a specific purpose while contributing to comprehensive production visibility and analytics.**

*Built for industry. Designed for reliability. Optimized for developer experience.* 🏭