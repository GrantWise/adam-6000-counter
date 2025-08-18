"use client"

import { useEffect } from "react"
import { config } from "@/config"
import { Badge } from "@/components/ui/badge"
import { useAuth } from "@/lib/auth/AuthContext"
import { useJobStore } from "@/lib/stores/jobStore"
import { useMetricsStore } from "@/lib/stores/metricsStore"
import { useStoppageStore } from "@/lib/stores/stoppageStore"
import { useChartStore } from "@/lib/stores/chartStore"
import { useAppStore } from "@/lib/stores/appStore"
import { JobControlPanel } from "./JobControlPanel"
import { MetricsDisplay } from "./MetricsDisplay"
import { ProductionChart } from "./ProductionChart"
import { StoppageManager } from "./StoppageManager"
import { JobProgress } from "./JobProgress"
import { ShiftTimeline } from "./ShiftTimeline"
import { Job } from "@/lib/types/oee"
import { OEEErrorBoundary } from "./OEEErrorBoundary"
import { useErrorHandler } from "@/components/ui/error-boundary"
import { toast } from "@/hooks/use-toast"

interface ChartDataPoint {
  time: string
  actualRate: number
  targetRate: number
  timestamp: number
}

interface ShiftTimelineSegment {
  time: string
  status: string
  duration: number
}


