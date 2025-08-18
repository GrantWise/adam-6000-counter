import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { Job } from '@/lib/types/oee'

interface JobState {
  // State
  currentJob: Job | null
  jobLoading: boolean
  jobError: string | null

  // Actions
  setCurrentJob: (job: Job | null) => void
  setJobLoading: (loading: boolean) => void
  setJobError: (error: string | null) => void
  clearJobState: () => void

  // Async actions
  fetchCurrentJob: (deviceId: string) => Promise<void>
  startJob: (deviceId: string, jobData: {
    jobNumber: string
    partNumber: string
    targetRate: number
    quantity: number
  }, operatorId?: string) => Promise<void>
  endJob: (deviceId: string, jobId: number) => Promise<void>
}

export const useJobStore = create<JobState>()(
  devtools(
    (set, get) => ({
      // Initial state
      currentJob: null,
      jobLoading: false,
      jobError: null,

      // Sync actions
      setCurrentJob: (job) => set({ currentJob: job }),
      setJobLoading: (loading) => set({ jobLoading: loading }),
      setJobError: (error) => set({ jobError: error }),
      clearJobState: () => set({ 
        currentJob: null, 
        jobLoading: false, 
        jobError: null 
      }),

      // Async actions
      fetchCurrentJob: async (deviceId) => {
        const state = get()
        state.setJobLoading(true)
        state.setJobError(null)

        try {
          const response = await fetch(`/api/jobs?deviceId=${encodeURIComponent(deviceId)}`, {
            credentials: 'include'
          })

          if (response.ok) {
            const job = await response.json()
            if (job) {
              state.setCurrentJob({
                jobId: job.jobId,
                jobNumber: job.jobNumber,
                partNumber: job.partNumber,
                targetRate: job.targetRate,
                quantity: job.quantity || 1000,
                startTime: new Date(job.startTime),
                status: job.status
              })
            } else {
              state.setCurrentJob(null)
            }
          } else if (response.status === 401) {
            state.setJobError('Authentication required')
          } else {
            const errorData = await response.json()
            state.setJobError(errorData.error || 'Failed to fetch job')
          }
        } catch (error) {
          console.error('Error fetching current job:', error)
          state.setJobError('Network error while fetching job')
        } finally {
          state.setJobLoading(false)
        }
      },

      startJob: async (deviceId, jobData, operatorId = 'system') => {
        const state = get()
        state.setJobLoading(true)
        state.setJobError(null)

        try {
          const response = await fetch('/api/jobs', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({
              jobNumber: jobData.jobNumber,
              partNumber: jobData.partNumber,
              targetRate: jobData.targetRate,
              deviceId,
              operatorId
            }),
          })

          if (response.ok) {
            const newJob = await response.json()
            state.setCurrentJob({
              jobId: newJob.jobId,
              jobNumber: newJob.jobNumber,
              partNumber: newJob.partNumber,
              targetRate: newJob.targetRate,
              quantity: jobData.quantity,
              startTime: new Date(newJob.startTime),
              status: newJob.status
            })
          } else {
            const errorData = await response.json()
            state.setJobError(errorData.error || 'Failed to start job')
            throw new Error(errorData.error)
          }
        } catch (error) {
          console.error('Error starting job:', error)
          const errorMessage = error instanceof Error ? error.message : 'Network error while starting job'
          state.setJobError(errorMessage)
          throw error
        } finally {
          state.setJobLoading(false)
        }
      },

      endJob: async (deviceId, jobId) => {
        const state = get()
        state.setJobLoading(true)
        state.setJobError(null)

        try {
          const response = await fetch(`/api/jobs/${jobId}/end`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/json',
            },
            credentials: 'include',
            body: JSON.stringify({ deviceId }),
          })

          if (response.ok) {
            state.setCurrentJob(null)
          } else {
            const errorData = await response.json()
            state.setJobError(errorData.error || 'Failed to end job')
            throw new Error(errorData.error)
          }
        } catch (error) {
          console.error('Error ending job:', error)
          const errorMessage = error instanceof Error ? error.message : 'Network error while ending job'
          state.setJobError(errorMessage)
          throw error
        } finally {
          state.setJobLoading(false)
        }
      }
    }),
    {
      name: 'job-store', // For devtools
    }
  )
)