"use client"

import { useState, useEffect, useCallback } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Progress } from "@/components/ui/progress"
import { Activity, Pause, Play, Download, RefreshCw, Wifi, AlertTriangle, CheckCircle } from "lucide-react"

interface DeviceHealth {
  deviceId: string
  deviceName: string
  status: "connected" | "warning" | "disconnected" | "disabled"
  lastRead: string
  successRate: number
  avgResponse: number
  failedReads: number
  uptime: string
}

interface CounterReading {
  deviceId: string
  deviceName: string
  channel: string
  currentValue: number
  ratePerMin: number
  ratePerHour: number
  lastUpdate: string
  dataQuality: "good" | "uncertain" | "bad" | "config_error"
}

interface RealTimeMonitoringProps {
  onNavigateToTab?: (tab: string, deviceId?: string) => void
}

const mockDeviceHealth: DeviceHealth[] = [
  {
    deviceId: "PROD_LINE_1",
    deviceName: "Main Production Line",
    status: "connected",
    lastRead: "2 seconds ago",
    successRate: 99.8,
    avgResponse: 23,
    failedReads: 2,
    uptime: "6 days, 4 hours",
  },
  {
    deviceId: "QUALITY_GATE_1",
    deviceName: "Quality Inspection",
    status: "warning",
    lastRead: "15 seconds ago",
    successRate: 95.2,
    avgResponse: 145,
    failedReads: 12,
    uptime: "2 days, 1 hour",
  },
  {
    deviceId: "BOTTLING_LINE_3",
    deviceName: "Bottling Line 3",
    status: "disconnected",
    lastRead: "2 minutes ago",
    successRate: 0,
    avgResponse: 0,
    failedReads: 45,
    uptime: "0 minutes",
  },
]

const mockCounterReadings: CounterReading[] = [
  {
    deviceId: "PROD_LINE_1",
    deviceName: "Main Production Line",
    channel: "Good_Products",
    currentValue: 15847,
    ratePerMin: 45,
    ratePerHour: 2700,
    lastUpdate: "Now",
    dataQuality: "good",
  },
  {
    deviceId: "PROD_LINE_1",
    deviceName: "Main Production Line",
    channel: "Rejects",
    currentValue: 234,
    ratePerMin: 2,
    ratePerHour: 120,
    lastUpdate: "Now",
    dataQuality: "good",
  },
  {
    deviceId: "QUALITY_GATE_1",
    deviceName: "Quality Inspection",
    channel: "Passed",
    currentValue: 8923,
    ratePerMin: 38,
    ratePerHour: 2280,
    lastUpdate: "5s ago",
    dataQuality: "uncertain",
  },
  {
    deviceId: "BOTTLING_LINE_3",
    deviceName: "Bottling Line 3",
    channel: "Bottles_Filled",
    currentValue: 0,
    ratePerMin: 0,
    ratePerHour: 0,
    lastUpdate: "2m ago",
    dataQuality: "bad",
  },
]

