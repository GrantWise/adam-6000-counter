import { create } from 'zustand'
import { devtools, persist } from 'zustand/middleware'
import type { 
  PlatformState, 
  User, 
  AuthToken, 
  Permission, 
  HierarchyContext,
  Notification,
  ModuleDefinition,
  ConnectionStatus
} from '@/types'

interface PlatformActions {
  // Authentication actions
  setUser: (user: User) => void
  setToken: (token: AuthToken) => void
  setAuthenticated: (isAuthenticated: boolean) => void
  setPermissions: (permissions: Permission[]) => void
  setHierarchyContext: (context: HierarchyContext | null) => void
  setAuthLoading: (loading: boolean) => void
  clearAuth: () => void
  
  // Application actions
  setTheme: (theme: 'light' | 'dark' | 'system') => void
  setSidebarCollapsed: (collapsed: boolean) => void
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp'>) => void
  removeNotification: (id: string) => void
  clearNotifications: () => void
  registerModule: (module: ModuleDefinition) => void
  unregisterModule: (moduleId: string) => void
  setConnectionStatus: (service: string, status: ConnectionStatus) => void
  setAppLoading: (loading: boolean) => void
  
  // Preferences actions
  setDashboardLayout: (layout: 'admin' | 'user') => void
  setModuleOrder: (order: string[]) => void
  setDefaultView: (view: string) => void
  setAutoRefreshInterval: (module: string, interval: number) => void
  setTimezone: (timezone: string) => void
}

type PlatformStore = PlatformState & PlatformActions

// Initial state
const initialAuthState = {
  user: null,
  token: null,
  isAuthenticated: false,
  permissions: [] as Permission[],
  hierarchyContext: null,
  loading: false
}

const initialAppState = {
  theme: 'system' as const,
  sidebarCollapsed: false,
  notifications: [] as Notification[],
  moduleRegistry: [] as ModuleDefinition[],
  connectionStatus: {} as Record<string, ConnectionStatus>,
  loading: false
}

const initialPreferencesState = {
  dashboardLayout: 'user' as const,
  moduleOrder: [] as string[],
  defaultView: '/dashboard',
  autoRefreshIntervals: {} as Record<string, number>,
  timezone: Intl.DateTimeFormat().resolvedOptions().timeZone
}

