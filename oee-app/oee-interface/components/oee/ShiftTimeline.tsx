"use client"

interface ShiftTimelineSegment {
  time: string
  status: string
  duration: number
}

interface ShiftTimelineProps {
  timeline: ShiftTimelineSegment[]
}

export function ShiftTimeline({ timeline }: ShiftTimelineProps) {
  const getStatusColor = (status: string) => {
    switch (status) {
      case "running": return "bg-green-500"
      case "stopped": return "bg-gray-400"
      case "slow": return "bg-yellow-500"
      case "breakdown": return "bg-red-500"
      default: return "bg-gray-300"
    }
  }

  return (
    <div className="space-y-2">
      <div className="text-sm font-medium text-gray-700">Machine Status Timeline</div>
      <div className="relative flex h-4">
        {timeline.map((segment, index) => (
          <div
            key={index}
            className={`h-full ${getStatusColor(segment.status)}`}
            style={{ width: `${(segment.duration / 480) * 100}%` }}
            title={`${segment.time} - ${segment.status} (${segment.duration}min)`}
          />
        ))}
        <div
          className="h-full bg-gray-100 border-l border-gray-300"
          style={{ width: `${((480 - 390) / 480) * 100}%` }}
          title="Remaining shift time"
        />
        <div
          className="absolute top-0 w-0.5 h-full bg-blue-600"
          style={{ left: `${(390 / 480) * 100}%` }}
          title="Current time: 12:30 PM"
        />
      </div>
      <div className="flex justify-between text-xs text-gray-500 mt-1">
        <span>6:00 AM</span>
        <span className="text-blue-600 font-medium">12:30 PM</span>
        <span>2:00 PM</span>
      </div>
      <div className="flex justify-between text-xs">
        <div className="flex items-center space-x-3">
          <div className="flex items-center space-x-1">
            <div className="w-2 h-2 bg-green-500 rounded-sm" />
            <span className="text-gray-600">Running</span>
          </div>
          <div className="flex items-center space-x-1">
            <div className="w-2 h-2 bg-yellow-500 rounded-sm" />
            <span className="text-gray-600">Slow</span>
          </div>
          <div className="flex items-center space-x-1">
            <div className="w-2 h-2 bg-gray-400 rounded-sm" />
            <span className="text-gray-600">Stopped</span>
          </div>
          <div className="flex items-center space-x-1">
            <div className="w-2 h-2 bg-red-500 rounded-sm" />
            <span className="text-gray-600">Breakdown</span>
          </div>
        </div>
      </div>
    </div>
  )
}