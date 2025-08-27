# Platform Frontend Architecture Brief
**For PRD Development - Internal Document**
**Date**: August 23, 2025

## Executive Vision

Design a **modular industrial platform frontend** that serves as the unified interface for all Industrial ADAM system modules. This is NOT a microservices architecture, but a monolithic React application with well-separated, pluggable modules that share common infrastructure.

### Key Principles
- **Self-hosted**: On-premise deployment in factories, not cloud SaaS
- **Pragmatic over dogmatic**: Follow CLAUDE.md philosophy - logical cohesion over arbitrary separation
- **Module-based**: Each business capability (Logger, OEE, Scheduling) is a distinct module
- **Role-based**: Users see only what their permissions allow
- **Scalable**: Support single machine to entire enterprise deployment

## Architecture Requirements

### 1. Two Distinct Interfaces

#### Admin Dashboard
- System configuration and management
- User and role management  
- Module installation/configuration
- System health monitoring
- Security audit logs
- Target users: System administrators, IT staff

#### User Dashboard  
- Operational interface for daily use
- Role-based module visibility
- Real-time data displays
- Production monitoring
- Target users: Operators, supervisors, managers, maintenance

### 2. Module System

**NOT microservices** - Modules are logical separations within a single React app:
- Lazy-loaded for performance
- Register themselves with the platform
- Share common components and services
- Can be enabled/disabled per user role
- Deploy as single application

### 3. Permission Model

Based on existing Industrial.Adam.Security implementation:
- **Roles**: Operator, Supervisor, Admin, SystemAdmin
- **Module access**: Each role sees different modules
- **Data scope**: Operators see their line, supervisors see their area, etc.
- **ISA-95 hierarchy**: Enterprise → Site → Area → Line → Equipment

## Existing Backend APIs (DO NOT CREATE NEW ENDPOINTS)

### Authentication & Security
**Industrial.Adam.Security** (Implemented)
- `POST /api/auth/login` - JWT authentication
- `POST /api/auth/refresh` - Token refresh
- `GET /api/security/audit` - Audit logs
- `GET /api/security/metrics` - Security metrics

### Logger Module
**Industrial.Adam.Logger.WebApi** (Port 5139)
- `GET /api/devices` - List all ADAM devices
- `GET /api/devices/{id}` - Get device details
- `POST /api/devices` - Configure device
- `GET /api/counters` - Query counter data
- `GET /api/counters/latest` - Latest readings
- `GET /api/health` - System health
- **WebSocket** `/health-hub` - Real-time updates

### OEE Module  
**Industrial.Adam.Oee.WebApi** (Port 5140)
- `GET /api/oee/current` - Current OEE metrics
- `GET /api/oee/history` - Historical OEE data
- `GET /api/workorders` - List work orders
- `POST /api/workorders/start` - Start work order
- `POST /api/workorders/complete` - Complete work order
- `GET /api/stoppages` - Stoppage events
- `POST /api/stoppages` - Record stoppage
- `GET /api/jobs` - Simple job queue
- **WebSocket** `/stoppage-hub` - Real-time notifications

### Equipment Scheduling Module
**Industrial.Adam.EquipmentScheduling.WebApi** (Port 5141)
- `GET /api/resources` - Equipment hierarchy
- `POST /api/resources` - Create resource
- `GET /api/patterns` - Operating patterns
- `POST /api/patterns` - Create pattern
- `GET /api/schedules` - Generated schedules
- `POST /api/schedules/generate` - Generate schedule
- `GET /api/availability` - Availability queries

## Technical Stack (Approved)

**Core Technologies**:
- React 18+ with TypeScript
- Tailwind CSS for styling
- shadcn/ui component library
- React Router for navigation
- Zustand or Context for state management
- React Query for API calls
- Socket.io client for WebSocket

**Development Standards**:
- Follow CLAUDE.md principles
- Logical component cohesion
- Centralized patterns for common functionality
- Clear module boundaries
- Comprehensive error handling