export const usePlatformStore = create<PlatformStore>()(
  devtools(
    persist(
      (set, get) => ({
        // Initial state
        auth: initialAuthState,
        app: initialAppState,
        preferences: initialPreferencesState,

        // Authentication actions
        setUser: (user) =>
          set(
            (state) => ({
              auth: { ...state.auth, user, isAuthenticated: true }
            }),
            false,
            'setUser'
          ),

        setToken: (token) =>
          set(
            (state) => ({
              auth: { ...state.auth, token }
            }),
            false,
            'setToken'
          ),

        setAuthenticated: (isAuthenticated) =>
          set(
            (state) => ({
              auth: { ...state.auth, isAuthenticated }
            }),
            false,
            'setAuthenticated'
          ),

        setPermissions: (permissions) =>
          set(
            (state) => ({
              auth: { ...state.auth, permissions }
            }),
            false,
            'setPermissions'
          ),

        setHierarchyContext: (hierarchyContext) =>
          set(
            (state) => ({
              auth: { ...state.auth, hierarchyContext }
            }),
            false,
            'setHierarchyContext'
          ),

        setAuthLoading: (loading) =>
          set(
            (state) => ({
              auth: { ...state.auth, loading }
            }),
            false,
            'setAuthLoading'
          ),

        clearAuth: () =>
          set(
            (state) => ({
              auth: initialAuthState,
              preferences: { ...state.preferences, dashboardLayout: 'user' }
            }),
            false,
            'clearAuth'
          ),

        // Application actions
        setTheme: (theme) =>
          set(
            (state) => ({
              app: { ...state.app, theme }
            }),
            false,
            'setTheme'
          ),

        setSidebarCollapsed: (sidebarCollapsed) =>
          set(
            (state) => ({
              app: { ...state.app, sidebarCollapsed }
            }),
            false,
            'setSidebarCollapsed'
          ),

        addNotification: (notification) => {
          const id = `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
          const newNotification: Notification = {
            ...notification,
            id,
            timestamp: new Date()
          }
          
          set(
            (state) => ({
              app: {
                ...state.app,
                notifications: [...(Array.isArray(state.app.notifications) ? state.app.notifications : []), newNotification]
              }
            }),
            false,
            'addNotification'
          )
          
          // Auto-remove notification after duration (default 5 seconds)
          if (notification.duration !== 0) {
            const duration = notification.duration ?? 5000
            setTimeout(() => {
              get().removeNotification(id)
            }, duration)
          }
        },

        removeNotification: (id) =>
          set(
            (state) => ({
              app: {
                ...state.app,
                notifications: Array.isArray(state.app.notifications) ? state.app.notifications.filter(n => n.id !== id) : []
              }
            }),
            false,
            'removeNotification'
          ),

        clearNotifications: () =>
          set(
            (state) => ({
              app: { ...state.app, notifications: [] }
            }),
            false,
            'clearNotifications'
          ),

        registerModule: (module) => {
          const existing = get().app.moduleRegistry.find(m => m.id === module.id)
          if (existing) {
            console.warn(`Module ${module.id} is already registered`)
            return
          }

          set(
            (state) => ({
              app: {
                ...state.app,
                moduleRegistry: [...state.app.moduleRegistry, module].sort(
                  (a, b) => a.priority - b.priority
                )
              }
            }),
            false,
            'registerModule'
          )
        },

        unregisterModule: (moduleId) =>
          set(
            (state) => ({
              app: {
                ...state.app,
                moduleRegistry: state.app.moduleRegistry.filter(m => m.id !== moduleId)
              }
            }),
            false,
            'unregisterModule'
          ),

        setConnectionStatus: (service, status) =>
          set(
            (state) => ({
              app: {
                ...state.app,
                connectionStatus: {
                  ...state.app.connectionStatus,
                  [service]: status
                }
              }
            }),
            false,
            'setConnectionStatus'
          ),

        setAppLoading: (loading) =>
          set(
            (state) => ({
              app: { ...state.app, loading }
            }),
            false,
            'setAppLoading'
          ),

        // Preferences actions
        setDashboardLayout: (dashboardLayout) =>
          set(
            (state) => ({
              preferences: { ...state.preferences, dashboardLayout }
            }),
            false,
            'setDashboardLayout'
          ),

        setModuleOrder: (moduleOrder) =>
          set(
            (state) => ({
              preferences: { ...state.preferences, moduleOrder }
            }),
            false,
            'setModuleOrder'
          ),

        setDefaultView: (defaultView) =>
          set(
            (state) => ({
              preferences: { ...state.preferences, defaultView }
            }),
            false,
            'setDefaultView'
          ),

        setAutoRefreshInterval: (module, interval) =>
          set(
            (state) => ({
              preferences: {
                ...state.preferences,
                autoRefreshIntervals: {
                  ...state.preferences.autoRefreshIntervals,
                  [module]: interval
                }
              }
            }),
            false,
            'setAutoRefreshInterval'
          ),

        setTimezone: (timezone) =>
          set(
            (state) => ({
              preferences: { ...state.preferences, timezone }
            }),
            false,
            'setTimezone'
          ),
      }),
      {
        name: 'adam-platform-storage',
        partialize: (state) => ({
          // Only persist non-sensitive data
          app: {
            theme: state.app.theme,
            sidebarCollapsed: state.app.sidebarCollapsed,
            // Don't persist notifications or connection status
          },
          preferences: state.preferences,
          // Don't persist auth state for security
        }),
        merge: (persistedState: any, currentState) => ({
          ...currentState,
          ...(persistedState || {}),
          app: {
            ...currentState.app,
            ...(persistedState?.app || {}),
            // Ensure notifications is always an array
            notifications: currentState.app.notifications,
          }
        }),
        version: 1,
      }
    ),
    {
      name: 'platform-store',
    }
  )
)

// Selectors for commonly used state
export const useAuth = () => usePlatformStore((state) => state.auth)
export const useAppState = () => usePlatformStore((state) => state.app)
export const usePreferences = () => usePlatformStore((state) => state.preferences)

// Helper hooks for specific state slices
export const useUser = () => usePlatformStore((state) => state.auth.user)
export const useIsAuthenticated = () => usePlatformStore((state) => state.auth.isAuthenticated)
export const usePermissions = () => usePlatformStore((state) => state.auth.permissions)
export const useHierarchyContext = () => usePlatformStore((state) => state.auth.hierarchyContext)
export const useModuleRegistry = () => usePlatformStore((state) => state.app.moduleRegistry)
export const useNotifications = () => usePlatformStore((state) => state.app.notifications)
export const useConnectionStatus = () => usePlatformStore((state) => state.app.connectionStatus)