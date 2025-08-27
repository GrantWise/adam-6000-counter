# Industrial ADAM Platform Frontend Design Specification
**Version**: 1.0  
**Date**: August 23, 2025  
**Status**: Implementation Ready

---

## 1. Executive Summary

### Design Vision
A unified, industrial-strength React platform that serves as the foundation for all Industrial ADAM system modules. This design specification transforms the Platform PRD requirements into detailed, implementation-ready frontend specifications using modern React patterns, TypeScript, Tailwind CSS, and shadcn/ui components.

### Design Principles
- **Industrial First**: Optimized for factory environments with high contrast, large touch targets, and clear status indicators
- **Pragmatic Architecture**: Logical component cohesion over arbitrary separation (following CLAUDE.md philosophy)
- **Role-Based Experience**: Two distinct interfaces (Admin/User) with role-appropriate complexity
- **Modular Foundation**: Plugin architecture supporting current and future business modules
- **Performance Optimized**: Lazy loading, efficient state management, and minimal bundle sizes

### Success Metrics
- Initial bundle load < 500KB, module chunks < 200KB each
- Time to interactive < 3 seconds on industrial hardware
- 100% role-based access control accuracy
- 99.5% module loading success rate

---

## 2. Visual Design System

### 2.1 Color Palette

#### Primary Colors (Industrial Optimized)
```typescript
// Tailwind CSS custom color configuration
const colors = {
  // Core brand colors
  primary: {
    50: '#f0f9ff',   // Light blue backgrounds
    100: '#e0f2fe',  // Subtle highlights
    200: '#bae6fd',  // Hover states
    300: '#7dd3fc',  // Active states
    400: '#38bdf8',  // Interactive elements
    500: '#0ea5e9',  // Primary buttons, links
    600: '#0284c7',  // Primary button hover
    700: '#0369a1',  // Dark mode primary
    800: '#075985',  // Header backgrounds
    900: '#0c4a6e',  // Text on light backgrounds
    950: '#082f49'   // Maximum contrast
  },
  
  // Industrial status colors (high contrast)
  status: {
    success: {
      DEFAULT: '#16a34a', // Green - running/healthy
      hover: '#15803d',
      light: '#dcfce7'
    },
    warning: {
      DEFAULT: '#ea580c', // Orange - attention needed
      hover: '#c2410c', 
      light: '#fed7aa'
    },
    error: {
      DEFAULT: '#dc2626', // Red - fault/emergency
      hover: '#b91c1c',
      light: '#fecaca'
    },
    info: {
      DEFAULT: '#2563eb', // Blue - information
      hover: '#1d4ed8',
      light: '#dbeafe'
    },
    neutral: {
      DEFAULT: '#6b7280', // Gray - offline/unknown
      hover: '#4b5563',
      light: '#f3f4f6'
    }
  },
  
  // Industrial grays (factory environment)
  neutral: {
    0: '#ffffff',     // Pure white - cards, inputs
    50: '#f9fafb',    // Background tint
    100: '#f3f4f6',   // Light gray background
    200: '#e5e7eb',   // Borders, dividers
    300: '#d1d5db',   // Disabled states
    400: '#9ca3af',   // Placeholder text
    500: '#6b7280',   // Secondary text
    600: '#4b5563',   // Primary text (light mode)
    700: '#374151',   // Headings (light mode)
    800: '#1f2937',   // Dark headers
    900: '#111827',   // Maximum contrast text
    950: '#030712'    // Dark mode backgrounds
  }
}
```

### 2.2 Typography System

#### Font Configuration
```typescript
// Tailwind CSS font configuration
const fontFamily = {
  sans: ['Inter Variable', 'ui-sans-serif', 'system-ui', 'sans-serif'],
  mono: ['JetBrains Mono', 'ui-monospace', 'monospace']
}

// Industrial typography scale (readable at distance)
const fontSize = {
  xs: ['0.75rem', { lineHeight: '1rem' }],     // 12px - labels, captions
  sm: ['0.875rem', { lineHeight: '1.25rem' }], // 14px - body text
  base: ['1rem', { lineHeight: '1.5rem' }],    // 16px - default body
  lg: ['1.125rem', { lineHeight: '1.75rem' }], // 18px - emphasized text
  xl: ['1.25rem', { lineHeight: '1.75rem' }],  // 20px - section headers
  '2xl': ['1.5rem', { lineHeight: '2rem' }],   // 24px - page headers
  '3xl': ['1.875rem', { lineHeight: '2.25rem' }], // 30px - dashboard titles
  '4xl': ['2.25rem', { lineHeight: '2.5rem' }]  // 36px - main headers
}
```

### 2.3 Spacing and Layout

#### Grid System
```typescript
// Container specifications
const containers = {
  sm: '640px',   // Mobile landscape
  md: '768px',   // Tablet portrait
  lg: '1024px',  // Tablet landscape / small desktop
  xl: '1280px',  // Desktop
  '2xl': '1536px' // Large desktop
}

// Industrial spacing scale (optimized for touch)
const spacing = {
  px: '1px',
  0.5: '0.125rem',  // 2px
  1: '0.25rem',     // 4px
  2: '0.5rem',      // 8px
  3: '0.75rem',     // 12px
  4: '1rem',        // 16px - base unit
  5: '1.25rem',     // 20px
  6: '1.5rem',      // 24px
  8: '2rem',        // 32px - section spacing
  10: '2.5rem',     // 40px
  12: '3rem',       // 48px - large gaps
  16: '4rem',       // 64px - major sections
  20: '5rem',       // 80px - page spacing
  24: '6rem'        // 96px - hero sections
}
```

### 2.4 Component Design Tokens

#### Interactive Elements
```typescript
// Button specifications
const buttonSizes = {
  sm: 'h-8 px-3 text-xs',      // 32px height - compact UI
  default: 'h-10 px-4 py-2',   // 40px height - standard
  lg: 'h-12 px-8 text-base',   // 48px height - primary actions
  icon: 'h-10 w-10',           // Square icon buttons
  'touch': 'h-14 px-6 text-lg' // 56px height - factory floor
}

// Input field specifications
const inputSizes = {
  default: 'h-10 px-3',        // Standard form inputs
  lg: 'h-12 px-4',            // Emphasized inputs
  touch: 'h-14 px-4 text-lg'  // Touch-optimized
}

// Border radius scale
const borderRadius = {
  none: '0',
  sm: '0.125rem',   // 2px - subtle elements
  DEFAULT: '0.25rem', // 4px - standard components
  md: '0.375rem',   // 6px - cards
  lg: '0.5rem',     // 8px - larger components
  xl: '0.75rem',    // 12px - prominent cards
  '2xl': '1rem',    // 16px - major containers
  full: '9999px'    // Fully rounded
}
```

---

## 3. Information Architecture

### 3.1 Application Structure

#### Overall Layout Hierarchy
```
Industrial ADAM Platform
├── Authentication Layer
│   ├── Login Interface
│   ├── Token Management
│   └── Session Handling
├── Admin Dashboard
│   ├── Navigation Shell
│   │   ├── Top Bar (User, System Status, Logout)
│   │   ├── Sidebar (Module Navigation)
│   │   └── Breadcrumbs
│   ├── Module Container
│   │   ├── User Management
│   │   ├── Hierarchy Configuration
│   │   ├── System Configuration
│   │   └── Pluggable Modules
│   └── Status Monitoring
└── User Dashboard
    ├── Simplified Navigation
    │   ├── Module Switcher
    │   ├── User Menu
    │   └── Status Indicator
    ├── Content Area
    │   ├── Role-filtered Modules
    │   ├── Real-time Displays
    │   └── Contextual Actions
    └── Notification Area
```

