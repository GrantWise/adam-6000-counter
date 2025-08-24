/**
 * Core types for the Industrial ADAM Platform
 */

// User and Authentication Types
export interface User {
  id: string
  username: string
  email: string
  fullName: string
  role: UserRole
  permissions: Permission[]
  hierarchyAssignments: HierarchyAssignment[]
  status: 'active' | 'inactive' | 'locked'
  lastLogin?: Date
  createdAt: Date
  updatedAt: Date
}

export type UserRole = 'Operator' | 'Supervisor' | 'Admin' | 'SystemAdmin'

export type Permission = 
  | 'VIEW_DEVICES'
  | 'MANAGE_DEVICES'
  | 'VIEW_OEE'
  | 'MANAGE_OEE'
  | 'VIEW_SCHEDULING'
  | 'MANAGE_SCHEDULING'
  | 'VIEW_USERS'
  | 'MANAGE_USERS'
  | 'VIEW_HIERARCHY'
  | 'MANAGE_HIERARCHY'
  | 'VIEW_SECURITY'
  | 'MANAGE_SECURITY'
  | 'SYSTEM_ADMIN'

export interface LoginCredentials {
  username: string
  password: string
  rememberMe?: boolean
}

export interface AuthToken {
  accessToken: string
  refreshToken: string
  expiresAt: Date
  refreshExpiresAt?: Date
}

export interface LoginResult {
  success: boolean
  user?: User
  token?: AuthToken
  redirectTo?: string
  error?: string
}

// ISA-95 Hierarchy Types
export interface HierarchyNode {
  id: string
  name: string
  type: HierarchyLevel
  parentId?: string
  children: HierarchyNode[]
  properties: Record<string, any>
  assignedUsers: UserSummary[]
  deviceConfigs?: DeviceConfig[]
  path: string[] // Full path from enterprise to this node
  level: number // 0 = Enterprise, 1 = Site, etc.
}

export type HierarchyLevel = 'Enterprise' | 'Site' | 'Area' | 'Line' | 'Equipment'

export interface HierarchyAssignment {
  userId: string
  nodeId: string
  node: HierarchyNode
  assignedAt: Date
  assignedBy: string
}

export interface HierarchyContext {
  currentNode: HierarchyNode
  userAccess: HierarchyNode[]
  breadcrumbs: HierarchyNode[]
}

// Module System Types
export interface ModuleDefinition {
  id: string
  name: string
  displayName: string
  version: string
  description: string
  
  // Visual representation
  icon: React.ComponentType<{ className?: string }>
  category: ModuleCategory
  priority: number
  
  // Access control
  permissions: Permission[]
  requiredRoles: UserRole[]
  
  // Routing configuration
  routes: ModuleRoute[]
  defaultRoute: string
  
  // Lazy-loaded component
  component: React.LazyExoticComponent<React.ComponentType>
  
  // Platform integration
  platformServices?: PlatformServiceRequirements
  webSocketHubs?: string[]
  
  // Module lifecycle
  onLoad?: () => void
  onUnload?: () => void
  healthCheck?: () => Promise<ModuleHealth>
}

export interface ModuleRoute {
  path: string
  component: React.LazyExoticComponent<React.ComponentType>
  exact?: boolean
  permissions?: Permission[]
  layout?: 'default' | 'fullscreen' | 'minimal'
}

export type ModuleCategory = 'monitoring' | 'configuration' | 'administration' | 'analytics' | 'maintenance'

export interface PlatformServiceRequirements {
  apiClient: boolean
  websocket: boolean
  notifications: boolean
  navigation: boolean
}

export interface ModuleHealth {
  status: 'healthy' | 'warning' | 'error'
  message?: string
  details?: Record<string, any>
}

// API and Data Types
export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  error?: ApiError
  message?: string
}

export interface ApiError {
  code: string
  message: string
  details?: Record<string, any>
  timestamp: Date
  retryable?: boolean
}

