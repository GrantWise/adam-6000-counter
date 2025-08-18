import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { StoppageEvent } from '@/lib/types/oee'

interface StoppageState {
  // State
  unclassifiedStoppages: StoppageEvent[]
  stoppagesLoading: boolean
  stoppageError: string | null
  currentStoppage: StoppageEvent | null

  // Actions
  setUnclassifiedStoppages: (stoppages: StoppageEvent[]) => void
  setStoppagesLoading: (loading: boolean) => void
  setStoppageError: (error: string | null) => void
  setCurrentStoppage: (stoppage: StoppageEvent | null) => void
  clearStoppageState: () => void

  // Async actions
  fetchUnclassifiedStoppages: (deviceId: string) => Promise<void>
  classifyStoppage: (deviceId: string, eventId: number, classification: {
    category: string
    subCategory: string
    comments?: string
  }, operatorId: string) => Promise<void>
}

export const useStoppageStore = create<StoppageState>()(
  devtools(
    (set, get) => ({
      // Initial state
      unclassifiedStoppages: [],
      stoppagesLoading: false,
      stoppageError: null,
      currentStoppage: null,

      // Sync actions
      setUnclassifiedStoppages: (stoppages) => set({ unclassifiedStoppages: stoppages }),
      setStoppagesLoading: (loading) => set({ stoppagesLoading: loading }),
      setStoppageError: (error) => set({ stoppageError: error }),
      setCurrentStoppage: (stoppage) => set({ currentStoppage: stoppage }),
      clearStoppageState: () => set({
        unclassifiedStoppages: [],
        stoppagesLoading: false,
        stoppageError: null,
        currentStoppage: null
      }),

      // Async actions
      fetchUnclassifiedStoppages: async (deviceId) => {
        const state = get()
        state.setStoppagesLoading(true)
        state.setStoppageError(null)

        try {
          const response = await fetch(`/api/stoppages?deviceId=${encodeURIComponent(deviceId)}`, {
            credentials: 'include'
          })

          if (response.ok) {
            const stoppages = await response.json()
            state.setUnclassifiedStoppages(stoppages)
          } else if (response.status === 401) {
            state.setStoppageError('Authentication required')
          } else {
            const errorData = await response.json()
            state.setStoppageError(errorData.error || 'Failed to fetch stoppages')
          }
        } catch (error) {
          console.error('Error fetching unclassified stoppages:', error)
          state.setStoppageError('Network error while fetching stoppages')
        } finally {
          state.setStoppagesLoading(false)
        }
      },

      classifyStoppage: async (deviceId, eventId, classification, operatorId) => {
        const state = get()
        state.setStoppagesLoading(true)
        state.setStoppageError(null)

        try {
          const response = await fetch(`/api/stoppages/${eventId}/classify`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({
              category: classification.category,
              subCategory: classification.subCategory,
              comments: classification.comments || null,
              deviceId,
              operatorId
            }),
          })

          if (response.ok) {
            // Remove the classified stoppage from unclassified list
            const updatedStoppages = state.unclassifiedStoppages.filter(
              stoppage => stoppage.event_id !== eventId
            )
            state.setUnclassifiedStoppages(updatedStoppages)
            
            // Clear current stoppage if it was the one being classified
            if (state.currentStoppage?.event_id === eventId) {
              state.setCurrentStoppage(null)
            }
          } else {
            const errorData = await response.json()
            state.setStoppageError(errorData.error || 'Failed to classify stoppage')
            throw new Error(errorData.error)
          }
        } catch (error) {
          console.error('Error classifying stoppage:', error)
          const errorMessage = error instanceof Error ? error.message : 'Network error while classifying stoppage'
          state.setStoppageError(errorMessage)
          throw error
        } finally {
          state.setStoppagesLoading(false)
        }
      }
    }),
    {
      name: 'stoppage-store', // For devtools
    }
  )
)