### 3.2 Navigation Design

#### Admin Dashboard Navigation
```typescript
interface AdminNavigation {
  topBar: {
    logo: ReactNode;
    systemHealth: StatusIndicator;
    userMenu: UserDropdown;
    notifications: NotificationCenter;
  };
  sidebar: {
    sections: [
      {
        title: "System Management";
        items: [
          { name: "Users & Roles"; route: "/admin/users"; icon: Users },
          { name: "Hierarchy"; route: "/admin/hierarchy"; icon: TreeIcon },
          { name: "Security"; route: "/admin/security"; icon: Shield }
        ]
      },
      {
        title: "Modules";
        items: ModuleRegistration[]; // Dynamic from registry
      },
      {
        title: "System";
        items: [
          { name: "Health"; route: "/admin/health"; icon: Activity },
          { name: "Logs"; route: "/admin/logs"; icon: FileText }
        ]
      }
    ]
  };
  breadcrumbs: BreadcrumbTrail;
}
```

#### User Dashboard Navigation
```typescript
interface UserNavigation {
  topBar: {
    moduleDropdown: ModuleSelector;
    hierarchyBreadcrumb: HierarchyPath;
    userMenu: SimpleUserMenu;
  };
  quickActions: {
    moduleCards: ModuleCard[];
    recentItems: RecentAccess[];
    favoriteViews: BookmarkedViews[];
  };
}
```

### 3.3 Module Organization

#### Module Categories
```typescript
enum ModuleCategory {
  MONITORING = 'monitoring',     // Real-time data displays
  CONFIGURATION = 'configuration', // System setup
  ADMINISTRATION = 'administration', // User management
  ANALYTICS = 'analytics',       // Reports and trends
  MAINTENANCE = 'maintenance'    // Service operations
}

interface ModuleMetadata {
  id: string;
  displayName: string;
  description: string;
  category: ModuleCategory;
  icon: LucideIcon;
  permissions: Permission[];
  routes: ModuleRoute[];
  priority: number; // Display order
}
```

---

## 4. User Experience Flows

### 4.1 Authentication Flow

#### Login Process Design
```
1. Login Page Display
   ├── Industrial-styled login form
   ├── Large, touch-friendly inputs
   ├── Clear validation messaging
   └── "Remember Me" option

2. Authentication Request
   ├── JWT token acquisition
   ├── Role and permission loading
   ├── Hierarchy assignment retrieval
   └── Module access determination

3. Dashboard Redirect
   ├── Role-based dashboard selection
   │   ├── SystemAdmin/Admin → Admin Dashboard
   │   └── Operator/Supervisor → User Dashboard
   ├── Module registry initialization
   └── Real-time connection establishment
```

#### Component Specifications

**LoginForm Component**
```typescript
interface LoginFormProps {
  onSubmit: (credentials: LoginCredentials) => Promise<void>;
  isLoading: boolean;
  error?: string;
}

// Visual Design
const LoginForm = styled({
  container: 'min-h-screen bg-neutral-50 flex items-center justify-center',
  card: 'w-full max-w-md space-y-8 bg-white p-8 shadow-xl rounded-xl',
  logo: 'mx-auto h-16 w-auto', // Industrial branding
  title: 'text-3xl font-bold text-neutral-900 text-center',
  form: 'mt-8 space-y-6',
  input: 'h-12 w-full px-4 text-base border-2 border-neutral-300 rounded-md focus:border-primary-500',
  button: 'w-full h-12 bg-primary-600 text-white font-semibold rounded-md hover:bg-primary-700',
  error: 'text-error-DEFAULT text-sm text-center p-3 bg-error-light rounded-md'
});
```

### 4.2 User Management Workflow (Admin)

#### User Creation Flow
```
1. User List View
   ├── Data table with sorting/filtering
   ├── User status indicators
   ├── "Add User" primary action button
   └── Bulk operations toolbar

2. User Creation Modal
   ├── Multi-step form wizard
   │   ├── Step 1: Basic Information
   │   ├── Step 2: Role Assignment
   │   └── Step 3: Hierarchy Assignment
   ├── Validation and preview
   └── Confirmation dialog

3. Success Confirmation
   ├── Toast notification
   ├── User list refresh
   └── Optional: Send credentials email
```

#### Component Specifications

**UserManagementTable Component**
```typescript
interface User {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: UserRole;
  hierarchyAssignments: HierarchyAssignment[];
  status: 'active' | 'inactive' | 'locked';
  lastLogin?: Date;
  createdAt: Date;
}

interface UserManagementTableProps {
  users: User[];
  onEdit: (user: User) => void;
  onDelete: (user: User) => void;
  onToggleStatus: (user: User) => void;
  loading: boolean;
}

// Table Features
const tableFeatures = {
  sorting: ['username', 'email', 'role', 'lastLogin'],
  filtering: ['role', 'status', 'hierarchyLevel'],
  pagination: { defaultPageSize: 25, options: [25, 50, 100] },
  bulkActions: ['deactivate', 'delete', 'exportData'],
  search: { fields: ['username', 'email', 'fullName'] }
};
```

### 4.3 Hierarchy Configuration Workflow

#### Tree Management Interface
```
1. Hierarchy Overview
   ├── Expandable tree view (ISA-95 levels)
   ├── Node information panel
   ├── Drag-and-drop reordering
   └── Context menus for operations

2. Node Creation/Editing
   ├── Inline editing for names
   ├── Property panel for detailed configuration
   ├── Equipment association (for Equipment level)
   └── Validation rules enforcement

3. User Assignment Integration
   ├── Visual user assignment indicators
   ├── Assignment dialog from hierarchy view
   └── Permission inheritance display
```

#### Component Specifications

**HierarchyTree Component**
```typescript
interface HierarchyNode {
  id: string;
  name: string;
  type: 'Enterprise' | 'Site' | 'Area' | 'Line' | 'Equipment';
  parentId?: string;
  children: HierarchyNode[];
  properties: Record<string, any>;
  assignedUsers: UserSummary[];
  deviceConfigs?: DeviceConfig[]; // For Equipment level
}

interface HierarchyTreeProps {
  nodes: HierarchyNode[];
  selectedNode?: HierarchyNode;
  onSelect: (node: HierarchyNode) => void;
  onEdit: (node: HierarchyNode) => void;
  onDelete: (node: HierarchyNode) => void;
  onMove: (nodeId: string, newParentId: string) => void;
  editable: boolean;
}

// Visual Design
const treeStyles = {
  container: 'h-full border rounded-lg bg-white',
  node: 'flex items-center py-2 px-3 hover:bg-neutral-50 cursor-pointer',
  nodeSelected: 'bg-primary-50 border-l-4 border-l-primary-500',
  icon: 'w-4 h-4 mr-2 text-neutral-500',
  name: 'font-medium text-neutral-900',
  userCount: 'ml-auto text-sm text-neutral-500 bg-neutral-100 px-2 py-1 rounded-full'
};
```

### 4.4 Module Navigation Patterns

