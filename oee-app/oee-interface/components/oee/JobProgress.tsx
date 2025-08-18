"use client"

import { Job, CurrentMetricsResponse } from "@/lib/types/oee"

interface JobProgressProps {
  currentJob: Job | null
  metrics: CurrentMetricsResponse | null
}

export function JobProgress({ currentJob, metrics }: JobProgressProps) {
  const calculateJobProgress = () => {
    if (!currentJob || !metrics) return 0
    
    const timeElapsed = (Date.now() - currentJob.startTime.getTime()) / (1000 * 60) // minutes
    const unitsProduced = Math.floor(timeElapsed * (metrics.currentRate || 0))
    return Math.min((unitsProduced / currentJob.quantity) * 100, 100)
  }

  if (!currentJob) return null

  const progress = calculateJobProgress()

  return (
    <div className="space-y-2">
      <div className="flex justify-between text-sm font-medium text-gray-700">
        <span>Job Progress</span>
        <span>{Math.round(progress)}%</span>
      </div>
      <div className="w-full bg-gray-200 rounded-full h-2">
        <div
          className="bg-blue-600 h-2 rounded-full transition-all duration-300"
          style={{ width: `${progress}%` }}
        />
      </div>
      {metrics && (
        <div className="flex justify-between text-xs text-gray-500">
          <span>
            {Math.floor(
              ((Date.now() - currentJob.startTime.getTime()) / (1000 * 60)) * (metrics.currentRate || 0),
            )}{" "}
            units
          </span>
          <span>{currentJob.quantity} target</span>
        </div>
      )}
    </div>
  )
}