export default function OEEDashboard() {
  const { user } = useAuth()
  const throwError = useErrorHandler()
  
  // Global app state
  const { deviceId, isOnline, initializeApp } = useAppStore()
  
  // Store states
  const { currentJob, jobLoading, jobError, fetchCurrentJob, startJob, endJob } = useJobStore()
  const { metrics, isLoading, error, connectionStatus, startPolling, stopPolling } = useMetricsStore()
  const { classifyStoppage } = useStoppageStore()
  const { chartData, historyLoading, fetchChartHistory, addRealtimeDataPoint, clearChartData } = useChartStore()

  // Mock shift timeline (would come from real data in production)
  const shiftTimeline: ShiftTimelineSegment[] = [
    { time: "06:00", status: "running", duration: 45 },
    { time: "06:45", status: "stopped", duration: 15 },
    { time: "07:00", status: "running", duration: 90 },
    { time: "08:30", status: "slow", duration: 30 },
    { time: "09:00", status: "running", duration: 120 },
    { time: "11:00", status: "breakdown", duration: 45 },
    { time: "11:45", status: "running", duration: 45 },
  ]

  // Initialize app and start polling on mount
  useEffect(() => {
    initializeApp(deviceId)
    fetchCurrentJob(deviceId)
    startPolling(deviceId)

    return () => {
      stopPolling()
    }
  }, [deviceId, initializeApp, fetchCurrentJob, startPolling, stopPolling])

  // Load chart history when job changes
  useEffect(() => {
    if (currentJob) {
      fetchChartHistory(deviceId)
    } else {
      clearChartData()
    }
  }, [currentJob, deviceId, fetchChartHistory, clearChartData])

  // Update chart data periodically with real-time metrics
  useEffect(() => {
    if (currentJob && metrics && metrics.status === 'running') {
      const interval = setInterval(() => {
        // Only add data points when machine is actually running
        if (metrics.currentRate > 0) {
          addRealtimeDataPoint(metrics.currentRate, metrics.targetRate)
        }
      }, config.app.timing.polling.chartUpdate)

      return () => clearInterval(interval)
    }
  }, [currentJob, metrics?.status, metrics?.currentRate, metrics?.targetRate, addRealtimeDataPoint])

  // Event handlers
  const handleStartJob = async (jobData: { 
    jobNumber: string
    partNumber: string
    targetRate: number
    quantity: number
  }) => {
    try {
      await startJob(deviceId, jobData, user?.username || 'system')
      clearChartData() // Reset chart data for new job
      toast({
        title: "Job Started",
        description: `Successfully started job ${jobData.jobNumber} for part ${jobData.partNumber}`,
      })
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error'
      toast({
        title: "Failed to Start Job",
        description: `Error starting job: ${errorMessage}`,
        variant: "destructive",
      })
      // Re-throw to trigger error boundary if needed
      throwError(new Error(`Job start failed: ${errorMessage}`))
    }
  }

  const handleEndJob = async () => {
    if (currentJob?.jobId) {
      try {
        await endJob(deviceId, currentJob.jobId)
        clearChartData()
        toast({
          title: "Job Completed",
          description: `Successfully completed job ${currentJob.jobNumber}`,
        })
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error'
        toast({
          title: "Failed to End Job",
          description: `Error ending job: ${errorMessage}`,
          variant: "destructive",
        })
        // Re-throw to trigger error boundary if needed
        throwError(new Error(`Job end failed: ${errorMessage}`))
      }
    }
  }

  const handleClassifyStoppage = async (classification: {
    category: string
    subCategory: string
    comments: string
  }) => {
    try {
      // In a real implementation, you'd get the actual event ID from unclassified stoppages
      const eventId = 1
      await classifyStoppage(deviceId, eventId, classification, user?.username || 'system')
      toast({
        title: "Stoppage Classified",
        description: `Stoppage classified as ${classification.category} - ${classification.subCategory}`,
      })
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error'
      toast({
        title: "Failed to Classify Stoppage",
        description: `Error classifying stoppage: ${errorMessage}`,
        variant: "destructive",
      })
      throwError(new Error(`Stoppage classification failed: ${errorMessage}`))
    }
  }

  // Show connection status in header if offline
  const connectionIndicator = !isOnline || connectionStatus === 'error' ? (
    <div className="fixed top-4 right-4 z-50">
      <Badge className="bg-red-100 text-red-800">
        {!isOnline ? 'OFFLINE' : 'CONNECTION ERROR'}
      </Badge>
    </div>
  ) : connectionStatus === 'disconnected' ? (
    <div className="fixed top-4 right-4 z-50">
      <Badge className="bg-yellow-100 text-yellow-800">RECONNECTING...</Badge>
    </div>
  ) : null

  return (
    <div className="h-screen bg-gray-50 p-3 overflow-hidden">
      {connectionIndicator}
      
      {/* Main Dashboard Grid - 4 sections */}
      <div className="grid grid-cols-2 gap-3 h-full max-w-none">
        {/* Section A: Job Control (Top Left) */}
        <div className="space-y-4">
          <OEEErrorBoundary componentName="Job Control Panel">
            <JobControlPanel
              currentJob={currentJob}
              jobLoading={jobLoading}
              onStartJob={handleStartJob}
              onEndJob={handleEndJob}
              deviceId={deviceId}
            />
          </OEEErrorBoundary>
          <OEEErrorBoundary componentName="Job Progress">
            <JobProgress currentJob={currentJob} metrics={metrics} />
          </OEEErrorBoundary>
          <OEEErrorBoundary componentName="Shift Timeline">
            <ShiftTimeline timeline={shiftTimeline} />
          </OEEErrorBoundary>
        </div>

        {/* Section B: Performance Metrics (Top Right) */}
        <OEEErrorBoundary componentName="Metrics Display">
          <MetricsDisplay
            metrics={metrics}
            isLoading={isLoading}
            error={error}
          />
        </OEEErrorBoundary>

        {/* Section C: Real-time Rate Chart (Bottom Left) */}
        <OEEErrorBoundary componentName="Production Chart">
          <ProductionChart
            currentJob={currentJob}
            chartData={chartData}
            historyLoading={historyLoading}
          />
        </OEEErrorBoundary>

        {/* Section D: Stoppage Management (Bottom Right) */}
        <OEEErrorBoundary componentName="Stoppage Manager">
          <StoppageManager
            metrics={metrics}
            currentJob={currentJob}
            onClassifyStoppage={handleClassifyStoppage}
            deviceId={deviceId}
          />
        </OEEErrorBoundary>
      </div>
    </div>
  )
}