#### Module Loading States
```typescript
// Progressive loading experience
const ModuleLoadingStates = {
  skeleton: {
    duration: '200ms',
    pattern: 'cards-and-navigation',
    feedback: 'subtle-shimmer'
  },
  loading: {
    spinner: 'module-specific-icon',
    message: 'Loading [Module Name]...',
    timeout: 10000 // 10 second fallback
  },
  error: {
    icon: 'alert-triangle',
    title: 'Module Loading Failed',
    actions: ['retry', 'report-issue', 'continue-without']
  },
  success: {
    transition: 'fade-in-up',
    duration: '300ms'
  }
};
```

---

## 5. Component Architecture

### 5.1 Shared Component Library

#### Core Components Specification

**DataTable Component**
```typescript
interface DataTableProps<T> {
  data: T[];
  columns: ColumnDef<T>[];
  loading?: boolean;
  error?: string;
  pagination?: PaginationConfig;
  sorting?: SortingConfig;
  filtering?: FilterConfig<T>;
  selection?: SelectionConfig<T>;
  actions?: ActionConfig<T>;
}

// Industrial styling
const DataTable = styled({
  table: 'w-full border-collapse bg-white shadow-sm rounded-lg overflow-hidden',
  header: 'bg-neutral-100 text-neutral-900 font-semibold',
  headerCell: 'px-4 py-3 text-left border-b border-neutral-200',
  row: 'hover:bg-neutral-50 transition-colors',
  cell: 'px-4 py-3 border-b border-neutral-100',
  loadingRow: 'animate-pulse bg-neutral-100',
  emptyState: 'text-center py-12 text-neutral-500'
});
```

**StatusIndicator Component**
```typescript
interface StatusIndicatorProps {
  status: 'healthy' | 'warning' | 'error' | 'offline' | 'unknown';
  label: string;
  description?: string;
  pulse?: boolean; // For real-time updates
  size?: 'sm' | 'default' | 'lg';
}

// High-contrast industrial colors
const statusStyles = {
  healthy: 'bg-status-success text-white',
  warning: 'bg-status-warning text-white',
  error: 'bg-status-error text-white',
  offline: 'bg-neutral-500 text-white',
  unknown: 'bg-neutral-400 text-white'
};
```

**MetricCard Component**
```typescript
interface MetricCardProps {
  title: string;
  value: string | number;
  unit?: string;
  trend?: {
    direction: 'up' | 'down' | 'stable';
    percentage: number;
    period: string;
  };
  status?: ComponentStatus;
  icon?: LucideIcon;
  loading?: boolean;
}

// Industrial dashboard styling
const MetricCard = styled({
  container: 'bg-white p-6 rounded-lg shadow-sm border',
  header: 'flex items-center justify-between mb-4',
  title: 'text-sm font-medium text-neutral-600',
  value: 'text-3xl font-bold text-neutral-900',
  unit: 'text-lg text-neutral-500 ml-2',
  trend: 'flex items-center mt-2 text-sm',
  trendUp: 'text-status-success',
  trendDown: 'text-status-error',
  trendStable: 'text-neutral-500'
});
```

### 5.2 Module Registration System

#### Module Interface Contract
```typescript
interface ModuleDefinition {
  // Module identification
  id: string;
  name: string;
  displayName: string;
  version: string;
  description: string;
  
  // Visual representation
  icon: LucideIcon;
  category: ModuleCategory;
  priority: number;
  
  // Access control
  permissions: Permission[];
  requiredRoles: UserRole[];
  
  // Routing configuration
  routes: ModuleRoute[];
  defaultRoute: string;
  
  // Lazy-loaded component
  component: React.LazyExoticComponent<React.ComponentType>;
  
  // Platform integration
  platformServices?: PlatformServiceRequirements;
  webSocketHubs?: string[];
  
  // Module lifecycle
  onLoad?: () => void;
  onUnload?: () => void;
  healthCheck?: () => Promise<ModuleHealth>;
}

interface ModuleRoute {
  path: string;
  component: React.LazyExoticComponent<React.ComponentType>;
  exact?: boolean;
  permissions?: Permission[];
  layout?: 'default' | 'fullscreen' | 'minimal';
}
```

#### Module Registry Implementation
```typescript
class ModuleRegistry {
  private modules = new Map<string, ModuleDefinition>();
  private loadedModules = new Set<string>();
  
  register(module: ModuleDefinition): void {
    this.validateModule(module);
    this.modules.set(module.id, module);
  }
  
  getAccessibleModules(user: User): ModuleDefinition[] {
    return Array.from(this.modules.values())
      .filter(module => this.hasAccess(user, module))
      .sort((a, b) => a.priority - b.priority);
  }
  
  async loadModule(moduleId: string): Promise<React.ComponentType> {
    const module = this.modules.get(moduleId);
    if (!module) throw new Error(`Module ${moduleId} not found`);
    
    if (!this.loadedModules.has(moduleId)) {
      module.onLoad?.();
      this.loadedModules.add(moduleId);
    }
    
    return module.component;
  }
  
  private hasAccess(user: User, module: ModuleDefinition): boolean {
    return module.requiredRoles.includes(user.role) &&
           module.permissions.every(perm => user.permissions.includes(perm));
  }
}
```

### 5.3 Platform Services Architecture

#### Service Provider Pattern
```typescript
interface PlatformServices {
  auth: AuthenticationService;
  api: ApiClient;
  websocket: WebSocketManager;
  notifications: NotificationService;
  navigation: NavigationService;
  theme: ThemeService;
}

// React Context for dependency injection
const PlatformContext = createContext<PlatformServices | null>(null);

// Custom hooks for service access
export const useAuth = () => {
  const services = useContext(PlatformContext);
  if (!services) throw new Error('useAuth must be used within PlatformProvider');
  return services.auth;
};

export const useApiClient = () => {
  const services = useContext(PlatformContext);
  if (!services) throw new Error('useApiClient must be used within PlatformProvider');
  return services.api;
};
```

#### Authentication Service Design
```typescript
interface AuthenticationService {
  // State
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  permissions: Permission[];
  
  // Methods
  login(credentials: LoginCredentials): Promise<LoginResult>;
  logout(): Promise<void>;
  refreshToken(): Promise<void>;
  checkPermission(permission: Permission): boolean;
  checkRole(role: UserRole): boolean;
  
  // Hierarchy context
  getCurrentHierarchy(): HierarchyContext;
  switchHierarchyContext(nodeId: string): Promise<void>;
}

interface LoginResult {
  success: boolean;
  user?: User;
  redirectTo?: string;
  error?: string;
}
```

---

## 6. Technical Implementation Patterns

### 6.1 State Management Architecture

#### Global State Structure
```typescript
// Using Zustand for global state
interface PlatformState {
  // Authentication state
  auth: {
    user: User | null;
    token: string | null;
    refreshToken: string | null;
    isAuthenticated: boolean;
    permissions: Permission[];
    hierarchyContext: HierarchyContext;
  };
  
  // Application state
  app: {
    theme: 'light' | 'dark' | 'system';
    sidebarCollapsed: boolean;
    notifications: Notification[];
    moduleRegistry: ModuleRegistry;
    connectionStatus: ConnectionStatus;
  };
  
  // User preferences
  preferences: {
    dashboardLayout: DashboardLayout;
    moduleOrder: string[];
    defaultView: string;
    autoRefreshIntervals: Record<string, number>;
  };
}

// State management with Zustand
const usePlatformStore = create<PlatformState>((set, get) => ({
  auth: initialAuthState,
  app: initialAppState,
  preferences: initialPreferencesState,
  
  // Actions
  setUser: (user: User) => set(state => ({
    auth: { ...state.auth, user, isAuthenticated: true }
  })),
  
  clearAuth: () => set(state => ({
    auth: { ...initialAuthState }
  })),
  
  updatePreferences: (preferences: Partial<UserPreferences>) =>
    set(state => ({
      preferences: { ...state.preferences, ...preferences }
    }))
}));
```

