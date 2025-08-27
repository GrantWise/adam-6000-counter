# Industrial ADAM Platform Frontend Infrastructure PRD
**Version**: 1.0  
**Date**: August 23, 2025  
**Status**: Draft for Review

---

## 1. Executive Summary

### Purpose
Define and specify the platform infrastructure for the Industrial ADAM Frontend - a monolithic React application that serves as the unified foundation for all Industrial ADAM system modules. This PRD establishes the core platform architecture, not individual module features.

### Scope
This PRD covers the **platform infrastructure only** - the operating system that enables modules to plug in, communicate, and operate cohesively. Individual module functionality (Logger, OEE, Equipment Scheduling, etc.) is addressed in separate module-specific PRDs.

### Success Vision
A single, self-hosted React application that provides:
- Role-based access to industrial monitoring capabilities
- Seamless module integration without code coupling
- Real-time industrial data visualization
- Scalable foundation for current and future business modules

---

## 2. Goals and Non-Goals

### Goals
**PLAT-G-001**: Create a unified platform shell supporting two distinct interfaces (Admin & User Dashboards)  
**PLAT-G-002**: Implement a module registry system enabling lazy-loaded, pluggable business modules  
**PLAT-G-003**: Establish authentication and authorization infrastructure using existing JWT-based security APIs  
**PLAT-G-004**: Provide shared component library and utilities for consistent module development  
**PLAT-G-005**: Enable real-time data updates across all modules via WebSocket infrastructure  
**PLAT-G-006**: Support role-based visibility and ISA-95 hierarchy-based data scoping

### Non-Goals
- Individual module business logic (handled by module-specific PRDs)
- Backend API modifications (use existing endpoints only)
- Microservices architecture (monolithic application required)
- Cloud deployment (self-hosted on-premise only)
- Multi-tenant SaaS features

---

## 3. User Personas

### Primary: System Administrator
**Profile**: IT staff responsible for system configuration and management  
**Context**: Technical users who understand industrial systems architecture  
**Needs**: Complete visibility, direct access to all settings, detailed diagnostic information  
**Interface**: Admin Dashboard

### Primary: Production Operator  
**Profile**: Factory floor workers monitoring equipment and production  
**Context**: Non-technical users focused on operational tasks  
**Needs**: Simple, clear displays relevant to their equipment/line only  
**Interface**: User Dashboard

### Secondary: Production Supervisor
**Profile**: Mid-level management overseeing multiple production lines  
**Context**: Technical operations knowledge, broader scope than operators  
**Needs**: Aggregated views across their area of responsibility  
**Interface**: User Dashboard (elevated permissions)

---

## 4. Platform Architecture Requirements

### 4.1 Application Structure
**REQ-001** (Must Have): The platform SHALL be implemented as a single monolithic React application  
**REQ-002** (Must Have): Business modules SHALL be logically separated but deployed together  
**REQ-003** (Must Have): Modules SHALL be lazy-loaded for optimal performance  
**REQ-004** (Must Have): The platform SHALL support both Admin and User dashboard interfaces

### 4.2 Technology Stack
**REQ-005** (Must Have): React 18+ with TypeScript for type safety  
**REQ-006** (Must Have): Tailwind CSS for consistent styling  
**REQ-007** (Must Have): shadcn/ui component library for industrial UI patterns  
**REQ-008** (Must Have): React Router for client-side navigation  
**REQ-009** (Must Have): React Query for API state management  
**REQ-010** (Should Have): Zustand for global application state where Context is insufficient

### 4.3 Build and Deployment
**REQ-011** (Must Have): Single build artifact containing all enabled modules  
**REQ-012** (Must Have): Docker container deployment support  
**REQ-013** (Should Have): Environment-based module enabling/disabling  
**REQ-014** (Could Have): Future support for dynamic module loading from configuration

---

## 5. Module System Specification

### 5.1 Module Registration
**REQ-015** (Must Have): Modules SHALL register themselves with the platform via a centralized registry  
**REQ-016** (Must Have): Module registration SHALL include metadata: name, description, routes, permissions, icon  
**REQ-017** (Must Have): The platform SHALL validate module registrations at startup  
**REQ-018** (Must Have): Modules SHALL declare their required permissions for access control

