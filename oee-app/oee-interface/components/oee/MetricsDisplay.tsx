"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { CurrentMetricsResponse } from "@/lib/types/oee"

interface MetricsDisplayProps {
  metrics: CurrentMetricsResponse | null
  isLoading: boolean
  error: string | null
}

export function MetricsDisplay({ metrics, isLoading, error }: MetricsDisplayProps) {
  const getBadgeColor = (value: number) => {
    if (value >= 95) return "bg-green-100 text-green-800"
    if (value >= 85) return "bg-yellow-100 text-yellow-800"
    return "bg-red-100 text-red-800"
  }

  if (isLoading) {
    return (
      <Card className="p-4 space-y-3">
        <CardHeader className="pb-4">
          <CardTitle className="text-xl font-bold text-gray-900">Performance</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex justify-center items-center h-32">
            <div className="text-gray-500">Loading metrics...</div>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card className="p-4 space-y-3">
        <CardHeader className="pb-4">
          <CardTitle className="text-xl font-bold text-gray-900">Performance</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex justify-center items-center h-32">
            <div className="text-red-500">Error: {error}</div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="p-4 space-y-3">
      <CardHeader className="pb-4">
        <CardTitle className="text-xl font-bold text-gray-900">Performance</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-3 text-center">
          <div>
            <div className="text-sm text-gray-600">Target</div>
            <div className="text-2xl font-bold text-gray-900">
              {metrics ? `${metrics.targetRate}/min` : "--/min"}
            </div>
          </div>
          <div>
            <div className="text-sm text-gray-600">Actual</div>
            <div className="text-2xl font-bold text-gray-900">
              {metrics ? `${metrics.currentRate}/min` : "--/min"}
            </div>
          </div>
        </div>

        <div className="space-y-3">
          <div className="flex justify-between items-center">
            <span className="font-medium">Performance:</span>
            <Badge className={`${getBadgeColor(metrics?.performancePercent || 0)} text-base px-3 py-1`}>
              {metrics?.performancePercent || 0}%
            </Badge>
          </div>
          <div className="flex justify-between items-center">
            <span className="font-medium">Quality:</span>
            <Badge className={`${getBadgeColor(metrics?.qualityPercent || 0)} text-base px-3 py-1`}>
              {metrics?.qualityPercent || 0}%
            </Badge>
          </div>
          <div className="flex justify-between items-center">
            <span className="font-medium">Availability:</span>
            <Badge className={`${getBadgeColor(metrics?.availabilityPercent || 0)} text-base px-3 py-1`}>
              {metrics?.availabilityPercent || 0}%
            </Badge>
          </div>
        </div>

        <div className="pt-4 border-t">
          <div className="text-center">
            <div className="text-sm text-gray-600">Overall OEE</div>
            <div
              className={`text-3xl font-bold ${
                (metrics?.oeePercent || 0) >= 85
                  ? "text-green-600"
                  : (metrics?.oeePercent || 0) >= 75
                  ? "text-yellow-600"
                  : "text-red-600"
              }`}
            >
              {metrics?.oeePercent || 0}%
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}