### 6.2 API Integration Patterns

#### Centralized API Client
```typescript
class ApiClient {
  private axios: AxiosInstance;
  private authStore: AuthStore;
  
  constructor(baseURL: string, authStore: AuthStore) {
    this.authStore = authStore;
    this.axios = axios.create({ baseURL });
    this.setupInterceptors();
  }
  
  private setupInterceptors() {
    // Request interceptor for auth headers
    this.axios.interceptors.request.use(config => {
      const token = this.authStore.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
    
    // Response interceptor for error handling
    this.axios.interceptors.response.use(
      response => response,
      async error => {
        if (error.response?.status === 401) {
          await this.authStore.refreshToken();
          return this.axios.request(error.config);
        }
        return Promise.reject(this.transformError(error));
      }
    );
  }
  
  // Module-specific clients
  get devices() {
    return new DevicesApiClient(this.axios);
  }
  
  get oee() {
    return new OeeApiClient(this.axios);
  }
  
  get scheduling() {
    return new SchedulingApiClient(this.axios);
  }
}
```

#### React Query Integration
```typescript
// Custom hooks for data fetching
export const useDevices = () => {
  const { api } = usePlatformServices();
  
  return useQuery({
    queryKey: ['devices'],
    queryFn: () => api.devices.getAll(),
    staleTime: 30000, // 30 seconds
    refetchInterval: 60000, // 1 minute
    retry: 3
  });
};

export const useCreateDevice = () => {
  const { api } = usePlatformServices();
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (device: CreateDeviceRequest) => api.devices.create(device),
    onSuccess: () => {
      queryClient.invalidateQueries(['devices']);
      showNotification('Device created successfully', 'success');
    },
    onError: (error) => {
      showNotification(`Failed to create device: ${error.message}`, 'error');
    }
  });
};
```

### 6.3 Real-Time Data Patterns

#### WebSocket Management
```typescript
class WebSocketManager {
  private connections = new Map<string, HubConnection>();
  private subscriptions = new Map<string, Set<WebSocketSubscription>>();
  
  async connect(hubName: string, url: string): Promise<void> {
    if (this.connections.has(hubName)) return;
    
    const connection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();
    
    await connection.start();
    this.connections.set(hubName, connection);
    
    // Setup global error handling
    connection.onreconnecting(() => {
      this.notifyConnectionStatus(hubName, 'reconnecting');
    });
    
    connection.onreconnected(() => {
      this.notifyConnectionStatus(hubName, 'connected');
    });
    
    connection.onclose(() => {
      this.notifyConnectionStatus(hubName, 'disconnected');
    });
  }
  
  subscribe<T>(
    hubName: string, 
    method: string, 
    callback: (data: T) => void
  ): () => void {
    const connection = this.connections.get(hubName);
    if (!connection) throw new Error(`Hub ${hubName} not connected`);
    
    connection.on(method, callback);
    
    // Return unsubscribe function
    return () => connection.off(method, callback);
  }
}

// React hook for WebSocket subscriptions
export const useWebSocketData = <T>(
  hubName: string,
  method: string,
  initialData?: T
) => {
  const [data, setData] = useState<T | undefined>(initialData);
  const { websocket } = usePlatformServices();
  
  useEffect(() => {
    const unsubscribe = websocket.subscribe<T>(hubName, method, setData);
    return unsubscribe;
  }, [hubName, method, websocket]);
  
  return data;
};
```

### 6.4 Error Handling Patterns

#### Error Boundary Implementation
```typescript
interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
  errorInfo?: ErrorInfo;
}

class ModuleErrorBoundary extends Component<
  PropsWithChildren<{ moduleName: string }>,
  ErrorBoundaryState
> {
  constructor(props: PropsWithChildren<{ moduleName: string }>) {
    super(props);
    this.state = { hasError: false };
  }
  
  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }
  
  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.setState({ errorInfo });
    
    // Log error to monitoring service
    this.logError(error, errorInfo);
  }
  
  private logError(error: Error, errorInfo: ErrorInfo) {
    console.error(`Module ${this.props.moduleName} error:`, error);
    // TODO: Send to error reporting service
  }
  
  render() {
    if (this.state.hasError) {
      return (
        <div className="p-8 text-center">
          <AlertTriangle className="w-16 h-16 text-status-error mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-neutral-900 mb-2">
            Module Error
          </h2>
          <p className="text-neutral-600 mb-6">
            The {this.props.moduleName} module encountered an error and couldn't load.
          </p>
          <Button onClick={() => this.setState({ hasError: false })}>
            Try Again
          </Button>
        </div>
      );
    }
    
    return this.props.children;
  }
}
```

#### Global Error Handler
```typescript
interface PlatformError {
  code: string;
  message: string;
  details?: any;
  timestamp: Date;
  context: ErrorContext;
}

class ErrorHandler {
  private static instance: ErrorHandler;
  
  handleApiError(error: AxiosError): PlatformError {
    const platformError: PlatformError = {
      code: error.response?.data?.code || 'API_ERROR',
      message: this.getUserFriendlyMessage(error),
      details: error.response?.data,
      timestamp: new Date(),
      context: { type: 'api', url: error.config?.url }
    };
    
    this.logError(platformError);
    this.showUserNotification(platformError);
    
    return platformError;
  }
  
  private getUserFriendlyMessage(error: AxiosError): string {
    if (error.response?.status === 404) {
      return 'The requested resource was not found.';
    }
    if (error.response?.status === 403) {
      return 'You do not have permission to perform this action.';
    }
    if (error.response?.status >= 500) {
      return 'A server error occurred. Please try again later.';
    }
    return error.message || 'An unexpected error occurred.';
  }
}
```

---

## 7. Responsive Design Strategy

### 7.1 Breakpoint Strategy

#### Industrial-Optimized Breakpoints
```typescript
const breakpoints = {
  // Mobile devices (phones) - minimum viable
  mobile: '320px',
  
  // Tablet portrait - primary factory interface
  tablet: '768px',
  
  // Tablet landscape - standard workstation
  desktop: '1024px',
  
  // Large desktop - control room displays
  wide: '1440px',
  
  // Ultra-wide displays - dashboard walls
  ultrawide: '1920px'
};
```

### 7.2 Layout Adaptations

#### Admin Dashboard Responsive Behavior
```typescript
const AdminLayoutBreakpoints = {
  mobile: {
    navigation: 'bottom-tabs',
    sidebar: 'overlay-drawer',
    content: 'single-column',
    actions: 'floating-action-button'
  },
  
  tablet: {
    navigation: 'collapsible-sidebar',
    sidebar: 'auto-collapse',
    content: 'adaptive-grid',
    actions: 'header-toolbar'
  },
  
  desktop: {
    navigation: 'persistent-sidebar',
    sidebar: 'expanded',
    content: 'multi-column-grid',
    actions: 'context-aware'
  },
  
  wide: {
    navigation: 'persistent-sidebar',
    sidebar: 'expanded-with-details',
    content: 'dashboard-optimized',
    actions: 'inline-and-toolbar'
  }
};
```

