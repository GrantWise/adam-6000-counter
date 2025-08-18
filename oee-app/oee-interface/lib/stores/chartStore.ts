import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { config } from '@/config'

interface ChartDataPoint {
  time: string
  actualRate: number
  targetRate: number
  timestamp: number
}

interface ChartState {
  // State
  chartData: ChartDataPoint[]
  historyLoading: boolean
  chartError: string | null
  maxDataPoints: number

  // Actions
  addDataPoint: (point: ChartDataPoint) => void
  setChartData: (data: ChartDataPoint[]) => void
  setHistoryLoading: (loading: boolean) => void
  setChartError: (error: string | null) => void
  setMaxDataPoints: (max: number) => void
  clearChartData: () => void

  // Async actions
  fetchChartHistory: (deviceId: string, hours?: number) => Promise<void>
  addRealtimeDataPoint: (actualRate: number, targetRate: number) => void
}

export const useChartStore = create<ChartState>()(
  devtools(
    (set, get) => ({
      // Initial state
      chartData: [],
      historyLoading: false,
      chartError: null,
      maxDataPoints: config.app.ui.chart.maxDataPoints,

      // Sync actions
      addDataPoint: (point) => {
        const state = get()
        const updatedData = [...state.chartData, point]
        // Keep only the most recent data points to prevent memory issues
        const trimmedData = updatedData.slice(-state.maxDataPoints)
        set({ chartData: trimmedData })
      },
      
      setChartData: (chartData) => set({ chartData }),
      setHistoryLoading: (historyLoading) => set({ historyLoading }),
      setChartError: (chartError) => set({ chartError }),
      setMaxDataPoints: (maxDataPoints) => set({ maxDataPoints }),
      clearChartData: () => set({ chartData: [], chartError: null }),

      // Async actions
      fetchChartHistory: async (deviceId, hours = 8) => {
        const state = get()
        state.setHistoryLoading(true)
        state.setChartError(null)

        try {
          const response = await fetch(
            `/api/metrics/history?deviceId=${encodeURIComponent(deviceId)}&hours=${hours}`,
            { credentials: 'include' }
          )

          if (response.ok) {
            const history = await response.json()
            const transformedData: ChartDataPoint[] = history.timestamps.map(
              (timestamp: string, index: number) => ({
                time: new Date(timestamp).toLocaleTimeString([], { 
                  hour: "2-digit", 
                  minute: "2-digit" 
                }),
                actualRate: history.rates[index] || 0,
                targetRate: history.targetRate || 100,
                timestamp: new Date(timestamp).getTime()
              })
            )
            state.setChartData(transformedData)
          } else {
            const errorData = await response.json()
            state.setChartError(errorData.error || 'Failed to fetch chart history')
          }
        } catch (error) {
          console.error('Error fetching chart history:', error)
          state.setChartError('Network error while fetching chart history')
        } finally {
          state.setHistoryLoading(false)
        }
      },

      addRealtimeDataPoint: (actualRate, targetRate) => {
        const state = get()
        const now = new Date()
        
        // Prevent duplicate data points within configured threshold
        const existingPoint = state.chartData.find(point => 
          Math.abs(point.timestamp - now.getTime()) < config.app.ui.chart.duplicateThreshold
        )
        
        if (existingPoint) {
          return // Skip duplicate data point
        }
        
        const newDataPoint: ChartDataPoint = {
          time: now.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
          actualRate,
          targetRate,
          timestamp: now.getTime(),
        }
        
        state.addDataPoint(newDataPoint)
      }
    }),
    {
      name: 'chart-store', // For devtools
    }
  )
)