export interface PaginatedResponse<T> {
  data: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface SortConfig {
  field: string
  direction: 'asc' | 'desc'
}

export interface FilterConfig {
  field: string
  operator: 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte' | 'contains' | 'in'
  value: any
}

// Device and Logger Types (from existing APIs)
export interface DeviceConfig {
  id: string
  name: string
  ipAddress: string
  port: number
  deviceType: string
  channels: ChannelConfig[]
  status: DeviceStatus
  hierarchyNodeId: string
  createdAt: Date
  updatedAt: Date
}

export interface ChannelConfig {
  id: string
  name: string
  address: number
  dataType: 'Counter' | 'Digital' | 'Analog'
  unit?: string
  scalingFactor?: number
  enabled: boolean
}

export type DeviceStatus = 'online' | 'offline' | 'error' | 'maintenance'

export interface CounterReading {
  deviceId: string
  channelId: string
  timestamp: Date
  value: number
  rate?: number
  quality: DataQuality
}

export type DataQuality = 'good' | 'uncertain' | 'bad' | 'unavailable' | 'simulated'

// 21 CFR Part 11 Compliance Types for Data Quality Tracking
export interface DataWithQuality<T> {
  value: T
  quality: DataQuality
  timestamp: Date
  isRealData: boolean
  source: 'api' | 'simulated' | 'cached' | 'estimated' | 'fallback'
  warning?: string
  auditInfo?: {
    sourceSystem: string
    dataIntegrity: 'verified' | 'unverified' | 'synthetic'
    complianceFlags: string[]
  }
}

export interface QualityAwareApiResponse<T = any> extends ApiResponse<T> {
  dataQuality?: DataQuality
  complianceWarnings?: string[]
  auditTrail?: {
    generated: Date
    sourceSystem: string
    dataIntegrity: 'verified' | 'unverified' | 'synthetic'
  }
}

// OEE Types
export interface OEEMetrics {
  equipmentId: string
  timestamp: Date
  availability: number
  performance: number
  quality: number
  oee: number
  plannedProductionTime: number
  actualProductionTime: number
  idealRunRate: number
  actualRunRate: number
  goodCount: number
  totalCount: number
}

export interface WorkOrder {
  id: string
  equipmentId: string
  productCode: string
  plannedQuantity: number
  actualQuantity: number
  startTime?: Date
  endTime?: Date
  status: 'planned' | 'active' | 'completed' | 'cancelled'
}

export interface StoppageEvent {
  id: string
  equipmentId: string
  workOrderId?: string
  startTime: Date
  endTime?: Date
  category: string
  reason: string
  description?: string
  duration?: number
}

// Equipment Scheduling Types
export interface Resource {
  id: string
  name: string
  type: ResourceType
  hierarchyNodeId: string
  operatingPattern?: OperatingPattern
  availability: ResourceAvailability[]
}

export type ResourceType = 'Equipment' | 'Line' | 'Area'

export interface OperatingPattern {
  id: string
  name: string
  type: '24/7' | 'Two-Shift' | 'One-Shift' | 'Custom'
  schedule: TimeSlot[]
}

export interface TimeSlot {
  dayOfWeek: number // 0 = Sunday
  startTime: string // HH:mm format
  endTime: string // HH:mm format
}

export interface ResourceAvailability {
  resourceId: string
  date: Date
  isAvailable: boolean
  reason?: string
}

// UI Component Types
export interface SelectOption {
  value: string
  label: string
  disabled?: boolean
}

export interface TableColumn<T = any> {
  key: keyof T | string
  title: string
  sortable?: boolean
  render?: (value: any, record: T, index: number) => React.ReactNode
  width?: string | number
  align?: 'left' | 'center' | 'right'
}

export interface TableProps<T = any> {
  data: T[]
  columns: TableColumn<T>[]
  loading?: boolean
  error?: string
  pagination?: {
    current: number
    pageSize: number
    total: number
    onChange: (page: number, pageSize: number) => void
  }
  sorting?: SortConfig
  onSort?: (field: string) => void
  selection?: {
    selectedRowKeys: string[]
    onChange: (selectedRowKeys: string[], selectedRows: T[]) => void
  }
  rowKey?: keyof T | ((record: T) => string)
}

export interface CardProps {
  title?: string
  subtitle?: string
  icon?: React.ComponentType<{ className?: string }>
  actions?: React.ReactNode
  loading?: boolean
  className?: string
  children: React.ReactNode
}

export interface StatusIndicatorProps {
  status: 'healthy' | 'warning' | 'error' | 'offline' | 'unknown'
  label: string
  description?: string
  pulse?: boolean
  size?: 'sm' | 'default' | 'lg'
}

// Notification Types
export interface Notification {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title: string
  message?: string
  duration?: number
  actions?: NotificationAction[]
  timestamp: Date
}

export interface NotificationAction {
  label: string
  action: () => void
  variant?: 'default' | 'destructive'
}

// WebSocket Types
export interface WebSocketMessage<T = any> {
  type: string
  data: T
  timestamp: Date
}

export interface ConnectionStatus {
  connected: boolean
  reconnecting: boolean
  lastConnected?: Date
  error?: string
}

// Utility Types
export interface UserSummary {
  id: string
  username: string
  fullName: string
  role: UserRole
}

export interface LoadingState {
  loading: boolean
  error?: string
}

export interface FormState<T = any> {
  data: T
  errors: Record<string, string>
  touched: Record<string, boolean>
  dirty: boolean
  valid: boolean
}

// Global State Types
export interface PlatformState {
  // Authentication state
  auth: {
    user: User | null
    token: AuthToken | null
    isAuthenticated: boolean
    permissions: Permission[]
    hierarchyContext: HierarchyContext | null
    loading: boolean
  }
  