#### User Dashboard Responsive Behavior
```typescript
const UserLayoutBreakpoints = {
  mobile: {
    layout: 'single-card-stack',
    navigation: 'tab-based',
    interactions: 'touch-optimized',
    textSize: 'large'
  },
  
  tablet: {
    layout: 'two-column-grid',
    navigation: 'drawer-and-tabs',
    interactions: 'touch-first',
    textSize: 'medium-large'
  },
  
  desktop: {
    layout: 'three-column-dashboard',
    navigation: 'sidebar-and-tabs',
    interactions: 'mouse-and-touch',
    textSize: 'standard'
  }
};
```

### 7.3 Component Responsive Patterns

#### Responsive Data Tables
```typescript
interface ResponsiveTableProps<T> {
  data: T[];
  columns: ResponsiveColumn<T>[];
  mobileConfig: MobileTableConfig;
}

interface ResponsiveColumn<T> {
  key: keyof T;
  header: string;
  priority: 'essential' | 'important' | 'optional'; // Hide order on small screens
  mobileDisplay?: 'card' | 'detail' | 'hidden';
  render?: (value: T[keyof T], item: T) => ReactNode;
}

const ResponsiveTable = <T,>({ data, columns, mobileConfig }: ResponsiveTableProps<T>) => {
  const isMobile = useMediaQuery('(max-width: 768px)');
  
  if (isMobile && mobileConfig.displayMode === 'cards') {
    return <MobileCardView data={data} columns={columns} />;
  }
  
  const visibleColumns = isMobile 
    ? columns.filter(col => col.priority === 'essential')
    : columns;
    
  return <StandardTable columns={visibleColumns} data={data} />;
};
```

---

## 8. Performance Optimization Strategy

### 8.1 Bundle Optimization

#### Code Splitting Strategy
```typescript
// Route-based code splitting
const ModuleRoutes = lazy(() => import('@/modules/logger/routes'));
const OeeRoutes = lazy(() => import('@/modules/oee/routes'));
const SchedulingRoutes = lazy(() => import('@/modules/scheduling/routes'));

// Component-based code splitting for large modules
const OeeDashboard = lazy(() => import('@/modules/oee/dashboard/OeeDashboard'));
const SchedulingCalendar = lazy(() => import('@/modules/scheduling/calendar/SchedulingCalendar'));

// Preloading strategy
const ModulePreloader = {
  preloadModules: (userPermissions: Permission[]) => {
    // Preload likely-to-be-used modules based on role
    if (userPermissions.includes('VIEW_OEE')) {
      import('@/modules/oee/routes');
    }
    if (userPermissions.includes('MANAGE_DEVICES')) {
      import('@/modules/logger/routes');
    }
  }
};
```

#### Asset Optimization
```typescript
// Image optimization configuration
const imageConfig = {
  formats: ['webp', 'png', 'jpg'],
  sizes: [16, 32, 48, 96, 128], // Icon sizes
  quality: 85,
  domains: ['localhost', 'adam-platform.local']
};

// Font optimization
const fontConfig = {
  preload: [
    '/fonts/inter-variable.woff2',
    '/fonts/jetbrains-mono-variable.woff2'
  ],
  fallbacks: {
    sans: ['system-ui', '-apple-system', 'sans-serif'],
    mono: ['ui-monospace', 'monospace']
  }
};
```

### 8.2 Rendering Performance

#### Virtual Scrolling Implementation
```typescript
interface VirtualizedListProps<T> {
  items: T[];
  renderItem: (item: T, index: number) => ReactNode;
  itemHeight: number;
  containerHeight: number;
  overscan?: number;
}

const VirtualizedList = <T,>({ 
  items, 
  renderItem, 
  itemHeight, 
  containerHeight,
  overscan = 5 
}: VirtualizedListProps<T>) => {
  const [scrollTop, setScrollTop] = useState(0);
  
  const startIndex = Math.floor(scrollTop / itemHeight);
  const endIndex = Math.min(
    startIndex + Math.ceil(containerHeight / itemHeight) + overscan,
    items.length - 1
  );
  
  const visibleItems = items.slice(startIndex, endIndex + 1);
  const offsetY = startIndex * itemHeight;
  
  return (
    <div 
      className="overflow-auto"
      style={{ height: containerHeight }}
      onScroll={e => setScrollTop(e.currentTarget.scrollTop)}
    >
      <div style={{ height: items.length * itemHeight }}>
        <div style={{ transform: `translateY(${offsetY}px)` }}>
          {visibleItems.map((item, index) => 
            renderItem(item, startIndex + index)
          )}
        </div>
      </div>
    </div>
  );
};
```

#### Memoization Patterns
```typescript
// Component memoization for expensive renders
const MetricCard = memo(({ title, value, trend, status }: MetricCardProps) => {
  return (
    <Card className="p-6">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-neutral-600">{title}</h3>
        <StatusIndicator status={status} size="sm" />
      </div>
      <div className="mt-2 flex items-baseline">
        <p className="text-2xl font-semibold text-neutral-900">{value}</p>
        {trend && <TrendIndicator trend={trend} />}
      </div>
    </Card>
  );
});

// Selector memoization for complex computations
const selectFilteredDevices = createSelector(
  [(state: AppState) => state.devices, (state: AppState) => state.filters],
  (devices, filters) => {
    return devices.filter(device => {
      return filters.status === 'all' || device.status === filters.status;
    });
  }
);
```

---

## 9. Accessibility and Usability

### 9.1 WCAG Compliance Strategy

#### Accessibility Requirements
```typescript
interface AccessibilityConfig {
  // WCAG 2.1 AA compliance targets
  colorContrast: {
    normal: 4.5, // 4.5:1 ratio minimum
    large: 3.0,  // 3:1 for large text
    graphics: 3.0 // 3:1 for UI graphics
  };
  
  // Keyboard navigation
  keyboardNavigation: {
    skipLinks: true,
    tabOrder: 'logical',
    focusVisible: 'always',
    customShortcuts: true
  };
  
  // Screen reader support
  screenReader: {
    landmarks: 'semantic-html',
    headingStructure: 'hierarchical',
    liveRegions: 'aria-live',
    descriptions: 'aria-describedby'
  };
  
  // Motor accessibility
  touchTargets: {
    minimum: 44, // 44px minimum touch target
    recommended: 56, // 56px for industrial use
    spacing: 8 // 8px minimum between targets
  };
}
```

#### Implementation Patterns
```typescript
// Accessible form components
const AccessibleFormField = ({ 
  label, 
  error, 
  required, 
  description,
  children 
}: AccessibleFormFieldProps) => {
  const id = useId();
  const descriptionId = `${id}-description`;
  const errorId = `${id}-error`;
  
  return (
    <div className="space-y-2">
      <label 
        htmlFor={id}
        className="block text-sm font-medium text-neutral-900"
      >
        {label}
        {required && <span className="text-status-error ml-1">*</span>}
      </label>
      
      {description && (
        <p id={descriptionId} className="text-sm text-neutral-600">
          {description}
        </p>
      )}
      
      {cloneElement(children as ReactElement, {
        id,
        'aria-describedby': [
          description && descriptionId,
          error && errorId
        ].filter(Boolean).join(' '),
        'aria-invalid': error ? 'true' : 'false',
        required
      })}
      
      {error && (
        <p id={errorId} className="text-sm text-status-error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
};
```