### 5.2 Module Lifecycle
**REQ-019** (Must Have): Modules SHALL be lazy-loaded only when accessed by authorized users  
**REQ-020** (Must Have): Module registration SHALL happen at application initialization  
**REQ-021** (Should Have): Modules SHALL support graceful loading and error states  
**REQ-022** (Should Have): Failed module loads SHALL NOT crash the entire platform

### 5.3 Module Interface Contract
**REQ-023** (Must Have): All modules SHALL implement a standard interface: `{ name, routes, permissions, component }`  
**REQ-024** (Must Have): Module routes SHALL be prefixed to avoid conflicts (e.g., `/modules/logger/*`)  
**REQ-025** (Must Have): Modules SHALL access platform services via provided context/hooks  
**REQ-026** (Should Have): Modules SHALL support common platform events (logout, permission changes)

---

## 6. Authentication & Authorization Flows

### 6.1 Authentication Infrastructure
**REQ-027** (Must Have): JWT-based authentication using existing `/api/auth/login` endpoint  
**REQ-028** (Must Have): Automatic token refresh using existing `/api/auth/refresh` endpoint  
**REQ-029** (Must Have): Secure token storage in httpOnly cookies or secure localStorage  
**REQ-030** (Must Have): Automatic logout on token expiration with user notification

### 6.2 Authorization Implementation  
**REQ-031** (Must Have): Role-based access control supporting: Operator, Supervisor, Admin, SystemAdmin  
**REQ-032** (Must Have): Module visibility based on user role permissions  
**REQ-033** (Must Have): API request authentication via Authorization header injection  
**REQ-034** (Must Have): ISA-95 hierarchy-based data scoping (Enterprise → Site → Area → Line → Equipment)

### 6.3 Security Requirements
**REQ-035** (Must Have): Session timeout with configurable duration  
**REQ-036** (Must Have): Logout functionality clearing all session data  
**REQ-037** (Should Have): "Remember Me" functionality for extended sessions  
**REQ-038** (Should Have): Login attempt limiting with lockout protection

---

## 7. Navigation & Layout Structure

### 7.1 Admin Dashboard Layout
**REQ-039** (Must Have): Top navigation bar with user menu and logout  
**REQ-040** (Must Have): Left sidebar with module navigation organized by category  
**REQ-041** (Must Have): Main content area with breadcrumb navigation  
**REQ-042** (Must Have): System status indicator showing overall health

### 7.2 User Dashboard Layout  
**REQ-043** (Must Have): Simplified navigation showing only authorized modules  
**REQ-044** (Must Have): Large, touch-friendly interface elements for factory floor use  
**REQ-045** (Must Have): High-contrast design suitable for industrial lighting conditions  
**REQ-046** (Must Have): Mobile-responsive design for tablet use

### 7.3 Navigation Behavior
**REQ-047** (Must Have): Active route highlighting in navigation  
**REQ-048** (Must Have): Breadcrumb navigation showing current location hierarchy  
**REQ-049** (Should Have): Quick module switcher for frequently accessed modules  
**REQ-050** (Should Have): Collapsible navigation for different screen sizes

---

## 8. User Management System

### 8.1 User Administration Interface (Admin Dashboard)
**REQ-051** (Must Have): Admin dashboard SHALL provide user management interface for system administrators  
**REQ-052** (Must Have): User creation form SHALL capture: username, email, full name, and assigned role  
**REQ-053** (Must Have): Role assignment SHALL support: Operator, Supervisor, Admin, SystemAdmin  
**REQ-054** (Must Have): User list view SHALL display all users with current status, role, and hierarchy assignment  
**REQ-055** (Must Have): User edit functionality SHALL allow role changes and hierarchy reassignment  
**REQ-056** (Must Have): User deactivation SHALL disable login without data deletion  
**REQ-057** (Should Have): Bulk user operations for efficient management of large user bases

