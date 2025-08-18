import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { config } from '@/config'

interface AppState {
  // Global app state
  deviceId: string
  isOnline: boolean
  lastActivity: Date | null
  notifications: Notification[]

  // Actions
  setDeviceId: (deviceId: string) => void
  setOnlineStatus: (isOnline: boolean) => void
  updateActivity: () => void
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp'>) => void
  removeNotification: (id: string) => void
  clearNotifications: () => void

  // Compound actions
  initializeApp: (deviceId: string) => void
  cleanup: () => void
}

interface Notification {
  id: string
  type: 'info' | 'warning' | 'error' | 'success'
  title: string
  message: string
  timestamp: Date
  autoClose?: boolean
  duration?: number
}

export const useAppStore = create<AppState>()(
  devtools(
    (set, get) => ({
      // Initial state
      deviceId: config.app.device.defaultId,
      isOnline: true,
      lastActivity: null,
      notifications: [],

      // Actions
      setDeviceId: (deviceId) => set({ deviceId }),
      setOnlineStatus: (isOnline) => set({ isOnline }),
      updateActivity: () => set({ lastActivity: new Date() }),

      addNotification: (notificationData) => {
        const notification: Notification = {
          ...notificationData,
          id: `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          timestamp: new Date()
        }
        
        set(state => ({
          notifications: [...state.notifications, notification]
        }))

        // Auto-remove notification if specified
        if (notification.autoClose !== false) {
          const duration = notification.duration || config.app.ui.notifications.defaultDuration
          setTimeout(() => {
            get().removeNotification(notification.id)
          }, duration)
        }
      },

      removeNotification: (id) => set(state => ({
        notifications: state.notifications.filter(n => n.id !== id)
      })),

      clearNotifications: () => set({ notifications: [] }),

      // Compound actions
      initializeApp: (deviceId) => {
        set({ 
          deviceId,
          lastActivity: new Date()
        })
        
        // Add initialization notification
        get().addNotification({
          type: 'info',
          title: 'OEE Dashboard',
          message: `Initialized for device ${deviceId}`,
          autoClose: true,
          duration: 3000
        })
      },

      cleanup: () => {
        set({
          isOnline: false,
          notifications: [],
          lastActivity: null
        })
      }
    }),
    {
      name: 'app-store', // For devtools
    }
  )
)

// Clean up timers and listeners
const cleanupHandlers: (() => void)[] = []

// Browser online/offline detection
if (typeof window !== 'undefined') {
  window.addEventListener('online', () => {
    const state = useAppStore.getState()
    if (!state.isOnline) { // Only notify if we were previously offline
      state.setOnlineStatus(true)
      state.addNotification({
        type: 'success',
        title: 'Connection Restored',
        message: 'Internet connection is back online',
        autoClose: true,
        duration: 3000
      })
    }
  })

  window.addEventListener('offline', () => {
    const state = useAppStore.getState()
    if (state.isOnline) { // Only notify if we were previously online
      state.setOnlineStatus(false)
      state.addNotification({
        type: 'error',
        title: 'Connection Lost',
        message: 'Working offline - some features may be limited',
        autoClose: false
      })
    }
  })

  // Activity tracking
  const activityEvents = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart']
  let activityTimeout: NodeJS.Timeout | null = null

  const throttledActivityUpdate = () => {
    if (activityTimeout) clearTimeout(activityTimeout)
    
    activityTimeout = setTimeout(() => {
      useAppStore.getState().updateActivity()
    }, config.app.ui.notifications.activityThrottle)
  }

  activityEvents.forEach(event => {
    document.addEventListener(event, throttledActivityUpdate, true)
    cleanupHandlers.push(() => {
      document.removeEventListener(event, throttledActivityUpdate, true)
    })
  })

  // Global cleanup function
  cleanupHandlers.push(() => {
    if (activityTimeout) {
      clearTimeout(activityTimeout)
      activityTimeout = null
    }
  })
}

// Export cleanup function for use in app teardown
export const cleanupAppStore = () => {
  cleanupHandlers.forEach(cleanup => cleanup())
  cleanupHandlers.length = 0
}