### 9.2 Industrial Usability Features

#### High Contrast Mode
```typescript
const industrialTheme = {
  // High contrast color palette
  highContrast: {
    background: '#ffffff',
    surface: '#f8fafc',
    primary: '#1e40af',
    success: '#16a34a',
    warning: '#ea580c',
    error: '#dc2626',
    text: '#1f2937',
    border: '#374151'
  },
  
  // Touch-friendly sizing
  touchOptimized: {
    buttonHeight: '56px',
    inputHeight: '52px',
    iconSize: '24px',
    fontSize: '18px',
    lineHeight: '1.6'
  }
};
```

#### Keyboard Shortcuts
```typescript
const KeyboardShortcuts = {
  global: {
    'ctrl+/': 'Show help',
    'ctrl+k': 'Command palette',
    'ctrl+shift+m': 'Switch modules',
    'esc': 'Close modal/drawer'
  },
  
  navigation: {
    'h': 'Go to home dashboard',
    'u': 'User management (admin)',
    's': 'System settings (admin)',
    '1-9': 'Switch to module by number'
  },
  
  dataEntry: {
    'ctrl+s': 'Save form',
    'ctrl+enter': 'Submit form',
    'escape': 'Cancel/close'
  }
};

const useKeyboardShortcuts = (shortcuts: Record<string, () => void>) => {
  useEffect(() => {
    const handleKeydown = (event: KeyboardEvent) => {
      const key = [
        event.ctrlKey && 'ctrl',
        event.shiftKey && 'shift',
        event.altKey && 'alt',
        event.key.toLowerCase()
      ].filter(Boolean).join('+');
      
      const handler = shortcuts[key];
      if (handler) {
        event.preventDefault();
        handler();
      }
    };
    
    window.addEventListener('keydown', handleKeydown);
    return () => window.removeEventListener('keydown', handleKeydown);
  }, [shortcuts]);
};
```

---

## 10. Testing Strategy

### 10.1 Component Testing

#### Unit Testing Patterns
```typescript
// Component testing with React Testing Library
describe('MetricCard', () => {
  it('displays metric value with correct formatting', () => {
    const props = {
      title: 'Overall Equipment Effectiveness',
      value: 85.7,
      unit: '%',
      status: 'healthy' as const,
      trend: {
        direction: 'up' as const,
        percentage: 2.3,
        period: '24h'
      }
    };
    
    render(<MetricCard {...props} />);
    
    expect(screen.getByText('Overall Equipment Effectiveness')).toBeInTheDocument();
    expect(screen.getByText('85.7')).toBeInTheDocument();
    expect(screen.getByText('%')).toBeInTheDocument();
    expect(screen.getByText('+2.3% vs 24h')).toBeInTheDocument();
  });
  
  it('handles loading state correctly', () => {
    render(<MetricCard title="Test" value="--" loading />);
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });
});
```

#### Integration Testing
```typescript
// Module integration tests
describe('Device Management Integration', () => {
  it('allows admin to create and configure device', async () => {
    const user = mockUser({ role: 'Admin' });
    
    renderWithProviders(<DeviceManagement />, { user });
    
    // Navigate to create device
    await user.click(screen.getByRole('button', { name: /add device/i }));
    
    // Fill out device form
    await user.type(screen.getByLabelText(/device name/i), 'Test Device');
    await user.type(screen.getByLabelText(/ip address/i), '192.168.1.100');
    await user.selectOptions(screen.getByLabelText(/device type/i), 'ADAM-6051');
    
    // Submit form
    await user.click(screen.getByRole('button', { name: /create device/i }));
    
    // Verify device appears in list
    await waitFor(() => {
      expect(screen.getByText('Test Device')).toBeInTheDocument();
    });
    
    // Verify API was called correctly
    expect(mockApiClient.devices.create).toHaveBeenCalledWith({
      name: 'Test Device',
      ipAddress: '192.168.1.100',
      deviceType: 'ADAM-6051'
    });
  });
});
```

### 10.2 Visual Regression Testing

#### Screenshot Testing Setup
```typescript
// Visual regression tests with Playwright
test.describe('Platform Visual Tests', () => {
  test('admin dashboard layout', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Take full page screenshot
    await expect(page).toHaveScreenshot('admin-dashboard.png', {
      fullPage: true,
      threshold: 0.2
    });
  });
  
  test('user dashboard responsive design', async ({ page }) => {
    // Test different viewport sizes
    const viewports = [
      { width: 375, height: 667 },  // Mobile
      { width: 768, height: 1024 }, // Tablet
      { width: 1440, height: 900 }  // Desktop
    ];
    
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.goto('/dashboard');
      await page.waitForLoadState('networkidle');
      
      await expect(page).toHaveScreenshot(
        `user-dashboard-${viewport.width}x${viewport.height}.png`
      );
    }
  });
});
```

---

## 11. Build and Deployment Architecture

### 11.1 Build Configuration

#### Vite Configuration
```typescript
// vite.config.ts
export default defineConfig({
  plugins: [react(), tsconfigPaths()],
  
  build: {
    // Chunk splitting for optimal caching
    rollupOptions: {
      output: {
        manualChunks: {
          // Vendor chunks
          vendor: ['react', 'react-dom'],
          ui: ['@radix-ui/react-toast', '@radix-ui/react-dialog'],
          charts: ['recharts'],
          
          // Module chunks
          'module-logger': ['@/modules/logger'],
          'module-oee': ['@/modules/oee'],
          'module-scheduling': ['@/modules/scheduling']
        }
      }
    },
    
    // Performance budgets
    chunkSizeWarningLimit: 200000, // 200KB per chunk
    
    // Asset optimization
    assetsInlineLimit: 4096, // Inline assets < 4KB
    
    // Source maps for debugging
    sourcemap: process.env.NODE_ENV === 'development'
  },
  
  // Development server configuration
  server: {
    port: 3000,
    host: true, // Allow external connections
    proxy: {
      '/api/auth': 'http://localhost:5139',
      '/api/devices': 'http://localhost:5139',
      '/api/oee': 'http://localhost:5140',
      '/api/scheduling': 'http://localhost:5141'
    }
  }
});
```

#### Environment Configuration
```typescript
// Environment configuration
interface EnvironmentConfig {
  NODE_ENV: 'development' | 'production' | 'test';
  
  // API endpoints
  API_BASE_URL: string;
  API_TIMEOUT: number;
  
  // WebSocket endpoints
  WEBSOCKET_HEALTH_HUB: string;
  WEBSOCKET_STOPPAGE_HUB: string;
  
  // Feature flags
  FEATURES: {
    ENABLE_ANALYTICS: boolean;
    ENABLE_DEBUGGING: boolean;
    ENABLE_PERFORMANCE_MONITORING: boolean;
  };
  
  // Build configuration
  BUILD: {
    BUNDLE_ANALYZER: boolean;
    GENERATE_SOURCEMAPS: boolean;
    OPTIMIZE_IMAGES: boolean;
  };
}

// Configuration validation
const validateConfig = (config: EnvironmentConfig): void => {
  const required = ['API_BASE_URL', 'WEBSOCKET_HEALTH_HUB'];
  
  for (const key of required) {
    if (!config[key as keyof EnvironmentConfig]) {
      throw new Error(`Missing required environment variable: ${key}`);
    }
  }
};
```