## Module Requirements

### Current Modules to Support

1. **Device Management** (Logger)
   - Configure ADAM devices
   - Monitor device health
   - View real-time counters
   - Manage channels

2. **OEE Monitoring**  
   - Real-time OEE dashboard
   - Work order management
   - Stoppage tracking
   - Performance trends

3. **Equipment Scheduling**
   - Resource hierarchy management
   - Pattern configuration
   - Schedule generation
   - Availability planning

4. **Security Administration**
   - User management
   - Role assignment
   - Audit log viewing
   - Security metrics

### Future Module Considerations

Platform must support adding:
- Supervisor module (oversee multiple lines)
- Maintenance module
- Quality module
- Inventory module
- Energy monitoring module

## User Experience Requirements

### Admin Dashboard UX
- Full visibility and control
- Technical users who understand the system
- Direct access to all settings (no wizards)
- Detailed diagnostic information

### User Dashboard UX
- Role-appropriate simplicity
- Operators see simple, clear displays
- Supervisors see aggregated views
- Real-time updates without confusion
- Mobile-responsive for tablet use on factory floor

## Deployment Model

### Single Application Deployment
- One React build artifact
- Modules included/excluded at build time initially
- Future: Dynamic module loading from configuration
- Docker container deployment
- Nginx serving static files
- API gateway configuration for backend services

### Multi-Instance Scaling
- Multiple logger instances (each monitoring 10 ADAM units)
- Hierarchical supervision structure
- Shared database, separate module instances
- Role-based data filtering

## Platform Infrastructure Components

### Required Platform Services

1. **Authentication Service**
   - JWT token management
   - Auto-refresh handling
   - Login/logout flows
   - Permission checking

2. **Navigation Shell**
   - Top navigation bar
   - Module switcher
   - User menu
   - Breadcrumbs
   - Responsive sidebar

3. **Module Registry**
   - Simple registration system
   - Route management
   - Permission-based visibility
   - Lazy loading coordination

4. **Shared Components**
   - Data tables
   - Charts
   - Forms
   - Alerts/notifications
   - Loading states
   - Error boundaries

5. **Common Utilities**
   - API client with auth
   - WebSocket manager
   - Date/time formatting
   - Number formatting
   - Error handling
   - Logging

## Design Principles

### Visual Design
- Industrial/professional appearance
- High contrast for factory lighting
- Large touch targets for gloved hands
- Clear status indicators (green/yellow/red)
- Consistent with existing shadcn/ui patterns

### Information Architecture
- ISA-95 hierarchy navigation
- Clear module separation
- Consistent patterns across modules
- Progressive disclosure for complex features
- Context-aware help

## Success Criteria

1. **Single codebase** deploying all modules
2. **Role-based access** working correctly
3. **Real-time updates** via WebSocket
4. **Module independence** - can add/remove modules without affecting others
5. **Performance** - Lazy loading keeps initial bundle small
6. **Maintainability** - New developers can add modules easily

## Constraints

1. **NO new backend endpoints** - Use existing APIs only
2. **NO microservices** - Monolithic deployment
3. **NO external dependencies** - Self-contained deployment
4. **MUST follow CLAUDE.md** principles
5. **MUST support offline operation** where possible

## PRD Deliverable Requirements

The PRD should focus on:
1. **Platform infrastructure** specification (not individual modules)
2. **Module registration** and management system
3. **Authentication and authorization** flows
4. **Navigation and layout** structure
5. **Shared component** library requirements
6. **Build and deployment** architecture
7. **Developer guidelines** for adding new modules

Do NOT include:
- Detailed module-specific features (these come later)
- Backend API changes
- Cloud deployment options
- Multi-tenant SaaS features

---

*This brief should be used to create a Platform Frontend Infrastructure PRD that establishes the foundation for all current and future Industrial ADAM system modules.*