### 8.2 User Profile Management
**REQ-058** (Must Have): Users SHALL be able to view their own profile and hierarchy assignment  
**REQ-059** (Must Have): Users SHALL be able to change their password  
**REQ-060** (Should Have): Users SHALL be able to update basic profile information (email, full name)  
**REQ-061** (Could Have): Profile preferences for dashboard customization

### 8.3 Role Management
**REQ-062** (Must Have): System SHALL enforce role-based permissions as defined in existing backend  
**REQ-063** (Must Have): Role definitions SHALL be consistent with Industrial.Adam.Security module:  
- **Operator**: Access to assigned equipment/line data only
- **Supervisor**: Access to assigned area data and subordinate equipment
- **Admin**: Full access to site data and user management
- **SystemAdmin**: Complete system access including security configuration

**REQ-064** (Should Have): Visual role hierarchy display showing permission scope  
**REQ-065** (Could Have): Future support for custom role definitions

---

## 9. ISA-95 Hierarchy Configuration

### 9.1 Hierarchy Definition (Admin Dashboard)
**REQ-066** (Must Have): Admin dashboard SHALL provide ISA-95 hierarchy management interface  
**REQ-067** (Must Have): Hierarchy SHALL support standard ISA-95 levels: Enterprise → Site → Area → Line → Equipment  
**REQ-068** (Must Have): Tree view interface SHALL display current hierarchy with expand/collapse functionality  
**REQ-069** (Must Have): Node creation SHALL allow adding new entities at any hierarchy level  
**REQ-070** (Must Have): Node editing SHALL support renaming and property modification  
**REQ-071** (Must Have): Node deletion SHALL include cascade validation (prevent deletion with children)  
**REQ-072** (Should Have): Drag-and-drop reordering within hierarchy levels

### 9.2 Hierarchy Validation
**REQ-073** (Must Have): System SHALL enforce hierarchy integrity (no orphaned nodes)  
**REQ-074** (Must Have): Equipment nodes SHALL be associable with ADAM device configurations  
**REQ-075** (Must Have): Hierarchy changes SHALL validate against existing user assignments  
**REQ-076** (Should Have): Import/export functionality for hierarchy backup and migration

### 9.3 Hierarchy Navigation
**REQ-077** (Must Have): User dashboard SHALL display hierarchy breadcrumbs showing user's position  
**REQ-078** (Must Have): Navigation SHALL respect user's hierarchy scope (no access above assigned level)  
**REQ-079** (Should Have): Quick navigation between peer nodes in hierarchy  
**REQ-080** (Could Have): Hierarchy search functionality for large organizations

---

## 10. User-Hierarchy Association

