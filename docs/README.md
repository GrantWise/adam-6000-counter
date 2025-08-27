# Industrial ADAM Platform Documentation

Welcome to the comprehensive documentation for the Industrial ADAM-6000 Counter Monitoring Platform. This documentation is organized to help you quickly find the information you need.

## 📚 Documentation Structure

### [01. Getting Started](./01-getting-started/)
Essential guides for new developers and system setup.

- **[Development Setup](./01-getting-started/development-setup.md)** - Complete development environment setup
- **[Quick Start Guide](./01-getting-started/quickstart.md)** - Get running in 5 minutes
- **[Onboarding Guide](./01-getting-started/onboarding.md)** - New team member onboarding
- **[Troubleshooting](./01-getting-started/troubleshooting.md)** - Common issues and solutions
- **[Dev Credentials](./03-development/development-credentials.md)** - Default development accounts

### [02. Architecture](./02-architecture/)
System design, patterns, and architectural decisions.

- **[System Overview](./02-architecture/system-overview.md)** - High-level architecture and data flow
- **[Canonical Object Catalog](./02-architecture/canonical-object-catalog.md)** - Domain models and entities
- **Clean Architecture** - CQRS, DDD patterns (coming soon)
- **Microservices Design** - Service boundaries and communication (coming soon)

### [03. Development](./03-development/)
Coding standards, testing, and development practices.

- **[Coding Standards](./03-development/coding-standards.md)** - Industrial software development standards
- **[CFR Part 11 Compliance](./03-development/cfr-compliance.md)** - Regulatory compliance for data integrity
- **API Reference** - Complete API documentation (coming soon)
- **Testing Guide** - Unit, integration, and E2E testing (coming soon)

### [04. Deployment](./04-deployment/)
Production deployment, configuration, and monitoring.

- **[Configuration Guide](./04-deployment/configuration.md)** - System configuration reference
- **[Production Guide](./04-deployment/production-guide.md)** - Service startup and deployment
- **Docker Setup** - Container orchestration (coming soon)
- **Monitoring** - Grafana, Prometheus setup (coming soon)

### [05. Modules](./05-modules/)
Detailed documentation for each system module.

#### [Logger Service](./05-modules/logger-service/)
Core data acquisition and counter monitoring service.
- Device integration and Modbus communication
- Counter data processing and overflow handling
- TimescaleDB storage and data retention

#### [OEE Service](./05-modules/oee-service/)
Overall Equipment Effectiveness calculation and monitoring.
- OEE metrics calculation (Availability, Performance, Quality)
- Work order management
- Stoppage event tracking
- Production scheduling integration

#### [Frontend Platform](./05-modules/frontend/)
React-based monitoring and management interface.
- Real-time dashboard and monitoring
- Device configuration and management
- OEE visualization and reporting
- User management and security

#### [Scheduling Service](./05-modules/scheduling-service/)
Equipment scheduling and production planning.
- Production schedule management
- Resource allocation
- Shift management

#### [Security Module](./05-modules/security-module/)
Authentication, authorization, and security features.
- JWT-based authentication
- Role-based access control (RBAC)
- Audit logging

### [06. Hardware](./06-hardware/)
ADAM-6000 series hardware integration and specifications.

- **[ADAM-6051 Specifications](./06-hardware/ADAM-6000/)** - Hardware specs and capabilities
- **[Simulator Guide](./06-hardware/simulator-guide.md)** - Device simulator configuration
- **[ADAM Documentation](./06-hardware/ADAM_Documentation/)** - Official ADAM manuals
- **Modbus Integration** - Protocol implementation (coming soon)

### [Archive](./archive/)
Historical documents, completed plans, and legacy documentation.

- **[Completed Plans](./archive/completed-plans/)** - Successfully implemented features
- **[Legacy Documentation](./archive/legacy-docs/)** - Deprecated documentation
- **[Old Reports](./archive/old-reports/)** - Historical analysis and reports

## 🚀 Quick Links

### For Developers
- [Quick Start Guide](./01-getting-started/quickstart.md) - Get running quickly
- [Development Setup](./01-getting-started/development-setup.md) - Full environment setup
- [Coding Standards](./03-development/coding-standards.md) - Code quality guidelines
- [API Reference](./05-modules/logger-service/) - Service APIs

### For System Administrators
- [Production Guide](./04-deployment/production-guide.md) - Deploy to production
- [Configuration](./04-deployment/configuration.md) - System configuration
- [Troubleshooting](./01-getting-started/troubleshooting.md) - Common issues

### For Business Users
- [System Overview](./02-architecture/system-overview.md) - What the system does
- [OEE Module](./05-modules/oee-service/) - Production metrics
- [Frontend Platform](./05-modules/frontend/) - User interface guide

## 📋 Key Technologies

- **Backend**: .NET 8/9, C#, Clean Architecture, CQRS, DDD
- **Frontend**: React 18, TypeScript, Vite, TailwindCSS, shadcn/ui
- **Database**: TimescaleDB (PostgreSQL), time-series data
- **Hardware**: ADAM-6051 Modbus TCP/IP counters
- **Infrastructure**: Docker, Docker Compose, nginx
- **Monitoring**: Grafana, Prometheus
- **Testing**: xUnit, Vitest, Playwright

## 🔒 Compliance & Standards

- **21 CFR Part 11**: FDA regulatory compliance for electronic records
- **ISA-95**: Industrial automation standards
- **Clean Architecture**: Robert C. Martin's architecture principles
- **SOLID Principles**: Object-oriented design principles
- **DDD**: Domain-Driven Design patterns

## 📞 Support

For questions, issues, or contributions:
- Check [Troubleshooting Guide](./01-getting-started/troubleshooting.md)
- Review [Development Standards](./03-development/coding-standards.md)
- Consult [CLAUDE.md](../CLAUDE.md) for AI-assisted development

## 📝 Document Status

This documentation is actively maintained. Last updated: January 2025

| Section | Status | Completeness |
|---------|--------|--------------|
| Getting Started | ✅ Active | 100% |
| Architecture | ✅ Active | 80% |
| Development | ✅ Active | 75% |
| Deployment | ✅ Active | 80% |
| Logger Service | ✅ Active | 90% |
| OEE Service | ✅ Active | 95% |
| Frontend | ✅ Active | 90% |
| Hardware | ✅ Active | 85% |

---

*For AI-assisted development, refer to [CLAUDE.md](../CLAUDE.md) in the root directory.*