  // Application state
  app: {
    theme: 'light' | 'dark' | 'system'
    sidebarCollapsed: boolean
    notifications: Notification[]
    moduleRegistry: ModuleDefinition[]
    connectionStatus: Record<string, ConnectionStatus>
    loading: boolean
  }
  
  // User preferences
  preferences: {
    dashboardLayout: 'admin' | 'user'
    moduleOrder: string[]
    defaultView: string
    autoRefreshIntervals: Record<string, number>
    timezone: string
  }
}

export type ComponentStatus = 'idle' | 'loading' | 'success' | 'error'

// CFR Part 11 Data Quality Indicators for UI Display
export interface DataQualityIndicator {
  quality: DataQuality
  icon: 'check' | 'warning' | 'error' | 'info' | 'alert'
  color: 'green' | 'yellow' | 'red' | 'blue' | 'orange'
  label: string
  description: string
  showWarning: boolean
}

// Constants for data quality visualization
export const DATA_QUALITY_INDICATORS: Record<DataQuality, DataQualityIndicator> = {
  good: {
    quality: 'good',
    icon: 'check',
    color: 'green',
    label: 'VERIFIED',
    description: 'Data verified from source system',
    showWarning: false
  },
  uncertain: {
    quality: 'uncertain',
    icon: 'warning',
    color: 'yellow',
    label: 'UNCERTAIN',
    description: 'Data quality uncertain - verify before use',
    showWarning: true
  },
  bad: {
    quality: 'bad',
    icon: 'error',
    color: 'red',
    label: 'BAD DATA',
    description: 'Data quality poor - do not use for decisions',
    showWarning: true
  },
  unavailable: {
    quality: 'unavailable',
    icon: 'alert',
    color: 'red',
    label: 'NO DATA',
    description: 'Source data unavailable',
    showWarning: true
  },
  simulated: {
    quality: 'simulated',
    icon: 'info',
    color: 'blue',
    label: 'SIMULATED',
    description: '⚠️ SYNTHETIC DATA - Not from actual system',
    showWarning: true
  }
}

// Admin Dashboard Types - Phase 1

// System Health Types
export interface ServiceStatus {
  serviceName: string
  status: 'healthy' | 'warning' | 'error' | 'offline'
  uptime: number
  responseTimeMs: number
  errorCount: number
  lastCheck: Date
  endpoint: string
  version?: string
}

export interface SystemMetrics {
  timestamp: Date
  cpuUsage: number
  memoryUsage: number
  diskUsage: number
  networkIn: number
  networkOut: number
  activeConnections: number
  databaseConnections: number
}

export interface SystemAlert {
  id: string
  type: 'error' | 'warning' | 'info'
  severity: 'low' | 'medium' | 'high' | 'critical'
  title: string
  message: string
  source: string
  timestamp: Date
  acknowledged: boolean
  acknowledgedBy?: string
  acknowledgedAt?: Date
  resolved: boolean
  resolvedAt?: Date
}

export interface HealthTimelineEvent {
  id: string
  timestamp: Date
  type: 'service_up' | 'service_down' | 'error' | 'maintenance' | 'deployment'
  serviceName: string
  message: string
  severity: 'low' | 'medium' | 'high' | 'critical'
  duration?: number
}

export interface DatabaseHealth {
  connected: boolean
  responseTimeMs: number
  connectionCount: number
  maxConnections: number
  version: string
  diskUsage: number
  queryPerformance: {
    slowQueries: number
    avgQueryTime: number
  }
}

// Security Audit Types
export interface LoginAttempt {
  id: string
  username: string
  ipAddress: string
  userAgent: string
  success: boolean
  failureReason?: string
  timestamp: Date
  location?: string
  deviceFingerprint?: string
}

export interface AuditLogEntry {
  id: string
  userId: string
  username: string
  action: string
  entityType: string
  entityId: string
  oldValue?: any
  newValue?: any
  ipAddress: string
  userAgent: string
  timestamp: Date
  sessionId: string
  success: boolean
  details?: Record<string, any>
}

export interface UserActivity {
  id: string
  userId: string
  username: string
  action: string
  module: string
  details: string
  ipAddress: string
  timestamp: Date
  duration?: number
  success: boolean
}

export interface SecuritySession {
  id: string
  userId: string
  username: string
  ipAddress: string
  userAgent: string
  startedAt: Date
  lastActivity: Date
  endedAt?: Date
  isActive: boolean
  location?: string
  deviceInfo: {
    browser: string
    os: string
    device: string
  }
}

export interface ComplianceMetrics {
  totalUsers: number
  activeUsers: number
  lockedUsers: number
  passwordExpirations: number
  failedLogins24h: number
  suspiciousActivities: number
  dataIntegrityChecks: number
  auditLogRetention: {
    days: number
    totalEntries: number
    oldestEntry: Date
  }
}

// System Logs Types
export interface LogEntry {
  id: string
  timestamp: Date
  level: 'trace' | 'debug' | 'info' | 'warn' | 'error' | 'fatal'
  serviceName: string
  message: string
  exception?: string
  properties: Record<string, any>
  correlationId?: string
  userId?: string
  ipAddress?: string
}

export interface LogFilter {
  serviceName?: string
  level?: string[]
  startTime?: Date
  endTime?: Date
  searchText?: string
  correlationId?: string
  userId?: string
}

export interface LogSearchResult {
  entries: LogEntry[]
  total: number
  page: number
  pageSize: number
  searchTime: number
  hasMore: boolean
}

export interface LogService {
  name: string
  displayName: string
  logCount: number
  lastLog: Date
  errorCount: number
  enabled: boolean
}

// WebSocket Types for Real-time Updates
export interface HealthUpdate {
  type: 'service_status' | 'metrics' | 'alert' | 'database_status'
  data: ServiceStatus | SystemMetrics | SystemAlert | DatabaseHealth
  timestamp: Date
}

export interface SecurityEvent {
  type: 'login_attempt' | 'audit_entry' | 'user_activity' | 'session_event'
  data: LoginAttempt | AuditLogEntry | UserActivity | SecuritySession
  timestamp: Date
}

export interface LogStreamEvent {
  type: 'new_log' | 'log_batch'
  data: LogEntry | LogEntry[]
  timestamp: Date
}

// Chart Data Types for Recharts
export interface TimeSeriesDataPoint {
  timestamp: Date
  value: number
  label?: string
}

export interface LoginAttemptsChartData {
  timestamp: Date
  successful: number
  failed: number
  total: number
}

export interface MetricsChartData {
  timestamp: Date
  cpu: number
  memory: number
  disk: number
  network: number
}

// Admin Dashboard Component Props
export interface ServiceStatusCardProps {
  service: ServiceStatus
  onClick?: () => void
}

export interface MetricsChartProps {
  metrics: SystemMetrics[]
  height?: number
  timeRange?: '1h' | '6h' | '24h' | '7d'
}

export interface AlertsTableProps {
  alerts: SystemAlert[]
  loading?: boolean
  onAcknowledge: (alertId: string) => void
  onResolve?: (alertId: string) => void
  pagination?: {
    current: number
    pageSize: number
    total: number
    onChange: (page: number, pageSize: number) => void
  }
}

export interface HealthTimelineProps {
  events: HealthTimelineEvent[]
  height?: number
  timeRange?: '1h' | '6h' | '24h' | '7d'
}

export interface LogViewerProps {
  logs: LogEntry[]
  loading?: boolean
  height?: number
  onLoadMore?: () => void
  hasMore?: boolean
  highlightText?: string
}

export interface LogFiltersProps {
  services: LogService[]
  filters: LogFilter
  onFiltersChange: (filters: LogFilter) => void
  onClear: () => void
}