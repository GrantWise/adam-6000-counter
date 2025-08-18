"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { LineChart, Line, XAxis, YAxis, ResponsiveContainer, ReferenceLine } from "recharts"
import { Job } from "@/lib/types/oee"

interface ChartDataPoint {
  time: string
  actualRate: number
  targetRate: number
  timestamp: number
}

interface ProductionChartProps {
  currentJob: Job | null
  chartData: ChartDataPoint[]
  historyLoading: boolean
}

export function ProductionChart({ 
  currentJob, 
  chartData, 
  historyLoading 
}: ProductionChartProps) {
  const chartConfig = {
    actualRate: {
      label: "Actual Rate",
      color: "hsl(var(--chart-1))",
    },
    targetRate: {
      label: "Target Rate", 
      color: "hsl(var(--chart-2))",
    },
  }

  return (
    <Card className="p-4">
      <CardHeader className="pb-4">
        <CardTitle className="text-xl font-bold text-gray-900">Production Rate</CardTitle>
      </CardHeader>
      <CardContent>
        {historyLoading ? (
          <div className="h-48 bg-gray-100 rounded-lg flex items-center justify-center">
            <div className="text-gray-500">Loading chart data...</div>
          </div>
        ) : currentJob && chartData.length > 0 ? (
          <ChartContainer config={chartConfig} className="h-48 w-full">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <XAxis dataKey="time" tick={{ fontSize: 12 }} interval="preserveStartEnd" />
                <YAxis tick={{ fontSize: 12 }} domain={[0, "dataMax + 20"]} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <ReferenceLine
                  y={currentJob.targetRate}
                  stroke="hsl(var(--chart-2))"
                  strokeDasharray="5 5"
                  label={{ value: "Target", position: "topRight" as any }}
                />
                <Line
                  type="monotone"
                  dataKey="actualRate"
                  stroke="hsl(var(--chart-1))"
                  strokeWidth={2}
                  dot={false}
                  name="Actual Rate"
                />
              </LineChart>
            </ResponsiveContainer>
          </ChartContainer>
        ) : (
          <div className="h-48 bg-gray-100 rounded-lg flex items-center justify-center border-2 border-dashed border-gray-300">
            <div className="text-center text-gray-500">
              <div className="text-lg font-medium">No Active Job</div>
              <div className="text-sm">Start a job to see real-time production data</div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}