### 10.1 Assignment Management (Admin Dashboard)
**REQ-081** (Must Have): Admin interface SHALL allow assigning users to specific hierarchy positions  
**REQ-082** (Must Have): User assignment SHALL define their data scope and access boundaries  
**REQ-083** (Must Have): Multiple assignment support SHALL enable users to access multiple areas/lines  
**REQ-084** (Must Have): Assignment validation SHALL prevent privilege escalation (supervisors can't access peer areas)  
**REQ-085** (Must Have): Visual assignment display SHALL show user-hierarchy relationships clearly

### 10.2 Data Scoping Implementation
**REQ-086** (Must Have): All module data SHALL be filtered based on user's hierarchy assignments  
**REQ-087** (Must Have): API requests SHALL automatically include hierarchy scope parameters  
**REQ-088** (Must Have): Real-time data updates SHALL respect user's hierarchy boundaries  
**REQ-089** (Must Have): Cross-hierarchy access SHALL be explicitly denied at the platform level

### 10.3 Assignment Inheritance
**REQ-090** (Must Have): Supervisor assignments SHALL automatically include subordinate hierarchy access  
**REQ-091** (Must Have): Admin assignments SHALL provide site-wide access  
**REQ-092** (Should Have): Temporary assignment capability for shift coverage scenarios  
**REQ-093** (Could Have): Time-based assignment scheduling (different shifts = different scope)

---

## 11. Shared Component Library

### 8.1 Data Display Components
**REQ-051** (Must Have): Reusable data tables with sorting, filtering, and pagination  
**REQ-052** (Must Have): Industrial-appropriate charts (line, bar, pie) for metrics visualization  
**REQ-053** (Must Have): Status indicators with clear green/yellow/red states  
**REQ-054** (Must Have): Real-time value displays with automatic refresh

### 8.2 Form and Input Components
**REQ-055** (Must Have): Standardized form components following shadcn/ui patterns  
**REQ-056** (Must Have): Validation components with industrial-appropriate error messaging  
**REQ-057** (Must Have): Date/time pickers suitable for shift scheduling  
**REQ-058** (Should Have): Numeric input components with unit display and validation

### 8.3 Feedback and State Components
**REQ-059** (Must Have): Loading states for API operations  
**REQ-060** (Must Have): Error boundaries preventing module failures from affecting platform  
**REQ-061** (Must Have): Toast notifications for user feedback  
**REQ-062** (Must Have): Confirmation dialogs for destructive actions

---

## 9. Technical Implementation

### 9.1 API Integration
**REQ-063** (Must Have): Centralized API client with automatic authentication header injection  
**REQ-064** (Must Have): Request/response interceptors for error handling and logging  
**REQ-065** (Must Have): API client SHALL support all existing backend endpoints without modification  
**REQ-066** (Must Have): Consistent error handling across all API operations

### 9.2 WebSocket Management
**REQ-067** (Must Have): WebSocket client supporting multiple hubs (health-hub, stoppage-hub)  
**REQ-068** (Must Have): Automatic reconnection on connection loss  
**REQ-069** (Must Have): WebSocket message routing to appropriate modules  
**REQ-070** (Should Have): Connection status indication for users

### 9.3 State Management
**REQ-071** (Must Have): User authentication state managed globally  
**REQ-072** (Must Have): Module registration state accessible platform-wide  
**REQ-073** (Should Have): Global application settings (theme, language, timezone)  
**REQ-074** (Should Have): Module-specific state isolated to prevent conflicts

### 9.4 Performance Requirements
**REQ-075** (Must Have): Initial bundle size under 500KB (before modules)  
**REQ-076** (Must Have): Module lazy loading reducing initial load time  
**REQ-077** (Must Have): API response caching for frequently accessed data  
**REQ-078** (Should Have): Service worker for offline capability where applicable

---

## 10. Acceptance Criteria

### Authentication Flow
**Given** a user with valid credentials  
**When** they log in via the platform  
**Then** they receive a JWT token and can access authorized modules

**Given** an authenticated user's token expires  
**When** they make an API request  
**Then** the token is automatically refreshed without user disruption

### Module Loading
**Given** an authorized user navigates to a module  
**When** the module is accessed for the first time  
**Then** it is lazy-loaded and displays correctly within the platform shell

**Given** a module fails to load  
**When** the error occurs  
**Then** an appropriate error message displays and other modules remain functional

### Permission Enforcement
**Given** a user with Operator role  
**When** they access the platform  
**Then** they see only modules appropriate for their role and data scope

### Real-time Updates
**Given** a WebSocket connection is established  
**When** real-time data is received  
**Then** appropriate modules are notified and update their displays

---

## 11. Success Metrics

### Technical Metrics
- **Bundle Size**: Initial load < 500KB, individual modules < 200KB each
- **Load Time**: Time to interactive < 3 seconds on typical industrial hardware
- **API Response**: 95% of API calls complete within 2 seconds
- **WebSocket Reliability**: > 99% uptime with automatic reconnection

### User Experience Metrics
- **Module Load Success**: > 99.5% successful lazy loads
- **Authentication Success**: > 99% login success rate
- **Role Accuracy**: 100% correct module visibility per user role
- **Error Recovery**: < 5 seconds to recover from temporary failures

### Developer Experience Metrics
- **Module Development Time**: New module integration < 4 hours
- **Code Reuse**: > 80% of UI components use shared library
- **Development Setup**: New developer productive within 2 hours
- **Build Time**: Full application build < 2 minutes

---

## 12. Development Guidelines for Module Authors

### 12.1 Module Structure Requirements
Module developers MUST follow these patterns:

```typescript
// Module registration interface
interface ModuleRegistration {
  name: string;
  displayName: string;
  description: string;
  icon: React.ComponentType;
  routes: ModuleRoute[];
  permissions: string[];
  category: 'monitoring' | 'configuration' | 'administration';
}

// Module route interface
interface ModuleRoute {
  path: string;
  component: React.LazyExoticComponent<React.ComponentType>;
  exact?: boolean;
  permissions?: string[];
}
```

### 12.2 Platform Service Access
Modules SHALL access platform services via provided hooks:

```typescript
// Available platform hooks
const { user, permissions } = useAuth();
const { apiClient } = useApiClient();
const { subscribe, unsubscribe } = useWebSocket();
const { showNotification } = useNotifications();
const { navigate } = usePlatformNavigation();
```

### 12.3 Component Standards
- Use shared components from `@/components/shared/` whenever possible
- Follow shadcn/ui patterns for consistency
- Implement proper loading and error states
- Support the platform's theme system
- Include comprehensive TypeScript types

### 12.4 Testing Requirements  
- Unit tests for all business logic
- Integration tests for API interactions
- Visual regression tests for UI components
- Accessibility testing compliance

### 12.5 Performance Guidelines
- Implement virtual scrolling for large data sets
- Use React.memo for expensive re-renders
- Debounce user input for search/filter operations
- Cache API responses appropriately

---

## 13. Dependencies

### Backend API Dependencies
- **Industrial.Adam.Security** (Port 5139): Authentication and authorization
- **Industrial.Adam.Logger.WebApi** (Port 5139): Device management and counter data
- **Industrial.Adam.OEE.WebApi** (Port 5140): OEE calculations and work orders
- **Industrial.Adam.EquipmentScheduling.WebApi** (Port 5141): Resource scheduling

### External Dependencies
- Node.js 18+ for development environment
- Docker for containerized deployment
- Modern web browser with ES2020 support
- Network access to backend services

### Development Dependencies
- TypeScript 5.0+
- React 18+
- Vite for build tooling
- Tailwind CSS 3.0+
- Testing framework (Vitest recommended)

---

## 14. Out of Scope

The following items are explicitly excluded from this platform infrastructure PRD:

### Module-Specific Features
- Logger module device configuration UI
- OEE calculation display logic
- Equipment scheduling calendar interfaces
- Security audit log visualization

### Backend Modifications
- New API endpoints or modifications to existing ones
- Changes to authentication mechanisms
- Database schema modifications
- New backend services

### Advanced Features (Future Consideration)
- Multi-language internationalization
- Advanced theming beyond light/dark mode
- Mobile application development
- Advanced analytics and reporting platform
- Third-party system integrations

---

## 15. Open Questions

1. **Theme Customization**: Should the platform support factory-specific branding and color schemes?

2. **Module Configuration**: How should module-specific configuration be stored and managed at the platform level?

3. **Offline Capability**: What level of offline functionality should be supported for each module type?

4. **Error Reporting**: Should the platform include automatic error reporting to administrators?

5. **Module Dependencies**: How should inter-module dependencies be handled (e.g., OEE depending on Logger data)?

6. **Performance Monitoring**: Should the platform include built-in performance monitoring and alerting?

7. **Accessibility Compliance**: What specific accessibility standards must be met for industrial environments?

---

## 16. Implementation Phases

### Phase 1: Core Platform (Weeks 1-2)
- Authentication infrastructure
- Basic navigation shell
- Module registry system
- Shared component foundation

### Phase 2: Module Integration (Weeks 3-4)  
- Logger module integration
- OEE module integration
- Real-time WebSocket infrastructure
- Error handling and boundaries

### Phase 3: Enhancement (Weeks 5-6)
- Equipment Scheduling module integration
- Performance optimization
- Advanced shared components
- Testing and quality assurance

### Phase 4: Production Readiness (Weeks 7-8)
- Security hardening
- Deployment automation
- Documentation completion
- User acceptance testing

---

*This PRD establishes the foundation for Industrial ADAM platform development. Module-specific functionality should be addressed in separate PRDs that build upon this platform infrastructure.*