export function RealTimeMonitoring({ onNavigateToTab }: RealTimeMonitoringProps) {
  const [deviceHealth, setDeviceHealth] = useState<DeviceHealth[]>(mockDeviceHealth)
  const [counterReadings, setCounterReadings] = useState<CounterReading[]>(mockCounterReadings)
  const [autoRefresh, setAutoRefresh] = useState(true)
  const [refreshInterval, setRefreshInterval] = useState("2")
  const [searchFilter, setSearchFilter] = useState("")

  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(() => {
      // Simulate real-time updates
      setCounterReadings((prev) =>
        prev.map((reading) => ({
          ...reading,
          currentValue:
            reading.dataQuality === "good"
              ? reading.currentValue + Math.floor(Math.random() * 3)
              : reading.currentValue,
          lastUpdate: reading.dataQuality === "good" ? "Now" : reading.lastUpdate,
        })),
      )
    }, Number.parseInt(refreshInterval) * 1000)

    return () => clearInterval(interval)
  }, [autoRefresh, refreshInterval])

  const getStatusIcon = (status: DeviceHealth["status"]) => {
    switch (status) {
      case "connected":
        return <CheckCircle className="w-5 h-5 text-green-500" />
      case "warning":
        return <AlertTriangle className="w-5 h-5 text-yellow-500" />
      case "disconnected":
        return <Wifi className="w-5 h-5 text-red-500" />
      case "disabled":
        return <Pause className="w-5 h-5 text-gray-500" />
      default:
        return <Activity className="w-5 h-5 text-gray-500" />
    }
  }

  const getDataQualityBadge = (quality: CounterReading["dataQuality"]) => {
    switch (quality) {
      case "good":
        return <Badge className="bg-green-500">Good</Badge>
      case "uncertain":
        return <Badge className="bg-yellow-500">Uncertain</Badge>
      case "bad":
        return <Badge className="bg-red-500">Bad</Badge>
      case "config_error":
        return <Badge className="bg-orange-500">Config Error</Badge>
      default:
        return <Badge variant="secondary">Unknown</Badge>
    }
  }

  const filteredReadings = counterReadings.filter(
    (reading) =>
      reading.deviceName.toLowerCase().includes(searchFilter.toLowerCase()) ||
      reading.channel.toLowerCase().includes(searchFilter.toLowerCase()),
  )

  const handleViewLogs = useCallback(
    (deviceId: string) => {
      if (onNavigateToTab) {
        onNavigateToTab("diagnostics", deviceId)
      }
    },
    [onNavigateToTab],
  )

  return (
    <div className="space-y-6">
      {/* System Health Overview */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="w-5 h-5" />
            System Health Overview
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">Healthy</div>
              <div className="text-sm text-gray-600">Overall System Status</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">97.3%</div>
              <div className="text-sm text-gray-600">Average Success Rate</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">45ms</div>
              <div className="text-sm text-gray-600">Average Response Time</div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Device Health Dashboard */}
      <Card>
        <CardHeader>
          <CardTitle>Device Health Dashboard</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {deviceHealth.map((device) => (
              <Card key={device.deviceId} className="border-2">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(device.status)}
                      <span className="font-medium">{device.deviceName}</span>
                    </div>
                    <Badge variant={device.status === "connected" ? "default" : "destructive"}>{device.status}</Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="text-sm">
                    <div className="flex justify-between">
                      <span>Last Read:</span>
                      <span>{device.lastRead}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Success Rate:</span>
                      <span>{device.successRate}%</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Avg Response:</span>
                      <span>{device.avgResponse}ms</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Failed Reads:</span>
                      <span>{device.failedReads}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Uptime:</span>
                      <span>{device.uptime}</span>
                    </div>
                  </div>
                  <Progress value={device.successRate} className="h-2" />
                  <div className="flex gap-1">
                    <Button variant="outline" size="sm">
                      <RefreshCw className="w-3 h-3" />
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => handleViewLogs(device.deviceId)}>
                      Logs
                    </Button>
                    <Button variant="outline" size="sm">
                      Test
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Counter Values Display */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Counter Values</CardTitle>
            <div className="flex items-center space-x-2">
              <Input
                placeholder="Search devices or channels..."
                value={searchFilter}
                onChange={(e) => setSearchFilter(e.target.value)}
                className="w-64"
              />
              <Select value={refreshInterval} onValueChange={setRefreshInterval}>
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">1 second</SelectItem>
                  <SelectItem value="2">2 seconds</SelectItem>
                  <SelectItem value="5">5 seconds</SelectItem>
                  <SelectItem value="10">10 seconds</SelectItem>
                  <SelectItem value="30">30 seconds</SelectItem>
                  <SelectItem value="60">1 minute</SelectItem>
                </SelectContent>
              </Select>
              <Button variant="outline" size="sm" onClick={() => setAutoRefresh(!autoRefresh)}>
                {autoRefresh ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
                {autoRefresh ? "Pause" : "Resume"}
              </Button>
              <Button variant="outline" size="sm">
                <Download className="w-4 h-4 mr-2" />
                Export CSV
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Device</TableHead>
                <TableHead>Channel</TableHead>
                <TableHead>Current Value</TableHead>
                <TableHead>Rate/Min</TableHead>
                <TableHead>Rate/Hour</TableHead>
                <TableHead>Last Update</TableHead>
                <TableHead>Data Quality</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredReadings.map((reading, index) => (
                <TableRow key={`${reading.deviceId}-${reading.channel}`}>
                  <TableCell className="font-medium">{reading.deviceName}</TableCell>
                  <TableCell>{reading.channel}</TableCell>
                  <TableCell className={`font-mono text-lg ${reading.lastUpdate === "Now" ? "font-bold" : ""}`}>
                    {reading.currentValue.toLocaleString()}
                  </TableCell>
                  <TableCell>{reading.ratePerMin}</TableCell>
                  <TableCell>{reading.ratePerHour.toLocaleString()}</TableCell>
                  <TableCell className={reading.lastUpdate.includes("m ago") ? "text-red-600" : ""}>
                    {reading.lastUpdate}
                  </TableCell>
                  <TableCell>{getDataQualityBadge(reading.dataQuality)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  )
}