### 11.2 Docker Deployment

#### Multi-stage Dockerfile
```dockerfile
# Build stage
FROM node:18-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./
COPY pnpm-lock.yaml ./

# Install dependencies
RUN npm install -g pnpm
RUN pnpm install --frozen-lockfile

# Copy source code
COPY . .

# Build application
RUN pnpm build

# Production stage
FROM nginx:alpine AS production

# Copy built application
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/ || exit 1

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

#### Nginx Configuration
```nginx
# nginx.conf
events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript 
               application/javascript application/xml+rss 
               application/json application/xml;
    
    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
    
    server {
        listen 80;
        server_name _;
        root /usr/share/nginx/html;
        index index.html;
        
        # SPA routing
        location / {
            try_files $uri $uri/ /index.html;
        }
        
        # API proxy
        location /api/ {
            proxy_pass http://backend:5139/api/;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
        
        # WebSocket proxy
        location /hubs/ {
            proxy_pass http://backend:5139/hubs/;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
        }
        
        # Static asset caching
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
}
```

---

## 12. Design Rationale and Decisions

### 12.1 Architecture Decisions

#### Monolithic vs Microservices Frontend
**Decision**: Monolithic React application with modular architecture  
**Rationale**: 
- Simpler deployment and maintenance for self-hosted industrial environments
- Shared components and services reduce duplication
- Better performance through shared bundles and caching
- Easier testing and debugging
- Module boundaries provide logical separation without operational complexity

#### State Management Choice
**Decision**: Zustand for global state, React Query for server state  
**Rationale**:
- Zustand provides simple, TypeScript-friendly global state
- React Query handles server state caching and synchronization
- Avoiding Redux complexity while maintaining scalability
- Better developer experience and smaller bundle size

#### Styling Approach
**Decision**: Tailwind CSS with shadcn/ui components  
**Rationale**:
- Utility-first approach provides consistency and maintainability
- shadcn/ui provides industrial-appropriate component patterns
- Excellent TypeScript support and documentation
- Small bundle size with purging unused styles
- Customizable design system for industrial needs

### 12.2 User Experience Decisions

#### Dual Dashboard Approach
**Decision**: Separate Admin and User dashboard interfaces  
**Rationale**:
- Admin users need full system visibility and control
- Operators need simplified, role-appropriate interfaces
- Prevents cognitive overload for non-technical users
- Improves security by hiding administrative functions
- Allows specialized optimization for each user type

#### Navigation Patterns
**Decision**: Sidebar navigation for Admin, simplified top navigation for Users  
**Rationale**:
- Admin users need quick access to many system functions
- Operator users benefit from simplified, focused navigation
- Industrial environments require clear visual hierarchy
- Touch-friendly design for factory floor tablets
- Consistent with industrial software conventions

### 12.3 Technical Implementation Decisions

#### Component Architecture
**Decision**: Compound components with composition patterns  
**Rationale**:
- Following CLAUDE.md principle of logical cohesion
- Compound components reduce prop drilling
- Composition provides flexibility without complexity
- Better TypeScript inference and autocomplete
- Easier testing of component behavior

#### Error Handling Strategy
**Decision**: Error boundaries with graceful degradation  
**Rationale**:
- Critical for industrial environments where uptime is essential
- Module failures shouldn't crash entire platform
- Clear error messaging helps with troubleshooting
- Automatic recovery attempts when possible
- Centralized error logging for system monitoring

#### Performance Optimization
**Decision**: Aggressive lazy loading with intelligent preloading  
**Rationale**:
- Industrial hardware may have limited resources
- Users typically access specific modules repeatedly
- Preloading based on role improves perceived performance
- Code splitting reduces initial bundle size
- Virtual scrolling handles large datasets efficiently

---

## 13. Implementation Guidelines

### 13.1 Development Workflow

#### Component Development Standards
```typescript
// Component template structure
const ComponentTemplate = {
  // 1. Interface definitions
  interface: `
    interface ComponentProps {
      // Required props first
      id: string;
      title: string;
      
      // Optional props with defaults
      variant?: 'default' | 'compact';
      disabled?: boolean;
      
      // Event handlers
      onAction?: (data: ActionData) => void;
      
      // Style customization
      className?: string;
      style?: CSSProperties;
    }
  `,
  
  // 2. Component implementation
  implementation: `
    const Component = memo(({ 
      id, 
      title, 
      variant = 'default',
      disabled = false,
      onAction,
      className,
      style 
    }: ComponentProps) => {
      // Hooks first
      const [state, setState] = useState(initialState);
      const { data, loading, error } = useQuery(queryKey, queryFn);
      
      // Event handlers
      const handleAction = useCallback((data: ActionData) => {
        onAction?.(data);
      }, [onAction]);
      
      // Render with early returns for loading/error states
      if (loading) return <ComponentSkeleton />;
      if (error) return <ComponentError error={error} />;
      
      return (
        <div 
          className={cn(baseStyles[variant], className)}
          style={style}
        >
          {/* Component content */}
        </div>
      );
    });
  `
};
```

#### Testing Standards
```typescript
// Test structure template
const TestTemplate = `
  describe('ComponentName', () => {
    // Test utilities
    const renderComponent = (props: Partial<ComponentProps> = {}) => {
      const defaultProps: ComponentProps = {
        // Required props
      };
      
      return render(
        <TestWrapper>
          <ComponentName {...defaultProps} {...props} />
        </TestWrapper>
      );
    };
    
    // Happy path tests
    describe('when rendering normally', () => {
      it('displays correct content', () => {
        renderComponent({ title: 'Test Title' });
        expect(screen.getByText('Test Title')).toBeInTheDocument();
      });
    });
    
    // Edge cases
    describe('when handling errors', () => {
      it('displays error state gracefully', () => {
        renderComponent({ error: 'Test error' });
        expect(screen.getByText('Test error')).toBeInTheDocument();
      });
    });
    
    // Interaction tests
    describe('when user interacts', () => {
      it('calls event handler correctly', async () => {
        const onAction = jest.fn();
        renderComponent({ onAction });
        
        await user.click(screen.getByRole('button'));
        expect(onAction).toHaveBeenCalledWith(expectedData);
      });
    });
  });
`;
```

### 13.2 Module Development Guide

#### Creating New Modules
```typescript
// Module creation checklist
const ModuleCreationSteps = [
  {
    step: 1,
    task: 'Create module directory structure',
    structure: `
      src/modules/[module-name]/
      ├── components/           # Module-specific components
      ├── hooks/               # Module-specific hooks
      ├── services/            # API clients and business logic
      ├── types/               # TypeScript interfaces
      ├── utils/               # Utility functions
      ├── routes.tsx           # Route definitions
      ├── module.config.ts     # Module registration
      └── index.ts             # Public exports
    `
  },
  
  {
    step: 2,
    task: 'Implement module registration',
    code: `
      // module.config.ts
      export const moduleDefinition: ModuleDefinition = {
        id: 'example-module',
        name: 'Example Module',
        displayName: 'Example Management',
        version: '1.0.0',
        description: 'Manages example resources',
        
        icon: ExampleIcon,
        category: ModuleCategory.MONITORING,
        priority: 100,
        
        permissions: ['VIEW_EXAMPLES', 'MANAGE_EXAMPLES'],
        requiredRoles: ['Operator', 'Supervisor', 'Admin'],
        
        routes: [
          {
            path: '/examples',
            component: lazy(() => import('./components/ExampleDashboard')),
            exact: true
          },
          {
            path: '/examples/:id',
            component: lazy(() => import('./components/ExampleDetail'))
          }
        ],
        
        defaultRoute: '/examples',
        component: lazy(() => import('./routes'))
      };
    `
  },
  
  {
    step: 3,
    task: 'Register module with platform',
    code: `
      // In main application
      import { moduleDefinition as exampleModule } from '@/modules/example/module.config';
      
      // Register during app initialization
      moduleRegistry.register(exampleModule);
    `
  }
];
```

### 13.3 Quality Assurance

#### Pre-deployment Checklist
```typescript
const QualityChecklist = [
  {
    category: 'Functionality',
    items: [
      'All user stories implemented and tested',
      'Error states handled gracefully', 
      'Loading states provide clear feedback',
      'API integrations working correctly',
      'WebSocket connections stable',
      'Role-based access working correctly'
    ]
  },
  
  {
    category: 'Performance',
    items: [
      'Bundle size under target limits',
      'Lazy loading working correctly',
      'No memory leaks in long-running sessions',
      'Acceptable performance on target hardware',
      'Virtual scrolling for large datasets',
      'Efficient re-rendering patterns'
    ]
  },
  
  {
    category: 'Accessibility',
    items: [
      'Keyboard navigation working',
      'Screen reader compatibility',
      'Color contrast ratios met',
      'Touch targets sized appropriately',
      'Focus management working',
      'ARIA labels and descriptions present'
    ]
  },
  
  {
    category: 'Security',
    items: [
      'Authentication working correctly',
      'Authorization enforced properly',
      'No sensitive data in client storage',
      'API calls properly authenticated',
      'XSS protection implemented',
      'CSRF protection where needed'
    ]
  }
];
```

---

## 14. Future Considerations

### 14.1 Scalability Planning

#### Module Ecosystem Growth
```typescript
// Extensibility planning
const FutureModules = {
  // Phase 2 modules (planned)
  maintenance: {
    description: 'Equipment maintenance scheduling and tracking',
    dependencies: ['scheduling', 'equipment-hierarchy'],
    estimatedComplexity: 'high'
  },
  
  quality: {
    description: 'Quality metrics and defect tracking', 
    dependencies: ['oee', 'statistical-analysis'],
    estimatedComplexity: 'medium'
  },
  
  // Phase 3 modules (future consideration)
  inventory: {
    description: 'Parts and materials tracking',
    dependencies: ['maintenance', 'procurement'],
    estimatedComplexity: 'high'
  },
  
  energy: {
    description: 'Energy consumption monitoring',
    dependencies: ['device-integration', 'analytics'],
    estimatedComplexity: 'medium'
  }
};
```

#### Performance Scaling Strategies
```typescript
const ScalingStrategies = {
  // Component-level optimizations
  componentLevel: {
    virtualScrolling: 'For datasets > 1000 items',
    memoization: 'For expensive calculations',
    lazyLoading: 'For non-critical UI sections',
    codesplitting: 'For feature boundaries'
  },
  
  // Application-level optimizations  
  applicationLevel: {
    serviceWorkers: 'For offline capability and caching',
    webWorkers: 'For heavy computations',
    streamingData: 'For real-time high-frequency updates',
    clientSideCaching: 'For frequently accessed data'
  },
  
  // Infrastructure scaling
  infrastructureLevel: {
    cdnDeployment: 'For geographic distribution',
    serverSideRendering: 'For improved initial load',
    edgeCaching: 'For API response optimization',
    databaseOptimization: 'For query performance'
  }
};
```

### 14.2 Technology Evolution

#### Framework Migration Strategy
```typescript
const MigrationStrategy = {
  // React version upgrades
  reactUpgrades: {
    strategy: 'incremental',
    approach: 'feature-flag-based',
    rollback: 'immediate-capability',
    testing: 'comprehensive-regression'
  },
  
  // Next.js consideration for SSR
  nextjsMigration: {
    triggers: ['performance-requirements', 'seo-needs'],
    benefits: ['faster-initial-load', 'better-caching'],
    risks: ['complexity-increase', 'deployment-changes'],
    timeline: 'evaluate-in-phase-2'
  },
  
  // State management evolution
  stateManagement: {
    currentApproach: 'zustand-plus-react-query',
    alternativeOptions: ['redux-toolkit', 'jotai', 'valtio'],
    migrationTriggers: ['team-preference', 'feature-requirements'],
    migrationStrategy: 'gradual-module-by-module'
  }
};
```

### 14.3 Integration Opportunities

#### Third-party System Integration
```typescript
const IntegrationOpportunities = {
  // ERP system integration
  erpSystems: {
    sap: 'Manufacturing execution system integration',
    oracle: 'Supply chain visibility',
    microsoft: 'Business intelligence integration'
  },
  
  // Industrial communication protocols
  industrialProtocols: {
    opcua: 'Standard industrial communication',
    mqtt: 'IoT device integration', 
    profinet: 'Industrial Ethernet communication',
    ethercat: 'Real-time industrial communication'
  },
  
  // Analytics and reporting
  analyticsTools: {
    powerbi: 'Executive dashboard integration',
    tableau: 'Advanced visualization',
    grafana: 'Technical monitoring dashboards',
    elastic: 'Log analysis and search'
  }
};
```

---

## 15. Conclusion

This comprehensive design specification provides the blueprint for implementing the Industrial ADAM Platform Frontend Infrastructure. The design balances modern web development best practices with the specific needs of industrial environments, creating a maintainable, scalable, and user-friendly platform.

### Key Success Factors

1. **Industrial-First Design**: Every decision prioritizes the needs of factory environments
2. **Modular Architecture**: Clean separation enables independent development and testing
3. **Performance Optimization**: Efficient loading and rendering for industrial hardware
4. **Accessibility Compliance**: Inclusive design for all users and environments
5. **Developer Experience**: Clear patterns and guidelines for maintainable code

### Implementation Readiness

This specification provides:
- Detailed component specifications with TypeScript interfaces
- Complete styling system using Tailwind CSS and shadcn/ui
- Implementation patterns for common functionality
- Testing strategies and quality assurance guidelines
- Deployment configurations for production environments

The frontend development team can begin implementation immediately using this specification alongside the Platform PRD, confident that all architectural decisions have been thoughtfully considered and documented.

### Next Steps

1. **Environment Setup**: Configure development environment with specified tools
2. **Core Platform**: Implement authentication, navigation, and module registry
3. **Shared Components**: Build the component library following design specifications
4. **Module Integration**: Implement modules following the established patterns
5. **Testing and Quality**: Apply testing strategies throughout development
6. **Deployment**: Use provided Docker and Nginx configurations for production

This design specification serves as the definitive reference for all frontend implementation decisions, ensuring consistency, quality, and maintainability throughout the development process.

---

*Generated with [Claude Code](https://claude.ai/code)*  
*Version: 1.0 | Date: August 23, 2025*