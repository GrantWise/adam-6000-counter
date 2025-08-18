import { create } from 'zustand'
import { devtools, subscribeWithSelector } from 'zustand/middleware'
import { CurrentMetricsResponse } from '@/lib/types/oee'
import { config, timing } from '@/config'

interface MetricsState {
  // State
  metrics: CurrentMetricsResponse | null
  isLoading: boolean
  error: string | null
  lastUpdated: Date | null
  connectionStatus: 'connected' | 'disconnected' | 'error'
  retryCount: number

  // Settings
  pollingEnabled: boolean
  pollingInterval: number

  // Actions
  setMetrics: (metrics: CurrentMetricsResponse | null) => void
  setLoading: (loading: boolean) => void
  setError: (error: string | null) => void
  setConnectionStatus: (status: 'connected' | 'disconnected' | 'error') => void
  setRetryCount: (count: number) => void
  setPollingEnabled: (enabled: boolean) => void
  setPollingInterval: (interval: number) => void
  clearMetricsState: () => void

  // Async actions
  fetchMetrics: (deviceId: string) => Promise<void>
  startPolling: (deviceId: string) => void
  stopPolling: () => void
}

// Global polling interval reference
let pollingIntervalId: NodeJS.Timeout | null = null

export const useMetricsStore = create<MetricsState>()(
  devtools(
    subscribeWithSelector((set, get) => ({
      // Initial state
      metrics: null,
      isLoading: true,
      error: null,
      lastUpdated: null,
      connectionStatus: 'disconnected',
      retryCount: 0,
      pollingEnabled: true,
      pollingInterval: timing.metricsPolling(),

      // Sync actions
      setMetrics: (metrics) => set({ 
        metrics, 
        lastUpdated: new Date(),
        error: null,
        retryCount: 0,
        connectionStatus: metrics?.status === 'error' ? 'error' : 'connected'
      }),
      setLoading: (isLoading) => set({ isLoading }),
      setError: (error) => set({ error }),
      setConnectionStatus: (connectionStatus) => set({ connectionStatus }),
      setRetryCount: (retryCount) => set({ retryCount }),
      setPollingEnabled: (pollingEnabled) => set({ pollingEnabled }),
      setPollingInterval: (pollingInterval) => set({ pollingInterval }),
      clearMetricsState: () => set({
        metrics: null,
        isLoading: false,
        error: null,
        lastUpdated: null,
        connectionStatus: 'disconnected',
        retryCount: 0
      }),

      // Async actions
      fetchMetrics: async (deviceId) => {
        const state = get()

        try {
          // Abort previous request if still pending
          const controller = new AbortController()
          const timeoutId = setTimeout(() => controller.abort(), timing.apiTimeout())
          
          const response = await fetch(`/api/metrics/current?deviceId=${encodeURIComponent(deviceId)}`, {
            method: 'GET',
            headers: {
              'Cache-Control': 'no-cache'
            },
            credentials: 'include',
            signal: controller.signal
          })
          
          clearTimeout(timeoutId)

          if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`)
          }

          const data: CurrentMetricsResponse = await response.json()
          state.setMetrics(data)
          state.setLoading(false)

        } catch (error) {
          console.error('Error fetching metrics:', error)
          const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred'
          
          state.setError(errorMessage)
          state.setConnectionStatus('error')
          
          // Implement exponential backoff retry logic
          const maxRetries = timing.maxRetries()
          const baseRetryDelay = timing.retryDelay()
          const currentRetryCount = state.retryCount

          if (currentRetryCount < maxRetries) {
            const delay = Math.min(baseRetryDelay * Math.pow(2, currentRetryCount), config.app.timing.retry.maxDelay)
            state.setRetryCount(currentRetryCount + 1)
            
            setTimeout(() => {
              state.fetchMetrics(deviceId)
            }, delay)
          } else {
            state.setConnectionStatus('disconnected')
            state.setLoading(false)
          }
        }
      },

      startPolling: (deviceId) => {
        const state = get()
        
        if (!state.pollingEnabled) return

        // Clear existing interval
        if (pollingIntervalId) {
          clearInterval(pollingIntervalId)
        }

        // Initial fetch
        state.fetchMetrics(deviceId)

        // Set up polling interval
        pollingIntervalId = setInterval(() => {
          const currentState = get()
          if (currentState.pollingEnabled) {
            currentState.fetchMetrics(deviceId)
          }
        }, state.pollingInterval)
      },

      stopPolling: () => {
        if (pollingIntervalId) {
          clearInterval(pollingIntervalId)
          pollingIntervalId = null
        }
        set({ connectionStatus: 'disconnected' })
      }
    })),
    {
      name: 'metrics-store', // For devtools
    }
  )
)

// Subscribe to polling enabled changes to start/stop polling automatically
useMetricsStore.subscribe(
  (state) => state.pollingEnabled,
  (pollingEnabled, previousPollingEnabled) => {
    if (pollingEnabled !== previousPollingEnabled) {
      if (!pollingEnabled) {
        useMetricsStore.getState().stopPolling()
      }
      // Starting polling requires deviceId, so we don't auto-start here
    }
  }
)